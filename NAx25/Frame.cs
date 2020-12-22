using MoreLinq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAx25
{
    public class Frame
    {
        /// <summary>
        /// http://nic.vajn.icu/PDF/ham/AX25/ax25.html#2.2.13
        /// </summary>
        public byte[] AddressFieldBytes { get; set; }

        /// <summary>
        /// http://nic.vajn.icu/PDF/ham/AX25/ax25.html#2.3.2.1
        /// </summary>
        public byte ControlByte { get; set; }

        public ControlFields ControlFields => Decoding.DecodeControlByte(ControlByte);

        public InformationFrameFields InformationFrameFields { get; set; }
        public SupervisoryFrameFields SupervisoryFrameFields { get; set; }

        /// <summary>
        /// http://practicingelectronics.com/articles/article-100003/article.php
        /// </summary>
        public UInt16 FcsField { get; set; }

        public FrameType FrameType { get; set; }

        public AddressField DestinationAddress => new AddressField(AddressFieldBytes.Take(7).ToArray());
        public IList<AddressField> SourceAddresses => AddressFieldBytes.Skip(7).Batch(7).Select(addressBytes => new AddressField(addressBytes.ToArray())).ToList();

        public static bool TryParse(byte[] data, out Frame frame)
        {
            var frameWithoutFlags = RemoveFlags(data);

            byte[] theRest;
            var result = new Frame();
            (result.AddressFieldBytes, theRest) = Decoding.ConsumeAddressField(frameWithoutFlags);
            (result.ControlByte, theRest) = Decoding.ConsumeByte(theRest);
            result.FrameType = GetFrameType(result.ControlByte);

            if (result.FrameType == FrameType.Information || result.FrameType == FrameType.UnnumberedInformation)
            {
                result.InformationFrameFields = new InformationFrameFields();
                (result.InformationFrameFields.ProtocolIdByte, theRest) = Decoding.ConsumeByte(theRest);
                (result.InformationFrameFields.InfoBytes, theRest) = Decoding.ConsumeInformationField(theRest);
            }
            else if (result.FrameType == FrameType.Supervisory)
            {
                result.SupervisoryFrameFields = new SupervisoryFrameFields();
                result.SupervisoryFrameFields.SFrameType = GetAx25V2SFrameType(result.AddressFieldBytes);
            }

            (result.FcsField, theRest) = Decoding.ConsumeFcsField(theRest);

            if (theRest.Length > 0)
            {
                throw new Exception($"Frame decoding error - {theRest.Length} excess bytes after FCS field");
            }

            frame = result;
            return true;
        }

        private static byte[] RemoveFlags(byte[] data)
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

        private static SFrameType GetAx25V2SFrameType(byte[] addressBytes)
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

        private static FrameType GetFrameType(byte control)
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
    }

    public class InformationFrameFields
    {
        public byte ProtocolIdByte { get; set; }
        public Protocol ProtocolId => GetProtocolId(ProtocolIdByte);

        /// <summary>
        /// http://nic.vajn.icu/PDF/ham/AX25/ax25.html section 2.2.4 PID Field
        /// </summary>
        private static Protocol GetProtocolId(byte pidByte)
        {
            var ba = new BitArray(pidByte);

            if (ba[5] != ba[4])
            {
                return Protocol.Ax25Layer3Implemented;
            }

            return pidByte switch
            {
                0x01 => Protocol.Iso8202_CcittX25Plp,
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

        public byte[] InfoBytes { get; set; }
        public string InfoText => Encoding.ASCII.GetString(InfoBytes);

        public override string ToString() => InfoText;
    }

    public class SupervisoryFrameFields
    {
        public SFrameType SFrameType { get; set; }
    }

    public enum SFrameType
    {
        PreviousVersion, Command, Response
    }

    public enum FrameType
    {
        UnnumberedInformation, Information, Supervisory
    }

    public enum Protocol
    {
        Iso8202_CcittX25Plp,
        SegmentationFragment,
        Ax25Layer3Implemented,
        TexnetDatagramProtocol,
        LinkQualityProtocol,
        Appletalk,
        AppletalkArp,
        ArpaInternetProtocol,
        ArpaAddressResolution,
        NetRom,
        NoLayer3Protocol,
    }
}