﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using BuildXL.Utilities.Core;

namespace BuildXL.PipGraphFragmentGenerator
{
    /// <summary>
    /// Configuration specific for pip graph fragment generator.
    /// </summary>
    public class PipGraphFragmentGeneratorConfiguration
    {
        /// <summary>
        /// Output file.
        /// </summary>
        public AbsolutePath OutputFile { get; set; } = AbsolutePath.Invalid;

        /// <summary>
        /// Pip graph fragment description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Enables topological sort on pip graph fragment serialization.
        /// </summary>
        public bool TopSort { get; set; } = true;

        /// <summary>
        /// Symbol separator for efficiently storing symbols.
        /// </summary>
        public char AlternateSymbolSeparator { get; set; } = '_';
    }
}
