// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.ContractsLight;
using BuildXL.FrontEnd.Sdk;
using BuildXL.Utilities.Core;

namespace BuildXL.FrontEnd.Core.Incrementality
{
    /// <summary>
    /// Spec dependency provider that is based on the deserialized front end information.
    /// </summary>
    public sealed class SnapshotBasedSpecProvider : ISpecDependencyProvider
    {
        private readonly IWorkspaceBindingSnapshot m_snapshot;
        private static readonly HashSet<AbsolutePath> s_emptyPathSet = new HashSet<AbsolutePath>();
        private static readonly HashSet<string> s_emptyModuleSet = new HashSet<string>();

        /// <nodoc />
        public SnapshotBasedSpecProvider(IWorkspaceBindingSnapshot snapshot)
        {
            Contract.Requires(snapshot != null, "snapshot != null");

            m_snapshot = snapshot;

            // Need to materialize all the file names.
            snapshot.MaterializeDependencies();
        }

        /// <inheritdoc />
        public HashSet<AbsolutePath> GetFileDependenciesOf(AbsolutePath specPath)
        {
            var specState = m_snapshot.TryGetSpecState(specPath);

            if (specState == null)
            {
                return s_emptyPathSet;
            }

            return specState.FileDependencies.MaterializedSetOfPaths;
        }

        /// <inheritdoc />
        public HashSet<AbsolutePath> GetFileDependentsOf(AbsolutePath specPath)
        {
            var specState = m_snapshot.TryGetSpecState(specPath);

            if (specState == null)
            {
                return s_emptyPathSet;
            }

            return specState.FileDependents.MaterializedSetOfPaths;
        }

        /// <inheritdoc />
        public HashSet<string> GetModuleDependenciesOf(AbsolutePath specPath)
        {
            // File2File map doesn't keep the information about spec2module dependencies and this information
            // is not needed for incremental mode.
            return s_emptyModuleSet;
        }
    }
}
