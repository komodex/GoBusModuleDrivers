using System;
using Microsoft.SPOT;
using GoBus;
using Microsoft.SPOT.Hardware;
using System.Threading;
using Komodex.NETMF.Common;

// Seven Segment Display Module Driver
// Matt Isenhower, Komodex Systems LLC
// http://komodex.com

namespace Komodex.NETMF
{
    /// <summary>
    /// Module driver for the Komodex Labs Seven Segment Display module.
    /// </summary>
    public class SevenSegmentDisplay : GoModule
    {
        // Module parameters
        /// <summary>
        /// Unique GUID for this module.
        /// </summary>
        protected readonly Guid _moduleGuid = new Guid(new byte[] { 0x80, 0x3E, 0x42, 0x53, 0xAC, 0x60, 0x1C, 0x4B, 0x89, 0x83, 0xE7, 0x75, 0xD9, 0x65, 0x3E, 0xE0 });
        private const int _frameLength = 18;
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

        private const byte CMD_DISPLAYVALUE = 0x01;
        private const byte CMD_BRIGHTNESS = 0x02;
        private const byte CMD_COLON = 0x03;
        private const byte CMD_APOSTROPHE = 0x04;

        #region Constructors and Initialization

        /// <summary>
        /// Initializes a SevenSegmentDisplay connected to an automatically-detected socket.
        /// </summary>
        public SevenSegmentDisplay()
        {
            // Look for a valid socket
            var compatibleSockets = GetSocketsByUniqueId(_moduleGuid);
            if (compatibleSockets.Length == 0)
                throw new Exception(); // TODO: Replace with a more specific exception

            Initialize(compatibleSockets[0]);
        }

        /// <summary>
        /// Initializes a SevenSegmentDisplay connected to the specified GoSocket.
        /// </summary>
        /// <param name="socket">The GoSocket the display is connected to.</param>
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

        #region Resource Management

        /// <summary>
        /// Dispose
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up any managed code objects
                _irqPort.Dispose();
            }
            // Clean up any unmanaged code objects

            // Dispose of our base object
            base.Dispose(disposing);
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

        #region SetValue Methods

        /// <summary>
        /// Sets the value on the display to the specified number.
        /// </summary>
        /// <param name="value">The int to be displayed. Minimum: -999. Maximum: 9999.</param>
        /// <param name="leadingZeros">true to display leading zeros; otherwise, false.</param>
        public void SetValue(int value, bool leadingZeros = false)
        {
            if (value > 9999 || value < -999)
                throw new ArgumentOutOfRangeException("value");

            bool isNegative = (value < 0);
            value = System.Math.Abs(value);

            Digit d1, d2, d3, d4;

            IntToDigits(value, out d1, out d2, out d3, out d4);

            if (!leadingZeros)
                ClearLeadingZeros(ref d1, ref d2, ref d3, ref d4);

            if (isNegative)
                d1 = Digit.Dash;

            SetValue(d1, d2, d3, d4);
        }


        /// <summary>
        /// Sets the value on the display to the specified number.
        /// </summary>
        /// <param name="value">The double to be displayed. Minimum/maximum values depend on the number of decimal places to be displayed.</param>
        /// <param name="decimalPlaces">The number of decimal places to be displayed. Minimum: 0. Maximum: 3.</param>
        /// <param name="leadingZeros">true to display leading zeros; otherwise, false.</param>
        public void SetValue(double value, int decimalPlaces, bool leadingZeros = false)
        {
            if (decimalPlaces > 3 || decimalPlaces < 0)
                throw new ArgumentOutOfRangeException("decimalPlaces");

            int intValue;

            switch (decimalPlaces)
            {
                case 1:
                    intValue = (int)(value * 10);
                    break;
                case 2:
                    intValue = (int)(value * 100);
                    break;
                case 3:
                    intValue = (int)(value * 1000);
                    break;
                default:
                    intValue = (int)value;
                    break;
            }

            if (intValue > 9999 || intValue < -999)
                throw new ArgumentOutOfRangeException("value");

            bool isNegative = (intValue < 0);
            intValue = System.Math.Abs(intValue);

            Digit d1, d2, d3, d4;

            IntToDigits(intValue, out d1, out d2, out d3, out d4);

            switch (decimalPlaces)
            {
                case 0:
                    d4 |= Digit.Decimal;
                    break;
                case 1:
                    d3 |= Digit.Decimal;
                    break;
                case 2:
                    d2 |= Digit.Decimal;
                    break;
                case 3:
                    d1 |= Digit.Decimal;
                    break;
            }

            if (!leadingZeros)
                ClearLeadingZeros(ref d1, ref d2, ref d3, ref d4);

            if (isNegative)
            {
                if (d1 == (Digit.D0 | Digit.Decimal))
                    d1 = Digit.Dash | Digit.Decimal;
                else
                    d1 = Digit.Dash;
            }

            SetValue(d1, d2, d3, d4);
        }

        /// <summary>
        /// Sets the value on the display with four individual numbers.
        /// </summary>
        /// <param name="d1">The first int. Minimum: 0. Maximum: 9.</param>
        /// <param name="d2">The second int. Minimum: 0. Maximum: 9.</param>
        /// <param name="d3">The third int. Minimum: 0. Maximum: 9.</param>
        /// <param name="d4">The fourth int. Minimum: 0. Maximum: 9.</param>
        public void SetValue(int d1, int d2, int d3, int d4)
        {
            if (d1 < -1 || d1 > 9)
                throw new ArgumentOutOfRangeException("d1");
            if (d2 < -1 || d2 > 9)
                throw new ArgumentOutOfRangeException("d2");
            if (d3 < -1 || d3 > 9)
                throw new ArgumentOutOfRangeException("d3");
            if (d4 < -1 || d4 > 9)
                throw new ArgumentOutOfRangeException("d4");

            SetValue(GetDigit(d1), GetDigit(d2), GetDigit(d3), GetDigit(d4));
        }

        /// <summary>
        /// Sets the value on the display to the specified string.
        /// </summary>
        /// <param name="value">The string to be displayed. Up to four hexadecimal characters can be displayed. Decimal points are appended to the previous digit.</param>
        public void SetValue(string value)
        {
            char[] charValue = value.ToCharArray();

            Digit[] digits = new Digit[4];
            int pos = 0;
            Digit d;

            for (int i = 0; i < charValue.Length; i++)
            {
                d = GetDigit(charValue[i]);
                if (d == Digit.Decimal)
                {
                    // If we're not on the first digit and the previous digit doesn't already have a decimal point, rewind the position by 1
                    if (pos > 0 && (digits[pos - 1] & Digit.Decimal) == 0)
                        pos--;
                }

                // If we have too many digits, break
                if (pos >= digits.Length)
                    break;

                // Using an OR assign here in case we are adding a decimal point
                digits[pos] |= d;

                pos++;
            }

            SetValue(digits[0], digits[1], digits[2], digits[3]);
        }

        /// <summary>
        /// Sets the value on the display to the specified DateTime.
        /// </summary>
        /// <param name="value">The DateTime to be displayed.</param>
        /// <param name="show12HourTime">true to display the time in 12-hour mode; otherwise, false.</param>
        /// <param name="showPMIndicator">When displaying the time in 12-hour mode, true to use the last digit's decimal point as a PM indicator; false to disable PM indication. This parameter has no effect when displaying time in 24-hour mode.</param>
        public void SetValue(DateTime value, bool show12HourTime = true, bool showPMIndicator = true)
        {
            int displayValue = 0;
            bool is_pm = false;

            // Get the hour value
            if (show12HourTime)
            {
                if (value.Hour == 0)
                    displayValue = 1200;
                else if (value.Hour < 12)
                    displayValue = value.Hour * 100;
                else
                {
                    displayValue = (value.Hour - 12) * 100;
                    is_pm = true;
                }
            }
            else
            {
                displayValue = value.Hour * 100;
            }

            // Add the minutes
            displayValue += value.Minute;

            // Get the display digits
            Digit d1, d2, d3, d4;
            IntToDigits(displayValue, out d1, out d2, out d3, out d4);

            // If we're showing 12 hour time and the first digit is a zero, hide it
            if (show12HourTime && d1 == Digit.D0)
                d1 = Digit.Blank;

            // Add the decimal point PM indicator
            if (is_pm && showPMIndicator)
                d4 |= Digit.Decimal;

            // Send the value to the display
            SetValue(d1, d2, d3, d4);
        }

        /// <summary>
        /// Sets the value on the display to the specified TimeSpan.
        /// </summary>
        /// <param name="value">The TimeSpan to be displayed.</param>
        /// <param name="mode">The mode used to display the TimeSpan value.</param>
        public void SetValue(TimeSpan value, TimeSpanDisplayMode mode = TimeSpanDisplayMode.Automatic)
        {
            int displayValue = 0;

            if (mode == TimeSpanDisplayMode.Automatic)
            {
                // Automatically determine whether to show the TimeSpan value as MM:SS or HH:MM.
                // If the value is >= 1 hour or <= -1 hour, display as HH:MM.
                // If the value is negative we can only display up to -9:59, so automatically switch to HH:MM when minutes are <= -10.
                // Otherwise, display the value as MM:SS.
                if (value.Hours != 0 || value.Minutes <= -10)
                    mode = TimeSpanDisplayMode.HourMinute;
                else
                    mode = TimeSpanDisplayMode.MinuteSecond;
            }

            switch (mode)
            {
                case TimeSpanDisplayMode.HourMinute:
                    displayValue = (value.Hours * 100) + value.Minutes;
                    break;
                case TimeSpanDisplayMode.MinuteSecond:
                    displayValue = (value.Minutes * 100) + value.Seconds;
                    break;
            }

            SetValue(displayValue, true);
        }

        /// <summary>
        /// Clears the value on the display, leaving all digits blank.
        /// </summary>
        public void ClearValue()
        {
            SetValue(Digit.Blank, Digit.Blank, Digit.Blank, Digit.Blank);
        }

        /// <summary>
        /// Sets the value on the display with four individual Digits.
        /// </summary>
        /// <param name="d1">The first Digit.</param>
        /// <param name="d2">The second Digit.</param>
        /// <param name="d3">The third Digit.</param>
        /// <param name="d4">The fourth Digit.</param>
        public void SetValue(Digit d1, Digit d2, Digit d3, Digit d4)
        {
            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
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

                // Verify the data
                Digit verifyD1, verifyD2, verifyD3, verifyD4;
                if (GetValue(out verifyD1, out verifyD2, out verifyD3, out verifyD4))
                {
                    if (d1 == verifyD1 && d2 == verifyD2 && d3 == verifyD3 && d4 == verifyD4)
                        success = true;
                }

            }
        }

        // Retrieves the current value from the display (used to verify that the display received the intended value).
        private bool GetValue(out Digit d1, out Digit d2, out Digit d3, out Digit d4)
        {
            d1 = (Digit)(-1);
            d2 = (Digit)(-1);
            d3 = (Digit)(-1);
            d4 = (Digit)(-1);

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_DISPLAYVALUE;
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
                    if (_readFrameBuffer[2] == (CMD_READ | CMD_DISPLAYVALUE))
                    {
                        d1 = (Digit)_readFrameBuffer[3];
                        d2 = (Digit)_readFrameBuffer[4];
                        d3 = (Digit)_readFrameBuffer[5];
                        d4 = (Digit)_readFrameBuffer[6];
                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

        #region Display Brightness Methods

        /// <summary>
        /// Sets the display's brightness level.
        /// </summary>
        /// <param name="value">The decimal value to set the display's brightness level. Maximum: 1.0 (full brightness). Minimum: 0.0 (display off).</param>
        public void SetBrightness(double value)
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException("value");

            int brightness = (int)(value * 1023);

            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_BRIGHTNESS;
                _writeFrameBuffer[2] = (byte)(brightness >> 8);
                _writeFrameBuffer[3] = (byte)(brightness >> 0);
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                int verifyBrightness;
                if (GetBrightness(out verifyBrightness))
                {
                    if (brightness == verifyBrightness)
                        success = true;
                }
            }
        }

        // Retrieves the current brightness level from the display (used to verify that the display received the intended value).
        private bool GetBrightness(out int brightness)
        {
            brightness = -1;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_BRIGHTNESS;
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
                    if (_readFrameBuffer[2] == (CMD_READ | CMD_BRIGHTNESS))
                    {
                        brightness = (_readFrameBuffer[3] << 8);
                        brightness |= _readFrameBuffer[4];
                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

        #region Display Colon Methods

        /// <summary>
        /// Sets whether the colon should be displayed.
        /// </summary>
        /// <param name="value">true to display the colon; otherwise, false.</param>
        public void SetColon(bool value)
        {
            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_COLON;
                _writeFrameBuffer[2] = (value) ? (byte)1 : (byte)0;
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                bool verifyColon;
                if (GetColon(out verifyColon))
                {
                    if (value == verifyColon)
                        success = true;
                }
            }
        }

        // Retrieves the current colon state from the display (used to verify that the display received the intended value).
        private bool GetColon(out bool value)
        {
            value = false;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_COLON;
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
                    if (_readFrameBuffer[2] == (CMD_READ | CMD_COLON))
                    {
                        if (_readFrameBuffer[3] == 0)
                            value = false;
                        else
                            value = true;

                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

        #region Display Apostrophe Methods

        /// <summary>
        /// Sets whether the apostrophe (in the top-right corner) should be displayed.
        /// </summary>
        /// <param name="value">true to display the apostrophe; otherwise, false.</param>
        public void SetApostrophe(bool value)
        {
            ClearSPIWriteFrameBuffer();

            int retry = _writeRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_WRITE | CMD_APOSTROPHE;
                _writeFrameBuffer[2] = (value) ? (byte)1 : (byte)0;
                CalculateCRC();

                // Send the message
                _spi.Write(_writeFrameBuffer);

                // Verify the data
                bool verifyApostrophe;
                if (GetApostrophe(out verifyApostrophe))
                {
                    if (value == verifyApostrophe)
                        success = true;
                }
            }
        }

        // Retrieves the current apostrophe state from the display (used to verify that the display received the intended value).
        private bool GetApostrophe(out bool value)
        {
            value = false;

            ClearSPIWriteFrameBuffer();

            int retry = _readRetryCount;
            bool success = false;

            while (!success && (retry-- > 0))
            {
                // Set up the message
                _writeFrameBuffer[0] = 0x80;
                _writeFrameBuffer[1] = CMD_READ | CMD_APOSTROPHE;
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
                    if (_readFrameBuffer[2] == (CMD_READ | CMD_APOSTROPHE))
                    {
                        if (_readFrameBuffer[3] == 0)
                            value = false;
                        else
                            value = true;

                        success = true;
                    }
                }
            }

            return success;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Converts an int to a Digit.
        /// Range is 0-10, or -1 to return a blank Digit.
        /// </summary>
        /// <param name="value">The int value to convert.</param>
        /// <returns>The converted Digit value.</returns>
        public static Digit GetDigit(int value)
        {
            switch (value)
            {
                case -1:
                    return Digit.Blank;
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

        /// <summary>
        /// Converts a char to a digit (case-insensitive).
        /// Allowed values include '-', '_', '.', all numbers '0' through '9', and all hexadecimal digits 'A' through 'F'.
        /// </summary>
        /// <param name="value">The char value to convert.</param>
        /// <returns>The converted Digit value.</returns>
        public static Digit GetDigit(char value)
        {
            value = value.ToUpper();

            switch (value)
            {
                case '0':
                    return Digit.D0;
                case '1':
                    return Digit.D1;
                case '2':
                    return Digit.D2;
                case '3':
                    return Digit.D3;
                case '4':
                    return Digit.D4;
                case '5':
                    return Digit.D5;
                case '6':
                    return Digit.D6;
                case '7':
                    return Digit.D7;
                case '8':
                    return Digit.D8;
                case '9':
                    return Digit.D9;
                case 'A':
                    return Digit.A;
                case 'B':
                    return Digit.B;
                case 'C':
                    return Digit.C;
                case 'D':
                    return Digit.D;
                case 'E':
                    return Digit.E;
                case 'F':
                    return Digit.F;
                case '-':
                    return Digit.Dash;
                case '.':
                    return Digit.Decimal;
                case '_':
                    return Digit.Underscore;
            }

            return Digit.Blank;
        }

        /// <summary>
        /// Converts a Digit to an int value.
        /// </summary>
        /// <param name="value">The Digit value to convert.</param>
        /// <returns>The converted int value.</returns>
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

        /// <summary>
        /// Converts an integer value to four individual Digits.
        /// </summary>
        protected static void IntToDigits(int value, out Digit d1, out Digit d2, out Digit d3, out Digit d4)
        {
            d4 = GetDigit(value % 10);
            value /= 10;
            d3 = GetDigit(value % 10);
            value /= 10;
            d2 = GetDigit(value % 10);
            value /= 10;
            d1 = GetDigit(value % 10);
        }

        /// <summary>
        /// Clears leading zeros from the specified digit group.
        /// For example, "0012" becomes "12", with the first and second digits becoming blank.
        /// The fourth digit is always preserved, so "0000" will become "0".
        /// </summary>
        protected static void ClearLeadingZeros(ref Digit d1, ref Digit d2, ref Digit d3, ref Digit d4)
        {
            if (d1 == Digit.D0)
            {
                d1 = Digit.Blank;
                if (d2 == Digit.D0)
                {
                    d2 = Digit.Blank;
                    if (d3 == Digit.D0)
                    {
                        d3 = Digit.Blank;
                    }
                }
            }
        }

        #endregion

    }
}
