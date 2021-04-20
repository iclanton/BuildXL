// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using BuildXL.Engine.Distribution;
using BuildXL.Utilities.Tasks;
using System.Collections.Generic;
using System.Threading;
using PipGraphCacheDescriptor = BuildXL.Distribution.Grpc.PipGraphCacheDescriptor;
using AttachCompletionInfo = BuildXL.Engine.Distribution.OpenBond.AttachCompletionInfo;
using WorkerNotificationArgs = BuildXL.Engine.Distribution.OpenBond.WorkerNotificationArgs;
using PipBuildRequest = BuildXL.Distribution.Grpc.PipBuildRequest;
using SinglePipBuildRequest = BuildXL.Distribution.Grpc.SinglePipBuildRequest;
using GrpcPipBuildRequest = BuildXL.Distribution.Grpc.PipBuildRequest;
using FileArtifactKeyedHash = BuildXL.Distribution.Grpc.FileArtifactKeyedHash;
using BuildXL.Engine.Distribution.Grpc;
using BuildXL.Distribution.Grpc;
using BuildXL.Scheduler;
using System.Linq;
using static BuildXL.Engine.Distribution.Grpc.ClientConnectionManager;

namespace Test.BuildXL.Distribution
{
    internal sealed class GrpcMockData
    {
        public static BuildStartData BuildStartData =>
            new()
            {
                WorkerId = 1,
                CachedGraphDescriptor = new PipGraphCacheDescriptor(),
                SessionId = Guid.NewGuid().ToString(),
                FingerprintSalt = "salt",
                MasterLocation = new ServiceLocation()
                {
                    IpAddress = "192.168.1.1",
                    Port = 9090
                },
                SymlinkFileContentHash = DistributionHelpers.ToByteString(new ArraySegment<byte>())
            };

        public static BuildEndData GetBuildEndData(bool failed)
        {
            BuildEndData data = new BuildEndData();
            if (failed)
            {
                data.Failure = "Failed";
            }

            return data;
        }

        public static GrpcPipBuildRequest PipBuildRequest(int initialSequenceNumber, params (uint, PipExecutionStep)[] pips)
        {
            List<SinglePipBuildRequest> buildRequests = new List<SinglePipBuildRequest>(pips.Length);
            List<FileArtifactKeyedHash> hashes = pips.Select(p => new FileArtifactKeyedHash()).ToList();

            var i = initialSequenceNumber;
            foreach (var (pipId, step) in pips)
            {
                buildRequests.Add(new SinglePipBuildRequest()
                {
                    PipIdValue = pipId,
                    Step = (int)step,
                    SequenceNumber = i++
                });
            }

            var req = new PipBuildRequest();

            req.Pips.AddRange(buildRequests);
            req.Hashes.AddRange(hashes);
            return req;
        }
    }

    public sealed class WorkerServerMock : IServer
    {
        public WorkerService WorkerService;

        public int ShutdownCallCount;
        public bool ShutdownWasCalled => ShutdownCallCount > 0;

        public int StartCallCount;
        public bool StartWasCalled => StartCallCount > 0;

        // IServer methods that may be called by the WorkerService:
        Task IServer.ShutdownAsync()
        {
            ShutdownCallCount++;
            return Task.CompletedTask;
        }

        void IServer.Start(int port)
        {
            StartCallCount++;
        }

        Task IServer.DisposeAsync() => Task.CompletedTask;

        public void Dispose() { }

        // Server methods to better emulate the scenarios.
        // These should emulate what the homonymous methods do in GrpcWorkerServer 
        public void Attach(BuildStartData message)
        {
            var bondMessage = message.ToOpenBond();
            WorkerService.AttachCore(bondMessage, "OrchestratorName");
        }

        public void ExecutePips(GrpcPipBuildRequest message)
        {
            var bondMessage = message.ToOpenBond();
            WorkerService.ExecutePipsCore(bondMessage);
        }

        public void Exit(BuildEndData message)
        {
            WorkerService.ExitCallReceivedFromMaster();
            WorkerService.Exit(failure: message.Failure);
        }
    }

    public sealed class OrchestratorClientMock : IMasterClient
    {
        private static RpcCallResult<Unit> SuccessResult => new RpcCallResult<Unit>(Unit.Void, 1, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        private readonly TaskCompletionSource<bool> m_attachmentCompletedSource = new TaskCompletionSource<bool>();
        public Task<bool> AttachmentCompleted => m_attachmentCompletedSource.Task;
        
        Task<RpcCallResult<Unit>> IMasterClient.AttachCompletedAsync(AttachCompletionInfo attachCompletionInfo)
        {
            m_attachmentCompletedSource.TrySetResult(true);
            return Task.FromResult(SuccessResult);
        }

        Task IMasterClient.CloseAsync()
        {
            m_attachmentCompletedSource.TrySetResult(false);
            return Task.CompletedTask;
        }

        Task<RpcCallResult<Unit>> IMasterClient.NotifyAsync(WorkerNotificationArgs notificationArgs, IList<long> semiStableHashes, CancellationToken cancellationToken)
        {
            return Task.FromResult(SuccessResult);
        }

        void IMasterClient.Initialize(string ipAddress, int port, EventHandler<ConnectionTimeoutEventArgs> onConnectionTimeOutAsync)
        {

        }
    }
}