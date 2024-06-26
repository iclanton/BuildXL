// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using BuildXL.Tracing;
using BuildXL.Utilities.Core;
using BuildXL.Utilities.Collections;

namespace BuildXL.Pips.Filter
{
    /// <summary>
    /// Filters pips by pip id.
    /// </summary>
    public class PipIdFilter : PipFilter
    {
        private readonly long m_semiStableHash;

        /// <summary>
        /// Creates a new instance of <see cref="PipIdFilter"/>.
        /// </summary>
        public PipIdFilter(long value)
        {
            m_semiStableHash = value;
        }

        /// <inheritdoc/>
        public override void AddStatistics(ref FilterStatistics statistics)
        {
            statistics.PipIdFilterCount++;
        }

        /// <inheritdoc/>
        protected override int GetDerivedSpecificHashCode()
        {
            return m_semiStableHash.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool CanonicallyEquals(PipFilter pipFilter)
        {
            PipIdFilter pipIdFilter;
            return (pipIdFilter = pipFilter as PipIdFilter) != null && m_semiStableHash == pipIdFilter.m_semiStableHash;
        }

        /// <inheritdoc/>
        public override IReadOnlySet<FileOrDirectoryArtifact> FilterOutputsCore(
            IPipFilterContext context,
            bool negate = false,
            IList<PipId> constrainingPips = null)
        {
            return ParallelProcessAllOutputs<FileOrDirectoryArtifact>(
                context,
                (pipId, localOutputs) =>
                {
                    if ((context.GetSemiStableHash(pipId) == m_semiStableHash) ^ negate)
                    {
                        AddOutputs(context, pipId, localOutputs);
                    }
                },
                constrainingPips);
        }
    }
}
