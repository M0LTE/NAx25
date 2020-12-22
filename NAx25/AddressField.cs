using System;
using System.Collections;

namespace NAx25
{
    public class AddressField
    {
        public AddressField(byte[] data)
        {
            if (data.Length != 7)
            {
                throw new ArgumentException($"Invalid address data, expected 7 bytes, got {data.Length}");
            }

            for (int i = 0; i < 6; i++)
            {
                // The first (low-order or bit 0) bit of each octet is the HDLC address extension bit, 
                // which is set to zero on all but the last octet in the address field, where it is set to one.

                var b = data[i] >> 1;

                Callsign += (char)b;
            }

            Callsign = Callsign.Trim();

            var ba = new BitArray(new[] { data[6] });

            CommandResponseBit = ba[7];
            ReservedBit1 = ba[6];
            ReservedBit2 = ba[5];
            Ssid = new[] { ba[4], ba[3], ba[2], ba[1] }.ConvertToByte();
            IsLastAddress = ba[0];
        }

        public string Callsign { get; set; }
        public byte Ssid { get; set; }
        public bool CommandResponseBit { get; set; }
        public bool ReservedBit1 { get; set; }
        public bool ReservedBit2 { get; set; }
        public bool IsLastAddress { get; set; }

        public override string ToString() => Ssid == 0 ? Callsign : $"{Callsign}-{Ssid}";
    }
}