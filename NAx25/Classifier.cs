using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NAx25
{
    public class Classifier
    {
    }

    internal static class Decoding
    {
        public static (byte[] addressField, byte[] theRest) ConsumeAddressField(byte[] data)
        {
            // there's an optimisation built into the protocol
            // if the first bit of the last byte of each address is 1 then it's the last address field
            // our goal here isn't to decode the field, just to know how long it is

            int addressFieldLength = 0;
            for (int i = 0; i < data.Length; i += 7)
            {
                var addressFieldBytes = data[i..(i + 7)];
                var lastByte = addressFieldBytes[^1];
                var bitArray = new BitArray(new[] { lastByte });
                if (bitArray[0] == true)
                {
                    // is last address field
                    addressFieldLength = i + 7;
                    break;
                }
            }

            var addressField = data[0..(addressFieldLength)];
            var theRest = data[addressFieldLength..^0];
            return (addressField, theRest);
        }

        internal static ControlFields DecodeControlByte(byte controlByte)
        {
            var controlBits = new BitArray(controlByte);
            
            var result = new ControlFields();

            if (controlBits[0] == true && controlBits[1] == true)
            {
                // U frame
                result.MBits = new BitArray(new[] { controlBits[7], controlBits[6], controlBits[5], controlBits[3], controlBits[2] });
            }
            else if (controlBits[0] == true && controlBits[1] == false)
            {
                // S frame
            }
            else if (controlBits[0] == false)
            {
                // I frame
                var nrBits = new[] { false, controlBits[7], controlBits[6], controlBits[5] };

                result.Nr = BitConverter.ToUInt16(nrBits);
            }
            else
            {
                throw new Exception("Unknown frame type from control bytes");
            }

            result.PFBit = controlBits[4];
        }

        public static (byte controlByte, byte[] theRest) ConsumeByte(byte[] data)
        {
            var controlByte = data[0];
            var theRest = data[1..^0];

            return (controlByte, theRest);
        }

        public static (UInt16 Fcs, byte[] anythingElse) ConsumeFcsField(byte[] data)
        {
            var theTwoBytes = data[0..2];

            var fcs = BitConverter.ToUInt16(theTwoBytes);

            var theRest = data.Skip(2).ToArray();

            return (fcs, theRest);
        }

        internal static (byte[] infoField, byte[] theRest) ConsumeInfoField(byte[] theRest)
        {
            throw new NotImplementedException();
        }
    }

    public class SourceField
    {
        public string Callsign { get; set; }
        public byte Ssid { get; set; }
    }

    public class DestinationField
    {
        public DestinationField(byte[] data)
        {
            for (int i = 0; i < 6; i++)
            {

            }
        }

        public string Callsign { get; set; }
        public byte Ssid { get; set; }
        public bool CommandResponseBit { get; set; }
        public bool ReservedBit1 { get; set; }
        public bool ReservedBit2 { get; set; }
        public bool IsLastAddress { get; set; }
    }


}