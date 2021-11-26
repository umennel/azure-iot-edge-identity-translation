// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace IdentityTranslation
{
    public interface IDeviceRepository
    {
        bool Contains(string id);
        DeviceInfo? Get(string id);
        DeviceInfo Add(string id, string connectionString);    
    }
}