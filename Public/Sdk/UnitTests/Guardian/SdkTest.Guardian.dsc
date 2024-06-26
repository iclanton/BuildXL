// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as BuildXLSdk from "Sdk.BuildXL";

namespace StandardSdk.Guardian {
    export const guardianTest = Context.getCurrentHost().os === "win" && BuildXLSdk.sdkTest({
        testFiles: globR(d`.`, "Test.*.dsc"),
        sdkFolders: [
            d`${Context.getMount("SdkRoot").path}/Tools/Guardian`,
            d`${Context.getMount("SdkRoot").path}/Json`,
            d`${Context.getMount("SourceRoot").path}/Public/Sdk/Public/Deployment`,
        ],
        // autoFixLkgs: true // Uncomment this line to have all lkgs automatically updated.
    });
}
