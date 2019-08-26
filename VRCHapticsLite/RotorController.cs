using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Management;
using Microsoft.Win32;

namespace RotorController
{
    enum RotorStatus
    {
        NotInitialized,
        Initializing,
        Waiting,
        Sending,
    }

    class RotorController
    {
        private SerialPort _port;
        private RotorStatus _status = RotorStatus.NotInitialized;
        private string _device_name;
        private readonly int _interval; // msec

        public RotorController(string device_name, int interval = 10)
        {
            _device_name = device_name;
            _interval = interval;
        }

        //
        // https://www.softech.co.jp/mm_170705_tr.htm
        // 
        private string GetBluetoothRegistryName(string address)
        {
            string deviceName = "";
            string registryPath = @"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Devices";
            string devicePath = string.Format(@"{0}\{1}", registryPath, address);

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(devicePath))
            {
                if (key != null)
                {
                    var o = key.GetValue("Name");

                    byte[] raw = o as byte[];

                    if (raw != null)
                    {
                        deviceName = Encoding.ASCII.GetString(raw);
                    }
                }
            }
            return deviceName.TrimEnd('\0');
        }

        private Dictionary<string, string> GetBluetoothCOMPorts()
        {
            Regex regexPortName = new Regex(@"(COM\d+)");
            ManagementObjectSearcher searchSerial = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");

            var ret_dict = new Dictionary<string, string>();

            foreach (ManagementObject obj in searchSerial.Get())
            {
                var name = obj["Name"] as string;
                var classGuid = obj["ClassGuid"] as string;
                var devicePass = obj["DeviceID"] as string;

                if (classGuid != null && devicePass != null)
                {
                    if (string.Equals(classGuid, "{4d36e978-e325-11ce-bfc1-08002be10318}", StringComparison.InvariantCulture))
                    {
                        string[] tokens = devicePass.Split('&');
                        if (tokens.Length < 5)
                        {
                            continue;
                        }
                        string[] addressToken = tokens[4].Split('_');
                        string bluetoothAddress = addressToken[0];

                        var m = regexPortName.Match(name);
                        if (!m.Success)
                        {
                            continue;
                        }
                        var comPortNumber = m.Groups[1].ToString();

                        if (Convert.ToUInt64(bluetoothAddress, 16) <= 0)
                        {
                            continue;
                        }
                        string bluetoothName = GetBluetoothRegistryName(bluetoothAddress);

                        ret_dict.Add(bluetoothName, comPortNumber);
                    }
                }
            }

            return ret_dict;
        }

        public async Task SetupAsync()
        {
            if (_status == RotorStatus.Initializing)
            {
                return;
            }

            _status = RotorStatus.Initializing;

            SerialPort port;

            string[] ports = SerialPort.GetPortNames();

            // For Debugging
            foreach (string p in ports)
            {
                System.Diagnostics.Debug.WriteLine(p);
            }

            if (ports.Length <= 0)
            {
                _status = RotorStatus.NotInitialized;
                return;
            }

            if (_port != null && _port.IsOpen)
            {
                try
                {
                    _port.Close();
                }
                catch (Exception)
                {
                    _status = RotorStatus.NotInitialized;
                    return;
                }
            }

            var selected_port_name = ports[0];

            var bluetoothNameToCOMPortNumber = GetBluetoothCOMPorts();

            foreach (var pair in bluetoothNameToCOMPortNumber)
            {
                if (pair.Key.IndexOf(_device_name) != -1)
                {
                    selected_port_name = pair.Value;
                    break;
                }
            }

            // for debugging
            System.Diagnostics.Debug.WriteLine(selected_port_name + " is selected.");

            port = new SerialPort
            {
                PortName = selected_port_name,
                BaudRate = 115200,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            while (true)
            {
                try
                {
                    port.Open();
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(1000);
                }
            }

            _port = port;
            _status = RotorStatus.Waiting;
        }

        public async Task SendMessage(byte[] data, bool force)
        {
            if (_status != RotorStatus.Waiting && !force)
            {
                // drop the data
                System.Diagnostics.Debug.WriteLine("Dropped a packet.");
                return;
            }

            if (_port == null)
            {
                await SetupAsync();
                if (_port == null)
                {
                    return;
                }
            }

            if (!_port.IsOpen)
            {
                try
                {
                    _port.Open();
                }
                catch (Exception)
                {
                    return;
                }
            }

            _status = RotorStatus.Sending;

            byte num_rotor = (byte)data.Length;
            var send_data = new byte[19]; // zero cleared

            send_data[0] = 0xFA;
            send_data[1] = 0xAF;
            for (int i = 0; i < num_rotor; ++i)
            {
                send_data[2 + i] = data[i];
            }

            byte check_sum = 0;
            for (int i = 0; i < 18; ++i)
            {
                check_sum += send_data[i];
            }

            send_data[18] = check_sum;

            //var send_data = _port.Encoding.GetBytes(builder.ToString());
            await _port.BaseStream.WriteAsync(send_data, 0, send_data.Length);
            await _port.BaseStream.FlushAsync();
            await Task.Delay(_interval); // wait for 10 msec

            _status = RotorStatus.Waiting;
        }
    }
}
