using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NAx25
{
    public static class Kiss
    {
        public static (byte[] data, int portId, CommandCode commandCode) Unkiss(byte[] kissFrame)
        {
            const byte FEND = 0xc0;
            const byte FESC = 0xdb;
            const byte TFEND = 0xdc;
            const byte TFESC = 0xdd;

            // remove FEND from start
            // remove FEND from end
            // interpret second byte - first nibble is port index, second nibble is command code
            // unescape special characters in the right order

            var data = new List<byte>();

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

            var commandBits = new[] { false, false, false, false, bitArray[3], bitArray[2], bitArray[1], bitArray[0]};
            var commandCodeByte = commandBits.ConvertToByte();

            CommandCode commandCode;
            if (commandCodeByte == 0x0f && portIndex == 0x0f)
            {
                commandCode = CommandCode.ExitKissMode;
            }
            else
            {
                commandCode = (CommandCode)commandCodeByte;
            }

            var withoutFends = kissFrame.Where(b => b != FEND);
            var escapedBytes = withoutFends.Skip(1).ToArray();

            if (commandCode >= CommandCode.TxDelay && commandCode <= CommandCode.FullDuplex)
            {
                if (escapedBytes.Length != 1)
                {
                    throw new ArgumentException($"Invalid KISS frame - command code {commandCodeByte} expects 1 data byte only, got {escapedBytes.Length}");
                }
            }
            else if (commandCode == CommandCode.ExitKissMode)
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
                    data.Add(FEND);
                    i++;
                }
                else if (thisByte == FESC && nextByte == TFESC)
                {
                    data.Add(FESC);
                    i++;
                }
                else
                {
                    data.Add(thisByte);
                }
            }

            return (data.ToArray(), portIndex, commandCode);
        }

        public enum CommandCode : byte
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
}
