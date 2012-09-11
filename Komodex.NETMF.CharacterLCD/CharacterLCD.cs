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
        private const int _frameLength = 24;
        private const int _writeRetryCount = 36;
        private const int _readRetryCount = 4;

        // LCD parameters
        private readonly int _cols;
        private readonly int _rows;

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
        private const byte CMD_RAW = 0x02;
        private const byte CMD_LINE = 0x03;
        private const byte CMD_CUSTOMCHAR = 0x04;

        // Brightness and color
        private byte _red = 255;
        private byte _green = 255;
        private byte _blue = 255;
        private double _brightness = 1.0;

        #region Constructors and Initialization

        public CharacterLCD(int cols = 16, int rows = 2)
        {
            // Look for a valid socket
            var compatibleSockets = GetSocketsByUniqueId(_moduleGuid);
            if (compatibleSockets.Length == 0)
                throw new Exception(); // TODO: Replace with a more specific exception

            _cols = cols;
            _rows = rows;

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

            // Default backlight color
            UpdateColor();
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

        #region Brightness/Color Methods

        public void SetBrightness (double value)
        {
            _brightness = value;

            UpdateColor();
        }

        public void SetColor(byte red, byte green, byte blue)
        {
            _red = red;
            _green = green;
            _blue = blue;

            UpdateColor();
        }

        private void UpdateColor()
        {
            // Get actual brightness levels for each color
            byte red = (byte)((double)_red * _brightness);
            byte green = (byte)((double)_green * _brightness);
            byte blue = (byte)((double)_blue * _brightness);

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

        #region Raw Commands/Data

        public void WriteCommand(byte value)
        {
            WriteRaw(false, value);
        }

        public void WriteData(byte value)
        {
            WriteRaw(true, value);
        }

        private static byte _rawMessageID = 0;

        private void WriteRaw(bool data, byte value)
        {
            ClearSPIWriteFrameBuffer();

            byte messageID = _rawMessageID++;
            byte type = (byte)((data) ? 0x01 : 0x00);

            int retry = _writeRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_RAW;
                _writeFrameBuffer[2] = messageID;
                _writeFrameBuffer[3] = type;
                _writeFrameBuffer[4] = value;
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                byte verifyID, verifyType, verifyValue;
                if (GetRaw(out verifyID, out verifyType, out verifyValue))
                {
                    if (messageID == verifyID && type == verifyType && value == verifyValue)
                        success = true;
                }
            }
        }

        private bool GetRaw(out byte messageID, out byte type, out byte value)
        {
            messageID = 0;
            type = 0;
            value = 0;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_RAW;
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
                    if (_readFrameBuffer[2] == (CMD_READ | CMD_RAW))
                    {
                        messageID = _readFrameBuffer[3];
                        type = _readFrameBuffer[4];
                        value = _readFrameBuffer[5];
                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

        #region Display SetLine Methods

        public void SetLine1(string value, LineAlignment alignment = LineAlignment.Left)
        {
            SetLine(1, value, alignment);
        }

        public void SetLine2(string value, LineAlignment alignment = LineAlignment.Left)
        {
            SetLine(2, value, alignment);
        }

        public void SetLine(int line, string value, LineAlignment alignment = LineAlignment.Left)
        {
            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
            bool success = false;

            // Add spaces and truncate if necessary
            if (value.Length > _cols)
                value = value.Substring(0, _cols);

            switch (alignment)
            {
                case LineAlignment.Left:
                    // Do nothing
                    break;
                case LineAlignment.Center:
                    value = new string(' ', (_cols - value.Length) / 2) + value;
                    break;
                case LineAlignment.Right:
                    value = new string(' ', (_cols - value.Length)) + value;
                    break;
            }

            if (value.Length < _cols)
                value += new string(' ', (_cols - value.Length));

            // Get bytes
            byte[] lineBytes = Encoding.UTF8.GetBytes(value);

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_LINE;
                _writeFrameBuffer[2] = (byte)line;
                for (int i = 0; i < 20 && i < lineBytes.Length; i++)
                    _writeFrameBuffer[i + 3] = lineBytes[i];
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                string verifyValue;
                if (GetLine(line, out verifyValue))
                {
                    if (value == verifyValue)
                        success = true;
                }

            }
        }

        private bool GetLine(int line, out string value)
        {
            value = null;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_LINE;
                _writeFrameBuffer[2] = (byte)line;
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
                    if (_readFrameBuffer[1] == (CMD_READ | CMD_LINE) && _readFrameBuffer[2] == line)
                    {
                        char[] readChars = Encoding.UTF8.GetChars(_readFrameBuffer, 3, 20);
                        value = new string(readChars);
                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

        #region Custom Character Methods

        public void SetCustomCharacter(LCDCustomCharacter character, params byte[] values)
        {
            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
            bool success = false;

            byte[] charValues = new byte[8];
            for (int i = 0; i < values.Length && i < charValues.Length; i++)
                charValues[i] = values[i];

            int valueCount = values.Length;
            if (valueCount > 8)
                valueCount = 8;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_CUSTOMCHAR;
                _writeFrameBuffer[2] = character.Index;
                for (int i = 0; i < 8; i++)
                    _writeFrameBuffer[3 + i] = charValues[i];
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                byte[] verifyValues;
                if (GetCustomCharacter(character, out verifyValues))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (charValues[i] == verifyValues[i])
                            success = true;
                        else
                        {
                            success = false;
                            break;
                        }
                    }
                }

            }
        }

        private bool GetCustomCharacter(LCDCustomCharacter character, out byte[] values)
        {
            values = null;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_CUSTOMCHAR;
                _writeFrameBuffer[2] = character.Index;
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
                    if (_readFrameBuffer[1] == (CMD_READ | CMD_CUSTOMCHAR) && _readFrameBuffer[2] == character.Index)
                    {
                        values = new byte[8];
                        for (int i = 0; i < 8; i++)
                            values[i] = _readFrameBuffer[3 + i];
                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

    }
}
