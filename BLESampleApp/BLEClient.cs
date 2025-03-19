//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Text;
//using System.Threading.Tasks;
//using Windows.Devices.Bluetooth;
//using Windows.Devices.Bluetooth.GenericAttributeProfile;
//using Windows.Devices.Enumeration;
//using Windows.Storage.Streams;

//namespace BLESampleApp
//{
//    public class BLEClient
//    {
//        private BluetoothLEDevice bleDevice;
//        private GattCharacteristic readCharacteristic;
//        private GattCharacteristic writeCharacteristic;
//        private GattCharacteristic notifyCharacteristic;

//        public event EventHandler<string> MessageReceived;

//        private readonly Guid ServiceUuid = Guid.Parse("0000180D-0000-1000-8000-00805F9B34FB");
//        private readonly Guid ReadCharacteristicUuid = Guid.Parse("00002A37-0000-1000-8000-00805F9B34FB");
//        private readonly Guid WriteCharacteristicUuid = Guid.Parse("00002A38-0000-1000-8000-00805F9B34FB");
//        private readonly Guid NotifyCharacteristicUuid = Guid.Parse("00002A39-0000-1000-8000-00805F9B34FB");

//        public async Task<bool> ConnectToServerAsync()
//        {
//            string selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);
//            var devices = await DeviceInformation.FindAllAsync(selector);

//            foreach (var device in devices)
//            {
//                bleDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
//                if (bleDevice != null)
//                {
//                    Debug.WriteLine($"Connected to BLE Device: {bleDevice.Name}");
//                    return await DiscoverServicesAsync();
//                }
//            }

//            Debug.WriteLine("No BLE Server found!");
//            return false;
//        }

//        private async Task<bool> DiscoverServicesAsync()
//        {
//            var servicesResult = await bleDevice.GetGattServicesForUuidAsync(ServiceUuid);
//            if (servicesResult.Status != GattCommunicationStatus.Success)
//                return false;

//            foreach (var service in servicesResult.Services)
//            {
//                var characteristicsResult = await service.GetCharacteristicsAsync();
//                if (characteristicsResult.Status != GattCommunicationStatus.Success)
//                    continue;

//                foreach (var characteristic in characteristicsResult.Characteristics)
//                {
//                    if (characteristic.Uuid == ReadCharacteristicUuid)
//                    {
//                        readCharacteristic = characteristic;
//                    }
//                    else if (characteristic.Uuid == WriteCharacteristicUuid)
//                    {
//                        writeCharacteristic = characteristic;
//                    }
//                    else if (characteristic.Uuid == NotifyCharacteristicUuid)
//                    {
//                        notifyCharacteristic = characteristic;
//                        notifyCharacteristic.ValueChanged += NotifyCharacteristic_ValueChanged;
//                        await notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
//                            GattClientCharacteristicConfigurationDescriptorValue.Notify);
//                    }
//                }
//            }

//            return readCharacteristic != null && writeCharacteristic != null && notifyCharacteristic != null;
//        }

//        public async Task<string> ReadDataAsync()
//        {
//            if (readCharacteristic == null)
//                return null;

//            var result = await readCharacteristic.ReadValueAsync();
//            if (result.Status == GattCommunicationStatus.Success)
//            {
//                using (var reader = DataReader.FromBuffer(result.Value))
//                {
//                    return reader.ReadString(reader.UnconsumedBufferLength);
//                }
//            }

//            return null;
//        }

//        public async Task<bool> WriteDataAsync(string message)
//        {
//            if (writeCharacteristic == null)
//                return false;

//            var writer = new DataWriter();
//            writer.WriteString(message);

//            var status = await writeCharacteristic.WriteValueAsync(writer.DetachBuffer());
//            return status == GattCommunicationStatus.Success;
//        }

//        private void NotifyCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
//        {
//            using (var reader = DataReader.FromBuffer(args.CharacteristicValue))
//            {
//                string receivedMessage = reader.ReadString(reader.UnconsumedBufferLength);
//                MessageReceived?.Invoke(this, receivedMessage);
//            }
//        }

//        public void Disconnect()
//        {
//            bleDevice?.Dispose();
//            bleDevice = null;
//        }
//    }
//}
