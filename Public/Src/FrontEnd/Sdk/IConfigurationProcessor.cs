// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using BuildXL.FrontEnd.Sdk.Workspaces;
using BuildXL.Utilities.Core;
using BuildXL.Utilities.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BuildXL.FrontEnd.Sdk
{
    /// <summary>
    /// Frontends that implement this interface can parse configuration files.
    /// </summary>
    public interface IConfigurationProcessor
    {
        /// <summary>
        /// Finds the config file given the startup configuration.
        /// </summary>
        /// <remarks>
        /// can return an invalid path. In that case one wasn't found a default one should be used.
        /// </remarks>
        AbsolutePath FindPrimaryConfiguration([NotNull]IStartupConfiguration startupConfiguration);

        /// <summary>
        /// Interprets the configuration file.
        /// </summary>
        /// <remarks>
        /// Applies all defaults and the command line overrides.
        /// If the primaryConfiguration is invalid, a default instance should be used.
        /// </remarks>
        [return: MaybeNull]
        IConfiguration InterpretConfiguration(
            AbsolutePath primaryConfiguration,
            [NotNull] ICommandLineConfiguration startupConfiguration);

        /// <summary>
        /// Initializes the frontend for configuraiont parsing
        /// </summary>
        void Initialize([NotNull]FrontEndHost host, [NotNull]FrontEndContext context);

        /// <summary>
        /// Gets a workspace computed during main configuration parsing.
        /// </summary>
        IWorkspace PrimaryConfigurationWorkspace { get; }

        /// <summary>
        /// Gets the statistics for configuration processing stage.
        /// </summary>
        IConfigurationStatistics GetConfigurationStatistics();
    }

    /// <nodoc />
    public interface IConfigurationStatistics
    {
    }
}
