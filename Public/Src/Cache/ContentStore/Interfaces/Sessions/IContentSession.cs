// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.FileSystem;
using BuildXL.Cache.ContentStore.Interfaces.Results;
using BuildXL.Cache.ContentStore.Interfaces.Stores;
using BuildXL.Cache.ContentStore.Interfaces.Tracing;

// ReSharper disable UnusedParameter.Global
namespace BuildXL.Cache.ContentStore.Interfaces.Sessions
{
    /// <summary>
    ///     A related set of accesses to a content store.
    /// </summary>
    public interface IContentSession : IName, IStartupShutdown
    {
        /// <summary>
        ///     Ensure content does not get deleted.
        /// </summary>
        Task<PinResult> PinAsync(
            Context context,
            ContentHash contentHash,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal);

        /// <summary>
        /// Pin in a configurable way.
        /// </summary>
        Task<IEnumerable<Task<Indexed<PinResult>>>> PinAsync(
            Context context,
            IReadOnlyList<ContentHash> contentHashes,
            PinOperationConfiguration config);

        /// <summary>
        ///     Open a stream to content.
        /// </summary>
        Task<OpenStreamResult> OpenStreamAsync(
            Context context,
            ContentHash contentHash,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal);

        /// <summary>
        ///     Materialize content to the filesystem.
        /// </summary>
        Task<PlaceFileResult> PlaceFileAsync(
            Context context,
            ContentHash contentHash,
            AbsolutePath path,
            FileAccessMode accessMode,
            FileReplacementMode replacementMode,
            FileRealizationMode realizationMode,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal);

        /// <summary>
        ///     Ensure content does not get deleted in bulk.
        /// </summary>
        Task<IEnumerable<Task<Indexed<PinResult>>>> PinAsync(
            Context context,
            IReadOnlyList<ContentHash> contentHashes,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal);

        /// <summary>
        ///     Materialize content to the filesystem in bulk.
        /// </summary>
        Task<IEnumerable<Task<Indexed<PlaceFileResult>>>> PlaceFileAsync(
            Context context,
            IReadOnlyList<ContentHashWithPath> hashesWithPaths,
            FileAccessMode accessMode,
            FileReplacementMode replacementMode,
            FileRealizationMode realizationMode,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal);

        /// <summary>
        ///     Add content from a file.
        /// </summary>
        /// <param name="context">
        ///     Tracing context.
        /// </param>
        /// <param name="hashType">
        ///     Hash type to use when hashing the content.
        /// </param>
        /// <param name="path">
        ///     Path to file containing the content to add.
        /// </param>
        /// <param name="realizationMode">
        ///     Content ingress method allowed by caller.
        /// </param>
        /// <param name="cts">
        ///     A token that can signal this call should return as soon as possible.
        /// </param>
        /// <param name="urgencyHint">
        ///     Hint as to how urgent this request is.
        /// </param>
        /// <returns>
        ///     Result providing the call's completion status.
        /// </returns>
        Task<PutResult> PutFileAsync(
            Context context,
            HashType hashType,
            AbsolutePath path,
            FileRealizationMode realizationMode,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal
        );

        /// <summary>
        ///     Add content from a file.
        /// </summary>
        /// <param name="context">
        ///     Tracing context.
        /// </param>
        /// <param name="contentHash">
        ///     Hash of the specific content or the special no value.
        /// </param>
        /// <param name="path">
        ///     Path to file containing the content to add.
        /// </param>
        /// <param name="realizationMode">
        ///     Content ingress method allowed by caller.
        /// </param>
        /// <param name="cts">
        ///     A token that can signal this call should return as soon as possible.
        /// </param>
        /// <param name="urgencyHint">
        ///     Hint as to how urgent this request is.
        /// </param>
        /// <returns>
        ///     Result providing the call's completion status.
        /// </returns>
        Task<PutResult> PutFileAsync(
            Context context,
            ContentHash contentHash,
            AbsolutePath path,
            FileRealizationMode realizationMode,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal
        );

        /// <summary>
        ///     Add content from a stream.
        /// </summary>
        /// <param name="context">
        ///     Tracing context.
        /// </param>
        /// <param name="hashType">
        ///     Hash type to use when hashing the content.
        /// </param>
        /// <param name="stream">
        ///     Stream containing content to add.
        /// </param>
        /// <param name="cts">
        ///     A token that can signal this call should return as soon as possible.
        /// </param>
        /// <param name="urgencyHint">
        ///     Hint as to how urgent this request is.
        /// </param>
        /// <returns>
        ///     Result providing the call's completion status.
        /// </returns>
        Task<PutResult> PutStreamAsync(
            Context context,
            HashType hashType,
            Stream stream,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal
        );

        /// <summary>
        ///     Add content from a stream.
        /// </summary>
        /// <param name="context">
        ///     Tracing context.
        /// </param>
        /// <param name="contentHash">
        ///     Hash of the specific content or the special no value.
        /// </param>
        /// <param name="stream">
        ///     Stream containing content to add.
        /// </param>
        /// <param name="cts">
        ///     A token that can signal this call should return as soon as possible.
        /// </param>
        /// <param name="urgencyHint">
        ///     Hint as to how urgent this request is.
        /// </param>
        /// <returns>
        ///     Result providing the call's completion status.
        /// </returns>
        Task<PutResult> PutStreamAsync(
            Context context,
            ContentHash contentHash,
            Stream stream,
            CancellationToken cts,
            UrgencyHint urgencyHint = UrgencyHint.Nominal
        );
    }
}
