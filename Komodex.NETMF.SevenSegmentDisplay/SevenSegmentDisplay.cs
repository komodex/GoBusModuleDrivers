using System;
using Microsoft.SPOT;
using GoBus;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace Komodex.NETMF
{
    public class SevenSegmentDisplay : GoModule
    {
        // Module parameters
        //protected readonly Guid _moduleGuid = new Guid(new byte[] { 0x80, 0x3E, 0x42, 0x53, 0xAC, 0x60, 0x1C, 0x4B, 0x89, 0x83, 0xE7, 0x75, 0xD9, 0x65, 0x3E, 0xE0 });
        private readonly Guid _moduleGuid = new Guid(new byte[] { 0x80, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
        private const int _frameLength = 18;

        // SPI interface
        private SPI _spi;
        private SPI.Configuration _spiConfig;

        // SPI data buffers
        private readonly byte[] _writeFrameBuffer = new byte[_frameLength];
        private readonly byte[] _readFrameBuffer = new byte[_frameLength];

        // IRQ management
        private InterruptPort _irqPort;
        private readonly AutoResetEvent _irqPortSignal = new AutoResetEvent(false);

        #region Constructors and Initialization

        public SevenSegmentDisplay()
        {
            // Look for a valid socket
            var compatibleSockets = GetSocketsByUniqueId(_moduleGuid);
            if (compatibleSockets.Length == 0)
                throw new Exception(); // TODO: Replace with a more specific exception

            Initialize(compatibleSockets[0]);
        }

        public SevenSegmentDisplay(GoSocket socket)
        {
            Initialize(socket);
        }

        private void Initialize(GoSocket socket)
        {
            // Attempt to bind the socket
            if (!BindSocket(socket, _moduleGuid))
                throw new ArgumentException(); // TODO: Replace with a more specific exception

            // Get socket resources
            Cpu.Pin socketGpioPin;
            SPI.SPI_module socketSpiModule;
            Cpu.Pin socketSpiSlaveSelectPin;
            socket.GetPhysicalResources(out socketGpioPin, out socketSpiModule, out socketSpiSlaveSelectPin);

            // SPI configuration
            _spiConfig = new SPI.Configuration(socketSpiSlaveSelectPin, false, 0, 0, false, false, 500, socketSpiModule);
            _spi = new SPI(_spiConfig);

            // IRQ configuration
            _irqPort = new InterruptPort(socketGpioPin, false, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeLow);
            _irqPort.OnInterrupt += new NativeEventHandler(_irqPort_OnInterrupt);
        }

        #endregion

        #region IRQ Port

        private void _irqPort_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            _irqPortSignal.Set();
        }

        #endregion

        #region Display Value

        public void SetValue(int value)
        {
            if (value > 9999 || value < -999)
                throw new ArgumentOutOfRangeException("value");


        }

        public void SetValue(decimal value)
        {
            if (value > 9999 || value < -999)
                throw new ArgumentOutOfRangeException("value");


        }

        public void SetValue(int d1, int d2, int d3, int d4)
        {
            if (d1 < 0 || d1 > 9)
                throw new ArgumentOutOfRangeException("d1");
            if (d2 < 0 || d2 > 9)
                throw new ArgumentOutOfRangeException("d2");
            if (d3 < 0 || d3 > 9)
                throw new ArgumentOutOfRangeException("d3");
            if (d4 < 0 || d4 > 9)
                throw new ArgumentOutOfRangeException("d4");


        }

        public void SetValue(Digit d1, Digit d2, Digit d3, Digit d4)
        {

        }

        private void GetValue()
        {

        }

        #endregion

        #region Display Brightness

        public void SetBrightness(float value)
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException("value");

            int brightness = (int)(value * 1023);
        }

        private void GetBrightness()
        {

        }

        #endregion

        #region Display Apostrophe

        public void SetApostrophe(bool value)
        {

        }

        private void GetApostrophe()
        {

        }

        #endregion

        #region Display Colon

        public void SetColon(bool value)
        {

        }

        private void GetColon()
        {

        }

        #endregion


    }
}
