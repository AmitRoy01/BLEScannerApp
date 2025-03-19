
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BLEServer1
{
    public sealed partial class MainPage : Page
    {
        private BLEServer bleServer;

        public MainPage()
        {
            this.InitializeComponent();
            bleServer = new BLEServer();
            bleServer.ClientConnected += BleServer_ClientConnected;
            bleServer.ClientDisconnected += BleServer_ClientDisconnected;
            bleServer.DataReceived += BleServer_DataReceived;
        }

        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            bool success = await bleServer.StartServerAsync();
            if (success)
            {
                StatusText.Text = "BLE Server Running...";
                StartServerButton.IsEnabled = false;
                StopServerButton.IsEnabled = true;
                SendButton.IsEnabled = true;
            }
            else
            {
                StatusText.Text = "Failed to start server!";
            }
        }

        private void StopServerButton_Click(object sender, RoutedEventArgs e)
        {
            bleServer.StopServer();
            StatusText.Text = "Server Stopped.";
            StartServerButton.IsEnabled = true;
            StopServerButton.IsEnabled = false;
            SendButton.IsEnabled = false;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var message = SendTextBox.Text;
            bleServer.SendNotification(message);
            SendTextBox.ClearUndoRedoHistory();
        }

        private void BleServer_ClientConnected(object sender, EventArgs e)
        {
            ConnectedClientsText.Text = $"Connected Clients: {bleServer.ConnectedClientsCount}";
        }

        private void BleServer_ClientDisconnected(object sender, EventArgs e)
        {
            ConnectedClientsText.Text = $"Connected Clients: {bleServer.ConnectedClientsCount}";
        }

        private async void BleServer_DataReceived(object sender, string data)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ReceivedTextBox.Text += $"Client: {data}\n";
            });
        }
    }
}




//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Controls.Primitives;
//using Windows.UI.Xaml.Data;
//using Windows.UI.Xaml.Input;
//using Windows.UI.Xaml.Media;
//using Windows.UI.Xaml.Navigation;

//// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

//namespace BLEServer1
//{
//    /// <summary>
//    /// An empty page that can be used on its own or navigated to within a Frame.
//    /// </summary>
//    public sealed partial class MainPage : Page
//    {
//        private BLEServer bleServer;

//        public MainPage()
//        {
//            this.InitializeComponent();
//            bleServer = new BLEServer();
//        }

//        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
//        {
//            bool success = await bleServer.StartServerAsync();
//            if (success)
//            {
//                StatusText.Text = "BLE Server Running...";
//                StartServerButton.IsEnabled = false;
//                StopServerButton.IsEnabled = true;
//            }
//            else
//            {
//                StatusText.Text = "Failed to start server!";
//            }
//        }

//        private void StopServerButton_Click(object sender, RoutedEventArgs e)
//        {
//            bleServer.StopServer();
//            StatusText.Text = "Server Stopped.";
//            StartServerButton.IsEnabled = true;
//            StopServerButton.IsEnabled = false;
//        }
//    }
//}
