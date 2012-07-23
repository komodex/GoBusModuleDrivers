using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoGo;
using NetduinoGo;

// Module Test App
// Matt Isenhower, Komodex Systems LLC
// http://komodex.com

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
