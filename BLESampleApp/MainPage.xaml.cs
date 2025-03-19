using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BLESampleApp;
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
        //private BLEScanner bleScanner;
       /// private Dictionary<ulong, BLEDevice> discoveredDevices = new Dictionary<ulong, BLEDevice>();
        private BLEScanner bleScanner;
        private string selectedDeviceName;

        public MainPage()
        {
            this.InitializeComponent();

            InitializeServer();
            InitializeScanner();
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
        }

        private async void ServerSendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = ServerMessageTextBox.Text;
            if (!string.IsNullOrEmpty(message))
            {
                bool sent = await bleServer.SendNotificationAsync(message);
                if (sent)
                {
                    ServerReceivedMessagesTextBox.Text += $"[SENT] {message}\n";
                    ServerMessageTextBox.Text = string.Empty;
                }
                else
                {
                    ServerReceivedMessagesTextBox.Text += $"[ERROR] Failed to send: {message}\n";
                }
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
        private void InitializeScanner()
        {
            bleScanner = new BLEScanner();
            //bleScanner.DeviceDiscovered += BleScanner_DeviceDiscovered;
            //bleScanner.MessageReceived += BleScanner_MessageReceived;
            //bleScanner.ConnectionStatusChanged += BleScanner_ConnectionStatusChanged;
            bleScanner = new BLEScanner();
            bleScanner.DeviceFound += BleScanner_DeviceFound;
            bleScanner.MessageReceived += BleScanner_MessageReceived;
        }

        //private void StartScanButton_Click(object sender, RoutedEventArgs e)
        //{
        //    DeviceListView.Items.Clear();
        //    discoveredDevices.Clear();
        //    bleScanner.StartScanning();
        //    ScanStatusText.Text = "Scanner: Running";
        //    StartScanButton.IsEnabled = false;
        //    StopScanButton.IsEnabled = true;
        //}

        //private void StopScanButton_Click(object sender, RoutedEventArgs e)
        //{
        //    bleScanner.StopScanning();
        //    ScanStatusText.Text = "Scanner: Stopped";
        //    StartScanButton.IsEnabled = true;
        //    StopScanButton.IsEnabled = false;
        //}

        //private async void BleScanner_DeviceDiscovered(object sender, BLEDevice device)
        //{
        //    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //    {
        //        if (!discoveredDevices.ContainsKey(device.Address))
        //        {
        //            discoveredDevices[device.Address] = device;
        //            DeviceListView.Items.Add(device);
        //            ConnectButton.IsEnabled = DeviceListView.Items.Count > 0;
        //        }
        //    });

        //    //string deviceName = device.Name;
        //    //string deviceId = device.Address.ToString();

        //    //if (!string.IsNullOrEmpty(deviceName))
        //    //{
        //    //    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //    //    {
        //    //        DeviceListView.Items.Add($"{deviceName} ({deviceId})");
        //    //        ConnectButton.IsEnabled = DeviceListView.Items.Count > 0;

        //    //    });

        //    //    Debug.WriteLine($"Found: {deviceName} ({deviceId})");
        //    //}
        //}

        //private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (DeviceListView.SelectedItem is BLEDevice selectedDevice)
        //    {
        //        ConnectButton.IsEnabled = false;
        //        bool connected = await bleScanner.ConnectToDeviceAsync(selectedDevice.Address);
        //        if (!connected)
        //        {
        //            ConnectionStatusText.Text = "Status: Connection Failed";
        //            ConnectButton.IsEnabled = true;
        //        }
        //    }
        //}

        //private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    await bleScanner.DisconnectAsync();
        //}

        //private async void ClientSendButton_Click(object sender, RoutedEventArgs e)
        //{
        //    string message = ClientMessageTextBox.Text;
        //    if (!string.IsNullOrEmpty(message))
        //    {
        //        bool sent = await bleScanner.SendMessageAsync(message);
        //        if (sent)
        //        {
        //            ClientReceivedMessagesTextBox.Text += $"[SENT] {message}\n";
        //            ClientMessageTextBox.Text = string.Empty;
        //        }
        //        else
        //        {
        //            ClientReceivedMessagesTextBox.Text += $"[ERROR] Failed to send: {message}\n";
        //        }
        //    }
        //}

        //private async void BleScanner_MessageReceived(object sender, string message)
        //{
        //    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
        //    {
        //        ClientReceivedMessagesTextBox.Text += $"[RECEIVED] {message}\n";
        //    });
        //}

        //private async void BleScanner_ConnectionStatusChanged(object sender, bool isConnected)
        //{
        //    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
        //    {
        //        if (isConnected)
        //        {
        //            ConnectionStatusText.Text = "Status: Connected";
        //            ConnectButton.IsEnabled = false;
        //            DisconnectButton.IsEnabled = true;
        //            ClientSendButton.IsEnabled = true;
        //        }
        //        else
        //        {
        //            ConnectionStatusText.Text = "Status: Disconnected";
        //            ConnectButton.IsEnabled = DeviceListView.Items.Count > 0;
        //            DisconnectButton.IsEnabled = false;
        //            ClientSendButton.IsEnabled = false;
        //        }
        //    });
        //}
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            DevicesList.Items.Clear();
            await bleScanner.StartScanningAsync();
        }

        private async void BleScanner_DeviceFound(object sender, string deviceName)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (!DevicesList.Items.Contains(deviceName))
                {
                    DevicesList.Items.Add(deviceName);
                }
            });
        }

        private void DevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DevicesList.SelectedItem != null)
            {
                selectedDeviceName = DevicesList.SelectedItem.ToString();
                ConnectButton.IsEnabled = true;
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDeviceName != null)
            {
                bool connected = await bleScanner.ConnectToDeviceAsync(selectedDeviceName);
                if (connected)
                {
                    SendButton.IsEnabled = true;
                }
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MessageBox.Text))
            {
                await bleScanner.WriteDataAsync(MessageBox.Text);
            }
        }

        private void BleScanner_MessageReceived(object sender, string message)
        {
            ReceivedMessage.Text = "Received: " + message;
        }
        #endregion
    }
}