using System;
using Microsoft.SPOT;

// Seven Segment Display Module Driver
// Matt Isenhower, Komodex Systems LLC
// http://komodex.com

namespace Komodex.NETMF
{
    public enum Digit
    {
        Blank = 0x00,
        D0 = 0x3F,
        D1 = 0x06,
        D2 = 0x5B,
        D3 = 0x4F,
        D4 = 0x66,
        D5 = 0x6D,
        D6 = 0x7D,
        D7 = 0x07,
        D8 = 0x7F,
        D9 = 0x6F,
        A = 0x77,
        B = 0x7C,
        C = 0x39,
        D = 0x5E,
        E = 0x79,
        F = 0x71,
        Decimal = 0x80,
        Dash = 0x40,
    }
}
