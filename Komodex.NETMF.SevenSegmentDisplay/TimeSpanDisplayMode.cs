using System;
using Microsoft.SPOT;

// Seven Segment Display Module Driver
// Matt Isenhower, Komodex Systems LLC
// http://komodex.com

namespace Komodex.NETMF
{
    /// <summary>
    /// Specifies the mode to be used when displaying a TimeSpan value.
    /// </summary>
    public enum TimeSpanDisplayMode
    {
        /// <summary>
        /// Automatically determine how to display the TimeSpan value.
        /// If the value is greater than or equal to 1 hour (or if the value is less than or equal to -10 minutes), it will be displayed as HH:MM; otherwise the value will be displayed as MM:SS.
        /// </summary>
        Automatic,

        /// <summary>
        /// Display the TimeSpan value as HH:MM.
        /// </summary>
        HourMinute,

        /// <summary>
        /// Display the TimeSpan value as MM:SS.
        /// </summary>
        MinuteSecond,
    }
}
