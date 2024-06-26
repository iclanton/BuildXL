// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using BuildXL.Engine.Cache.Fingerprints;
using BuildXL.Pips.Builders;
using BuildXL.Pips.Graph;
using BuildXL.Pips.Operations;
using BuildXL.Scheduler.Graph;
using BuildXL.Storage;
using BuildXL.Storage.Fingerprints;
using BuildXL.Utilities.Core;
using Test.BuildXL.TestUtilities;
using Test.BuildXL.TestUtilities.Xunit;
using Xunit;
using Xunit.Abstractions;
using static Test.BuildXL.Scheduler.SchedulerTestHelper;

namespace Test.BuildXL.Scheduler
{
    public class PipStaticFingerprintConstructionTests : BuildXL.TestUtilities.Xunit.XunitBuildXLTest
    {
        public PipStaticFingerprintConstructionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void TestPartialSealDirectoryMembersAffectFingerprints()
        {
            ContentFingerprint fingerprint1 = CreateFingerprintForPartialSealWithMember("f.txt");
            ContentFingerprint fingerprint2 = CreateFingerprintForPartialSealWithMember("f.txt");

            XAssert.AreEqual(fingerprint1, fingerprint2);

            fingerprint1 = CreateFingerprintForPartialSealWithMember("f.txt");
            fingerprint2 = CreateFingerprintForPartialSealWithMember("g.txt");

            XAssert.AreNotEqual(fingerprint1, fingerprint2);
        }

        [Fact]
        public void TestCompositeSharedOpaqueMembersAffectFingerprints()
        {
            ContentFingerprint fingerprint1 = CreateFingerprintForCompositeSharedOpaque(@"\\root", new[] { @"\\root\so1", @"\\root\so2" }, null);
            ContentFingerprint fingerprint2 = CreateFingerprintForCompositeSharedOpaque(@"\\root", new[] { @"\\root\so1", @"\\root\so2" }, null);

            XAssert.AreEqual(fingerprint1, fingerprint2);

            fingerprint1 = CreateFingerprintForCompositeSharedOpaque(@"\\root", new[] { @"\\root\so1", @"\\root\so2" }, null);
            fingerprint2 = CreateFingerprintForCompositeSharedOpaque(@"\\root", new[] { @"\\root\so1", @"\\root\so3" }, null);

            XAssert.AreNotEqual(fingerprint1, fingerprint2);
        }

        [Fact]
        public void TestCompositeSharedOpaqueFilterAffectsFingerprints()
        {
            ContentFingerprint fingerprint1 = CreateFingerprintForCompositeSharedOpaque(
                @"\\root", new[] { @"\\root\so1" }, new SealDirectoryContentFilter(SealDirectoryContentFilter.ContentFilterKind.Include, ".*"));
            ContentFingerprint fingerprint2 = CreateFingerprintForCompositeSharedOpaque(
                @"\\root", new[] { @"\\root\so1" }, new SealDirectoryContentFilter(SealDirectoryContentFilter.ContentFilterKind.Include, ".*"));

            XAssert.AreEqual(fingerprint1, fingerprint2);

            fingerprint1 = CreateFingerprintForCompositeSharedOpaque(
                @"\\root", new[] { @"\\root\so1" }, new SealDirectoryContentFilter(SealDirectoryContentFilter.ContentFilterKind.Include, ".*exe"));
            fingerprint2 = CreateFingerprintForCompositeSharedOpaque(
                @"\\root", new[] { @"\\root\so1" }, new SealDirectoryContentFilter(SealDirectoryContentFilter.ContentFilterKind.Include, ".*txt"));

            XAssert.AreNotEqual(fingerprint1, fingerprint2);

            fingerprint1 = CreateFingerprintForCompositeSharedOpaque(
                @"\\root", new[] { @"\\root\so1" }, new SealDirectoryContentFilter(SealDirectoryContentFilter.ContentFilterKind.Include, ".*"));
            fingerprint2 = CreateFingerprintForCompositeSharedOpaque(
                @"\\root", new[] { @"\\root\so1" }, new SealDirectoryContentFilter(SealDirectoryContentFilter.ContentFilterKind.Exclude, ".*"));

            XAssert.AreNotEqual(fingerprint1, fingerprint2);
        }

        [Fact]
        public void TestSharedOpaqueProducersAffectFingerprints()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                var root = env.Paths.CreateAbsolutePath(@"\\root");

                // We create two shared opaques with same root and empty content
                var sharedOpaque1 = CreateSharedOpaque(env, root);
                var sharedOpaque2 = CreateSharedOpaque(env, root);
                var graph = AssertSuccessGraphBuilding(env);

                // But their fingerprints should be different
                var fingerprint1 = CreateFingerprintForSharedOpaque(sharedOpaque1, graph);
                var fingerprint2 = CreateFingerprintForSharedOpaque(sharedOpaque2, graph);

                XAssert.AreNotEqual(fingerprint1, fingerprint2);
            }
        }

        private ContentFingerprint CreateFingerprintForPartialSealWithMember(string fileName)
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath root = env.Paths.CreateAbsolutePath(@"\\dummyPath\Root");
                FileArtifact member = FileArtifact.CreateSourceFile(root.Combine(env.PathTable, fileName));

                var staticDirectory = env.PipConstructionHelper.SealDirectoryPartial(
                    root,
                    new[] {member});

                var pipBuilder = CreatePipBuilderWithTag(env, nameof(TestPartialSealDirectoryMembersAffectFingerprints));
                var outputPath = env.Paths.CreateAbsolutePath(@"\\dummyPath\out");
                pipBuilder.AddOutputFile(outputPath);
                pipBuilder.AddInputDirectory(staticDirectory);

                env.PipConstructionHelper.AddProcess(pipBuilder);
                var graph = AssertSuccessGraphBuilding(env);
                var producerId = graph.TryGetProducer(FileArtifact.CreateOutputFile(outputPath));

                XAssert.IsTrue(producerId.IsValid);
                XAssert.IsTrue(graph.TryGetPipFingerprint(producerId, out ContentFingerprint fingerprint));

                return fingerprint;
            }
        }

        private ContentFingerprint CreateFingerprintForCompositeSharedOpaque(
            string composedSharedOpaqueRoot, string[] sharedOpaqueMembers, SealDirectoryContentFilter? contentFilter)
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                var sharedOpaqueDirectoryArtifactMembers = new DirectoryArtifact[sharedOpaqueMembers.Length];
                for (int i = 0; i < sharedOpaqueMembers.Length; i++)
                {
                    sharedOpaqueDirectoryArtifactMembers[i] = CreateSharedOpaque(env, env.Paths.CreateAbsolutePath(sharedOpaqueMembers[i]));
                }

                var success = env.PipConstructionHelper.TryComposeSharedOpaqueDirectory(
                    env.Paths.CreateAbsolutePath(composedSharedOpaqueRoot), 
                    sharedOpaqueDirectoryArtifactMembers, 
                    actionKind: SealDirectoryCompositionActionKind.WidenDirectoryCone,
                    contentFilter: contentFilter,
                    description: null, 
                    tags: new string[0], 
                    out var sharedOpaqueDirectory);
                XAssert.IsTrue(success);

                var graph = AssertSuccessGraphBuilding(env);
                var fingerprint = CreateFingerprintForSharedOpaque(sharedOpaqueDirectory, graph);

                return fingerprint;
            }
        }

        private static ContentFingerprint CreateFingerprintForSharedOpaque(DirectoryArtifact sharedOpaqueDirectory, PipGraph graph)
        {
            var sealDirectoryPipId = graph.GetSealedDirectoryNode(sharedOpaqueDirectory).ToPipId();
            XAssert.IsTrue(graph.TryGetPipFingerprint(sealDirectoryPipId, out ContentFingerprint fingerprint));
            return fingerprint;
        }

        private DirectoryArtifact CreateSharedOpaque(TestEnv env, AbsolutePath root)
        {
            var pipBuilder = CreatePipBuilderWithTag(env, nameof(TestPartialSealDirectoryMembersAffectFingerprints));
            pipBuilder.AddOutputDirectory(root, SealDirectoryKind.SharedOpaque);
            var outputs = env.PipConstructionHelper.AddProcess(pipBuilder);

            outputs.TryGetOutputDirectory(root, out var sharedOpaqueDirectoryArtifact);
            return sharedOpaqueDirectoryArtifact.Root;
        }
    }
}
