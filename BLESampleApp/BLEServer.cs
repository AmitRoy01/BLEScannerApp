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
        private GattLocalCharacteristic readCharacteristic;
        private GattLocalCharacteristic writeCharacteristic;
        private GattLocalCharacteristic notifyCharacteristic;

        // Using Heart Rate Service UUID for demonstration
        public static readonly Guid ServiceUuid = Guid.Parse("0000180D-0000-1000-8000-00805F9B34FB");
        public static readonly Guid ReadCharacteristicUuid = Guid.Parse("00002A37-0000-1000-8000-00805F9B34FB");
        public static readonly Guid WriteCharacteristicUuid = Guid.Parse("00002A38-0000-1000-8000-00805F9B34FB");
        public static readonly Guid NotifyCharacteristicUuid = Guid.Parse("00002A39-0000-1000-8000-00805F9B34FB");

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
            // Read Characteristic
            var readParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Read,
                ReadProtectionLevel = GattProtectionLevel.Plain
            };
            var readResult = await serviceProvider.Service.CreateCharacteristicAsync(ReadCharacteristicUuid, readParameters);
            if (readResult.Error == BluetoothError.Success)
            {
                readCharacteristic = readResult.Characteristic;
                readCharacteristic.ReadRequested += ReadCharacteristic_ReadRequested;
            }

            // Write Characteristic
            var writeParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Write | GattCharacteristicProperties.WriteWithoutResponse,
                WriteProtectionLevel = GattProtectionLevel.Plain
            };
            var writeResult = await serviceProvider.Service.CreateCharacteristicAsync(WriteCharacteristicUuid, writeParameters);
            if (writeResult.Error == BluetoothError.Success)
            {
                writeCharacteristic = writeResult.Characteristic;
                writeCharacteristic.WriteRequested += WriteCharacteristic_WriteRequested;
            }

            // Notify Characteristic
            var notifyParameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = GattCharacteristicProperties.Notify,
                ReadProtectionLevel = GattProtectionLevel.Plain
            };
            var notifyResult = await serviceProvider.Service.CreateCharacteristicAsync(NotifyCharacteristicUuid, notifyParameters);
            if (notifyResult.Error == BluetoothError.Success)
            {
                notifyCharacteristic = notifyResult.Characteristic;
                notifyCharacteristic.SubscribedClientsChanged += NotifyCharacteristic_SubscribedClientsChanged;
            }
        }

        private async void ReadCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();

                var writer = new DataWriter();
                writer.WriteString("Hello from BLE Server!");

                request.RespondWithValue(writer.DetachBuffer());
            }
            finally
            {
                deferral.Complete();
            }
        }

        private async void WriteCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var request = await args.GetRequestAsync();

                var reader = DataReader.FromBuffer(request.Value);
                string receivedData = reader.ReadString(reader.UnconsumedBufferLength);

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

        private void NotifyCharacteristic_SubscribedClientsChanged(GattLocalCharacteristic sender, object args)
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

        public async Task<bool> SendNotificationAsync(string message)
        {
            if (notifyCharacteristic == null || subscribedClients.Count == 0)
                return false;

            var writer = new DataWriter();
            writer.WriteString(message);

            var results = await notifyCharacteristic.NotifyValueAsync(writer.DetachBuffer());
            return results.All(result => result.Status == GattCommunicationStatus.Success);
        }
    }
}