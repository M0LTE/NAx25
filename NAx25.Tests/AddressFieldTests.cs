using FluentAssertions;
using System.Collections;
using System.Linq;
using Xunit;

namespace NAx25.Tests
{
    public class AddressFieldTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void AddressSubfieldDecoding(bool isLastAddress, bool cBit)
        {
            const bool r1 = true, r2 = true;
            const byte ssid = 5;

            var testData = new byte[] { 0xae, 0x84, 0x68, 0x94, 0x8c, 0x92, GetA7Byte(isLastAddress, cBit, r1, r2, ssid) };

            var target = new AddressField(testData);

            target.Callsign.Should().Be("WB4JFI");
            target.CommandResponseBit.Should().Be(cBit);
            target.ReservedBit1.Should().Be(r1);
            target.ReservedBit2.Should().Be(r2);
            target.Ssid.Should().Be(ssid);
            target.IsLastAddress.Should().Be(isLastAddress);
        }

        private static byte GetA7Byte(bool isLastAddress, bool cBit, bool r1, bool r2, byte ssid)
        {
            bool bit7 = cBit;
            bool bit6 = r2;
            bool bit5 = r1;
            var ssidBitArray = new BitArray(new[] { ssid });
            bool bit4 = ssidBitArray[3];
            bool bit3 = ssidBitArray[2];
            bool bit2 = ssidBitArray[1];
            bool bit1 = ssidBitArray[0];
            bool bit0 = isLastAddress;

            var bitArray = new BitArray(new[] { bit0, bit1, bit2, bit3, bit4, bit5, bit6, bit7 });

            byte[] bytes = new byte[1];
            bitArray.CopyTo(bytes, 0);
            return bytes[0];
        }
    }
}
