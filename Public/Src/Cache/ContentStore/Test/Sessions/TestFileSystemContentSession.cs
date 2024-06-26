// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Sessions;
using BuildXL.Cache.ContentStore.Stores;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.Stores;

namespace ContentStoreTest.Sessions
{
    public class TestFileSystemContentSession : FileSystemContentSession
    {
        public TestFileSystemContentSession(string name, ImplicitPin implicitPin, FileSystemContentStoreInternal store)
            : base(name, store, implicitPin)
        {
        }

        public async Task<IReadOnlyList<ContentHash>> EnumerateHashes()
        {
            IReadOnlyList<ContentInfo> contentInfoList = await Store.EnumerateContentInfoAsync();
            return contentInfoList.Select(x => x.ContentHash).ToList();
        }
    }
}
