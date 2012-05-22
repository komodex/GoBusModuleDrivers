using System;
using Microsoft.SPOT;

namespace Komodex.NETMF
{
    public enum Digit
    {
        None = 0x00,
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
        Decimal = 0x80,
        Dash = 0x40,
    }
}
