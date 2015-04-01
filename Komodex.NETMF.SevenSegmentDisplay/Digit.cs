using System;
using Microsoft.SPOT;

// Seven Segment Display Module Driver
// Matt Isenhower, Komodex Systems LLC
// http://komodex.com

namespace Komodex.NETMF
{
    /// <summary>
    /// Contains Seven Segment Display representations of common digits.
    /// </summary>
    public enum Digit
    {
        /// <summary>
        /// Blank digit (all segments off).
        /// </summary>
        Blank = 0x00,
        /// <summary>
        /// Number: 0
        /// </summary>
        D0 = 0x3F,
        /// <summary>
        /// Number: 1
        /// </summary>
        D1 = 0x06,
        /// <summary>
        /// Number: 2
        /// </summary>
        D2 = 0x5B,
        /// <summary>
        /// Number: 3
        /// </summary>
        D3 = 0x4F,
        /// <summary>
        /// Number: 4
        /// </summary>
        D4 = 0x66,
        /// <summary>
        /// Number: 5
        /// </summary>
        D5 = 0x6D,
        /// <summary>
        /// Number: 6
        /// </summary>
        D6 = 0x7D,
        /// <summary>
        /// Number: 7
        /// </summary>
        D7 = 0x07,
        /// <summary>
        /// Number: 8
        /// </summary>
        D8 = 0x7F,
        /// <summary>
        /// Number: 9
        /// </summary>
        D9 = 0x6F,
        /// <summary>
        /// Hex number: A
        /// </summary>
        A = 0x77,
        /// <summary>
        /// Hex number: B
        /// </summary>
        B = 0x7C,
        /// <summary>
        /// Hex number: C
        /// </summary>
        C = 0x39,
        /// <summary>
        /// Hex number: D
        /// </summary>
        D = 0x5E,
        /// <summary>
        /// Hex number: E
        /// </summary>
        E = 0x79,
        /// <summary>
        /// Hex number: F
        /// </summary>
        F = 0x71,
        /// <summary>
        /// Decimal point (.).
        /// </summary>
        Decimal = 0x80,
        /// <summary>
        /// Dash character (-).
        /// </summary>
        Dash = 0x40,
        /// <summary>
        /// Underscore character (_).
        /// </summary>
        Underscore = 0x08,
    }
}
