using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NAx25
{
    public static class KissFraming
    {
        const byte FEND = 0xc0;
        const byte FESC = 0xdb;
        const byte TFEND = 0xdc;
        const byte TFESC = 0xdd;

        public static byte[] Kiss(IEnumerable<byte> rawFrame, uint portIndex, KissCommandCode commandCode)
        {
            if (portIndex > 15)
            {
                throw new ArgumentException();
            }

            // escape special characters
            // pack second byte - first nibble is port index, second nibble is command code
            // add FEND to start and end

            var bytes = new List<byte>();

            bytes.Add(FEND);

            // next four bits is port index
            // next four bits is command code

            byte secondByte = (byte)(portIndex << 4 | (byte)commandCode);
            bytes.Add(secondByte);

            foreach (var b in rawFrame)
            {
                // If the FEND or FESC codes appear in the data to be transferred, they need to be escaped. 
                // The FEND code is then sent as FESC, TFEND and the FESC is then sent as FESC, TFESC.

                if (b == FEND)
                {
                    bytes.Add(FESC);
                    bytes.Add(TFEND);
                }
                else if (b == FESC)
                {
                    bytes.Add(FESC);
                    bytes.Add(TFESC);
                }
                else
                {
                    bytes.Add(b);
                }
            }

            bytes.Add(FEND);
            return bytes.ToArray();
        }

        public static (byte[] rawFrame, int portId, KissCommandCode commandCode) Unkiss(byte[] kissFrame)
        {
            // remove FEND from start
            // remove FEND from end
            // interpret second byte - first nibble is port index, second nibble is command code
            // unescape special characters in the right order

            var rawFrame = new List<byte>();

            if (kissFrame.Length < 3)
            {
                throw new ArgumentException("Not a KISS frame - too short");
            }

            if (kissFrame[0] != FEND)
            {
                throw new ArgumentException("Not a KISS frame - doesn't start with FEND");
            }

            var bitArray = new BitArray(new[] { kissFrame[1] });

            var portIndexBits = new[] { false, false, false, false, bitArray[7], bitArray[6], bitArray[5], bitArray[4] };
            var portIndex = portIndexBits.ConvertToByte();

            var commandBits = new[] { false, false, false, false, bitArray[3], bitArray[2], bitArray[1], bitArray[0] };
            var commandCodeByte = commandBits.ConvertToByte();

            KissCommandCode commandCode;
            if (commandCodeByte == 0x0f && portIndex == 0x0f)
            {
                commandCode = KissCommandCode.ExitKissMode;
            }
            else
            {
                commandCode = (KissCommandCode)commandCodeByte;
            }

            var withoutFends = kissFrame.Where(b => b != FEND);
            var escapedBytes = withoutFends.Skip(1).ToArray();

            if (commandCode >= KissCommandCode.TxDelay && commandCode <= KissCommandCode.FullDuplex)
            {
                if (escapedBytes.Length != 1)
                {
                    throw new ArgumentException($"Invalid KISS frame - command code {commandCodeByte} expects 1 data byte only, got {escapedBytes.Length}");
                }
            }
            else if (commandCode == KissCommandCode.ExitKissMode)
            {
                if (escapedBytes.Length != 0)
                {
                    throw new ArgumentException($"Invalid KISS frame - command code {commandCodeByte} expects zero data bytes, got {escapedBytes.Length}");
                }
            }

            // If the FEND or FESC codes appear in the data to be transferred, they need to be escaped. 
            // The FEND code is then sent as FESC, TFEND and the FESC is then sent as FESC, TFESC.

            for (int i = 0; i < escapedBytes.Length; i++)
            {
                var thisByte = escapedBytes[i];
                var nextByte = i == escapedBytes.Length - 1 ? 0 : escapedBytes[i + 1];

                if (thisByte == FESC && nextByte == TFEND)
                {
                    rawFrame.Add(FEND);
                    i++;
                }
                else if (thisByte == FESC && nextByte == TFESC)
                {
                    rawFrame.Add(FESC);
                    i++;
                }
                else
                {
                    rawFrame.Add(thisByte);
                }
            }

            return (rawFrame.ToArray(), portIndex, commandCode);
        }
    }

    public enum KissCommandCode : byte
    {
        DataFrame = 0x00,
        TxDelay = 0x01,
        Persistence = 0x02,
        SlotTime = 0x03,
        TxTail = 0x04,
        FullDuplex = 0x05,
        SetHardware = 0x06,
        ExitKissMode = 0xff
    }
}