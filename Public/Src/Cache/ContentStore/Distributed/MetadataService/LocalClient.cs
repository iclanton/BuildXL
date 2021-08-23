// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Interfaces.Stores;
using BuildXL.Cache.ContentStore.Tracing;
using BuildXL.Cache.ContentStore.Tracing.Internal;
using BuildXL.Cache.ContentStore.Utils;

namespace BuildXL.Cache.ContentStore.Distributed.MetadataService
{
    /// <summary>
    /// Represents a connection to a local instance of <see cref="TClient"/>
    /// </summary>
    public class LocalClient<TClient> : StartupShutdownComponentBase
    {
        public TClient Client { get; }

        public MachineLocation Location { get; }

        protected override Tracer Tracer { get; } = new Tracer(nameof(LocalClient<TClient>));

        public override bool AllowMultipleStartupAndShutdowns => true;

        public LocalClient(MachineLocation location, TClient client)
        {
            Location = location;
            Client = client;
            if (client is IStartupShutdownSlim component)
            {
                LinkLifetime(component);
            }
        }

    }
}