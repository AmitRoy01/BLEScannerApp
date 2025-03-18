using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.Advertisement;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BLEScannerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BluetoothLEAdvertisementWatcher _watcher;

        public MainPage()
        {
            this.InitializeComponent();
            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            _watcher.Received += Watcher_Received;
        }

        private async void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            string deviceName = args.Advertisement.LocalName;
            string deviceId = args.BluetoothAddress.ToString();

            if (!string.IsNullOrEmpty(deviceName))
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    DeviceList.Items.Add($"{deviceName} ({deviceId})");
                });

                Debug.WriteLine($"Found: {deviceName} ({deviceId})");
            }
        }

        private void StartScan_Click(object sender, RoutedEventArgs e)
        {
            DeviceList.Items.Clear();
            _watcher.Start();
            Debug.WriteLine("Scanning started...");
        }

        private void StopScan_Click(object sender, RoutedEventArgs e)
        {
            _watcher.Stop();
            Debug.WriteLine("Scanning stopped.");
        }
    }
}
