using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace BLESampleApp
{
    public class BLEClient
    {
        private DeviceWatcher deviceWatcher;
        private List<DeviceInformation> deviceList = new List<DeviceInformation>();
        private BluetoothLEDevice bluetoothLeDevice;
        private GattDeviceService batteryService;
        private GattCharacteristic readTextCharacteristic;
        private GattCharacteristic writeTextCharacteristic;
        private GattCharacteristic notifyTextCharacteristic;
        private CoreDispatcher dispatcher;
        private TextBlock statusBlock;

        // Known UUIDs for your BLE Server
        public static readonly Guid BatteryServiceUuid = Guid.Parse("0000180F-0000-1000-8000-00805F9B34FB");
        public static readonly Guid ReadTextCharacteristicUuid = Guid.Parse("0000D1F4-0000-1000-8000-00805F9B34FB");
        public static readonly Guid WriteTextCharacteristicUuid = Guid.Parse("0000B2D2-0000-1000-8000-00805F9B34FB");
        public static readonly Guid NotifyTextCharacteristicUuid = Guid.Parse("0000C3E3-0000-1000-8000-00805F9B34FB");

        // Events for UI updates
        public event EventHandler<List<DeviceInformation>> DeviceListUpdated;
        public event EventHandler<string> StatusUpdated;
        public event EventHandler<string> NotificationReceived;

        public BLEClient(CoreDispatcher dispatcher, TextBlock statusBlock)
        {
            this.dispatcher = dispatcher;
            this.statusBlock = statusBlock;
        }

        public List<DeviceInformation> GetDeviceList()
        {
            return deviceList;
        }

        public void StartDeviceWatcher()
        {
            // Clear previous device list
            deviceList.Clear();

            // Request specific properties
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            // Create a device watcher to find BLE devices
            deviceWatcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                requestedProperties,
                DeviceInformationKind.AssociationEndpoint);

            // Register event handlers
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            // Start the watcher
            deviceWatcher.Start();
            UpdateStatus("Scanning for BLE server...");
        }

        public void StopDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                deviceWatcher.Stop();
                deviceWatcher = null;
                UpdateStatus("Device scanning stopped");
            }
        }

        public async Task<bool> ConnectToDeviceAsync(DeviceInformation deviceInfo)
        {
            UpdateStatus($"Connecting to {deviceInfo.Name}...");

            try
            {
                // Note: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent
                bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);

                if (bluetoothLeDevice == null)
                {
                    UpdateStatus("Failed to connect to device");
                    return false;
                }

                UpdateStatus($"Connected to {bluetoothLeDevice.Name}");

                // Check if this device has our specific Battery Service
                batteryService = await GetSpecificServiceAsync(BatteryServiceUuid);

                if (batteryService == null)
                {
                    UpdateStatus("This device is not the expected BLE server (Battery service not found)");
                    DisconnectFromDevice();
                    return false;
                }

                // Get our specific characteristics
                bool success = await GetSpecificCharacteristicsAsync();

                if (!success)
                {
                    UpdateStatus("This device is not the expected BLE server (Required characteristics not found)");
                    DisconnectFromDevice();
                    return false;
                }

                UpdateStatus("Successfully connected to BLE server and found required characteristics");
                return true;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error connecting to device: {ex.Message}");
                return false;
            }
        }

        public void DisconnectFromDevice()
        {
            // Unsubscribe from notification if needed
            if (notifyTextCharacteristic != null)
            {
                try
                {
                    notifyTextCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                }
                catch { }
            }

            if (bluetoothLeDevice != null)
            {
                bluetoothLeDevice.Dispose();
                bluetoothLeDevice = null;

                // Reset service and characteristics
                batteryService = null;
                readTextCharacteristic = null;
                writeTextCharacteristic = null;
                notifyTextCharacteristic = null;

                UpdateStatus("Disconnected from BLE server");
            }
        }

        public async Task<string> ReadTextAsync()
        {
            if (readTextCharacteristic == null)
            {
                UpdateStatus("Read characteristic not available");
                return string.Empty;
            }

            try
            {
                GattReadResult result = await readTextCharacteristic.ReadValueAsync();

                if (result.Status == GattCommunicationStatus.Success)
                {
                    var reader = DataReader.FromBuffer(result.Value);
                    byte[] data = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(data);

                    string textValue = System.Text.Encoding.UTF8.GetString(data);
                    UpdateStatus($"Read value: {textValue}");
                    return textValue;
                }
                else
                {
                    UpdateStatus($"Error reading characteristic: {result.Status}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }

            return string.Empty;
        }

        public async Task<bool> WriteTextAsync(string text)
        {
            if (writeTextCharacteristic == null)
            {
                UpdateStatus("Write characteristic not available");
                return false;
            }

            try
            {
                var writer = new DataWriter();
                writer.WriteString(text);

                GattCommunicationStatus status = await writeTextCharacteristic.WriteValueAsync(writer.DetachBuffer());

                if (status == GattCommunicationStatus.Success)
                {
                    UpdateStatus($"Successfully wrote: {text}");
                    return true;
                }
                else
                {
                    UpdateStatus($"Error writing to characteristic: {status}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }

            return false;
        }

        public async Task<bool> SubscribeToNotificationsAsync()
        {
            if (notifyTextCharacteristic == null)
            {
                UpdateStatus("Notification characteristic not available");
                return false;
            }

            try
            {
                // First, write to the CCCD to enable notifications
                GattCommunicationStatus status = await notifyTextCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);

                if (status == GattCommunicationStatus.Success)
                {
                    // Register for notifications
                    notifyTextCharacteristic.ValueChanged += Characteristic_ValueChanged;
                    UpdateStatus("Successfully subscribed to notifications");
                    return true;
                }
                else
                {
                    UpdateStatus($"Error subscribing to notifications: {status}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }

            return false;
        }

        public async Task<bool> UnsubscribeFromNotificationsAsync()
        {
            if (notifyTextCharacteristic == null)
            {
                UpdateStatus("Notification characteristic not available");
                return false;
            }

            try
            {
                // Write to the CCCD to disable notifications
                GattCommunicationStatus status = await notifyTextCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.None);

                if (status == GattCommunicationStatus.Success)
                {
                    // Unregister from notifications
                    notifyTextCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                    UpdateStatus("Successfully unsubscribed from notifications");
                    return true;
                }
                else
                {
                    UpdateStatus($"Error unsubscribing from notifications: {status}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }

            return false;
        }

        public bool HasRequiredCharacteristics()
        {
            return readTextCharacteristic != null && writeTextCharacteristic != null && notifyTextCharacteristic != null;
        }

        #region Private Helper Methods

        private async Task<GattDeviceService> GetSpecificServiceAsync(Guid serviceUuid)
        {
            try
            {
                // Get the service directly using its UUID
                GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesForUuidAsync(serviceUuid);

                if (result.Status == GattCommunicationStatus.Success && result.Services.Count > 0)
                {
                    return result.Services[0];
                }

                UpdateStatus($"Service with UUID {serviceUuid} not found");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error finding service: {ex.Message}");
            }

            return null;
        }

        private async Task<bool> GetSpecificCharacteristicsAsync()
        {
            try
            {
                // Get all characteristics for the battery service
                GattCharacteristicsResult result = await batteryService.GetCharacteristicsAsync();

                if (result.Status != GattCommunicationStatus.Success)
                {
                    UpdateStatus($"Error getting characteristics: {result.Status}");
                    return false;
                }

                // Find our specific characteristics
                foreach (var characteristic in result.Characteristics)
                {
                    if (characteristic.Uuid == ReadTextCharacteristicUuid)
                    {
                        readTextCharacteristic = characteristic;
                        UpdateStatus("Read characteristic found");
                    }
                    else if (characteristic.Uuid == WriteTextCharacteristicUuid)
                    {
                        writeTextCharacteristic = characteristic;
                        UpdateStatus("Write characteristic found");
                    }
                    else if (characteristic.Uuid == NotifyTextCharacteristicUuid)
                    {
                        notifyTextCharacteristic = characteristic;
                        UpdateStatus("Notification characteristic found");
                    }
                }

                // Check if we found all required characteristics
                return HasRequiredCharacteristics();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Event Handlers

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Only add devices with a name
                if (!string.IsNullOrEmpty(deviceInfo.Name))
                {
                    deviceList.Add(deviceInfo);
                    DeviceListUpdated?.Invoke(this, deviceList);
                }
            });
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Find and update the device in the list
                for (int i = 0; i < deviceList.Count; i++)
                {
                    if (deviceList[i].Id == deviceInfoUpdate.Id)
                    {
                        // Just re-get the device to update it
                        DeviceInformation updatedDevice = DeviceInformation.CreateFromIdAsync(deviceInfoUpdate.Id).GetAwaiter().GetResult();
                        if (updatedDevice != null)
                        {
                            deviceList[i] = updatedDevice;
                            DeviceListUpdated?.Invoke(this, deviceList);
                        }
                        break;
                    }
                }
            });
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Find and remove the device from the list
                for (int i = 0; i < deviceList.Count; i++)
                {
                    if (deviceList[i].Id == deviceInfoUpdate.Id)
                    {
                        deviceList.RemoveAt(i);
                        DeviceListUpdated?.Invoke(this, deviceList);
                        break;
                    }
                }
            });
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateStatus($"Device scanning completed. Found {deviceList.Count} devices.");
            });
        }

        private async void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateStatus("Device scanning stopped");
            });
        }

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Read the value from the buffer
                var reader = DataReader.FromBuffer(args.CharacteristicValue);
                byte[] data = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(data);

                string textValue = System.Text.Encoding.UTF8.GetString(data);

                UpdateStatus($"Notification received: {textValue}");
                NotificationReceived?.Invoke(this, textValue);
            });
        }

        #endregion

        private void UpdateStatus(string message)
        {
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (statusBlock != null)
                {
                    statusBlock.Text = message;
                }

                StatusUpdated?.Invoke(this, message);
            });
        }
    }
}