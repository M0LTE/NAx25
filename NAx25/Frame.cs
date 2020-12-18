using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NAx25
{
    public class ControlFields
    {
        public BitArray MBits { get; internal set; }
        public bool PFBit { get; internal set; }
    }

    public class Frame
    {
        /// <summary>
        /// http://nic.vajn.icu/PDF/ham/AX25/ax25.html#2.2.13
        /// </summary>
        public byte[] AddressBytes { get; set; }

        /// <summary>
        /// http://nic.vajn.icu/PDF/ham/AX25/ax25.html#2.3.2.1
        /// </summary>
        public byte ControlByte { get; set; }

        public ControlFields ControlFields { get => Decoding.DecodeControlByte(ControlByte); }

        private void SetControlFields(byte value)
        {
            throw new NotImplementedException();
        }

        public InformationFrameFields InformationFrameFields { get; set; }

        public UInt16 FcsField { get; set; }

        public FrameType FrameType { get; set; }

        public DestinationField DestinationAddress { get; }
        public ICollection<SourceField> SourceAddresses { get; set; }

        public SFrameType? SFrameType { get; set; }

        public Frame(byte[] data)
        {
            var frameWithoutFlags = data.Where(b => b != 0x7e).ToArray();

            byte[] theRest;
            (AddressBytes, theRest) = Decoding.ConsumeAddressField(frameWithoutFlags);
            (ControlByte, theRest) = Decoding.ConsumeByte(theRest);
            FrameType = GetFrameType(ControlByte);

            if (FrameType == FrameType.Information || FrameType == FrameType.UnnumberedInformation)
            {
                InformationFrameFields = new InformationFrameFields();
                (InformationFrameFields.ProtocolIdByte, theRest) = Decoding.ConsumeByte(theRest);
                (InformationFrameFields.InfoBytes, theRest) = Decoding.ConsumeInfoField(theRest);
            }

            if (FrameType == FrameType.Supervisory)
            {
                SFrameType = GetAx25V2SFrameType(AddressBytes);
            }

            (FcsField, theRest) = Decoding.ConsumeFcsField(theRest);

            if (theRest.Length > 0)
            {
                throw new Exception("Frame decoding error - excess bytes");
            }
        }

        private SFrameType GetAx25V2SFrameType(byte[] addressBytes)
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

        private FrameType GetFrameType(byte control)
        {
            var bitArray = new BitArray(control);

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
    }

    public class InformationFrameFields
    {
        public byte ProtocolIdByte { get; set; }
        public byte[] InfoBytes { get; set; }
    }

    public enum SFrameType
    {
        PreviousVersion, Command, Response
    }

    public enum FrameType
    {
        UnnumberedInformation, Information, Supervisory
    }
}