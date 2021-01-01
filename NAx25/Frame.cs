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
        public ControlFields ControlFields { get; set; } = new ControlFields();
        public InformationFrameFields InformationFrameFields { get; set; }
        public SupervisoryFrameFields SupervisoryFrameFields { get; set; }

        /// <summary>
        /// http://practicingelectronics.com/articles/article-100003/article.php
        /// </summary>
        public UInt16 Fcs { get; private set; }

        public FrameType FrameType { get; set; }

        public AddressField DestinationAddress { get; set; }
        public IList<AddressField> SourceAddresses { get; set; }

        private const byte FLAG = 0x7e;

        public Frame()
        {
        }

        public Frame(FrameType frameType, AddressField destinationAddress, AddressField[] sourceAddresses, bool pollFinalBit = false, byte receiveSequenceNumber = 0, byte sendSequenceNumber = 0, Protocol protocolId = Protocol.NoLayer3Protocol, byte? protocolIdByte = null, byte[] informationFrameData = null)
        {
            FrameType = frameType;
            DestinationAddress = destinationAddress;
            SourceAddresses = sourceAddresses;
            ControlFields.PollFinalBit = pollFinalBit;
            ControlFields.ReceiveSequenceNumber = receiveSequenceNumber;
            ControlFields.SendSequenceNumber = sendSequenceNumber;
            if (frameType == FrameType.Information)
            {
                InformationFrameFields = new InformationFrameFields();

                if (protocolIdByte != null)
                {
                    InformationFrameFields.ProtocolIdByte = protocolIdByte.Value;
                }
                else
                {
                    InformationFrameFields.ProtocolId = protocolId;
                }

                InformationFrameFields.InfoBytes = informationFrameData ?? Array.Empty<byte>();
            }
        }

        public byte[] ToBytes()
        {
            // U and S frame
            // flag, destination address, source addresses, control byte, fcs (16 bits), flag

            // I frame
            // flag, destination address, source addresses, control byte, pid byte, info bytes, fcs (16 bits), flag

            var bytes = new List<byte>();
            bytes.Add(FLAG);
            bytes.AddRange(DestinationAddress.ToBytes());
            bytes.AddRange(AddressField.SourceAddressesToBytes(SourceAddresses));
            bytes.Add(EncodeControlField());
            if (FrameType == FrameType.Information)
            {
                if (InformationFrameFields == null)
                {
                    throw new InvalidOperationException($"I frame does not have {nameof(InformationFrameFields)}");
                }

                var stdProtocol = (byte)InformationFrameFields.ProtocolId;
                bytes.Add(InformationFrameFields.ProtocolIdByte ?? stdProtocol);
                bytes.AddRange(InformationFrameFields.InfoBytes);
            }
            bytes.AddRange(FieldDecoding.EncodeFcs(CalculateFcs()));
            bytes.Add(FLAG);

            return bytes.ToArray();
        }

        private byte EncodeControlField() => 
            FieldDecoding.EncodeControlField(FrameType, ControlFields.PollFinalBit, ControlFields.SendSequenceNumber,
                                             ControlFields.ReceiveSequenceNumber, ControlFields.M7, ControlFields.M6,
                                             ControlFields.M5, ControlFields.M3, ControlFields.M2,
                                             ControlFields.SupervisoryControlFieldType ?? default);

        private ushort CalculateFcs()
        {
            // set FCS property, also return it
            ushort fcs = 0;
            Fcs = fcs;
            return fcs;
        }

        public static bool TryParse(byte[] data, out Frame frame)
        {
            var frameWithoutFlags = FieldDecoding.RemoveFlags(data);

            byte[] theRest;
            byte controlByte;
            byte[] addressFieldBytes;
            var result = new Frame();
            
            (addressFieldBytes, theRest) = FieldDecoding.ConsumeAddressField(frameWithoutFlags);
            result.DestinationAddress = new AddressField(addressFieldBytes.Take(7).ToArray());
            result.SourceAddresses = addressFieldBytes.Skip(7).Batch(7).Select(addressBytes => new AddressField(addressBytes.ToArray())).ToList();

            (controlByte, theRest) = FieldDecoding.ConsumeByte(theRest);
            result.ControlFields = FieldDecoding.DecodeControlByte(controlByte);
            result.FrameType = FieldDecoding.GetFrameType(controlByte);

            if (result.FrameType == FrameType.Information || result.FrameType == FrameType.UnnumberedInformation)
            {
                result.InformationFrameFields = new InformationFrameFields();
                byte protocolIdByte;
                (protocolIdByte, theRest) = FieldDecoding.ConsumeByte(theRest);
                result.InformationFrameFields.ProtocolId = FieldDecoding.GetProtocolId(protocolIdByte);
                result.InformationFrameFields.ProtocolIdByte = protocolIdByte;
                (result.InformationFrameFields.InfoBytes, theRest) = FieldDecoding.ConsumeInformationField(theRest);
            }
            else if (result.FrameType == FrameType.Supervisory)
            {
                result.SupervisoryFrameFields = new SupervisoryFrameFields();
                result.SupervisoryFrameFields.SFrameType = FieldDecoding.GetAx25V2SFrameType(addressFieldBytes);
            }

            (result.Fcs, theRest) = FieldDecoding.ConsumeFcsField(theRest);

            if (theRest.Length > 0)
            {
                throw new Exception($"Frame decoding error - {theRest.Length} excess bytes after FCS field");
            }

            frame = result;
            return true;
        }
    }

    public class InformationFrameFields
    {
        public byte? ProtocolIdByte { get; set; }
        public Protocol ProtocolId { get; set; }
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

    public enum Protocol : byte
    {
        Iso8208_CcittX25Plp = 0x01,
        SegmentationFragment = 0x08,
        Ax25Layer3Implemented,
        TexnetDatagramProtocol = 0xc3,
        LinkQualityProtocol = 0xc4,
        Appletalk = 0xca,
        AppletalkArp = 0xcb,
        ArpaInternetProtocol = 0xcc,
        ArpaAddressResolution = 0xcd,
        NetRom = 0xcf,
        NoLayer3Protocol = 0xf0,
    }
}