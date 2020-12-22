using System;
using System.Collections;
using System.Linq;

namespace NAx25
{
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

        /// <summary>
        /// 2.3.2 Control-Field Formats and State Variables
        /// 2.3.2.1 Control-Field Formats
        /// The control field is responsible for identifying the type of frame being sent, and is also used to convey
        /// commands and responses from one end of the link to the other in order to maintain proper link control.
        /// The control fields used in AX.25 use the CCITT X.25 control fields for balanced operation (LAPB), with an
        /// additional control field taken from ADCCP to allow connectionless and round- table operation.
        /// There are three general types of AX.25 frames. They are the Information frame (I frame), the Supervisory
        /// frame (S frame), and the Unnumbered frame (U frame). Fig. 5 shows the basic format of the control field 
        /// associated with these types of frames.
        /// </summary>
        internal static ControlFields DecodeControlByte(byte controlByte)
        {
            var controlBits = new BitArray(new[] { controlByte });
            
            var result = new ControlFields();

            static BitArray GetNrBits(BitArray controlBits) => new BitArray(new[] { controlBits[5], controlBits[6], controlBits[7], false, false, false, false, false });
            static BitArray GetNsBits(BitArray controlBits) => new BitArray(new[] { controlBits[1], controlBits[2], controlBits[3], false, false, false, false, false });

            if (controlBits[0] == true && controlBits[1] == true)
            {
                // U frame
                // see http://nic.vajn.icu/PDF/ham/AX25/ax25.html 2.3.4.3, these can be decoded further
                result.ModifierBits = new BitArray(new[] { controlBits[2], controlBits[3], controlBits[5], controlBits[6], controlBits[7] });
            }
            else if (controlBits[0] == true && controlBits[1] == false)
            {
                // S frame
                result.ReceiveSequenceNumber = GetNrBits(controlBits).ConvertToByte();
                result.SupervisoryControlFieldType = GetSupervisoryControlFieldType(controlBits[3], controlBits[2], controlBits[1], controlBits[0]);
            }
            else if (controlBits[0] == false)
            {
                // I frame
                result.ReceiveSequenceNumber = GetNrBits(controlBits).ConvertToByte();
                result.SendSequenceNumber = GetNsBits(controlBits).ConvertToByte();
            }
            else
            {
                throw new Exception("Unknown frame type from control bytes");
            }

            result.PollFinalBit = controlBits[4];

            return result;
        }

        /// <summary>
        /// http://nic.vajn.icu/PDF/ham/AX25/ax25.html 2.3.4.2 Supervisory Frame Control Field
        /// </summary>
        private static SupervisoryControlFieldType GetSupervisoryControlFieldType(bool b3, bool b2, bool b1, bool b0)
        {
            if (b3 == false && b2 == false && b1 == false && b0 == true)
            {
                return SupervisoryControlFieldType.ReceiveReady;
            }
            else if (b3 == false && b2 == true && b1 == false && b0 == true)
            {
                return SupervisoryControlFieldType.ReceiveNotReady;
            }
            else if (b3 == true && b2 == false && b1 == false && b0 == true)
            {
                return SupervisoryControlFieldType.Reject;
            }
            else
            {
                throw new ArgumentException($"Unknown supervisory control field type from {(b3 ? "1" : "0")}{(b2 ? "1" : "0")}{(b1 ? "1" : "0")}{(b0 ? "1" : "0")}");
            }
        }

        public static (byte controlByte, byte[] theRest) ConsumeByte(byte[] data)
        {
            var controlByte = data[0];
            var theRest = data[1..^0];

            return (controlByte, theRest);
        }

        public static (byte[] addressField, byte[] theRest) ConsumeInformationField(byte[] data)
        {
            var informationField = data[0..^2];
            var theRest = data[^2..^0];
            return (informationField, theRest);
        }

        public static (UInt16 Fcs, byte[] anythingElse) ConsumeFcsField(byte[] data)
        {
            var theTwoBytes = data[0..2];

            var fcs = BitConverter.ToUInt16(theTwoBytes);

            var theRest = data.Skip(2).ToArray();

            return (fcs, theRest);
        }
    }

    public enum SupervisoryControlFieldType
    {
        /// <summary>
        /// RR
        /// </summary>
        ReceiveReady, 
        
        /// <summary>
        /// RNR
        /// </summary>
        ReceiveNotReady,
        
        /// <summary>
        /// REJ
        /// </summary>
        Reject
    }
}