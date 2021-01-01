using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NAx25
{
    public static class FieldDecoding
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
        public static ControlFields DecodeControlByte(byte controlByte)
        {
            var controlBits = new BitArray(new[] { controlByte });

            var result = new ControlFields();

            static BitArray GetNrBits(BitArray controlBits) => new BitArray(new[] { controlBits[5], controlBits[6], controlBits[7], false, false, false, false, false });
            static BitArray GetNsBits(BitArray controlBits) => new BitArray(new[] { controlBits[1], controlBits[2], controlBits[3], false, false, false, false, false });

            if (controlBits[0] == true && controlBits[1] == true)
            {
                // U frame
                // see http://nic.vajn.icu/PDF/ham/AX25/ax25.html 2.3.4.3, these can be decoded further
                //result.ModifierBits = new BitArray(new[] { controlBits[2], controlBits[3], controlBits[5], controlBits[6], controlBits[7] });
                result.M7 = controlBits[7];
                result.M6 = controlBits[6];
                result.M5 = controlBits[5];
                result.M3 = controlBits[3];
                result.M2 = controlBits[2];
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

        public static byte EncodeControlField(FrameType frameType, bool pfBit, byte sendSequenceNumber = default, byte receiveSequenceNumber = default, bool m7 = default, bool m6 = default, bool m5 = default, bool m3 = default, bool m2 = default, SupervisoryControlFieldType supervisoryControlFieldType = default)
        {
            if (sendSequenceNumber > 0x07)
            {
                throw new ArgumentOutOfRangeException(nameof(sendSequenceNumber));
            }

            if (receiveSequenceNumber > 0x07)
            {
                throw new ArgumentOutOfRangeException(nameof(receiveSequenceNumber));
            }

            var ba = new BitArray(8);

            ba[4] = pfBit;

            if (frameType == FrameType.Information)
            {
                var nrBits = new BitArray(new[] { receiveSequenceNumber });
                ba[7] = nrBits[2];
                ba[6] = nrBits[1];
                ba[5] = nrBits[0];
                var nsBits = new BitArray(new[] { sendSequenceNumber });
                ba[3] = nsBits[2];
                ba[2] = nsBits[1];
                ba[1] = nsBits[0];
                ba[0] = false;
            }
            else if (frameType == FrameType.Supervisory)
            {
                var nrBits = new BitArray(new[] { receiveSequenceNumber });
                ba[7] = nrBits[2];
                ba[6] = nrBits[1];
                ba[5] = nrBits[0];
                ba[3] = supervisoryControlFieldType == SupervisoryControlFieldType.Reject;
                ba[2] = supervisoryControlFieldType == SupervisoryControlFieldType.ReceiveNotReady;
                ba[1] = false;
                ba[0] = true;
            }
            else if (frameType == FrameType.UnnumberedInformation)
            {
                ba[7] = m7;
                ba[6] = m6;
                ba[5] = m5;
                ba[3] = m3;
                ba[2] = m2;
                ba[1] = true;
                ba[0] = true;
            }

            byte result = ba.ConvertToByte();
            
            return result;
        }

        internal static IEnumerable<byte> EncodeFcs(ushort _)
        {
            return new byte[] { 0x00, 0x00 };
        }

        /// <summary>
        /// http://nic.vajn.icu/PDF/ham/AX25/ax25.html section 2.2.4 PID Field
        /// </summary>
        public static Protocol GetProtocolId(byte pidByte)
        {
            var ba = new BitArray(pidByte);

            if (ba[5] != ba[4])
            {
                return Protocol.Ax25Layer3Implemented;
            }

            return pidByte switch
            {
                0x01 => Protocol.Iso8208_CcittX25Plp,
                0x08 => Protocol.SegmentationFragment,
                0xc3 => Protocol.TexnetDatagramProtocol,
                0xc4 => Protocol.LinkQualityProtocol,
                0xca => Protocol.Appletalk,
                0xcb => Protocol.AppletalkArp,
                0xcc => Protocol.ArpaInternetProtocol,
                0xcd => Protocol.ArpaAddressResolution,
                0xcf => Protocol.NetRom,
                0xf0 => Protocol.NoLayer3Protocol,
                0xff => throw new ArgumentException("Escape PID not supported"),
                _ => throw new ArgumentException($"Unknown protocol id {pidByte.ToHexByte()}"),
            };
        }

        public static byte[] RemoveFlags(byte[] data)
        {
            const byte FLAG = 0x7e;

            bool inFrame = false;
            var frameResult = new List<byte>();
            for (int i = 0; i < data.Length; i++)
            {
                if (!inFrame)
                {
                    if (data[i] != FLAG)
                    {
                        inFrame = true;
                        frameResult.Add(data[i]);
                    }
                }
                else
                {
                    if (data[i] == FLAG && i == data.Length - 1)
                    {
                        return frameResult.ToArray();
                    }

                    frameResult.Add(data[i]);
                }
            }

            return frameResult.ToArray();
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

        public static SFrameType GetAx25V2SFrameType(byte[] addressBytes)
        {
            if (addressBytes.Length != 14)
            {
                throw new NotSupportedException($"Cannot work out S-frame type with {addressBytes.Length} address field bytes, only 14");
            }

            var bitArray = new BitArray(addressBytes);
            if (bitArray[14] == bitArray[7])
            {
                return NAx25.SFrameType.PreviousVersion;
            }

            if (bitArray[14] == true)
            {
                return NAx25.SFrameType.Command;
            }

            return NAx25.SFrameType.Response;
        }

        public static FrameType GetFrameType(byte control)
        {
            var bitArray = new BitArray(new[] { control });

            if (bitArray[0] && bitArray[1])
            {
                return FrameType.UnnumberedInformation;
            }
            else if (bitArray[0] && !bitArray[1])
            {
                return FrameType.Supervisory;
            }
            else if (!bitArray[0])
            {
                return FrameType.Information;
            }

            throw new ArgumentException($"Unknown frame type from {(bitArray[0] ? 1 : 0)},{(bitArray[1] ? 1 : 0)}", nameof(control));
        }

        public static ushort Crc16Ccitt(byte[] bytes)
        {
            // Required implementation: http://practicingelectronics.com/articles/article-100003/article.php

            // something I found: https://stackoverflow.com/a/34943214/17971
            
            // but this doesn't seem to match the AX.25 way of doing it
            // and I don't know what I'm doing

            const ushort poly = 0x1021; // 4129
            ushort[] table = new ushort[256];
            //ushort initialValue = 0x1d0f; // from SO comment;
            ushort initialValue = 0xffff; //original
            ushort temp, a;
            ushort crc = initialValue;

            for (int i = 0; i < table.Length; ++i)
            {
                temp = 0;
                a = (ushort)(i << 8);
                for (int j = 0; j < 8; ++j)
                {
                    if (((temp ^ a) & 0x8000) != 0)
                    {
                        temp = (ushort)((temp << 1) ^ poly);
                    }
                    else
                    {
                        temp <<= 1;
                    }
                    a <<= 1;
                }
                table[i] = temp;
            }

            for (int i = 0; i < bytes.Length; ++i)
            {
                crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
            }

            return crc;
        }
    }
}