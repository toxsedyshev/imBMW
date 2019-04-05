using imBMW.Clients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace imBMW.Universal.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DashboardPage));
        }

        private void ToolsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ToolsPage));
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await BluetoothClient.Current.Connect();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "imBMW Connect");
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BluetoothClient.Current.Disconnect();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "imBMW Disconnect");
            }
        }
    }
}
