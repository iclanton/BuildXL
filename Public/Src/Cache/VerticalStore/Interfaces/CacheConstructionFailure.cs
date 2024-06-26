// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.ContractsLight;
using System.Globalization;

namespace BuildXL.Cache.Interfaces
{
    /// <summary>
    /// Failure when trying to start a session that already exists
    /// </summary>
    public class CacheConstructionFailure : CacheBaseFailure
    {
        private readonly string m_cacheId;
        private readonly string m_rootCause;

        /// <summary>
        /// Failure to create a cache
        /// </summary>
        /// <param name="cacheId">The cache ID</param>
        /// <param name="rootCause">The root cause (optional)</param>
        public CacheConstructionFailure(string cacheId, string rootCause = null)
        {
            Contract.Requires(cacheId != null);

            m_cacheId = cacheId;
            m_rootCause = rootCause;
        }

        /// <summary>
        /// Failure to create a cache
        /// </summary>
        /// <param name="cacheId">The cache ID</param>
        /// <param name="rootCause">The root cause exception (optional)</param>
        public CacheConstructionFailure(string cacheId, Exception rootCause = null) : this(cacheId, rootCause.Message)
        {
        }

        /// <inheritdoc />
        public override string Describe()
        {
            if (m_rootCause != null)
            {
                return string.Format(CultureInfo.InvariantCulture, "Failed to create cache [{0}]\nRoot Cause: {1}", m_cacheId, m_rootCause);
            }

            return string.Format(CultureInfo.InvariantCulture, "Failed to create cache [{0}]", m_cacheId);
        }
    }
}
