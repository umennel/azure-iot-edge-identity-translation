namespace IdentityTranslation
{
    using System.Collections.Generic;
    using Microsoft.Azure.Devices.Client;

    public class MemoryDeviceRepository : IDeviceRepository
    {
        protected Dictionary<string, DeviceInfo> _deviceInfos;

        public MemoryDeviceRepository()
        {
            this._deviceInfos = new Dictionary<string, DeviceInfo>();
        }

        public bool Contains(string id)
        {
            return this._deviceInfos.ContainsKey(id); 
        }

        public DeviceInfo? Get(string id)
        {
            
            if (this._deviceInfos.TryGetValue(id, out var result))
            {
                return result; 
            }
            
            return null;
        }        

        public DeviceInfo Add(string id, string connectionString)
        {
            DeviceInfo info = new DeviceInfo(id, DeviceClient.CreateFromConnectionString(connectionString));
            
            this._deviceInfos.Add(id, info);
            
            return info;
        }
    }
}