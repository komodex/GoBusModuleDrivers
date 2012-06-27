using System;
using Microsoft.SPOT;

namespace Komodex.NETMF.Common
{
    // From the Netduino Go module drivers (via Reflector)

    internal static class CRC8
    {
        // Fields
        private static byte[] crcTab;

        // Methods
        public static byte Compute8(byte[] data, int index = 0, int count = -1)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }
            if (count == -1)
            {
                count = data.Length - index;
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            if ((data.Length - index) < count)
            {
                throw new ArgumentException();
            }
            if (crcTab == null)
            {
                crcTab = GenerateTable(7);
            }
            byte crc = 0;
            int endIndex = index + count;
            while (index < endIndex)
            {
                crc = crcTab[crc ^ data[index]];
                index++;
            }
            return crc;
        }

        public static byte[] GenerateTable(byte polynomial)
        {
            byte[] tab = new byte[0x100];
            for (int i = 0; i < 0x100; i++)
            {
                int curr = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((curr & 0x80) != 0)
                    {
                        curr = (curr << 1) ^ polynomial;
                    }
                    else
                    {
                        curr = curr << 1;
                    }
                }
                tab[i] = (byte)curr;
            }
            return tab;
        }
    }
}
