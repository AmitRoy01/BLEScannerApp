using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BLEServer1
{
    public class BLEServer
    {
        private GattServiceProvider serviceProvider;
        private GattLocalCharacteristic readCharacteristic;
        private GattLocalCharacteristic writeCharacteristic;
        private GattLocalCharacteristic notifyCharacteristic;

        private static readonly Guid ServiceUuid = Guid.Parse("0000180D-0000-1000-8000-00805F9B34FB");
        private static readonly Guid ReadCharacteristicUuid = Guid.Parse("00002A37-0000-1000-8000-00805F9B34FB");
        private static readonly Guid WriteCharacteristicUuid = Guid.Parse("00002A38-0000-1000-8000-00805F9B34FB");
        private static readonly Guid NotifyCharacteristicUuid = Guid.Parse("00002A39-0000-1000-8000-00805F9B34FB");

        private List<GattSubscribedClient> subscribedClients = new List<GattSubscribedClient>();

        public event EventHandler ClientConnected;
        public event EventHandler ClientDisconnected;
        public event EventHandler<string> DataReceived;

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
                CharacteristicProperties = GattCharacteristicProperties.Write,
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
            var request = await args.GetRequestAsync();

            var writer = new DataWriter();
            writer.WriteString("Hello from BLE Server!");

            request.RespondWithValue(writer.DetachBuffer());
            deferral.Complete();
        }

        private async void WriteCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();
            var request = await args.GetRequestAsync();

            var reader = DataReader.FromBuffer(request.Value);
            string receivedData = reader.ReadString(reader.UnconsumedBufferLength);

            DataReceived?.Invoke(this, receivedData);

            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }

            deferral.Complete();
        }

        private void NotifyCharacteristic_SubscribedClientsChanged(GattLocalCharacteristic sender, object args)
        {
            var subscribedClientsCollection = sender.SubscribedClients as IReadOnlyList<GattSubscribedClient>;
            if (subscribedClientsCollection != null)
            {
                subscribedClients = new List<GattSubscribedClient>(subscribedClientsCollection);
            }
            else
            {
                subscribedClients = new List<GattSubscribedClient>();
            }
            ClientConnected?.Invoke(this, EventArgs.Empty);
        }

        public async void SendNotification(string message)
        {
            if (subscribedClients.Count > 0)
            {
                var writer = new DataWriter();
                writer.WriteString(message);

                await notifyCharacteristic.NotifyValueAsync(writer.DetachBuffer());
            }
        }
    }
}




//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Windows.Devices.Bluetooth;
//using Windows.Devices.Bluetooth.Advertisement;
//using Windows.Devices.Bluetooth.GenericAttributeProfile;
//using Windows.Storage.Streams;

//namespace BLEServer1
//{
//    public class BLEServer
//    {
//        private GattServiceProvider serviceProvider;
//        private GattLocalCharacteristic readCharacteristic;
//        private GattLocalCharacteristic writeCharacteristic;
//        private GattLocalCharacteristic notifyCharacteristic;

//        private static readonly Guid ServiceUuid = Guid.Parse("0000180D-0000-1000-8000-00805F9B34FB");
//        private static readonly Guid ReadCharacteristicUuid = Guid.Parse("00002A37-0000-1000-8000-00805F9B34FB");
//        private static readonly Guid WriteCharacteristicUuid = Guid.Parse("00002A38-0000-1000-8000-00805F9B34FB");
//        private static readonly Guid NotifyCharacteristicUuid = Guid.Parse("00002A39-0000-1000-8000-00805F9B34FB");

//        private List<GattSubscribedClient> subscribedClients = new List<GattSubscribedClient>();

//        public async Task<bool> StartServerAsync()
//        {
//            var result = await GattServiceProvider.CreateAsync(ServiceUuid);
//            if (result.Error != BluetoothError.Success)
//                return false;

//            serviceProvider = result.ServiceProvider;

//            await AddCharacteristicsAsync();

//            var advParameters = new GattServiceProviderAdvertisingParameters
//            {
//                IsDiscoverable = true,
//                IsConnectable = true
//            };
//            serviceProvider.StartAdvertising(advParameters);

//            return true;
//        }

//        public void StopServer()
//        {
//            if (serviceProvider != null)
//            {
//                serviceProvider.StopAdvertising();
//                serviceProvider = null;
//            }
//        }

//        private async Task AddCharacteristicsAsync()
//        {
//            // Read Characteristic
//            var readParameters = new GattLocalCharacteristicParameters
//            {
//                CharacteristicProperties = GattCharacteristicProperties.Read,
//                ReadProtectionLevel = GattProtectionLevel.Plain
//            };

//            var readResult = await serviceProvider.Service.CreateCharacteristicAsync(ReadCharacteristicUuid, readParameters);
//            if (readResult.Error == BluetoothError.Success)
//            {
//                readCharacteristic = readResult.Characteristic;
//                readCharacteristic.ReadRequested += ReadCharacteristic_ReadRequested;
//            }

//            // Write Characteristic
//            var writeParameters = new GattLocalCharacteristicParameters
//            {
//                CharacteristicProperties = GattCharacteristicProperties.Write,
//                WriteProtectionLevel = GattProtectionLevel.Plain
//            };

//            var writeResult = await serviceProvider.Service.CreateCharacteristicAsync(WriteCharacteristicUuid, writeParameters);
//            if (writeResult.Error == BluetoothError.Success)
//            {
//                writeCharacteristic = writeResult.Characteristic;
//                writeCharacteristic.WriteRequested += WriteCharacteristic_WriteRequested;
//            }

//            // Notify Characteristic
//            var notifyParameters = new GattLocalCharacteristicParameters
//            {
//                CharacteristicProperties = GattCharacteristicProperties.Notify,
//                ReadProtectionLevel = GattProtectionLevel.Plain
//            };

//            var notifyResult = await serviceProvider.Service.CreateCharacteristicAsync(NotifyCharacteristicUuid, notifyParameters);
//            if (notifyResult.Error == BluetoothError.Success)
//            {
//                notifyCharacteristic = notifyResult.Characteristic;
//                notifyCharacteristic.SubscribedClientsChanged += NotifyCharacteristic_SubscribedClientsChanged;
//            }
//        }

//        private async void ReadCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
//        {
//            var deferral = args.GetDeferral();
//            var request = await args.GetRequestAsync();

//            var writer = new DataWriter();
//            writer.WriteString("Hello from BLE Server!");

//            request.RespondWithValue(writer.DetachBuffer());
//            deferral.Complete();
//        }

//        private async void WriteCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
//        {
//            var deferral = args.GetDeferral();
//            var request = await args.GetRequestAsync();

//            var reader = DataReader.FromBuffer(request.Value);
//            string receivedData = reader.ReadString(reader.UnconsumedBufferLength);

//            System.Diagnostics.Debug.WriteLine($"Received from client: {receivedData}");

//            if (request.Option == GattWriteOption.WriteWithResponse)
//            {
//                request.Respond();
//            }

//            deferral.Complete();
//        }

//        private void NotifyCharacteristic_SubscribedClientsChanged(GattLocalCharacteristic sender, object args)
//        {
//            subscribedClients = (List<GattSubscribedClient>)sender.SubscribedClients;
//        }

//        public async void SendNotification(string message)
//        {
//            if (subscribedClients.Count > 0)
//            {
//                var writer = new DataWriter();
//                writer.WriteString(message);

//                await notifyCharacteristic.NotifyValueAsync(writer.DetachBuffer());
//            }
//        }
//    }
//}
