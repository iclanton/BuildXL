﻿// Copyright (C) Microsoft Corporation. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.FileSystem;
using BuildXL.Cache.ContentStore.Interfaces.Results;
using BuildXL.Cache.ContentStore.Interfaces.Sessions;
using BuildXL.Cache.ContentStore.Interfaces.Tracing;
using BuildXL.Cache.ContentStore.Interfaces.Utils;
using BuildXL.Cache.ContentStore.Synchronization;
using BuildXL.Cache.ContentStore.Tracing;
using BuildXL.Cache.ContentStore.Tracing.Internal;
using BuildXL.Cache.MemoizationStore.Interfaces.Results;

namespace BuildXL.Cache.MemoizationStore.Interfaces.Sessions
{
    /// <summary>
    /// Wraps multiple underlying cache sessions under a single wrapper.
    /// </summary>
    /// TODO: Derive from ContentSessionBase
    public sealed class TwoLevelCacheSession : ICacheSession
    {
        private readonly Tracer _tracer = new Tracer(nameof(TwoLevelCacheSession));
        private readonly ICacheSession _localCacheSession;
        private readonly ICacheSession _remoteCacheSession;
        private readonly bool _remoteCacheIsReadOnly;
        private readonly bool _alwaysUpdateFromRemote;
        private readonly LockSet<ContentHash> _remoteFetchLock = new LockSet<ContentHash>();

        /// <summary>
        /// Initializes an instance of the <see cref="TwoLevelCacheSession"/> class
        /// </summary>
        /// <param name="name"></param>
        /// <param name="localCacheSession"></param>
        /// <param name="remoteCacheSession"></param>
        /// <param name="remoteCacheIsReadOnly"></param>
        /// <param name="alwaysUpdateFromRemote"></param>
        public TwoLevelCacheSession(
            string name,
            ICacheSession localCacheSession,
            ICacheSession remoteCacheSession,
            bool remoteCacheIsReadOnly,
            bool alwaysUpdateFromRemote)
        {
            Name = name;
            _remoteCacheSession = remoteCacheSession;
            _localCacheSession = localCacheSession;
            _remoteCacheIsReadOnly = remoteCacheIsReadOnly;
            _alwaysUpdateFromRemote = alwaysUpdateFromRemote;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public Task<BoolResult> StartupAsync(Context context)
        {
            return StartupCall<Tracer>.RunAsync(
                _tracer,
                context,
                async () =>
                {
                    // Important to parallelize session startup calls - 50 msec per path (approx).
                    StartupStarted = true;

                    var (local, remote) = await MultiLevel(session => session.StartupAsync(context));

                    var result = local & remote;
                    if (!result.Succeeded)
                    {
                        // One of the initialization failed.
                        // Need to shut down the stores that were properly initialized.
                        if (local.Succeeded)
                        {
                            await _localCacheSession.ShutdownAsync(context).TraceIfFailure(context);
                        }

                        if (remote.Succeeded)
                        {
                            await _remoteCacheSession.ShutdownAsync(context).TraceIfFailure(context);
                        }
                    }

                    StartupStarted = false;
                    StartupCompleted = true;
                    return result;
                });
        }

        /// <inheritdoc />
        public bool StartupCompleted { get; private set; }

        /// <inheritdoc />
        public bool StartupStarted { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            _remoteCacheSession.Dispose();
            _localCacheSession.Dispose();
        }

        /// <inheritdoc />
        public Task<BoolResult> ShutdownAsync(Context context)
        {
            return ShutdownCall<Tracer>.RunAsync(
                _tracer,
                context,
                async () =>
                {
                    ShutdownStarted = true;

                    var (localResult, remoteResult) = await MultiLevel(store => store.ShutdownAsync(context));

                    ShutdownStarted = false;
                    ShutdownCompleted = true;
                    return localResult & remoteResult;
                });
        }

        /// <inheritdoc />
        public bool ShutdownCompleted { get; private set; }

        /// <inheritdoc />
        public bool ShutdownStarted { get; private set; }

        /// <inheritdoc />
        public IAsyncEnumerable<GetSelectorResult> GetSelectors(
            Context context,
            Fingerprint weakFingerprint,
            CancellationToken cts,
            UrgencyHint urgencyHint)
        {
            return _localCacheSession.GetSelectors(context, weakFingerprint, cts, urgencyHint)
                .Concat(_remoteCacheSession.GetSelectors(context, weakFingerprint, cts, urgencyHint));
        }

        /// <inheritdoc />
        public async Task<GetContentHashListResult> GetContentHashListAsync(
            Context context,
            StrongFingerprint strongFingerprint,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return await OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                () => InternalGetContentHashListAsync(context, strongFingerprint, cts, urgencyHint),
                traceErrorsOnly: true);
        }

        // Enforces use of Extended result via compiler.
        private async Task<ExtendedGetContentHashListResult> InternalGetContentHashListAsync(
            Context context,
            StrongFingerprint strongFingerprint,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            GetContentHashListResult remoteContentHashListResult = null;
            if (_alwaysUpdateFromRemote)
            {
                remoteContentHashListResult = await lookupInRemoteAsync();
                if (remoteContentHashListResult.Succeeded && remoteContentHashListResult.ContentHashListWithDeterminism.ContentHashList != null)
                {
                    return ExtendedGetContentHashListResult.FromGetContentHashListResult(remoteContentHashListResult, ArtifactSource.BackingStore);
                }
            }

            // Looking into a local cache only when not forced getting the results from the remote.
            GetContentHashListResult localResult = await _localCacheSession.GetContentHashListAsync(context, strongFingerprint, cts, urgencyHint);
            if (localResult.Succeeded && localResult.ContentHashListWithDeterminism.ContentHashList != null)
            {
                return ExtendedGetContentHashListResult.FromGetContentHashListResult(localResult, ArtifactSource.LocalCache);
            }

            // Make sure we don't call the remote twice.
            remoteContentHashListResult ??= await lookupInRemoteAsync();
            if (!remoteContentHashListResult.Succeeded)
            {
                return ExtendedGetContentHashListResult.FromGetContentHashListResult(
                    remoteContentHashListResult,
                    ArtifactSource.Unknown);
            }

            // TODO: Need more info to determine L2 or L3
            return ExtendedGetContentHashListResult.FromGetContentHashListResult(remoteContentHashListResult, ArtifactSource.BackingStore);

            async Task<GetContentHashListResult> lookupInRemoteAsync()
            {
                var result = await _remoteCacheSession.GetContentHashListAsync(context, strongFingerprint, cts, urgencyHint);

                if (result.Succeeded && result.ContentHashListWithDeterminism.ContentHashList != null)
                {
                    // found in remote, add to local and content will be pinned by the implicit pin requirement.
                    _ = await _localCacheSession.AddOrGetContentHashListAsync(
                        context,
                        strongFingerprint,
                        result.ContentHashListWithDeterminism,
                        cts,
                        urgencyHint);
                }

                return result;
            }
        }

        /// <inheritdoc />
        public Task<PinResult> PinAsync(Context context, ContentHash contentHash, CancellationToken cts, UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                async () =>
                {
                    PinResult pinResult = await _localCacheSession.PinAsync(context, contentHash, cts, urgencyHint);
                    if (_remoteCacheIsReadOnly)
                    {
                        return pinResult;
                    }

                    return await _remoteCacheSession.PinAsync(context, contentHash, cts, urgencyHint);
                },
                extraEndMessage: r => $"Hash={contentHash.ToShortString()}",
                traceErrorsOnly: true);
        }

        /// <inheritdoc />
        public Task<OpenStreamResult> OpenStreamAsync(Context context, ContentHash contentHash, CancellationToken cts, UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                async () =>
                {
                    var openLocalAsync = () => _localCacheSession.OpenStreamAsync(
                        context, contentHash, cts, urgencyHint);

                    OpenStreamResult localOpenResult = await openLocalAsync();

                    if (localOpenResult.Code == OpenStreamResult.ResultCode.ContentNotFound)
                    {
                        using var _ = await _remoteFetchLock.AcquireAsync(contentHash, cts);

                        localOpenResult = await openLocalAsync();

                        if (localOpenResult.Code == OpenStreamResult.ResultCode.ContentNotFound)
                        {
                            ResultBase ingestFromRemoteResult = await IngestFileFromRemoteAsync(context, contentHash, urgencyHint, cts);
                            if (!ingestFromRemoteResult.Succeeded)
                            {
                                return new OpenStreamResult(ingestFromRemoteResult);
                            }

                            localOpenResult = await openLocalAsync();
                        }
                    }

                    return localOpenResult;
                },
                extraEndMessage: r => $"Hash={contentHash.ToShortString()}",
                traceErrorsOnly: true);
        }

        /// <inheritdoc />
        public Task<PlaceFileResult> PlaceFileAsync(
            Context context,
            ContentHash contentHash,
            AbsolutePath path,
            FileAccessMode accessMode,
            FileReplacementMode replacementMode,
            FileRealizationMode realizationMode,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                async () => (PlaceFileResult)await InternalPlaceFileAsync(
                    context,
                    contentHash,
                    path,
                    accessMode,
                    replacementMode,
                    realizationMode,
                    urgencyHint,
                    cts),
                extraEndMessage: r => $"Hash={contentHash.ToShortString()}, Path={path}",
                traceErrorsOnly: true);
        }

        // Enforces use of Extended result via compiler.
        private async Task<ExtendedPlaceFileResult> InternalPlaceFileAsync(
            Context context,
            ContentHash contentHash,
            AbsolutePath path,
            FileAccessMode accessMode,
            FileReplacementMode replacementMode,
            FileRealizationMode realizationMode,
            UrgencyHint urgencyHint,
            CancellationToken cts)
        {
            // TODO: We need more info to discern whether L2 or L3
            ArtifactSource source = ArtifactSource.LocalCache;

            var placeLocalAsync = () => _localCacheSession.PlaceFileAsync(
                context,
                contentHash,
                path,
                accessMode,
                replacementMode,
                realizationMode,
                cts);

            PlaceFileResult localPlaceResult = await placeLocalAsync();

            if (!localPlaceResult.IsPlaced() && localPlaceResult.Code != PlaceFileResult.ResultCode.NotPlacedAlreadyExists)
            {
                using var _ = await _remoteFetchLock.AcquireAsync(contentHash, cts);

                localPlaceResult = await placeLocalAsync();

                if (!localPlaceResult.IsPlaced() && localPlaceResult.Code != PlaceFileResult.ResultCode.NotPlacedAlreadyExists)
                {
                    // First download from datacenter/remote or backing store (L2 or L3) into L1.
                    ResultBase ingestFromRemoteResult = await IngestFileFromRemoteAsync(context, contentHash, urgencyHint, cts);
                    if (!ingestFromRemoteResult.Succeeded)
                    {
                        return new ExtendedPlaceFileResult(ingestFromRemoteResult);
                    }

                    localPlaceResult = await placeLocalAsync();

                    source = ArtifactSource.BackingStore;
                }
            }

            return ExtendedPlaceFileResult.FromPlaceFileResult(localPlaceResult, source);
        }

        private async Task<ResultBase> IngestFileFromRemoteAsync(Context context, ContentHash contentHash, UrgencyHint urgencyHint, CancellationToken cts)
        {
            OpenStreamResult openStreamResult = await _remoteCacheSession.OpenStreamAsync(context, contentHash, cts, urgencyHint);
            if (!openStreamResult.Succeeded)
            {
                return openStreamResult;
            }

            PutResult localPutResult = await _localCacheSession.PutStreamAsync(context, contentHash, openStreamResult.Stream, cts, urgencyHint);
            if (!localPutResult)
            {
                return localPutResult;
            }

            return BoolResult.Success;
        }

        /// <inheritdoc />
        public Task<AddOrGetContentHashListResult> AddOrGetContentHashListAsync(
            Context context,
            StrongFingerprint strongFingerprint,
            ContentHashListWithDeterminism contentHashList,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                async () =>
                {
                    if (_remoteCacheIsReadOnly)
                    {
                        // If the remote cache is readonly, then just relying on the local cache.
                        return await _localCacheSession.AddOrGetContentHashListAsync(
                            context,
                            strongFingerprint,
                            contentHashList,
                            cts,
                            urgencyHint);
                    }

                    // Remote is not readonly.
                    // In this case, calling the remote first to avoid race condition.
                    // It is possible that two machines are calling 'AddOrGetContentHashList' for the same fingerprint
                    // at the same time and remote store will get the content hash list if the current caller
                    // loses the race.
                    // In this case, we need change the contentHashList based on the results from the remote.
                    AddOrGetContentHashListResult remoteResult = await _remoteCacheSession.AddOrGetContentHashListAsync(
                        context,
                        strongFingerprint,
                        contentHashList,
                        cts,
                        urgencyHint);

                    if (!remoteResult.Succeeded)
                    {
                        return remoteResult;
                    }

                    ContentHashListWithDeterminism hashListForLocalCache = contentHashList;

                    // Remote result is successful.
                    if (remoteResult.ContentHashListWithDeterminism.ContentHashList != null)
                    {
                        // The fingerprint was added by another client.
                        hashListForLocalCache = remoteResult.ContentHashListWithDeterminism;
                    }

                    AddOrGetContentHashListResult localResult = await _localCacheSession.AddOrGetContentHashListAsync(
                        context,
                        strongFingerprint,
                        hashListForLocalCache,
                        cts,
                        urgencyHint);

                    if (!localResult.Succeeded)
                    {
                        return localResult;
                    }

                    return remoteResult;
                },
                traceErrorsOnly: true);
        }

        /// <inheritdoc />
        public Task<BoolResult> IncorporateStrongFingerprintsAsync(
            Context context,
            IEnumerable<Task<StrongFingerprint>> strongFingerprints,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                async () =>
                {
                    List<Task<StrongFingerprint>> strongFingerprintsList = strongFingerprints.ToList();
                    BoolResult result = await _localCacheSession.IncorporateStrongFingerprintsAsync(context, strongFingerprintsList, cts, urgencyHint);
                    if (!result || _remoteCacheIsReadOnly)
                    {
                        // TODO: is IncorporateStrongFingerprint actually a write? Or it is ok to call it for the remote even when _remoteMetadataIsReadonly is true?
                        return result;
                    }

                    return await _remoteCacheSession.IncorporateStrongFingerprintsAsync(
                        context,
                        strongFingerprintsList,
                        cts,
                        urgencyHint);
                },
                traceErrorsOnly: true);
        }

        /// <inheritdoc />
        public Task<PutResult> PutFileAsync(
            Context context,
            HashType hashType,
            AbsolutePath path,
            FileRealizationMode realizationMode,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                async () =>
                {
                    PutResult putResult = await _localCacheSession.PutFileAsync(context, hashType, path, realizationMode, cts, urgencyHint);
                    if (!putResult || _remoteCacheIsReadOnly)
                    {
                        return putResult;
                    }

                    PinResult pinResult = await _remoteCacheSession.PinAsync(context, putResult.ContentHash, cts, urgencyHint);
                    if (pinResult.Code == PinResult.ResultCode.Success)
                    {
                        // content was found in the remote - simply return local result at this point.
                        return putResult;
                    }

                    // content wasn't found in the remote - put the file in the remote now.
                    return await _remoteCacheSession.PutFileAsync(context, hashType, path, realizationMode, cts, urgencyHint);
                },
                extraEndMessage: r => $"HashType={hashType}, Path={path}",
                traceErrorsOnly: true);
        }

        /// <inheritdoc />
        public Task<PutResult> PutFileAsync(
            Context context,
            ContentHash contentHash,
            AbsolutePath path,
            FileRealizationMode realizationMode,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                async () =>
                {
                    PutResult putResult = await _localCacheSession.PutFileAsync(context, contentHash, path, realizationMode, cts, urgencyHint);
                    if (!putResult || _remoteCacheIsReadOnly)
                    {
                        return putResult;
                    }

                    PinResult pinResult = await _remoteCacheSession.PinAsync(context, putResult.ContentHash, cts, urgencyHint);
                    if (pinResult.Code == PinResult.ResultCode.Success)
                    {
                        // content was found in the remote - simply return local result at this point.
                        return putResult;
                    }

                    // content wasn't found in the remote - put the file in the remote now.
                    return await _remoteCacheSession.PutFileAsync(context, contentHash, path, realizationMode, cts, urgencyHint);
                },
                extraEndMessage: r => $"Hash={contentHash.ToShortString()}, Path={path}",
                traceErrorsOnly: true);
        }

        /// <inheritdoc />
        public Task<PutResult> PutStreamAsync(
            Context context,
            HashType hashType,
            Stream stream,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            return OperationContext(context, cts).PerformOperationAsync(
                _tracer,
                async () =>
                {
                    PutResult localPutResult = await _localCacheSession.PutStreamAsync(context, hashType, stream, cts, urgencyHint);
                    if (!localPutResult || _remoteCacheIsReadOnly)
                    {
                        return localPutResult;
                    }

                    return await PropagateStreamToRemote(context, localPutResult, stream, cts, urgencyHint);
                },
                extraEndMessage: r => $"HashType={hashType}",
                traceErrorsOnly: true);
        }

        /// <inheritdoc />
        public async Task<PutResult> PutStreamAsync(
            Context context,
            ContentHash contentHash,
            Stream stream,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            PutResult localPutResult = await _localCacheSession.PutStreamAsync(context, contentHash, stream, cts, urgencyHint);
            if (!localPutResult || _remoteCacheIsReadOnly)
            {
                return localPutResult;
            }

            return await PropagateStreamToRemote(context, localPutResult, stream, cts, urgencyHint);
        }

        /// <inheritdoc />
        public Task<IEnumerable<Task<Indexed<PinResult>>>> PinAsync(
            Context context,
            IReadOnlyList<ContentHash> contentHashes,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            if (_remoteCacheIsReadOnly)
            {
                return _localCacheSession.PinAsync(context, contentHashes, cts, urgencyHint);
            }

            return Workflows.RunWithFallback(
                contentHashes,
                hashes => _localCacheSession.PinAsync(context, hashes, cts, urgencyHint),
                hashes => _remoteCacheSession.PinAsync(context, hashes, cts, urgencyHint),
                // Pin the remote only when the remote is used for content as well.
                result => result.Code == PinResult.ResultCode.Success);
        }

        /// <inheritdoc />
        public Task<IEnumerable<Task<Indexed<PinResult>>>> PinAsync(Context context, IReadOnlyList<ContentHash> contentHashes, PinOperationConfiguration config)
        {
            if (_remoteCacheIsReadOnly)
            {
                return _localCacheSession.PinAsync(context, contentHashes, config);
            }

            return Workflows.RunWithFallback(
                contentHashes,
                hashes => _localCacheSession.PinAsync(context, hashes, config),
                hashes => _remoteCacheSession.PinAsync(context, hashes, config),
                // Pin the remote only when the remote is used for content as well.
                result => result.Code == PinResult.ResultCode.Success);
        }

        /// <inheritdoc />
        public Task<IEnumerable<Task<Indexed<PlaceFileResult>>>> PlaceFileAsync(Context context, IReadOnlyList<ContentHashWithPath> placeFileArgs, FileAccessMode accessMode, FileReplacementMode replacementMode, FileRealizationMode realizationMode, CancellationToken cts, UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            throw new NotImplementedException();
        }

        private async Task<PutResult> PropagateStreamToRemote(
            Context context,
            PutResult localPutResult,
            Stream stream,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal)
        {
            PinResult pinResult = await _remoteCacheSession.PinAsync(context, localPutResult.ContentHash, cts, urgencyHint);
            if (pinResult.Code == PinResult.ResultCode.Success)
            {
                // content was found in the remote - simply return local result at this point.
                return localPutResult;
            }

            // content wasn't found in remote ...
            if (stream.CanSeek)
            {
                // ... and stream is seekable, so can be used again.
                stream.Position = 0;
                PutResult remotePutResult = await _remoteCacheSession.PutStreamAsync(
                    context, localPutResult.ContentHash, stream, cts, urgencyHint);
                return remotePutResult;
            }

            // stream is not seekable, so we need a new one.
            OpenStreamResult localOpenResult = await _localCacheSession.OpenStreamAsync(
                context, localPutResult.ContentHash, cts, urgencyHint);

            if (!localOpenResult.Succeeded)
            {
                return new PutResult(localOpenResult);
            }

            using (localOpenResult.Stream)
            {
                PutResult remotePutResult = await _remoteCacheSession.PutStreamAsync(
                    context, localPutResult.ContentHash, localOpenResult.Stream, cts, urgencyHint);
                return remotePutResult;
            }
        }

        private static OperationContext OperationContext(Context context, CancellationToken token) => new OperationContext(context, token);

        private async Task<(TResult Local, TResult Remote)> MultiLevel<TResult>(Func<ICacheSession, Task<TResult>> func)
        {
            var localCacheTask = Task.Run(() => func(_localCacheSession));
            var remoteCacheTask = Task.Run(() => func(_remoteCacheSession));

            await Task.WhenAll(localCacheTask, remoteCacheTask);

            return (await localCacheTask, await remoteCacheTask);
        }
    }
}