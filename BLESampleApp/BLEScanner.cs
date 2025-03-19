using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using BLESampleApp;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace BLESampleApp
{

    public class BLEScanner
    {
        private BluetoothLEAdvertisementWatcher watcher;
        private BluetoothLEDevice connectedDevice;
        private GattCharacteristic readCharacteristic;
        private GattCharacteristic writeCharacteristic;
        private GattCharacteristic notifyCharacteristic;

        public event EventHandler<string> DeviceFound;
        public event EventHandler<string> MessageReceived;

        public async Task StartScanningAsync()
        {
            watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            watcher.Received += async (sender, args) =>
            {
                var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                if (device != null)
                {
                    DeviceFound?.Invoke(this, device.Name);
                }
            };

            watcher.Start();
        }

        public void StopScanning()
        {
            watcher?.Stop();
        }

        public async Task<bool> ConnectToDeviceAsync(string deviceName)
        {
            watcher.Stop();
            var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());

            foreach (var deviceInfo in devices)
            {
                var device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
                if (device != null && device.Name == deviceName)
                {
                    connectedDevice = device;
                    return await DiscoverServicesAsync();
                }
            }

            return false;
        }

        private async Task<bool> DiscoverServicesAsync()
        {
            var result = await connectedDevice.GetGattServicesAsync();
            if (result.Status != GattCommunicationStatus.Success)
                return false;

            foreach (var service in result.Services)
            {
                if (service.Uuid == BLEServer.ServiceUuid)
                {
                    return await DiscoverCharacteristicsAsync(service);
                }
            }

            return false;
        }

        private async Task<bool> DiscoverCharacteristicsAsync(GattDeviceService service)
        {
            var result = await service.GetCharacteristicsAsync();
            if (result.Status != GattCommunicationStatus.Success)
                return false;

            foreach (var characteristic in result.Characteristics)
            {
                if (characteristic.Uuid == BLEServer.ReadCharacteristicUuid)
                    readCharacteristic = characteristic;
                else if (characteristic.Uuid == BLEServer.WriteCharacteristicUuid)
                    writeCharacteristic = characteristic;
                else if (characteristic.Uuid == BLEServer.NotifyCharacteristicUuid)
                {
                    notifyCharacteristic = characteristic;
                    notifyCharacteristic.ValueChanged += NotifyCharacteristic_ValueChanged;
                    await notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                }
            }

            return true;
        }

        private async void NotifyCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            string message = reader.ReadString(reader.UnconsumedBufferLength);
            MessageReceived?.Invoke(this, message);
        }

        public async Task<string> ReadDataAsync()
        {
            if (readCharacteristic == null)
                return null;

            var result = await readCharacteristic.ReadValueAsync();
            if (result.Status != GattCommunicationStatus.Success)
                return null;

            var reader = DataReader.FromBuffer(result.Value);
            return reader.ReadString(reader.UnconsumedBufferLength);
        }

        public async Task<bool> WriteDataAsync(string message)
        {
            if (writeCharacteristic == null)
                return false;

            var writer = new DataWriter();
            writer.WriteString(message);

            var result = await writeCharacteristic.WriteValueAsync(writer.DetachBuffer());
            return result == GattCommunicationStatus.Success;
        }
    }

    //public class BLEDevice
    //{
    //    public string Name { get; set; }
    //    public ulong Address { get; set; }
    //    public bool IsConnectable { get; set; }

    //    public override string ToString()
    //    {
    //        return $"{Name} ({Address})";
    //    }
    //    //  public List<Guid> ServiceUuids { get; set; } = new List<Guid>();  // Initialize with an empty list

    //}

    //public class BLEScanner
    //{
    //    private BluetoothLEAdvertisementWatcher watcher;
    //    private BluetoothLEDevice connectedDevice;
    //    private GattDeviceService targetService;
    //    private GattCharacteristic readCharacteristic;
    //    private GattCharacteristic writeCharacteristic;
    //    private GattCharacteristic notifyCharacteristic;

    //    private List<BLEDevice> discoveredDevices = new List<BLEDevice>();

    //    public event EventHandler<BLEDevice> DeviceDiscovered;
    //    public event EventHandler<string> MessageReceived;
    //    public event EventHandler<bool> ConnectionStatusChanged;

    //    public bool IsConnected => connectedDevice != null;

    //    public BLEScanner()
    //    {
    //        watcher = new BluetoothLEAdvertisementWatcher
    //        {
    //            ScanningMode = BluetoothLEScanningMode.Active
    //        };


    //        watcher.Received += Watcher_Received;
    //        // watcher.Stopped += Watcher_Stopped;
    //    }

    //    private void Watcher_Stopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
    //    {
    //        // Optionally handle stop event
    //    }


    //    private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    //    {
    //        string deviceName = args.Advertisement.LocalName;
    //        if (string.IsNullOrEmpty(deviceName))
    //            deviceName = "Unknown Device";

    //        System.Diagnostics.Debug.WriteLine($"Found device: {deviceName}, Address: {args.BluetoothAddress}, ServiceUUIDs: {string.Join(", ", args.Advertisement.ServiceUuids)}");

    //        var device = new BLEDevice
    //        {
    //            Name = deviceName,
    //            Address = args.BluetoothAddress,
    //            IsConnectable = args.IsConnectable
    //        };

    //        // Check if we already discovered this device
    //        if (!discoveredDevices.Any(d => d.Address == device.Address))
    //        {
    //            discoveredDevices.Add(device);
    //            DeviceDiscovered?.Invoke(this, device);
    //        }
    //    }


    //    public void StartScanning()
    //    {
    //        discoveredDevices.Clear();
    //        watcher.Start();
    //    }

    //    public void StopScanning()
    //    {
    //        watcher.Stop();
    //    }

    //    public async Task<bool> ConnectToDeviceAsync(ulong deviceAddress)
    //    {
    //        if (connectedDevice != null)
    //        {
    //            await DisconnectAsync();
    //        }

    //        try
    //        {
    //            // Connect to the device
    //            connectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress);
    //            if (connectedDevice == null)
    //                return false;

    //            connectedDevice.ConnectionStatusChanged += ConnectedDevice_ConnectionStatusChanged;

    //            // Get the target service
    //            var servicesResult = await connectedDevice.GetGattServicesForUuidAsync(BLEServer.ServiceUuid);
    //            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
    //                return false;

    //            targetService = servicesResult.Services[0];

    //            // Get the characteristics
    //            var readResult = await targetService.GetCharacteristicsForUuidAsync(BLEServer.ReadCharacteristicUuid);
    //            if (readResult.Status == GattCommunicationStatus.Success && readResult.Characteristics.Count > 0)
    //            {
    //                readCharacteristic = readResult.Characteristics[0];
    //            }

    //            var writeResult = await targetService.GetCharacteristicsForUuidAsync(BLEServer.WriteCharacteristicUuid);
    //            if (writeResult.Status == GattCommunicationStatus.Success && writeResult.Characteristics.Count > 0)
    //            {
    //                writeCharacteristic = writeResult.Characteristics[0];
    //            }

    //            var notifyResult = await targetService.GetCharacteristicsForUuidAsync(BLEServer.NotifyCharacteristicUuid);
    //            if (notifyResult.Status == GattCommunicationStatus.Success && notifyResult.Characteristics.Count > 0)
    //            {
    //                notifyCharacteristic = notifyResult.Characteristics[0];

    //                // Subscribe to notifications
    //                if (notifyCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
    //                {
    //                    var status = await notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
    //                        GattClientCharacteristicConfigurationDescriptorValue.Notify);

    //                    if (status == GattCommunicationStatus.Success)
    //                    {
    //                        notifyCharacteristic.ValueChanged += NotifyCharacteristic_ValueChanged;
    //                    }
    //                }
    //            }

    //            ConnectionStatusChanged?.Invoke(this, true);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            await DisconnectAsync();
    //            return false;
    //        }
    //    }

    //    private void ConnectedDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
    //    {
    //        if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
    //        {
    //            DisconnectAsync().Wait();
    //            ConnectionStatusChanged?.Invoke(this, false);
    //        }
    //    }

    //    private void NotifyCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    //    {
    //        var reader = DataReader.FromBuffer(args.CharacteristicValue);
    //        string message = reader.ReadString(reader.UnconsumedBufferLength);
    //        MessageReceived?.Invoke(this, message);
    //    }

    //    public async Task<bool> SendMessageAsync(string message)
    //    {
    //        if (writeCharacteristic == null)
    //            return false;

    //        try
    //        {
    //            var writer = new DataWriter();
    //            writer.WriteString(message);

    //            var result = await writeCharacteristic.WriteValueAsync(
    //                writer.DetachBuffer(),
    //                GattWriteOption.WriteWithResponse);

    //            return result == GattCommunicationStatus.Success;
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //    }

    //    public async Task DisconnectAsync()
    //    {
    //        try
    //        {
    //            if (notifyCharacteristic != null)
    //            {
    //                notifyCharacteristic.ValueChanged -= NotifyCharacteristic_ValueChanged;
    //                await notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
    //                    GattClientCharacteristicConfigurationDescriptorValue.None);
    //                notifyCharacteristic = null;
    //            }

    //            readCharacteristic = null;
    //            writeCharacteristic = null;

    //            if (targetService != null)
    //            {
    //                targetService.Dispose();
    //                targetService = null;
    //            }

    //            if (connectedDevice != null)
    //            {
    //                connectedDevice.ConnectionStatusChanged -= ConnectedDevice_ConnectionStatusChanged;
    //                connectedDevice.Dispose();
    //                connectedDevice = null;
    //            }

    //            ConnectionStatusChanged?.Invoke(this, false);
    //        }
    //        catch
    //        {
    //            // Handle or log error
    //        }
    //    }
    //}
}