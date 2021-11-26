namespace IdentityTranslation
{
    using Microsoft.Azure.Devices.Client; 
    
    public enum DeviceInfoStatus {
        New,
        Initialize,
        WaitingConfirmation,
        Confirmed,
        Registered,
        NotRegistered
    }

    public class DeviceInfo
    {
        public string DeviceId { get; set; }
        public DeviceInfoStatus Status { get; set; }
        public string? SourceModuleId { get; set; }
        public DeviceClient DeviceClient { get; set; }

        public DeviceInfo(string id, DeviceClient deviceClient)
        {
            this.DeviceId = id;
            this.Status = DeviceInfoStatus.New;
            this.DeviceClient = deviceClient;
        }
    }
}