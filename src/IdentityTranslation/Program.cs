namespace IdentityTranslation
{
    using System;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;

    class Program
    {
        static IDeviceRepository? leafDevices;

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>?)s)?.SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages from OPC Publisher
        /// </summary>
        static async Task Init()
        {
            ModuleClient moduleClient = await ModuleClient.CreateFromEnvironmentAsync();
            await moduleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            leafDevices = new MemoryDeviceRepository();

            var config = new ConfigurationBuilder()
               .AddEnvironmentVariables()
               .Build();
               
            foreach (var deviceSection in config.GetSection("Devices").GetChildren())
            {
                var options = new DeviceOptions();
                deviceSection.Bind(options);
                leafDevices.Add(options.DeviceId, options.ConnectionString);
                Console.WriteLine($"Adding device with id: {options.DeviceId}");
            }

            await moduleClient.SetInputMessageHandlerAsync("opc", ForwardMessage, moduleClient);
        }

        public static void DeviceConnectionChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine($"Device connection status changed to '{status}', because of {reason}.");
        }

        /// <summary>
        /// Forward the messages without any change.
        /// Use the corresponding device client for message forwarding.
        /// </summary>
        private static async Task<MessageResponse> ForwardMessage(Message message, object userContext)
        {
            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain module client");
            }

            Console.WriteLine("Message received");

            byte[] messageBytes = message.GetBytes();
            var messageString = System.Text.Encoding.UTF8.GetString(messageBytes);
            if (messageString is null)
            {
                return MessageResponse.Completed;
            }

            var data = JArray.Parse(messageString);
            foreach (var item in data)
            {
                var publisherId = (string?)item.SelectToken("PublisherId");

                var deviceId = publisherId?.Split('_').FirstOrDefault();

                if (deviceId is null)
                {
                    Console.WriteLine("Message not assigned to any device, discarding message");
                    continue;
                }

                var device = leafDevices?.Get(deviceId);
                if (device is null)
                {
                    Console.WriteLine("Device {0} not found, discarding message", deviceId);
                    continue;
                }

                try
                {
                    await device.DeviceClient.SendEventAsync(new Message(messageBytes));

                    Console.WriteLine("Message forwarded to device {0}", deviceId);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not forward message: {0}", e.Message);
                }
            }

            return MessageResponse.Completed;
        }
    }
}