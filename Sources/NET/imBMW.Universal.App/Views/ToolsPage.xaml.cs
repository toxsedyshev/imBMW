using imBMW.iBus;
using imBMW.iBus.Devices.Real;
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
    public sealed partial class ToolsPage : Page
    {
        public ToolsPage()
        {
            this.InitializeComponent();
        }

        private void OpenDoorsButton_Click(object sender, RoutedEventArgs e)
        {
            BodyModule.UnlockDoors();
        }

        private void CloseDoorsButton_Click(object sender, RoutedEventArgs e)
        {
            BodyModule.LockDoors();
        }

        private void TestDoors1Button_Click(object sender, RoutedEventArgs e)
        {
            Manager.EnqueueMessage(new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x45, 0x01));
        }

        private void TestDoors2Button_Click(object sender, RoutedEventArgs e)
        {
            Manager.EnqueueMessage(new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x03, 0x01));
        }
    }
}
