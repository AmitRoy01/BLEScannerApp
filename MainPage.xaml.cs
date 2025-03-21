using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BLESampleApp;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace BLESampleApp
{
    public sealed partial class MainPage : Page
    {
        private BLEServer bleServer;
        private BLEClient bleClient;
        private DeviceInformation selectedDevice;
        private bool isSubscribed = false;


        public MainPage()
        {
            this.InitializeComponent();

            InitializeServer();
            InitializeClient();
        }

        #region Server Methods
        private void InitializeServer()
        {
            bleServer = new BLEServer();
            bleServer.ClientCountChanged += BleServer_ClientCountChanged;
            bleServer.MessageReceived += BleServer_MessageReceived;
        }

        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            bool started = await bleServer.StartServerAsync();
            if (started)
            {
                ServerStatusText.Text = "Server: Running";
                StartServerButton.IsEnabled = false;
                StopServerButton.IsEnabled = true;
                ServerSendButton.IsEnabled = true;
            }
            else
            {
                ServerStatusText.Text = "Server: Failed to start";
            }
        }

        private void StopServerButton_Click(object sender, RoutedEventArgs e)
        {
            bleServer.StopServer();
            ServerStatusText.Text = "Server: Stopped";
            StartServerButton.IsEnabled = true;
            StopServerButton.IsEnabled = false;
            ServerSendButton.IsEnabled = false;
            ClientCountText.Text = "Connected Clients: 0";
            ServerMessageTextBox.Text = string.Empty;
        }

        private async void ServerSendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = ServerMessageTextBox.Text;
            if (!string.IsNullOrEmpty(message))
            {
                bool sent = await bleServer.SendTextNotificationAsync(message);
                if (sent)
                {
                    ServerReceivedMessagesTextBox.Text += $"[SENT] Message: {message}\n";
                    ServerMessageTextBox.Text = string.Empty;
                }
                else
                {
                    ServerReceivedMessagesTextBox.Text += "[ERROR] Failed to send message.\n";
                }
            }
            else
            {
                ServerReceivedMessagesTextBox.Text += "[ERROR] Invalid message.\n";
            }
        }

        private async void BleServer_ClientCountChanged(object sender, int count)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ClientCountText.Text = $"Connected Clients: {count}";
            });
        }

        private async void BleServer_MessageReceived(object sender, string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ServerReceivedMessagesTextBox.Text += $"[RECEIVED] {message}\n";
            });
        }
        #endregion

        #region Client Methods
        private void InitializeClient()
        {
            // Initialize BLE client
            bleClient = new BLEClient(Dispatcher, StatusBlock);
            bleClient.DeviceListUpdated += BleClient_DeviceListUpdated;
            bleClient.StatusUpdated += BleClient_StatusUpdated;
            bleClient.NotificationReceived += BleClient_NotificationReceived;
        }
        #region Button Click Event Handlers

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous scan results
            DeviceListView.ItemsSource = null;
            ReadResultBlock.Text = "No data";
            NotificationBlock.Text = "No notifications received";

            // Disable connect button
            ConnectButton.IsEnabled = false;

            // Start scanning
            bleClient.StartDeviceWatcher();

            // Update button states
            ScanButton.IsEnabled = false;
            StopScanButton.IsEnabled = true;
        }

        private void StopScanButton_Click(object sender, RoutedEventArgs e)
        {
            bleClient.StopDeviceWatcher();

            // Update button states
            ScanButton.IsEnabled = true;
            StopScanButton.IsEnabled = false;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDevice != null)
            {
                // Connect to selected device
                bool success = await bleClient.ConnectToDeviceAsync(selectedDevice);

                if (success)
                {
                    // Update button states
                    ConnectButton.IsEnabled = false;
                    DisconnectButton.IsEnabled = true;
                    ReadButton.IsEnabled = true;
                    WriteButton.IsEnabled = true;
                    SubscribeButton.IsEnabled = true;
                }
                else
                {
                    // Reset selected device
                    DeviceListView.SelectedItem = null;
                    selectedDevice = null;
                }
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean up first
            if (isSubscribed)
            {
                bleClient.UnsubscribeFromNotificationsAsync();
                isSubscribed = false;
            }

            bleClient.DisconnectFromDevice();

            // Update button states
            ConnectButton.IsEnabled = selectedDevice != null;
            DisconnectButton.IsEnabled = false;
            ReadButton.IsEnabled = false;
            WriteButton.IsEnabled = false;
            SubscribeButton.IsEnabled = false;
            UnsubscribeButton.IsEnabled = false;

            // Clear results
            ReadResultBlock.Text = "No data";
            NotificationBlock.Text = "No notifications received";
        }

        private async void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            string value = await bleClient.ReadTextAsync();

            if (!string.IsNullOrEmpty(value))
            {
                ReadResultBlock.Text = value;
            }
            else
            {
                ReadResultBlock.Text = "Failed to read data";
            }
        }

        private async void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(WriteTextBox.Text))
            {
                await bleClient.WriteTextAsync(WriteTextBox.Text);
                // Clear text box after writing
                WriteTextBox.Text = string.Empty;
            }
        }

        private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            bool success = await bleClient.SubscribeToNotificationsAsync();

            if (success)
            {
                isSubscribed = true;
                SubscribeButton.IsEnabled = false;
                UnsubscribeButton.IsEnabled = true;
                NotificationBlock.Text = "Waiting for notifications...";
            }
        }

        private async void UnsubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSubscribed)
            {
                bool success = await bleClient.UnsubscribeFromNotificationsAsync();

                if (success)
                {
                    isSubscribed = false;
                    SubscribeButton.IsEnabled = true;
                    UnsubscribeButton.IsEnabled = false;
                }
            }
        }

        #endregion

        private void DeviceListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDevice = DeviceListView.SelectedItem as DeviceInformation;
            ConnectButton.IsEnabled = selectedDevice != null;
        }

        #region BLEClient Event Handlers

        private void BleClient_DeviceListUpdated(object sender, System.Collections.Generic.List<DeviceInformation> devices)
        {
            DeviceListView.ItemsSource = null;
            DeviceListView.ItemsSource = devices;
        }

        private void BleClient_StatusUpdated(object sender, string message)
        {
            // This handler is a backup in case we don't have access to the StatusBlock directly
        }

        private void BleClient_NotificationReceived(object sender, string notification)
        {
            NotificationBlock.Text = notification;
        }

        #endregion
        #endregion
    }
}