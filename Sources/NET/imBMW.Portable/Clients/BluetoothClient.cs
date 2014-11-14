using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace imBMW.Clients
{
    public class BluetoothClient : SocketClient
    {
        public BluetoothClient()
        {

        }

        public async Task Connect()
        {
            var deviceList = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));

            var device = deviceList.FirstOrDefault(d => d.Name.Contains("imBMW"));

            var sppService = await RfcommDeviceService.FromIdAsync(device.Id);

            await Connect(new SocketConnectionSettings(sppService.ConnectionHostName, sppService.ConnectionServiceName));
        }
    }
}
