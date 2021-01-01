using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NAx25
{
    public class AddressField
    {
        public AddressField(string callsign, byte ssid = 0, bool isLastAddress = false, bool commandResponseBit = false, bool reservedBit1 = true, bool reservedBit2 = true)
        {
            Callsign = callsign;
            Ssid = ssid;
            IsLastAddress = isLastAddress;
            CommandResponseBit = commandResponseBit;
            ReservedBit1 = reservedBit1;
            ReservedBit2 = reservedBit2;
        }

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

        public IEnumerable<byte> ToBytes()
        {
            var paddedCall = ValidateAndPadCallsign();

            var result = new List<byte>();
            foreach (char c in paddedCall)
            {
                int i = c << 1;
                result.Add((byte)i);
            }

            var ba = new BitArray(8);

            ba[7] = CommandResponseBit;
            ba[6] = ReservedBit1;
            ba[5] = ReservedBit2;

            var ssidBits = new BitArray(new[] { Ssid });
            ba[4] = ssidBits[3];
            ba[3] = ssidBits[2];
            ba[2] = ssidBits[1];
            ba[1] = ssidBits[0];

            ba[0] = IsLastAddress;

            var seventh = ba.ConvertToByte();
            result.Add(seventh);

            return result;
        }

        private string ValidateAndPadCallsign()
        {
            if (Callsign == null || Callsign.Trim().Length > 6)
            {
                throw new ArgumentException("Callsign is null or too long");
            }

            var callsign = Callsign.Trim();

            var spacesRequired = 6 - callsign.Length;

            return callsign + new string(' ', spacesRequired);
        }

        internal static IEnumerable<byte> SourceAddressesToBytes(IList<AddressField> sourceAddresses)
        {
            if (!sourceAddresses.Any())
            {
                throw new InvalidOperationException("No source addresses present, at least one is required");
            }

            EnsureLastAddressHasBitSet(sourceAddresses);

            var result = new List<byte>();
            foreach (var af in sourceAddresses)
            {
                var afBytes = af.ToBytes();
                result.AddRange(afBytes);
            }
            return result;
        }

        private static void EnsureLastAddressHasBitSet(IList<AddressField> sourceAddresses)
        {
            var last = sourceAddresses.Last();

            if (!last.IsLastAddress)
            {
                last.IsLastAddress = true;
            }
        }
    }
}