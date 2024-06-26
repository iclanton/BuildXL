// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.Results;
using BuildXL.Cache.ContentStore.Interfaces.Sessions;
using BuildXL.Cache.ContentStore.Interfaces.Stores;
using BuildXL.Cache.ContentStore.Interfaces.Tracing;

namespace ContentStoreTest.Stores
{
    /// <summary>
    ///     Test class for a store that always fails.
    /// </summary>
    internal class TestFailingContentStore : IContentStore
    {
        internal const string FailureMessage = "Test failure message from a store that always fails.";

        // <inheritdoc />
        public bool StartupCompleted { get; }

        // <inheritdoc />
        public bool StartupStarted { get; private set; }

        // <inheritdoc />
        public Task<BoolResult> StartupAsync(Context context)
        {
            StartupStarted = true;
            return Task.FromResult(new BoolResult(FailureMessage));
        }

        // <inheritdoc />
        public void Dispose()
        {
        }

        // <inheritdoc />
        public bool ShutdownCompleted { get; }

        // <inheritdoc />
        public bool ShutdownStarted { get; private set; }

        // <inheritdoc />
        public Task<BoolResult> ShutdownAsync(Context context)
        {
            ShutdownStarted = true;
            return Task.FromResult(new BoolResult(FailureMessage));
        }

        // <inheritdoc />
        public CreateSessionResult<IContentSession> CreateSession(Context context, string name, ImplicitPin implicitPin)
        {
            return new CreateSessionResult<IContentSession>(FailureMessage);
        }

        // <inheritdoc />
        public Task<GetStatsResult> GetStatsAsync(Context context)
        {
            return Task.FromResult(new GetStatsResult(FailureMessage));
        }

        /// <inheritdoc />
        public Task<DeleteResult> DeleteAsync(Context context, ContentHash contentHash, DeleteContentOptions deleteOptions = null)
        {
            return Task.FromResult(new DeleteResult(DeleteResult.ResultCode.ContentNotDeleted, FailureMessage));
        }

        /// <inheritdoc />
        public void PostInitializationCompleted(Context context) { }
    }
}
