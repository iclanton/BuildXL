// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.ContractsLight;
using System.Linq;
using BuildXL.Pips;
using BuildXL.Pips.Graph;
using BuildXL.Pips.Builders;
using BuildXL.Pips.Operations;
using BuildXL.Pips.Tracing;
using BuildXL.Utilities.Core;
using BuildXL.Utilities.Collections;
using Test.BuildXL.TestUtilities;
using Test.BuildXL.TestUtilities.Xunit;
using Xunit;
using Xunit.Abstractions;
using static Test.BuildXL.Scheduler.SchedulerTestHelper;

namespace Test.BuildXL.Scheduler
{
    /// <summary>
    /// Unit tests for the graph construction rules around sealing directories.
    /// These tests do not execute pips.
    /// </summary>
    public class SealedDirectoryGraphConstructionTests : BuildXL.TestUtilities.Xunit.XunitBuildXLTest
    {
        public SealedDirectoryGraphConstructionTests(ITestOutputHelper output)
            : base(output)
        {
            RegisterEventSource(global::BuildXL.Scheduler.ETWLogger.Log);
            RegisterEventSource(global::BuildXL.Pips.ETWLogger.Log);
        }

        [Fact]
        public void TestSealDirectoryWithCorrectSourceContents()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.SourceRoot, "seal");
                FileArtifact a = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directoryPath, "a"));
                FileArtifact b = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directoryPath, "subdir", "b"));

                ScheduleSealDirectory(env, directoryPath, a, b);
            }
        }

        [Fact]
        public void TestSealDirectoryWithCorrectSourceContentsMultipleTimes()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.SourceRoot, "seal");
                FileArtifact a = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directoryPath, "a"));
                FileArtifact b = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directoryPath, "subdir", "b"));

                ScheduleSealDirectory(env, directoryPath, a, b);
                ScheduleSealDirectory(env, directoryPath, a, b);
            }
        }

        [Fact]
        public void TestSealDirectoryFailureDueToUnrelatedFile()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.SourceRoot, "seal");
                FileArtifact a = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directoryPath, "a"));
                FileArtifact elsewhere = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(env.SourceRoot, "elsewhere"));

                AssertCannotScheduleSealDirectory(env, directoryPath, a, elsewhere);
                AssertErrorEventLogged(LogEventId.InvalidSealDirectoryContentSinceNotUnderRoot);

                ScheduleSealDirectory(env, directoryPath, a);
            }
        }

        [Fact]
        public void TestSealDirectoryWithUnderspecifiedSourceContents()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestSealDirectoryWithUnderspecifiedSourceContents)))
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.SourceRoot, "seal");
                FileArtifact a = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directoryPath, "a"));
                FileArtifact b = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directoryPath, "subdir", "b"));
                ScheduleConsumeSourceFile(env, directoryPath, "unspecified");

                ScheduleSealDirectory(env, directoryPath, a, b);

                XAssert.IsNotNull(env.PipGraph.Build());
                // Expect to succesfully pass
            }
        }

        [Fact]
        public void TestSealDirectoryWithCorrectOutputContents()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                FileArtifact a = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "a");
                FileArtifact b = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, @"subdir\b");

                ScheduleSealDirectory(env, directoryPath, a, b);
            }
        }

        [Fact]
        public void TestSealDirectoryWithUnderspecifiedOutputContents()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestSealDirectoryWithUnderspecifiedOutputContents)))
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                FileArtifact a = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "a");
                FileArtifact b = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, @"subdir\b");
                ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "unspecified");
                ScheduleWriteOutputFileUnderDirectory(env, directoryPath, @"subdir\unspecified");

                ScheduleSealDirectory(env, directoryPath, a, b);

                XAssert.IsNull(env.PipGraph.Build());
                AssertErrorEventLogged(LogEventId.InvalidGraphSinceFullySealedDirectoryIncomplete, 2);
            }
        }

        [Fact]
        public void TestSealDirectoryWithUnderspecifiedRewriteContentsOutputContents()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestSealDirectoryWithUnderspecifiedRewriteContentsOutputContents)))
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                FileArtifact a1 = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "a");
                FileArtifact b = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "b");
                FileArtifact a2 = ScheduleRewrite(env, b, a1);

                ScheduleSealDirectory(env, directoryPath, a2);

                XAssert.IsNull(env.PipGraph.Build());
                AssertErrorEventLogged(LogEventId.InvalidGraphSinceFullySealedDirectoryIncomplete);
            }
        }

        [Fact]
        public void TestTopLevelSourceSealDirectoryContainingOutputFile()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestTopLevelSourceSealDirectoryContainingOutputFile)))
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "ssd");
                ScheduleWriteOutputFileUnderDirectory(env, directoryPath, @"a\b\f.txt");

                ScheduleSourceSealDirectory(env, directoryPath, allDirectories: false);

                XAssert.IsNull(env.PipGraph.Build());
                AssertErrorEventLogged(LogEventId.InvalidGraphSinceSourceSealedDirectoryContainsOutputFile);
            }
        }

        [Fact]
        public void TestAllDirectoriesSourceSealDirectoryContainingOutputFile()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestAllDirectoriesSourceSealDirectoryContainingOutputFile)))
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "ssd");
                ScheduleWriteOutputFileUnderDirectory(env, directoryPath, @"a\b\f.txt");

                ScheduleSourceSealDirectory(env, directoryPath, allDirectories: true);

                XAssert.IsNull(env.PipGraph.Build());
                AssertErrorEventLogged(LogEventId.InvalidGraphSinceSourceSealedDirectoryContainsOutputFile);
            }
        }

        [Fact]
        public void TestSourceSealDirectoryCannotCoincideSourceFile1()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestSourceSealDirectoryCannotCoincideSourceFile1)))
            {
                AbsolutePath path = env.Paths.CreateAbsolutePath(env.ObjectRoot, "ssd");

                var pip1 = CreatePipBuilderWithTag(env, "test");
                pip1.AddInputFile(path);
                pip1.AddOutputFile(env.Paths.CreateAbsolutePath(env.ObjectRoot, "out1"));
                env.PipConstructionHelper.AddProcess(pip1);

                var ssd = env.PipConstructionHelper.SealDirectorySource(path);

                var pip2 = CreatePipBuilderWithTag(env, "test");
                pip2.AddInputDirectory(ssd);
                pip2.AddOutputFile(env.Paths.CreateAbsolutePath(env.ObjectRoot, "out2"));
                env.PipConstructionHelper.AddProcess(pip2);

                XAssert.IsNull(env.PipGraph.Build());
                AssertErrorEventLogged(LogEventId.InvalidGraphSinceSourceSealedDirectoryCoincidesSourceFile);
            }
        }

        [Fact]
        public void TestSourceSealDirectoryCannotCoincideSourceFile2()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestSourceSealDirectoryCannotCoincideSourceFile2)))
            {
                AbsolutePath path = env.Paths.CreateAbsolutePath(env.ObjectRoot, "ssd");

                var ssd = env.PipConstructionHelper.SealDirectorySource(path);

                var pip2 = CreatePipBuilderWithTag(env, "test");
                pip2.AddInputDirectory(ssd);
                pip2.AddOutputFile(env.Paths.CreateAbsolutePath(env.ObjectRoot, "out2"));
                env.PipConstructionHelper.AddProcess(pip2);

                var pip1 = CreatePipBuilderWithTag(env, "test");
                pip1.AddInputFile(path);
                pip1.AddOutputFile(env.Paths.CreateAbsolutePath(env.ObjectRoot, "out1"));
                env.PipConstructionHelper.AddProcess(pip1);

                XAssert.IsNull(env.PipGraph.Build());
                AssertErrorEventLogged(LogEventId.InvalidGraphSinceSourceSealedDirectoryCoincidesSourceFile);
            }
        }

        [Fact]
        public void TestSourceSealDirectoryCannotCoincideOutputFile1()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestSourceSealDirectoryCannotCoincideOutputFile1)))
            {
                AbsolutePath path = env.Paths.CreateAbsolutePath(env.ObjectRoot, "ssd");

                var pip1 = CreatePipBuilderWithTag(env, "test");
                pip1.AddOutputFile(path);
                env.PipConstructionHelper.AddProcess(pip1);

                var ssd = env.PipConstructionHelper.SealDirectorySource(path);

                var pip2 = CreatePipBuilderWithTag(env, "test");
                pip2.AddInputDirectory(ssd);
                pip2.AddOutputFile(env.Paths.CreateAbsolutePath(env.ObjectRoot, "out2"));
                env.PipConstructionHelper.AddProcess(pip2);

                XAssert.IsNull(env.PipGraph.Build());
                AssertErrorEventLogged(LogEventId.InvalidGraphSinceSourceSealedDirectoryCoincidesOutputFile);
            }
        }

        [Fact]
        public void TestSourceSealDirectoryCannotCoincideOutputFile2()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler(nameof(TestSourceSealDirectoryCannotCoincideOutputFile2)))
            {
                AbsolutePath path = env.Paths.CreateAbsolutePath(env.ObjectRoot, "ssd");

                var ssd = env.PipConstructionHelper.SealDirectorySource(path);

                var pip2 = CreatePipBuilderWithTag(env, "test");
                pip2.AddInputDirectory(ssd);
                pip2.AddOutputFile(env.Paths.CreateAbsolutePath(env.ObjectRoot, "out2"));
                env.PipConstructionHelper.AddProcess(pip2);

                var pip1 = CreatePipBuilderWithTag(env, "test");
                pip1.AddOutputFile(path);
                XAssert.IsFalse(env.PipConstructionHelper.TryAddProcess(pip1));

                AssertErrorEventLogged(LogEventId.InvalidOutputSinceDirectoryHasBeenSealed);
            }
        }

        [Fact]
        public void TestFailedWritesToSealedDirectory()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                FileArtifact existing = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "existing");

                ScheduleSealDirectory(env, directoryPath, existing);

                AssertCannotScheduleWriteOutputFileUnderDirectory(env, directoryPath, "existing");
                AssertErrorEventLogged(LogEventId.InvalidOutputSinceDirectoryHasBeenSealed);
                AssertCannotScheduleWriteOutputFileUnderDirectory(env, directoryPath, "new");
                AssertErrorEventLogged(LogEventId.InvalidOutputSinceDirectoryHasBeenSealed);
            }
        }

        [Fact]
        public void TestSealNonOverlappingPartialDirectories()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath outerPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "outer");
                AbsolutePath innerPath = env.Paths.CreateAbsolutePath(outerPath, "inner");

                FileArtifact a = ScheduleWriteOutputFileUnderDirectory(env, innerPath, "a");
                FileArtifact b = ScheduleWriteOutputFileUnderDirectory(env, innerPath, "b");
                FileArtifact c = ScheduleWriteOutputFileUnderDirectory(env, outerPath, "c");

                // Grandchild
                ScheduleSealPartialDirectory(env, outerPath, a);

                // Child and grandchild
                ScheduleSealPartialDirectory(env, outerPath, b, c);

                // Child 
                ScheduleSealPartialDirectory(env, innerPath, a, b);

                ScheduleSealPartialDirectory(env, outerPath, a, b, c);
                ScheduleSealPartialDirectory(env, innerPath, a, b);
            }
        }

        [Fact]
        public void TestWritesToPartiallySealedDirectory()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                FileArtifact existing = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "existing");

                ScheduleSealPartialDirectory(env, directoryPath, existing);

                ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "other");
            }
        }

        [Fact]
        public void TestFailedWritesToFileInPartiallySealedDirectory()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                FileArtifact a = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "a");
                FileArtifact b = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "b");

                ScheduleSealPartialDirectory(env, directoryPath, a, b);

                AssertCannotScheduleRewrite(env, a, b);
                AssertErrorEventLogged(LogEventId.InvalidOutputSinceFileHasBeenPartiallySealed);
            }
        }

        [Fact]
        public void TestFailedWritesToPartiallySealedDirectoryInsideFullySealedDirectory()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath outerPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "outer");
                AbsolutePath innerPath = env.Paths.CreateAbsolutePath(outerPath, "inner");
                FileArtifact partiallySealedFile = ScheduleWriteOutputFileUnderDirectory(env, innerPath, "existing");

                ScheduleSealPartialDirectory(env, innerPath, partiallySealedFile);
                FileArtifact nextFile = ScheduleWriteOutputFileUnderDirectory(env, innerPath, "newer");

                // Can't write partiallySealedFile
                AssertCannotScheduleRewrite(env, nextFile, partiallySealedFile);
                AssertErrorEventLogged(LogEventId.InvalidOutputSinceFileHasBeenPartiallySealed);

                // But can do the reverse (haven't sealed the sibling)
                FileArtifact rewrittenNextFile = ScheduleRewrite(env, partiallySealedFile, nextFile);

                AssertCannotScheduleSealDirectory(env, outerPath, partiallySealedFile, nextFile);
                AssertErrorEventLogged(LogEventId.InvalidInputSinceInputIsRewritten);
                ScheduleSealDirectory(env, outerPath, partiallySealedFile, rewrittenNextFile);

                // Now both are sealed, and no new files can be added.

                AssertCannotScheduleRewrite(env, partiallySealedFile, rewrittenNextFile);
                AssertErrorEventLogged(LogEventId.InvalidOutputSinceDirectoryHasBeenSealed);

                // What would be a double-write normally is now a seal-related error.
                AssertCannotScheduleWriteOutputFileUnderDirectory(env, innerPath, "newer");
                AssertErrorEventLogged(LogEventId.InvalidOutputSinceDirectoryHasBeenSealed);

                AssertCannotScheduleWriteOutputFileUnderDirectory(env, innerPath, "newest");
                AssertErrorEventLogged(LogEventId.InvalidOutputSinceDirectoryHasBeenSealed);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ScheduleFullSealDirectoryWithOpaqueAsContentBehavior(bool includeOpaqueAsContent)
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                AbsolutePath sod = env.Paths.CreateAbsolutePath(directoryPath, "sod");

                // A pip that produces an output directory under the seal root
                var pip = CreatePipBuilderWithTag(env, "test");
                pip.AddOutputDirectory(sod, SealDirectoryKind.SharedOpaque);
                var pipResult = env.PipConstructionHelper.AddProcess(pip);
                var sharedOpaqueArtifact = pipResult.GetOpaqueDirectory(sod);

                FileArtifact a = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "a");

                // We should be able to schedule a seal directory with the produced file and optionally the opaque as content
                DirectoryArtifact sealDirectory = ScheduleSealDirectory(
                    env, 
                    directoryPath, 
                    new[] { a }, 
                    includeOpaqueAsContent? new[] { sharedOpaqueArtifact } : new DirectoryArtifact[] { });

                var graph = env.PipGraph.Build();
                if (includeOpaqueAsContent)
                {
                    // Everything should be good in this case
                    Assert.NotNull(graph);

                    // The fully seal directory should have a dependency on the opaque
                    Assert.True(graph.DataflowGraph.IsReachableFrom(graph.GetSealedDirectoryNode(sharedOpaqueArtifact), graph.GetSealedDirectoryNode(sealDirectory)));
                }
                else
                {
                    // We should fail at scheduling the graph because of a missing output directory
                    Assert.Null(graph);
                    AssertErrorEventLogged(LogEventId.InvalidGraphSinceFullySealedDirectoryIncompleteDueToMissingOutputDirectories);
                }
            }
        }

        [Fact]
        public void TestFailedScheduleFullSealDirectoryWithOpaqueAsContentOutsideRoot()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                AbsolutePath sod = env.Paths.CreateAbsolutePath(env.ObjectRoot, "sod");

                // A pip that produces an output directory
                var pip = CreatePipBuilderWithTag(env, "test");
                pip.AddOutputDirectory(sod, SealDirectoryKind.SharedOpaque);
                var pipResult = env.PipConstructionHelper.AddProcess(pip);
                var sharedOpaqueArtifact = pipResult.GetOpaqueDirectory(sod);

                FileArtifact a = ScheduleWriteOutputFileUnderDirectory(env, directoryPath, "a");

                // We shouldn't be able to schedule a seal directory with an opaque outside its root
                var result = TryScheduleSealDirectory(env, directoryPath, SealDirectoryKind.Full, new[] { a }, new[] { sharedOpaqueArtifact }, out _);
                Assert.False(result);
                AssertErrorEventLogged(LogEventId.InvalidSealDirectoryDirectoryOutputContentSinceNotUnderRoot);
            }
        }

        [Fact]
        public void TestFailedScheduleFullSealDirectoryWithNonOpaqueAsContent()
        {
            using (TestEnv env = TestEnv.CreateTestEnvWithPausedScheduler())
            {
                AbsolutePath directoryPath = env.Paths.CreateAbsolutePath(env.ObjectRoot, "seal");
                AbsolutePath nestedDirectoryPath = env.Paths.CreateAbsolutePath(directoryPath, "another-seal");

                FileArtifact write = ScheduleWriteOutputFileUnderDirectory(env, nestedDirectoryPath, "file");

                DirectoryArtifact nestedSealDirectory = ScheduleSealDirectory(env, nestedDirectoryPath, write);
                
                // We shouldn't be able to schedule a seal directory with a directory as content that is not an output directory
                var result = TryScheduleSealDirectory(env, directoryPath, SealDirectoryKind.Full, new[] { write }, new[] { nestedSealDirectory }, out _);
                Assert.False(result);
                AssertErrorEventLogged(LogEventId.ScheduleFailAddPipInvalidSealDirectoryContentIsNotOutput);
            }
        }

        private static DirectoryArtifact ScheduleSealDirectory(TestEnv env, AbsolutePath path, params FileArtifact[] contents)
        {
            bool result = TryScheduleSealDirectory(env, path, SealDirectoryKind.Full, contents: contents,
                outputDirectoryContents: CollectionUtilities.EmptyArray<DirectoryArtifact>(), out var directoryArtifact);
            XAssert.IsTrue(result, "Unexpectedly failed to seal the directory at " + env.Paths.Expand(path));
            
            return directoryArtifact;
        }

        private static DirectoryArtifact ScheduleSealDirectory(TestEnv env, AbsolutePath path, FileArtifact[] contents, DirectoryArtifact[] outputDirectoryContent)
        {
            bool result = TryScheduleSealDirectory(env, path, SealDirectoryKind.Full, contents: contents,
                outputDirectoryContents: outputDirectoryContent, out var directoryArtifact);
            XAssert.IsTrue(result, "Unexpectedly failed to seal the directory at " + env.Paths.Expand(path));

            return directoryArtifact;
        }

        private static DirectoryArtifact ScheduleSealPartialDirectory(TestEnv env, AbsolutePath path, params FileArtifact[] contents)
        {
            bool result = TryScheduleSealDirectory(env, path, SealDirectoryKind.Partial, contents: contents, 
                outputDirectoryContents: CollectionUtilities.EmptyArray<DirectoryArtifact>(), out var directoryArtifact);
            XAssert.IsTrue(result, "Unexpectedly failed to seal a partial directory at " + env.Paths.Expand(path));

            return directoryArtifact;
        }

        private static DirectoryArtifact ScheduleSourceSealDirectory(TestEnv env, AbsolutePath path, bool allDirectories)
        {
            bool result = TryScheduleSealDirectory(
                env,
                path,
                allDirectories ? SealDirectoryKind.SourceAllDirectories : SealDirectoryKind.SourceTopDirectoryOnly,
                contents: new FileArtifact[0],
                outputDirectoryContents: CollectionUtilities.EmptyArray<DirectoryArtifact>(), out var directoryArtifact);
            XAssert.IsTrue(result, "Unexpectedly failed to seal a source directory at " + env.Paths.Expand(path));

            return directoryArtifact;
        }

        private static void AssertCannotScheduleSealDirectory(TestEnv env, AbsolutePath path, params FileArtifact[] contents)
        {
            bool result = TryScheduleSealDirectory(env, path, SealDirectoryKind.Full, contents: contents, 
                outputDirectoryContents: CollectionUtilities.EmptyArray<DirectoryArtifact>(), out _);
            XAssert.IsFalse(result, "Unexpectedly succedeeded at sealing the directory " + env.Paths.Expand(path));
        }

        private static void AssertCannotScheduleSealPartialDirectory(TestEnv env, AbsolutePath path, params FileArtifact[] contents)
        {
            bool result = TryScheduleSealDirectory(env, path, SealDirectoryKind.Partial, contents: contents, outputDirectoryContents: CollectionUtilities.EmptyArray<DirectoryArtifact>(), out _);
            XAssert.IsFalse(result, "Unexpectedly succedeeded at sealing a partial directory at " + env.Paths.Expand(path));
        }

        private static bool TryScheduleSealDirectory(TestEnv env, AbsolutePath path, SealDirectoryKind partial, FileArtifact[] contents, DirectoryArtifact[] outputDirectoryContents, out DirectoryArtifact artifact)
        {
            var pip = new SealDirectory(
                path,
                SortedReadOnlyArray<FileArtifact, OrdinalFileArtifactComparer>.CloneAndSort(contents, OrdinalFileArtifactComparer.Instance),
                SortedReadOnlyArray<DirectoryArtifact, OrdinalDirectoryArtifactComparer>.CloneAndSort(outputDirectoryContents, OrdinalDirectoryArtifactComparer.Instance),
                kind: partial,
                provenance: env.CreatePipProvenance(StringId.Invalid),
                tags: ReadOnlyArray<StringId>.Empty,
                patterns: ReadOnlyArray<StringId>.Empty);

            artifact = env.PipGraph.AddSealDirectory(pip, PipId.Invalid);
            bool succeeded = artifact.IsValid;

            if (succeeded)
            {
                FileArtifact[] actualContents = GetSealedDirectoryContents(env, artifact);
                XAssert.AreEqual(contents.Length, actualContents.Length, "Wrong number of contents sealed");

                for (int i = 0; i < contents.Length; i++)
                {
                    XAssert.IsTrue(contents[i] == actualContents[i], "Content artifact at position {0} mismatched", i);
                }
            }

            return succeeded;
        }

        /// <summary>
        /// Returns the final contents of a directory that was sealed.
        /// </summary>
        private static FileArtifact[] GetSealedDirectoryContents(TestEnv env, DirectoryArtifact directory)
        {
            Contract.Requires(env != null);
            Contract.Requires(directory.IsValid);

            return ((PipGraph.Builder) env.PipGraph).ListSealedDirectoryContents(directory).ToArray();
        }

        /// <summary>
        /// Schedules a pip to produce a file at the specified path under the given output directory.
        /// It is expected that scheduling the write will succeed.
        /// </summary>
        private FileArtifact ScheduleWriteOutputFileUnderDirectory(TestEnv env, AbsolutePath directory, string relativePath)
        {
            Contract.Requires(env != null);
            Contract.Requires(directory.IsValid);
            Contract.Requires(!string.IsNullOrEmpty(relativePath));

            FileArtifact target;
            bool success = TryScheduleWriteOutputFileUnderDirectory(env, directory, relativePath, out target);
            XAssert.IsTrue(success, "Failed to schedule a write to {0}", env.Paths.Expand(target.Path));
            return target;
        }

        /// <summary>
        /// Schedules a pip to produce a file at the specified path under the given output directory.
        /// It is expected that scheduling the write will not succeed.
        /// </summary>
        private static void AssertCannotScheduleWriteOutputFileUnderDirectory(TestEnv env, AbsolutePath directory, string relativePath)
        {
            Contract.Requires(env != null);
            Contract.Requires(directory.IsValid);
            Contract.Requires(!string.IsNullOrEmpty(relativePath));

            FileArtifact target;
            bool success = TryScheduleWriteOutputFileUnderDirectory(env, directory, relativePath, out target);
            XAssert.IsFalse(success, "Scheduling a write to {0} unexpectedly succeeded", env.Paths.Expand(target.Path));
        }

        /// <summary>
        /// Schedules a pip to produce a file at the specified path under the given output directory.
        /// </summary>
        private static bool TryScheduleWriteOutputFileUnderDirectory(
            TestEnv env,
            AbsolutePath directory,
            string relativePath,
            out FileArtifact target)
        {
            Contract.Requires(env != null);
            Contract.Requires(directory.IsValid);
            Contract.Requires(!string.IsNullOrEmpty(relativePath));

            target = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directory, env.Paths.CreateRelativePath(relativePath))).CreateNextWrittenVersion();
            var pip = new WriteFile(
                target,
                PipDataBuilder.CreatePipData(env.PathTable.StringTable, string.Empty, PipDataFragmentEscaping.NoEscaping, "content"),
                WriteFileEncoding.Utf8,
                ReadOnlyArray<StringId>.Empty,
                env.CreatePipProvenance(StringId.Invalid));

            return env.PipGraph.AddWriteFile(pip, PipId.Invalid);
        }

        /// <summary>
        /// Schedules a pip to rewrite a file (with contents from <paramref name="source"/>)
        /// It is expected that scheduling the write will not succeed.
        /// </summary>
        private static void AssertCannotScheduleRewrite(TestEnv env, FileArtifact source, FileArtifact target)
        {
            Contract.Requires(env != null);
            Contract.Requires(source.IsValid);
            Contract.Requires(target.IsValid);

            FileArtifact written;
            bool success = TryScheduleRewrite(env, source, target, out written);
            XAssert.IsFalse(success, "Scheduling a rewrite of {0} unexpectedly succeeded", env.Paths.Expand(target.Path));
        }

        /// <summary>
        /// Schedules a pip to rewrite a file (with contents from <paramref name="source"/>)
        /// It is expected that scheduling the write will succeed.
        /// </summary>
        private FileArtifact ScheduleRewrite(TestEnv env, FileArtifact source, FileArtifact target)
        {
            Contract.Requires(env != null);
            Contract.Requires(source.IsValid);
            Contract.Requires(target.IsValid);

            FileArtifact written;
            bool success = TryScheduleRewrite(env, source, target, out written);
            XAssert.IsTrue(success, "Failed to schedule a rewrite to {0}", env.Paths.Expand(target.Path));
            return written;
        }

        /// <summary>
        /// Schedules a pip to produce a file at the specified path under the given output directory.
        /// </summary>
        private static bool TryScheduleRewrite(
            TestEnv env,
            FileArtifact source,
            FileArtifact target,
            out FileArtifact written)
        {
            Contract.Requires(env != null);
            Contract.Requires(source.IsValid);
            Contract.Requires(target.IsValid);

            written = target.CreateNextWrittenVersion();
            var pip = new CopyFile(
                source,
                written,
                ReadOnlyArray<StringId>.Empty,
                env.CreatePipProvenance(StringId.Invalid));

            return env.PipGraph.AddCopyFile(pip, PipId.Invalid);
        }

        private static void ScheduleConsumeSourceFile(TestEnv env, AbsolutePath directory, string relativePath)
        {
            FileArtifact source = FileArtifact.CreateSourceFile(env.Paths.CreateAbsolutePath(directory, env.Paths.CreateRelativePath(relativePath)));
            AbsolutePath output = env.Paths.CreateAbsolutePath(env.ObjectRoot, Guid.NewGuid().ToString());
            env.PipConstructionHelper.TryCopyFile(source, output, CopyFile.Options.None, null, "Pretend a source file is used", out _);
        }
    }
}
