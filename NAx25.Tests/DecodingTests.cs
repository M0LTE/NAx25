using FluentAssertions;
using Xunit;

namespace NAx25.Tests
{
    public class DecodingTests
    {
        [Fact]
        public void Fcs()
        {
            byte[] asciiValues = new[] { (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', };

            ushort fcs = FieldDecoding.Crc16Ccitt(asciiValues);

            // expected result:
            // 01110110 00001001
            // 0x76     0x09

            fcs.Should().Be(0x7609);
        }
    }
}