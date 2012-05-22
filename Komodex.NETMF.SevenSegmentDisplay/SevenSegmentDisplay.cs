using System;
using Microsoft.SPOT;
using GoBus;
using Microsoft.SPOT.Hardware;
using System.Threading;
using Komodex.NETMF.Common;

namespace Komodex.NETMF
{
    public class SevenSegmentDisplay : GoModule
    {
        // Module parameters
        protected readonly Guid _moduleGuid = new Guid(new byte[] { 0x80, 0x3E, 0x42, 0x53, 0xAC, 0x60, 0x1C, 0x4B, 0x89, 0x83, 0xE7, 0x75, 0xD9, 0x65, 0x3E, 0xE0 });
        private const int _frameLength = 18;
        private const int _retryCount = 36;

        // SPI interface
        private SPI _spi;
        private SPI.Configuration _spiConfig;

        // SPI data buffers
        private readonly byte[] _writeFrameBuffer = new byte[_frameLength];
        private readonly byte[] _readFrameBuffer = new byte[_frameLength];

        // IRQ management
        private InterruptPort _irqPort;
        private readonly AutoResetEvent _irqPortSignal = new AutoResetEvent(false);

        // Message IDs
        private const byte CMD_READ = (0 << 7);
        private const byte CMD_WRITE = (1 << 7);

        private const byte CMD_DISPLAYVALUE = 0x01;
        private const byte CMD_BRIGHTNESS = 0x02;
        private const byte CMD_COLON = 0x03;
        private const byte CMD_APOSTROPHE = 0x04;

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

        #region SPI Messages

        private void ClearSPIWriteFrameBuffer()
        {
            for (int i = 0; i < _writeFrameBuffer.Length; i++)
                _writeFrameBuffer[i] = 0;
        }

        private void CalculateCRC()
        {
            _writeFrameBuffer[_writeFrameBuffer.Length - 1] = CRC8.Compute8(_writeFrameBuffer, 0, _writeFrameBuffer.Length - 1);
        }

        #endregion

        #region Display Value Methods

        public void SetValue(int value, bool leadingZeros = false)
        {
            if (value > 9999 || value < -999)
                throw new ArgumentOutOfRangeException("value");

            bool isNegative = (value < 0);
            value = System.Math.Abs(value);

            Digit d1, d2, d3, d4;

            d4 = GetDigit(value % 10);
            value /= 10;
            d3 = GetDigit(value % 10);
            value /= 10;
            d2 = GetDigit(value % 10);
            value /= 10;
            d1 = GetDigit(value % 10);

            if (!leadingZeros)
                ClearLeadingZeros(ref d1, ref d2, ref d3, ref d4);

            if (isNegative)
                d1 = Digit.Dash;

            SetValue(d1, d2, d3, d4);
        }

        public void SetValue(float value, int decimalPlaces)
        {
            if (value > 9999 || value < -999)
                throw new ArgumentOutOfRangeException("value");

            if (decimalPlaces > 4 || decimalPlaces < 0)
                throw new ArgumentOutOfRangeException("decimalPlaces");

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
            ClearSPIWriteFrameBuffer();

            int retry = _retryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_DISPLAYVALUE;
                _writeFrameBuffer[2] = (byte)d1;
                _writeFrameBuffer[3] = (byte)d2;
                _writeFrameBuffer[4] = (byte)d3;
                _writeFrameBuffer[5] = (byte)d4;
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // TEMP
                success = true;
            }
        }

        private void GetValue()
        {

        }

        #endregion

        #region Display Brightness Methods

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

        #region Display Colon Methods

        public void SetColon(bool value)
        {

        }

        private void GetColon()
        {

        }

        #endregion

        #region Display Apostrophe Methods

        public void SetApostrophe(bool value)
        {

        }

        private void GetApostrophe()
        {

        }

        #endregion

        #region Utility Methods

        public static Digit GetDigit(int value)
        {
            switch (value)
            {
                case 0:
                    return Digit.D0;
                case 1:
                    return Digit.D1;
                case 2:
                    return Digit.D2;
                case 3:
                    return Digit.D3;
                case 4:
                    return Digit.D4;
                case 5:
                    return Digit.D5;
                case 6:
                    return Digit.D6;
                case 7:
                    return Digit.D7;
                case 8:
                    return Digit.D8;
                case 9:
                    return Digit.D9;
                default:
                    throw new ArgumentOutOfRangeException("value");
            }
        }

        public static int GetInt(Digit value)
        {
            switch (value)
            {
                case Digit.D0:
                    return 0;
                case Digit.D1:
                    return 1;
                case Digit.D2:
                    return 2;
                case Digit.D3:
                    return 3;
                case Digit.D4:
                    return 4;
                case Digit.D5:
                    return 5;
                case Digit.D6:
                    return 6;
                case Digit.D7:
                    return 7;
                case Digit.D8:
                    return 8;
                case Digit.D9:
                    return 9;
                default:
                    throw new ArgumentOutOfRangeException("value");
            }
        }

        private void ClearLeadingZeros(ref Digit d1, ref Digit d2, ref Digit d3, ref Digit d4)
        {
            if (d1 == Digit.D0)
            {
                d1 = Digit.None;
                if (d2 == Digit.D0)
                {
                    d2 = Digit.None;
                    if (d3 == Digit.D0)
                    {
                        d3 = Digit.None;
                    }
                }
            }
        }

        #endregion

    }
}
