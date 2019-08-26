using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Management;
using Microsoft.Win32;
using System.Windows.Forms;
using RotorController;

namespace VRCHapticsLite
{
    enum RotorPosition : int
    {
        MainPosition = 0,
    }

    class RotorPlayer
    {
        RotorController.RotorController _rotorController;

        public RotorPlayer()
        {
            _rotorController = new RotorController.RotorController("HakoDev_waki");
        }

        public async Task SetupAsync()
        {
            await _rotorController.SetupAsync();
        }

        public async Task SendMessage(RotorPosition pos, byte[] data, bool force)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = (byte)(data[i] * 255 / 100);
            }

            await _rotorController.SendMessage(data, force);
        }
    }
}
