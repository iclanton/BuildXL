// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.FileSystem;
using BuildXL.Cache.ContentStore.Service;
using BuildXL.Cache.ContentStore.Stores;
using BuildXL.Cache.ContentStore.Interfaces.FileSystem;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.Sessions;
using BuildXL.Cache.ContentStore.Interfaces.Stores;
using BuildXL.Cache.ContentStore.Interfaces.Tracing;
using BuildXL.Cache.ContentStore.InterfacesTest.Results;
using ContentStoreTest.Stores;
using ContentStoreTest.Test;
using Xunit.Abstractions;

namespace ContentStoreTest.Sessions
{
    public abstract class ServiceClientContentSessionTestBase<T> : TestBase
        where T : ServiceClientContentStore, ITestServiceClientContentStore
    {
        protected ServiceClientContentStoreConfiguration CreateConfiguration()
        {
            return new ServiceClientContentStoreConfiguration(CacheName, rpcConfiguration: null, scenario: Scenario);
        }

        protected const string CacheName = "test";
        protected const uint GracefulShutdownSeconds = ServiceConfiguration.DefaultGracefulShutdownSeconds;

        protected virtual string SessionName { get; set; } = "name";

        protected const int ContentByteCount = 100;
        protected const HashType ContentHashType = HashType.Vso0;
        protected const long DefaultMaxSize = 1 * 1024 * 1024;
        protected static readonly CancellationToken Token = CancellationToken.None;
        protected string Scenario;

        protected ServiceClientContentSessionTestBase(string scenario, ITestOutputHelper output = null)
            : base(() => new PassThroughFileSystem(TestGlobal.Logger), TestGlobal.Logger, output)
        {
            Scenario = scenario + ScenarioSuffix;
        }

        protected static long MaxSize => DefaultMaxSize;

        protected async Task RunSessionTestAsync(
            Context context, IContentStore store, ImplicitPin implicitPin, Func<Context, IContentSession, Task> funcAsync)
        {
            var createResult = store.CreateSession(context, SessionName, implicitPin);
            createResult.ShouldBeSuccess();
            using (var session = createResult.Session)
            {
                try
                {
                    await session.StartupAsync(context).ShouldBeSuccess();
                    await funcAsync(context, session);
                }
                finally
                {
                    await session.ShutdownAsync(context).ShouldBeSuccess();
                }
            }
        }

        protected async Task RunStoreTestAsync(Func<Context, IContentStore, Task> funcAsync, LocalServerConfiguration localContentServerConfiguration = null, TimeSpan? heartbeatOverride = null, long cacheSize = DefaultMaxSize)
        {
            var context = new Context(Logger);
            // Using unique scenario to avoid flakiness when running the tests in parallel
            Scenario += Guid.NewGuid();
            using (var directory = new DisposableDirectory(FileSystem))
            {
                var config = new ContentStoreConfiguration(new MaxSizeQuota($"{cacheSize}"));

                using (var store = CreateStore(directory.Path, config, localContentServerConfiguration ?? TestConfigurationHelper.LocalContentServerConfiguration, heartbeatOverride))
                {
                    try
                    {
                        await store.StartupAsync(context).ShouldBeSuccess();
                        await funcAsync(context, store);
                    }
                    finally
                    {
                        await store.ShutdownAsync(context).ShouldBeSuccess();
                    }
                }
            }
        }

        protected Task RunSessionTestAsync(
            ImplicitPin implicitPin,
            Func<Context, IContentSession, Task> funcAsync,
            LocalServerConfiguration localContentServerConfiguration = null,
            long cacheSize = DefaultMaxSize)
        {
            return RunStoreTestAsync(
                (context, store) => RunSessionTestAsync(context, store, implicitPin, funcAsync),
                localContentServerConfiguration,
                cacheSize: cacheSize);
        }

        protected abstract T CreateStore(AbsolutePath rootPath, ContentStoreConfiguration configuration, LocalServerConfiguration localContentServerConfiguration, TimeSpan? heartbeatOverride);
    }
}
