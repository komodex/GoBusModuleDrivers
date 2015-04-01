using System;
using Microsoft.SPOT;

// Seven Segment Display Utilities
// Matt Isenhower, Komodex Systems LLC
// http://komodex.com
// 
// This class contains some sample code for use with the Seven Segment Display Module.
// Feel free to modify this code or include it in your own programs.

namespace Komodex.NETMF
{
    public static class SevenSegmentDisplayUtilities
    {
        #region Temperature Display

        /// <summary>
        /// Sets the value on the display to the specified temperature value.
        /// </summary>
        /// <param name="display">The SevenSegmentDisplay instance to be updated.</param>
        /// <param name="temperature">The temperature to be displayed.</param>
        /// <param name="unit">The temperature unit to display (usually Digit.F or Digit.C).</param>
        /// <param name="showDecimal">true to display the temperature with a decimal point (e.g., "98.6"); otherwise, false (e.g., "98").</param>
        public static void SetTemperatureDisplay(this SevenSegmentDisplay display, double temperature, Digit unit, bool showDecimal = true)
        {
            // Check input
            if (temperature > 999 || temperature < -99)
                throw new ArgumentOutOfRangeException("temperature");

            bool isNegative = (temperature < 0);

            // Determine if we should show a decimal place
            bool isDecimal = false;
            if (showDecimal && temperature > -10 && temperature < 100)
            {
                temperature *= 10;
                isDecimal = true;
            }

            // Get the value to be displayed
            int value = System.Math.Abs((int)temperature);

            // Get the Digit values for each number
            Digit d1, d2, d3;
            d3 = SevenSegmentDisplay.GetDigit(value % 10);
            value /= 10;
            d2 = SevenSegmentDisplay.GetDigit(value % 10);
            value /= 10;
            d1 = SevenSegmentDisplay.GetDigit(value % 10);

            // Add the decimal point if necessary
            if (isDecimal)
                d2 |= Digit.Decimal;

            // Add the negative sign if necessary and clear leading zeros
            if (isNegative)
            {
                d1 = Digit.Dash;
                if (d2 == Digit.D0)
                    d2 = Digit.Blank;
            }
            else if (d1 == Digit.D0)
            {
                d1 = Digit.Blank;
                if (d2 == Digit.D0)
                    d2 = Digit.Blank;
            }

            // Send the value to the display
            display.SetValue(d1, d2, d3, unit);

            // Turn the apostrophe on (so it looks like a degree symbol) and make sure the colon is turned off
            display.SetApostrophe(true);
            display.SetColon(false);
        }

        #endregion

    }
}
