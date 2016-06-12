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
        static BluetoothClient instance;

        protected BluetoothClient()
        {

        }

        public async Task Connect()
        {
            var deviceList = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));

            var device = deviceList.FirstOrDefault(d => d.Name.Contains("imBMWBlackBox"));
            if (device == null)
            {
                device = deviceList.FirstOrDefault(d => d.Name.Contains("imBMW"));
            }
            if (device == null)
            {
                throw new Exception("imBMW Bluetooth device not found");
            }

            var sppService = await RfcommDeviceService.FromIdAsync(device.Id);

            await Connect(new SocketConnectionSettings(sppService.ConnectionHostName, sppService.ConnectionServiceName));
        }

        public static BluetoothClient Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BluetoothClient();
                }
                return instance;
            }
        }
    }
}
