// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using MsBuildGraphBuilderTool;
using Test.BuildXL.TestUtilities.Xunit;
using Test.ProjectGraphBuilder.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Test.ProjectGraphBuilder
{
    public class MsBuildAssemblyLoaderTests : GraphBuilderToolTestBase
    {
        public MsBuildAssemblyLoaderTests(ITestOutputHelper output): base(output)
        {
        }

        [Fact]
        public void CorrectAssembliesAreSuccessfullyLoaded()
        {
            using (var reporter = new GraphBuilderReporter(Guid.NewGuid().ToString()))
            {
                var succeed = AssemblyLoader.TryLoadMsBuildAssemblies(
                    // The test deployment dir should have all assemblies needed by the loader
                    new [] {TestDeploymentDir},
                    reporter,
                    out _,
                    out var locatedAssemblyPaths,
                    out var locatedMsBuildExePath);

                // We expect success
                XAssert.IsTrue(succeed);

                // All located assemblies (and MSBuild.exe) should be the ones in the deployment directory
                XAssert.All(locatedAssemblyPaths.Values, locatedAssemblyPath => locatedAssemblyPath.StartsWith(TestDeploymentDir));
                XAssert.IsTrue(locatedMsBuildExePath.StartsWith(TestDeploymentDir));
            }
        }

        [Fact]
        public void NotFoundAssembliesGetReported()
        {
            using (var reporter = new GraphBuilderReporter(Guid.NewGuid().ToString()))
            {
                var succeed = AssemblyLoader.TryLoadMsBuildAssemblies(
                    // An empty location should result in not finding any of the required assemblies
                    new string[] {},
                    reporter,
                    out string failureReason,
                    out _,
                    out _);

                // We expect a failure
                XAssert.IsFalse(succeed);
                // And a non-empty failure reason
                XAssert.IsFalse(string.IsNullOrEmpty(failureReason));
            }
        }
    }
}