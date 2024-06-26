// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.ContractsLight;
using BuildXL.Utilities.Core;

namespace BuildXL.Utilities.Configuration.Mutable
{
    /// <nodoc />
    public class TrackedValue : ITrackedValue
    {
        /// <nodoc />
        public TrackedValue()
        {
        }

        /// <nodoc />
        public TrackedValue(ITrackedValue template, PathRemapper pathRemapper)
        {
            Contract.Assume(template != null);
            Contract.Assume(pathRemapper != null);

            Location = template.Location.IsValid ?
                new LocationData(pathRemapper.Remap(template.Location.Path), template.Location.Line, template.Location.Position) :
                LocationData.Invalid;
        }

        /// <inheritdoc />
        public LocationData Location { get; set; }
    }
}
