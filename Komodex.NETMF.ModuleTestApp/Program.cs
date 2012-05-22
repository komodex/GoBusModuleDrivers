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
            RgbLed statusLed = new RgbLed();
            statusLed.SetColor(0, 0, 25);

            SevenSegmentDisplay display = null;

            while (display == null)
            {
                try
                {
                    display = new SevenSegmentDisplay();
                }
                catch
                {
                    statusLed.SetColor(25, 0, 0);
                    Thread.Sleep(250);
                }
            }

            statusLed.SetColor(0, 25, 0);

            while (true)
            {
                display.SetValue(Digit.D3, Digit.None, Digit.None, Digit.None);
                Thread.Sleep(250);
                display.SetValue(Digit.D2, Digit.None, Digit.None, Digit.None);
                Thread.Sleep(250);
            }

        }
    }
}
