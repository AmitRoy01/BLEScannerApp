using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BLESampleApp
{
    public class BLEServer
    {
        private GattServiceProvider serviceProvider;
        private GattLocalCharacteristic readTextCharacteristic;
        private GattLocalCharacteristic writeTextCharacteristic;
        private GattLocalCharacteristic notifyTextCharacteristic;

        // Battery Level Service UUID (standard UUID)
        public static readonly Guid ServiceUuid = Guid.Parse("0000180F-0000-1000-8000-00805F9B34FB");

        // Custom UUIDs for text-based communication
        public static readonly Guid ReadTextCharacteristicUuid = Guid.Parse("0000D1F4-0000-1000-8000-00805F9B34FB");
        public static readonly Guid WriteTextCharacteristicUuid = Guid.Parse("0000B2D2-0000-1000-8000-00805F9B34FB");
        public static readonly Guid NotifyTextCharacteristicUuid = Guid.Parse("0000C3E3-0000-1000-8000-00805F9B34FB");

        private List<GattSubscribedClient> subscribedClients = new List<GattSubscribedClient>();

        public event EventHandler<int> ClientCountChanged;
        public event EventHandler<string> MessageReceived;

        public int ConnectedClientsCount => subscribedClients.Count;

        public async Task<bool> StartServerAsync()
        {
            var result = await GattServiceProvider.CreateAsync(ServiceUuid);
            if (result.Error != BluetoothError.Success)
                return false;

            serviceProvider = result.ServiceProvider;
            await AddCharacteristicsAsync();

            var advParameters = new GattServiceProviderAdvertisingParameters
            {
                IsDiscoverable = true,
                IsConnectable = true
            };

            System.Diagnostics.Debug.WriteLine($"Starting advertising with Service UUID: {ServiceUuid}");

            serviceProvider.StartAdvertising(advParameters);
            return true;
        }

        public void StopServer()
        {
            if (serviceProvider != null)
            {
                serviceProvider.StopAdvertising();
                serviceProvider = null;
            }
        }

        private async Task AddCharacteristicsAsync()
        {
            // Read Text Characteristic (Read)
            var readTextParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read,
                ReadProtectionLevel = GattProtectionLevel.Plain
            };
            var readTextResult = await serviceProvider.Service.CreateCharacteristicAsync(ReadTextCharacteristicUuid, readTextParameters);
            if (readTextResult.Error == BluetoothError.Success)
            {
                readTextCharacteristic = readTextResult.Characteristic;
                readTextCharacteristic.ReadRequested += ReadTextCharacteristic_ReadRequested;
            }

            // Write Text Characteristic
            var writeTextParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Write | GattCharacteristicProperties.WriteWithoutResponse,
                WriteProtectionLevel = GattProtectionLevel.Plain
            };
            var writeTextResult = await serviceProvider.Service.CreateCharacteristicAsync(WriteTextCharacteristicUuid, writeTextParameters);
            if (writeTextResult.Error == BluetoothError.Success)
            {
                writeTextCharacteristic = writeTextResult.Characteristic;
                writeTextCharacteristic.WriteRequested += WriteTextCharacteristic_WriteRequested;
            }

            // Notify Text Characteristic
            var notifyTextParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain
            };
            var notifyTextResult = await serviceProvider.Service.CreateCharacteristicAsync(NotifyTextCharacteristicUuid, notifyTextParameters);
            if (notifyTextResult.Error == BluetoothError.Success)
            {
                notifyTextCharacteristic = notifyTextResult.Characteristic;
                notifyTextCharacteristic.SubscribedClientsChanged += NotifyTextCharacteristic_SubscribedClientsChanged;
            }
        }

        private async void ReadTextCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();
                var writer = new DataWriter();

                // Simulating reading a message
                string message = "Battery level: 75%"; // Example text message
                writer.WriteString(message);

                request.RespondWithValue(writer.DetachBuffer());
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void WriteTextCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();
                var reader = DataReader.FromBuffer(request.Value);
                string receivedData = reader.ReadString(reader.UnconsumedBufferLength);

                // Handle the received text message (e.g., store it or notify another device)
                MessageReceived?.Invoke(this, receivedData);

                if (request.Option == GattWriteOption.WriteWithResponse)
                {
                    request.Respond();
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void NotifyTextCharacteristic_SubscribedClientsChanged(GattLocalCharacteristic sender, object args)
        {
            var subscribedClientsCollection = sender.SubscribedClients;
            if (subscribedClientsCollection != null)
            {
                subscribedClients = new List<GattSubscribedClient>(subscribedClientsCollection);
            }
            else
            {
                subscribedClients = new List<GattSubscribedClient>();
            }

            ClientCountChanged?.Invoke(this, ConnectedClientsCount);
        }

        public async Task<bool> SendTextNotificationAsync(string message)
        {
            if (notifyTextCharacteristic == null || subscribedClients.Count == 0)
                return false;

            var writer = new DataWriter();
            writer.WriteString(message);

            var results = await notifyTextCharacteristic.NotifyValueAsync(writer.DetachBuffer());
            return results.All(result => result.Status == GattCommunicationStatus.Success);
        }
    }
}

    //public class BLEServer
    //{
    //    private GattServiceProvider serviceProvider;
    //    private GattLocalCharacteristic readCharacteristic;
    //    private GattLocalCharacteristic writeCharacteristic;
    //    private GattLocalCharacteristic notifyCharacteristic;

    //    // Using Heart Rate Service UUID for demonstration
    //    public static readonly Guid ServiceUuid = Guid.Parse("0000180D-0000-1000-8000-00805F9B34FB");
    //    public static readonly Guid ReadCharacteristicUuid = Guid.Parse("00002A37-0000-1000-8000-00805F9B34FB");
    //    public static readonly Guid WriteCharacteristicUuid = Guid.Parse("00002A38-0000-1000-8000-00805F9B34FB");
    //    public static readonly Guid NotifyCharacteristicUuid = Guid.Parse("00002A39-0000-1000-8000-00805F9B34FB");

    //    private List<GattSubscribedClient> subscribedClients = new List<GattSubscribedClient>();

    //    public event EventHandler<int> ClientCountChanged;
    //    public event EventHandler<string> MessageReceived;

    //    public int ConnectedClientsCount => subscribedClients.Count;


    //    public async Task<bool> StartServerAsync()
    //    {
    //        var result = await GattServiceProvider.CreateAsync(ServiceUuid);
    //        if (result.Error != BluetoothError.Success)
    //            return false;

    //        serviceProvider = result.ServiceProvider;
    //        await AddCharacteristicsAsync();

    //        var advParameters = new GattServiceProviderAdvertisingParameters
    //        {
    //            IsDiscoverable = true,
    //            IsConnectable = true
    //        };

    //        System.Diagnostics.Debug.WriteLine($"Starting advertising with Service UUID: {ServiceUuid}");

    //        serviceProvider.StartAdvertising(advParameters);
    //        return true;
    //    }

    //    public void StopServer()
    //    {
    //        if (serviceProvider != null)
    //        {
    //            serviceProvider.StopAdvertising();
    //            serviceProvider = null;
    //        }
    //    }

    //    private async Task AddCharacteristicsAsync()
    //    {
    //        // Read Characteristic
    //        var readParameters = new GattLocalCharacteristicParameters
    //        {
    //            CharacteristicProperties = GattCharacteristicProperties.Read,
    //            ReadProtectionLevel = GattProtectionLevel.Plain
    //        };
    //        var readResult = await serviceProvider.Service.CreateCharacteristicAsync(ReadCharacteristicUuid, readParameters);
    //        if (readResult.Error == BluetoothError.Success)
    //        {
    //            readCharacteristic = readResult.Characteristic;
    //            readCharacteristic.ReadRequested += ReadCharacteristic_ReadRequested;
    //        }

    //        // Write Characteristic
    //        var writeParameters = new GattLocalCharacteristicParameters
    //        {
    //            CharacteristicProperties = GattCharacteristicProperties.Write | GattCharacteristicProperties.WriteWithoutResponse,
    //            WriteProtectionLevel = GattProtectionLevel.Plain
    //        };
    //        var writeResult = await serviceProvider.Service.CreateCharacteristicAsync(WriteCharacteristicUuid, writeParameters);
    //        if (writeResult.Error == BluetoothError.Success)
    //        {
    //            writeCharacteristic = writeResult.Characteristic;
    //            writeCharacteristic.WriteRequested += WriteCharacteristic_WriteRequested;
    //        }

    //        // Notify Characteristic
    //        var notifyParameters = new GattLocalCharacteristicParameters
    //        {
    //            CharacteristicProperties = GattCharacteristicProperties.Notify,
    //            ReadProtectionLevel = GattProtectionLevel.Plain
    //        };
    //        var notifyResult = await serviceProvider.Service.CreateCharacteristicAsync(NotifyCharacteristicUuid, notifyParameters);
    //        if (notifyResult.Error == BluetoothError.Success)
    //        {
    //            notifyCharacteristic = notifyResult.Characteristic;
    //            notifyCharacteristic.SubscribedClientsChanged += NotifyCharacteristic_SubscribedClientsChanged;
    //        }
    //    }

    //    private async void ReadCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
    //    {
    //        var deferral = args.GetDeferral();
    //        try
    //        {
    //            var request = await args.GetRequestAsync();

    //            var writer = new DataWriter();
    //            writer.WriteString("Hello from BLE Server!");

    //            request.RespondWithValue(writer.DetachBuffer());
    //        }
    //        finally
    //        {
    //            deferral.Complete();
    //        }
    //    }

    //    private async void WriteCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
    //    {
    //        var deferral = args.GetDeferral();
    //        try
    //        {
    //            var request = await args.GetRequestAsync();

    //            var reader = DataReader.FromBuffer(request.Value);
    //            string receivedData = reader.ReadString(reader.UnconsumedBufferLength);

    //            MessageReceived?.Invoke(this, receivedData);

    //            if (request.Option == GattWriteOption.WriteWithResponse)
    //            {
    //                request.Respond();
    //            }
    //        }
    //        finally
    //        {
    //            deferral.Complete();
    //        }
    //    }

    //    private void NotifyCharacteristic_SubscribedClientsChanged(GattLocalCharacteristic sender, object args)
    //    {
    //        var subscribedClientsCollection = sender.SubscribedClients;
    //        if (subscribedClientsCollection != null)
    //        {
    //            subscribedClients = new List<GattSubscribedClient>(subscribedClientsCollection);
    //        }
    //        else
    //        {
    //            subscribedClients = new List<GattSubscribedClient>();
    //        }

    //        ClientCountChanged?.Invoke(this, ConnectedClientsCount);
    //    }

    //    public async Task<bool> SendNotificationAsync(string message)
    //    {
    //        if (notifyCharacteristic == null || subscribedClients.Count == 0)
    //            return false;

    //        var writer = new DataWriter();
    //        writer.WriteString(message);

    //        var results = await notifyCharacteristic.NotifyValueAsync(writer.DetachBuffer());
    //        return results.All(result => result.Status == GattCommunicationStatus.Success);
    //    }
    //}
