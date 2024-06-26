﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using BuildXL.Pips.Builders;
using BuildXL.Pips.Graph;
using BuildXL.Pips.Operations;
using BuildXL.ProcessPipExecutor;
using BuildXL.Utilities.Core;
using Test.BuildXL.Executables.TestProcess;
using Test.BuildXL.Scheduler;
using Test.BuildXL.TestUtilities;
using Test.BuildXL.TestUtilities.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTest.BuildXL.Scheduler
{
    [Trait("Category", "SharedOpaqueDirectoryTests")]
    [Feature(Features.SharedOpaqueDirectory)]
    public class DirectoryRenameTests : SchedulerIntegrationTestBase
    {
        private const string NestedDirSrc   = "nested-tmp";
        private const string NestedDirDest  = "nested";
        private const string FileNameInSrc  = "file-in-src";
        private const string FileNameInDest = "file-in-dest";

        public DirectoryRenameTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// Creates the following process
        /// 
        ///   CreateDir (sodPath)/(NestedDirSrc)
        ///   WriteFile (sodPath)/(NestedDirSrc)/(FileNameInSrc)
        ///   MoveDir   (sodPath)/(NestedDirSrc) (sodPath)/(NestedDirDest)
        ///   WriteFile (sodPath)/(NestedDirDest)/(FileNameInDest)
        /// </summary>
        private ProcessBuilder CreateMoveDirectoryProcessBuilder(AbsolutePath rootDir)
        {
            return CreatePipBuilder(new[]
            {
                OpCreateDir(NestedDirSrc),
                OpWriteFile(NestedDirSrc, FileNameInSrc),
                OpMoveDir(NestedDirSrc, NestedDirDest),
                OpWriteFile(NestedDirDest, FileNameInDest)
            });

            Operation OpCreateDir(string nestedDirName)
            {
                return Operation.CreateDir(OutDir(nestedDirName), doNotInfer: true);
            }

            Operation OpWriteFile(string nestedDirName, string fileName)
            {
                var file = FileArtifact.CreateOutputFile(OutDir(nestedDirName).Path.Combine(Context.PathTable, fileName));
                return Operation.WriteFile(file, doNotInfer: true);
            }

            Operation OpMoveDir(string srcDir, string destDir)
            {
                return Operation.MoveDir(srcPath: OutDir(srcDir), destPath: OutDir(destDir));
            }

            DirectoryArtifact OutDir(string nestedDirName)
            {
                return OutputDirectory.Create(rootDir.Combine(Context.PathTable, nestedDirName));
            }
        }

        [Fact]
        public void TestRenameDirectoryInsideSharedOpaqueDirectory()
        {
            AbsolutePath rootDirPath = CreateUniqueObjPath("sod-nested-mov");

            var pipBuilder = CreateMoveDirectoryProcessBuilder(rootDirPath);
            pipBuilder.AddOutputDirectory(rootDirPath, SealDirectoryKind.SharedOpaque);

            SchedulePipBuilder(pipBuilder);

            RunScheduler().AssertSuccess();
            AssertSharedOpaqueOputputsInNestedDestDirectory(rootDirPath);
        }

        [Theory]
        [InlineData(SealDirectoryKind.SharedOpaque)]
        [InlineData(SealDirectoryKind.Opaque)]
        public void TestRenameUntrackedDirectoryToOpaqueDirectory(SealDirectoryKind dirKind)
        {
            AbsolutePath rootDirPath = CreateUniqueObjPath("untracked-mov-sod");

            var pipBuilder = CreateMoveDirectoryProcessBuilder(rootDirPath);
            pipBuilder.AddUntrackedDirectoryScope(Combine(rootDirPath, NestedDirSrc));
            pipBuilder.AddOutputDirectory(Combine(rootDirPath, NestedDirDest), dirKind);

            SchedulePipBuilder(pipBuilder);

            RunScheduler().AssertSuccess();

            if (dirKind == SealDirectoryKind.SharedOpaque)
            {
                AssertSharedOpaqueOputputsInNestedDestDirectory(rootDirPath);
            }
        }

        private void AssertSharedOpaqueOputputsInNestedDestDirectory(AbsolutePath rootDirPath)
        {
            string root = ToString(rootDirPath);
            var expectedOutputs = new[]
            {
                X($"{root}/{NestedDirDest}/{FileNameInSrc}"),
                X($"{root}/{NestedDirDest}/{FileNameInDest}"),
            };

            foreach (var path in expectedOutputs)
            {
                XAssert.IsTrue(
                    SharedOpaqueOutputHelper.IsSharedOpaqueOutput(path),
                    $"Path '{path}' does not have magic shared opaque output timestamp");
            }
        }

        private string ToString(AbsolutePath path) => path.ToString(Context.PathTable);
    }
}
