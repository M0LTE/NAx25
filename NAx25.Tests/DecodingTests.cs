using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace NAx25.Tests
{
    public class DecodingTests
    {
        [Fact]
        public void Fcs()
        {
            var asciiValues = Enumerable.Range('1', 9).Select(e => (byte)e).Select(b => ReverseBytes(b)).ToArray();

            var fcs = FieldDecoding.Crc16Ccitt(asciiValues);

            //fcs.Should().Be(0x906e);
            //fcs.Should().Be(0x7609);
        }

        public static byte ReverseBytes(byte value)
        {
            //return value;
            var lNewVal = (byte)((((ulong)value * 0x0202020202UL) & 0x010884422010UL) % 1023);
            return lNewVal;
        }
    }
}