using FluentAssertions;
using System;
using System.Collections;
using System.Linq;
using Xunit;

namespace NAx25.Tests
{
    public class FrameTests
    {
        // fig 3a http://nic.vajn.icu/PDF/ham/AX25/ax25.html

        private static readonly byte[] testData = new byte[] {
                0x7e, // 0  flag
                0x96, // 1  A1 = K
                0x70, // 2  A2 = 8
                0x9a, // 3  A3 = M
                0x9a, // 4  A4 = M
                0x9e, // 5  A5 = O
                0x40, // 6  A6 = space
                0xe0, // 7  A7 ssid = 11100000 = 0
                0xae, // 8  A8 = W
                0x84, // 9  A9 = B
                0x68, // 10 A10 = 4
                0x94, // 11 A11 = J
                0x8c, // 12 A12 = F
                0x92, // 13 A13 = I
                0x61, // 14 A14 ssid = 01100001 = 0 (last address)
                0x3e, // 15 Control = 00111110 = I
                0xf0, // 16 PID = 11110000 = none
                0x00, // 17 FCS1
                0x00, // 18 FCS2
                0x7e, // 19 flag
            };

        /* The frame shown is an I frame, 
         * not going through a level 2 repeater, 
         * from WB4JFI (SSID=0) 
         * to K8MMO (SSID=0), 
         * with no level 3 protocol. 
         * The P/F bit is set; 
         * the receive sequence number (N(R)) is 1; 
         * the send sequence number (N(S)) is 7.
         */

        [Fact]
        public void Parse_NonrepeaterAx25FrameDecoding()
        {
            Frame.TryParse(testData, out var iframe).Should().BeTrue();

            //iframe.AddressFieldBytes.Should().HaveCount(14); // from WB4JFI (SSID=0) to K8MMO(SSID = 0), not going through a level 2 repeater, 
            iframe.DestinationAddress.Callsign.Should().Be("K8MMO");
            iframe.DestinationAddress.Ssid.Should().Be(0);
            iframe.DestinationAddress.CommandResponseBit.Should().Be(true); // not validated!
            iframe.SourceAddresses.Should().HaveCount(1);
            iframe.SourceAddresses.First().Callsign.Should().Be("WB4JFI");
            iframe.SourceAddresses.First().Ssid.Should().Be(0);
            //iframe.ControlByte.Should().Be(0x3e); // The P/F bit is set; the receive sequence number (N(R)) is 1; the send sequence number (N(S)) is 7.
            iframe.ControlFields.PollFinalBit.Should().BeTrue();
            iframe.ControlFields.ReceiveSequenceNumber.Should().Be(1);
            iframe.ControlFields.SendSequenceNumber.Should().Be(7);
            iframe.FrameType.Should().Be(FrameType.Information); // The frame shown is an I frame, 
            iframe.InformationFrameFields.Should().NotBeNull();
            iframe.InformationFrameFields.ProtocolId.Should().Be(Protocol.NoLayer3Protocol); //  with no level 3 protocol. 
            iframe.InformationFrameFields.InfoBytes.Should().BeEmpty();
            iframe.Fcs.Should().Be(0);
        }

        [Fact]
        public void Encode_Destination_Address()
        {
            var af = new AddressField("WB4JFI", ssid: 15, commandResponseBit: false);
            var bytes = af.ToBytes();

            var expectedBytes = new byte[] {
                0xae, // 1  A1 = W
                0x84, // 2  A2 = B
                0x68, // 3  A3 = 4
                0x94, // 4  A4 = J
                0x8c, // 5  A5 = F
                0x92, // 6  A6 = I
                0x7e, // 7  C RR SSID 0
                      //    0 11 1111 0
            };

            bytes.Should().BeEquivalentTo(expectedBytes);
        }

        [Theory]
        [InlineData(false, 0x60)]
        [InlineData(true, 0x61)]
        public void Encode_Destination_Address_last(bool isLastAddress, byte expectedSeventhByte)
        {
            var af = new AddressField("WB4JFI", ssid: 0, commandResponseBit: false, isLastAddress: isLastAddress);
            var bytes = af.ToBytes();

            var expectedBytes = new byte[] {
                0xae, // 1  A1 = W
                0x84, // 2  A2 = B
                0x68, // 3  A3 = 4
                0x94, // 4  A4 = J
                0x8c, // 5  A5 = F
                0x92, // 6  A6 = I
                //0x7f, // 7  C RR SSID L
                        //    0 11 0000 0 false
                        //    0 11 0000 1 true
                expectedSeventhByte
            };

            bytes.Should().BeEquivalentTo(expectedBytes);
        }

        [Fact]
        public void Encode_Destination_Address_C()
        {
            var af = new AddressField("WB4JFI", ssid: 5, commandResponseBit: true);
            var bytes = af.ToBytes();

            var expectedBytes = new byte[] {
                0xae, // 1  A1 = W
                0x84, // 2  A2 = B
                0x68, // 3  A3 = 4
                0x94, // 4  A4 = J
                0x8c, // 5  A5 = F
                0x92, // 6  A6 = I
                0xea, // 7  C RR SSID 0
                      //    1 11 0101 0
            };

            bytes.Should().BeEquivalentTo(expectedBytes);
        }

        [Fact]
        public void Encode_Destination_Address_R1()
        {
            var af = new AddressField("WB4JFI", ssid: 5, commandResponseBit: true, reservedBit1: true, reservedBit2: false);
            var bytes = af.ToBytes();

            var expectedBytes = new byte[] {
                0xae, // 1  A1 = W
                0x84, // 2  A2 = B
                0x68, // 3  A3 = 4
                0x94, // 4  A4 = J
                0x8c, // 5  A5 = F
                0x92, // 6  A6 = I
                0xca, // 7  C RR SSID 0
                      //    1 10 0101 0
            };

            bytes.Should().BeEquivalentTo(expectedBytes);
        }

        [Fact]
        public void Encode_Destination_Address_R2()
        {
            var af = new AddressField("WB4JFI", ssid: 5, commandResponseBit: true, reservedBit1: false, reservedBit2: true);
            var bytes = af.ToBytes();

            var expectedBytes = new byte[] {
                0xae, // 1  A1 = W
                0x84, // 2  A2 = B
                0x68, // 3  A3 = 4
                0x94, // 4  A4 = J
                0x8c, // 5  A5 = F
                0x92, // 6  A6 = I
                0xaa, // 7  C RR SSID 0
                      //    1 01 0101 0
            };

            bytes.Should().BeEquivalentTo(expectedBytes);
        }

        [Fact]
        public void Produce_Test_Frame()
        {
            var frame = new Frame(
                frameType: FrameType.Information,
                destinationAddress: new AddressField("K8MMO", commandResponseBit: true),
                sourceAddresses: new[] { new AddressField("WB4JFI") },
                pollFinalBit: true,
                receiveSequenceNumber: 1,
                sendSequenceNumber: 7,
                protocolId: Protocol.NoLayer3Protocol);

            frame.DestinationAddress.IsLastAddress.Should().BeFalse();

            var bytes = frame.ToBytes();

            // byte 15
            // expected 0x3e 00111110, produced 0x00 00000000

            Frame.TryParse(bytes, out var parsed).Should().BeTrue();
            parsed.DestinationAddress.Callsign.Should().Be(frame.DestinationAddress.Callsign);
            parsed.DestinationAddress.CommandResponseBit.Should().Be(frame.DestinationAddress.CommandResponseBit);
            parsed.DestinationAddress.IsLastAddress.Should().Be(frame.DestinationAddress.IsLastAddress);
            parsed.DestinationAddress.ReservedBit1.Should().Be(frame.DestinationAddress.ReservedBit1);
            parsed.DestinationAddress.ReservedBit2.Should().Be(frame.DestinationAddress.ReservedBit2);
            parsed.DestinationAddress.Ssid.Should().Be(frame.DestinationAddress.Ssid);

            bytes.Should().BeEquivalentTo(testData);
        }

        [Fact]
        public void I_zero()
        {
            // NR = 000
            // P = 0
            // NS = 000
            // bit0 = 0

            FieldDecoding.EncodeControlField(FrameType.Information, pfBit: false, sendSequenceNumber: 0, receiveSequenceNumber: 0x00).Should().Be(0x00);
        }

        [Fact]
        public void I_NR_1()
        {
            // NR = 001
            // P = 0
            // NS = 000
            // bit0 = 0

            FieldDecoding.EncodeControlField(FrameType.Information, pfBit: false, sendSequenceNumber: 0, receiveSequenceNumber: 0x01).Should().Be(0x20);
        }

        [Fact]
        public void I_NR_max()
        {
            // NR = 111
            // P = 0
            // NS = 000
            // bit0 = 0

            FieldDecoding.EncodeControlField(FrameType.Information, pfBit: false, sendSequenceNumber: 0, receiveSequenceNumber: 0x07).Should().Be(0xe0);
        }

        [Fact]
        public void I_NR_overflow()
        {
            ((Action)(() => FieldDecoding.EncodeControlField(FrameType.Information, pfBit: false, sendSequenceNumber: 0, receiveSequenceNumber: 0x08))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void I_NS_1()
        {
            // NR = 000
            // P = 0
            // NS = 001
            // bit0 = 0

            FieldDecoding.EncodeControlField(FrameType.Information, pfBit: false, sendSequenceNumber: 0x01, receiveSequenceNumber: 0x00).Should().Be(0x02);
        }

        [Fact]
        public void I_NS_max()
        {
            // NR = 000
            // P = 0
            // NS = 111
            // bit0 = 0

            FieldDecoding.EncodeControlField(FrameType.Information, pfBit: false, sendSequenceNumber: 0x07, receiveSequenceNumber: 0x00).Should().Be(0x0e);
        }

        [Fact]
        public void I_NS_overflow()
        {
            ((Action)(() => FieldDecoding.EncodeControlField(FrameType.Information, pfBit: false, sendSequenceNumber: 0x08, receiveSequenceNumber: 0x00))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void I_pf_set()
        {
            // NR = 000
            // P = 1
            // NS = 000
            // bit0 = 0

            FieldDecoding.EncodeControlField(FrameType.Information, pfBit: true, sendSequenceNumber: 0x00, receiveSequenceNumber: 0x00).Should().Be(0x10);
        }

        [Fact]
        public void S_zero()
        {
            // NR = 000
            // P/F = 0
            // S3 = 0
            // S2 = 0
            // bit1,0 = 01

            FieldDecoding.EncodeControlField(FrameType.Supervisory, pfBit: false, receiveSequenceNumber: 0x00, supervisoryControlFieldType: SupervisoryControlFieldType.ReceiveReady).Should().Be(0x01);
        }

        [Fact]
        public void S_nr_1()
        {
            // NR = 001
            // P/F = 0
            // S3 = 0
            // S2 = 0
            // bit1,0 = 01

            FieldDecoding.EncodeControlField(FrameType.Supervisory, pfBit: false, receiveSequenceNumber: 0x01, supervisoryControlFieldType: SupervisoryControlFieldType.ReceiveReady).Should().Be(0x21);
        }

        [Fact]
        public void S_nr_max()
        {
            // NR = 111
            // P/F = 0
            // S3 = 0
            // S2 = 0
            // bit1,0 = 01

            FieldDecoding.EncodeControlField(FrameType.Supervisory, pfBit: false, receiveSequenceNumber: 0x07, supervisoryControlFieldType: SupervisoryControlFieldType.ReceiveReady).Should().Be(0xe1);
        }

        [Fact]
        public void S_nr_overflow()
        {
            ((Action)(() => FieldDecoding.EncodeControlField(FrameType.Supervisory, pfBit: false, receiveSequenceNumber: 0x08))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void S_pf()
        {
            // NR = 000
            // P/F = 1
            // S3 = 0
            // S2 = 0
            // bit1,0 = 01

            FieldDecoding.EncodeControlField(FrameType.Supervisory, pfBit: true, receiveSequenceNumber: 0x00, supervisoryControlFieldType: SupervisoryControlFieldType.ReceiveReady).Should().Be(0x11);
        }

        [Fact]
        public void S_rej()
        {
            // NR = 000
            // P/F = 0
            // S3 = 1
            // S2 = 0
            // bit1,0 = 01

            FieldDecoding.EncodeControlField(FrameType.Supervisory, pfBit: false, receiveSequenceNumber: 0x00, supervisoryControlFieldType: SupervisoryControlFieldType.Reject).Should().Be(0x09);
        }

        [Fact]
        public void S_rnr()
        {
            // NR = 000
            // P/F = 0
            // S3 = 0
            // S2 = 1
            // bit1,0 = 01

            FieldDecoding.EncodeControlField(FrameType.Supervisory, pfBit: false, receiveSequenceNumber: 0x00, supervisoryControlFieldType: SupervisoryControlFieldType.ReceiveNotReady).Should().Be(0x05);
        }

        [Fact]
        public void U_zero()
        {
            // M7-5   = 000
            // P/F    = 0
            // M3-2   = 00
            // bit1,0 = 11

            FieldDecoding.EncodeControlField(FrameType.UnnumberedInformation, pfBit: false, m7: false, m6: false, m5: false, m3: false, m2: false).Should().Be(0x03);
        }

        [Fact]
        public void U_m7()
        {
            // M7-5   = 100
            // P/F    = 0
            // M3-2   = 00
            // bit1,0 = 11

            FieldDecoding.EncodeControlField(FrameType.UnnumberedInformation, pfBit: false, m7: true, m6: false, m5: false, m3: false, m2: false).Should().Be(0x83);
        }

        [Fact]
        public void U_m6()
        {
            // M7-5   = 010
            // P/F    = 0
            // M3-2   = 00
            // bit1,0 = 11

            FieldDecoding.EncodeControlField(FrameType.UnnumberedInformation, pfBit: false, m7: false, m6: true, m5: false, m3: false, m2: false).Should().Be(0x43);
        }

        [Fact]
        public void U_m5()
        {
            // M7-5   = 001
            // P/F    = 0
            // M3-2   = 00
            // bit1,0 = 11

            FieldDecoding.EncodeControlField(FrameType.UnnumberedInformation, pfBit: false, m7: false, m6: false, m5: true, m3: false, m2: false).Should().Be(0x23);
        }

        [Fact]
        public void U_pf()
        {
            // M7-5   = 000
            // P/F    = 1
            // M3-2   = 00
            // bit1,0 = 11

            FieldDecoding.EncodeControlField(FrameType.UnnumberedInformation, pfBit: true, m7: false, m6: false, m5: false, m3: false, m2: false).Should().Be(0x13);
        }

        [Fact]
        public void U_m3()
        {
            // M7-5   = 000
            // P/F    = 0
            // M3-2   = 10
            // bit1,0 = 11

            FieldDecoding.EncodeControlField(FrameType.UnnumberedInformation, pfBit: false, m7: false, m6: false, m5: false, m3: true, m2: false).Should().Be(0x0b);
        }

        [Fact]
        public void U_m2()
        {
            // M7-5   = 000
            // P/F    = 0
            // M3-2   = 01
            // bit1,0 = 11

            FieldDecoding.EncodeControlField(FrameType.UnnumberedInformation, pfBit: false, m7: false, m6: false, m5: false, m3: false, m2: true).Should().Be(0x07);
        }

        [Fact]
        public void U_all_true()
        {
            // M7-5   = 111
            // P/F    = 1
            // M3-2   = 11
            // bit1,0 = 11

            FieldDecoding.EncodeControlField(FrameType.UnnumberedInformation, pfBit: true, m7: true, m6: true, m5: true, m3: true, m2: true).Should().Be(0xff);
        }
    }
}