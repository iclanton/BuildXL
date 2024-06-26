// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as Managed from "Sdk.Managed";
namespace TypeScript.Net {
    @@public
    export const dll = BuildXLSdk.library({
        assemblyName: "TypeScript.Net",
        sources: globR(d`.`, "*.cs"),
        addNotNullAttributeFile: true,
        references: [
             ...addIf(BuildXLSdk.isFullFramework,
                NetFx.System.Numerics.dll,
                NetFx.Microsoft.CSharp.dll
            ),
            importFrom("BuildXL.Utilities").dll,
            importFrom("BuildXL.Utilities").Script.Constants.dll,
            importFrom("BuildXL.Utilities").Utilities.Core.dll,
            ...addIf(BuildXLSdk.isFullFramework,
                importFrom("System.Collections.Immutable").pkg
            ),
        ],
        internalsVisibleTo: [
            "Test.BuildXL.FrontEnd.Script",
        ],
    });
}
