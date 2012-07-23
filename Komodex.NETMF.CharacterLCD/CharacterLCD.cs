using System;
using Microsoft.SPOT;
using GoBus;
using Microsoft.SPOT.Hardware;
using System.Threading;
using Komodex.NETMF.Common;
using System.Text;

// Character LCD Module Driver
// Matt Isenhower, Komodex Systems LLC
// http://komodex.com

namespace Komodex.NETMF
{
    public class CharacterLCD : GoModule
    {
        // Module parameters
        protected readonly Guid _moduleGuid = new Guid(new byte[] { 0x80, 0xD9, 0x0E, 0x6C, 0x5F, 0xE6, 0x54, 0x49, 0xB1, 0xAD, 0x9F, 0xEC, 0x61, 0x6C, 0xCB, 0xF7 });
        private const int _frameLength = 20;
        private const int _writeRetryCount = 36;
        private const int _readRetryCount = 4;

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

        private const byte CMD_COLOR = 0x01;
        private const byte CMD_LINE1 = 0x11;
        private const byte CMD_LINE2 = 0x12;

        #region Constructors and Initialization

        public CharacterLCD()
        {
            // Look for a valid socket
            var compatibleSockets = GetSocketsByUniqueId(_moduleGuid);
            if (compatibleSockets.Length == 0)
                throw new Exception(); // TODO: Replace with a more specific exception

            Initialize(compatibleSockets[0]);
        }

        public CharacterLCD(GoSocket socket)
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

        #region Color Methods

        public void SetColor(byte red, byte green, byte blue)
        {
            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_COLOR;
                _writeFrameBuffer[2] = red;
                _writeFrameBuffer[3] = green;
                _writeFrameBuffer[4] = blue;
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                byte verifyRed, verifyGreen, verifyBlue;
                if (GetColor(out verifyRed, out verifyGreen, out verifyBlue))
                {
                    if (red == verifyRed && green == verifyGreen && blue == verifyBlue)
                        success = true;
                }

            }
        }

        private bool GetColor(out byte red, out byte green, out byte blue)
        {
            red = 0;
            green = 0;
            blue = 0;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_COLOR;
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Wait for a response
                if (_irqPortSignal.WaitOne(3, false))
                {
                    // Request data from the module
                    ClearSPIWriteFrameBuffer();
                    _writeFrameBuffer[0] = 0x80;
                    CalculateCRC();
                    _spi.WriteRead(_writeFrameBuffer, _readFrameBuffer);

                    // Read the data
                    if (_readFrameBuffer[2] == (CMD_READ | CMD_COLOR))
                    {
                        red = _readFrameBuffer[3];
                        green = _readFrameBuffer[4];
                        blue = _readFrameBuffer[5];
                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

        #region Display Line 1 Methods

        public void SetLine1(string value)
        {
            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
            bool success = false;

            // Add spaces and truncate
            value += new string(' ', 16);
            value = value.Substring(0, 16);
            // Get bytes
            byte[] lineBytes = Encoding.UTF8.GetBytes(value);

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_LINE1;
                for (int i = 0; i < lineBytes.Length; i++)
                    _writeFrameBuffer[i + 2] = lineBytes[i];
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                string verifyLine;
                if (GetLine1(out verifyLine))
                {
                    if (value == verifyLine)
                        success = true;
                }

            }
        }

        private bool GetLine1(out string value)
        {
            value = null;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_LINE1;
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Wait for a response
                if (_irqPortSignal.WaitOne(3, false))
                {
                    // Request data from the module
                    ClearSPIWriteFrameBuffer();
                    _writeFrameBuffer[0] = 0x80;
                    CalculateCRC();
                    _spi.WriteRead(_writeFrameBuffer, _readFrameBuffer);

                    // Read the data
                    if (_readFrameBuffer[2] == (CMD_READ | CMD_LINE1))
                    {
                        char[] readChars = Encoding.UTF8.GetChars(_readFrameBuffer, 3, 16);
                        value = new string(readChars);
                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

        #region Display Line 2 Methods

        public void SetLine2(string value)
        {
            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
            bool success = false;

            // Add spaces and truncate
            value += new string(' ', 16);
            value = value.Substring(0, 16);
            // Get bytes
            byte[] lineBytes = Encoding.UTF8.GetBytes(value);

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_LINE2;
                for (int i = 0; i < lineBytes.Length; i++)
                    _writeFrameBuffer[i + 2] = lineBytes[i];
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                string verifyLine;
                if (GetLine2(out verifyLine))
                {
                    if (value == verifyLine)
                        success = true;
                }

            }
        }

        private bool GetLine2(out string value)
        {
            value = null;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_LINE2;
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Wait for a response
                if (_irqPortSignal.WaitOne(3, false))
                {
                    // Request data from the module
                    ClearSPIWriteFrameBuffer();
                    _writeFrameBuffer[0] = 0x80;
                    CalculateCRC();
                    _spi.WriteRead(_writeFrameBuffer, _readFrameBuffer);

                    // Read the data
                    if (_readFrameBuffer[2] == (CMD_READ | CMD_LINE2))
                    {
                        char[] readChars = Encoding.UTF8.GetChars(_readFrameBuffer, 3, 16);
                        value = new string(readChars);
                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

    }
}
