using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Linq;

namespace NAx25.Tests
{
    public class DestinationFieldTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void DestinationSubfieldDecoding(bool isLastAddress, bool cBit)
        {
            const bool r1 = true, r2 = true;
            const byte ssid = 5;

            var testData = new byte[] { 0xae, 0x84, 0x68, 0x94, 0x8c, 0x92, 0x00, GetA7Byte(isLastAddress, cBit, r1, r2, ssid) };

            var target = new DestinationField(testData);

            target.Callsign.Should().Be("WB4JFI");
            target.CommandResponseBit.Should().Be(cBit);
            target.ReservedBit1.Should().Be(r1);
            target.ReservedBit2.Should().Be(r2);
            target.Ssid.Should().Be(ssid);
            target.IsLastAddress.Should().Be(isLastAddress);
        }

        [Fact]
        public void NonrepeaterAx25FrameDecoding()
        {
            // fig 3a http://nic.vajn.icu/PDF/ham/AX25/ax25.html

            var testData = new byte[] {
                0x7e, // flag
                0x96, // A1 = K
                0x70, // A2 = 8
                0x9a, // A3 = M
                0x9a, // A4 = M
                0x9e, // A5 = O
                0x40, // A6 = space
                0xe0, // A7 ssid = 11100000 = 0
                0xae, // A8 = W
                0x84, // A9 = B
                0x68, // A10 = 4
                0x94, // A11 = J
                0x8c, // A12 = F
                0x92, // A13 = I
                0x61, // A14 ssid = 01100001 = 0 (last address)
                0x3e, // Control = 00111110 = I
                0xf0, // PID = 11110000 = none
                0x00, // FCS1
                0x00, // FCS2
                0x7e, // flag
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

            var iframe = new Frame(testData);

            iframe.AddressBytes.Should().HaveCount(14); // from WB4JFI (SSID=0) to K8MMO(SSID = 0), not going through a level 2 repeater, 
            iframe.DestinationAddress.Callsign.Should().Be("K8MMO");
            iframe.DestinationAddress.Ssid.Should().Be(0);
            iframe.SourceAddresses.Should().HaveCount(1);
            iframe.SourceAddresses.First().Callsign.Should().Be("WB4JFI");
            iframe.SourceAddresses.First().Ssid.Should().Be(0);
            iframe.ControlByte.Should().Be(0x3e); // The P/F bit is set; the receive sequence number (N(R)) is 1; the send sequence number (N(S)) is 7.
            iframe.FrameType.Should().Be(FrameType.Information); // The frame shown is an I frame, 
            iframe.InformationFrameFields.Should().NotBeNull();
            iframe.InformationFrameFields.ProtocolIdByte.Should().Be(0xf0); //  with no level 3 protocol. 
            iframe.InformationFrameFields.InfoBytes.Should().BeNull();
            iframe.FcsField.Should().Be(0);
            
        }

        private static byte GetA7Byte(bool isLastAddress, bool cBit, bool r1, bool r2, byte ssid)
        {
            bool bit7 = cBit;
            bool bit6 = r2;
            bool bit5 = r1;
            var ssidBitArray = new BitArray(ssid);
            bool bit4 = ssidBitArray[3];
            bool bit3 = ssidBitArray[2];
            bool bit2 = ssidBitArray[1];
            bool bit1 = ssidBitArray[0];
            bool bit0 = isLastAddress;

            var bitArray = new BitArray(new[] { bit7, bit6, bit5, bit4, bit3, bit2, bit1, bit0 });

            byte[] bytes = new byte[1];
            bitArray.CopyTo(bytes, 0);
            return bytes[0];
        }
    }
}
