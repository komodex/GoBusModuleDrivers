using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoGo;
using NetduinoGo;

namespace Komodex.NETMF.ModuleTestApp
{
    public class Program
    {
        public static void Main()
        {
            SevenSegmentDisplayDemo.Run();
        }

    }
}
