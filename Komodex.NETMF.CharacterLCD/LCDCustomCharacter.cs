using System;
using Microsoft.SPOT;

namespace Komodex.NETMF
{
    public sealed class LCDCustomCharacter
    {
        public static readonly LCDCustomCharacter C1 = new LCDCustomCharacter(0, 8);
        public static readonly LCDCustomCharacter C2 = new LCDCustomCharacter(1, 9);
        public static readonly LCDCustomCharacter C3 = new LCDCustomCharacter(2, 10);
        public static readonly LCDCustomCharacter C4 = new LCDCustomCharacter(3, 11);
        public static readonly LCDCustomCharacter C5 = new LCDCustomCharacter(4, 12);
        public static readonly LCDCustomCharacter C6 = new LCDCustomCharacter(5, 13);
        public static readonly LCDCustomCharacter C7 = new LCDCustomCharacter(6, 14);
        public static readonly LCDCustomCharacter C8 = new LCDCustomCharacter(7, 15);

        internal byte Index;
        internal byte Character;

        private LCDCustomCharacter(byte index, byte value)
        {
            Index = index;
            Character = value;
        }

        public static implicit operator char(LCDCustomCharacter obj)
        {
            return (char)obj.Character;
        }

        public override string ToString()
        {
            return new string((char)Character, 1);
        }
        
    }
}
