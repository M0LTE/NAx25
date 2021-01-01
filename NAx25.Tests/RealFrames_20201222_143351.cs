using FluentAssertions;
using System.Linq;
using Xunit;

namespace NAx25.Tests
{
    public class RealFrames_20201222_143351
    {
        [Fact]
        public void RealFrame_1()
        {
            var testData = new byte[] {
                0x82, 0xa0, 0xaa, 0x64, 0x6a, 0x9c, 0xe0, 0x8e,  // ...dj...
                0x60, 0xae, 0xb2, 0x8e, 0x40, 0x60, 0x9a, 0x84,  // `...@`..
                0x6e, 0xaa, 0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88,  // n..@....
                0x8a, 0x62, 0x40, 0xe0, 0x9a, 0x84, 0x6e, 0xaa,  // .b@...n.
                0x94, 0x40, 0x60, 0x8e, 0x60, 0x94, 0x9a, 0xa6,  // .@`.`...
                0x40, 0x61, 0x03, 0xf0, 0x3b, 0x45, 0x4c, 0x2d,  // @a..;EL-
                0x67, 0x30, 0x77, 0x79, 0x67, 0x20, 0x2a, 0x32,  // g0wyg *2
                0x32, 0x31, 0x34, 0x33, 0x32, 0x7a, 0x35, 0x31,  // 21432z51
                0x32, 0x35, 0x2e, 0x36, 0x35, 0x4e, 0x45, 0x30,  // 25.65NE0
                0x30, 0x30, 0x30, 0x30, 0x2e, 0x36, 0x39, 0x45,  // 0000.69E
                0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x2f,  // 0000000/
                0x31, 0x30, 0x33, 0x20, 0x4f, 0x6e, 0x20, 0x20,  // 103 On  
                0x40, 0x31, 0x34, 0x33, 0x32, 0x20, 0x37, 0x33,  // @1432 73
                0x0d,  // .
            };
            Frame.TryParse(testData, out var frame);
            Asserts.GenericAprs(frame);

            frame.DestinationAddress.Callsign.Should().Be("APU25N");
            frame.SourceAddresses.Should().HaveCount(5);
            frame.SourceAddresses[0].Callsign.Should().Be("G0WYG");
            frame.SourceAddresses[1].Callsign.Should().Be("MB7UG");
            frame.SourceAddresses[2].Callsign.Should().Be("WIDE1");
            frame.SourceAddresses[3].Callsign.Should().Be("MB7UJ");
            frame.SourceAddresses[4].Callsign.Should().Be("G0JMS");
            frame.SourceAddresses[0].IsLastAddress.Should().BeFalse();
            frame.SourceAddresses[1].IsLastAddress.Should().BeFalse();
            frame.SourceAddresses[2].IsLastAddress.Should().BeFalse();
            frame.SourceAddresses[3].IsLastAddress.Should().BeFalse();
            frame.SourceAddresses[4].IsLastAddress.Should().BeTrue();
            frame.SourceAddresses.Select(a => a.Ssid).Should().AllBeEquivalentTo(0);

            frame.Fcs.Should().Be(3379); // not verified
            frame.InformationFrameFields.ProtocolId.Should().Be(Protocol.NoLayer3Protocol);
            frame.InformationFrameFields.InfoText.Should().Be(";EL-g0wyg *221432z5125.65NE00000.69E0000000/103 On  @1432 7");
        }

        [Fact]
        public void RealFrame_2()
        {
            var testData = new byte[] {
                0x82, 0xa0, 0xaa, 0x64, 0x68, 0x9c, 0x60, 0x8e,  // ...dh.`.
                0x60, 0x82, 0x9e, 0xb4, 0x40, 0x60, 0x9a, 0x84,  // `...@`..
                0x6e, 0xaa, 0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88,  // n..@....
                0x8a, 0x64, 0x40, 0x63, 0x03, 0xf0, 0x3d, 0x35,  // .d@c..=5
                0x31, 0x34, 0x30, 0x2e, 0x36, 0x30, 0x4e, 0x2f,  // 140.60N/
                0x30, 0x30, 0x31, 0x32, 0x35, 0x2e, 0x33, 0x38,  // 00125.38
                0x57, 0x2d, 0x20, 0x7b, 0x55, 0x49, 0x56, 0x33,  // W- {UIV3
                0x32, 0x7d, 0x0d,  // 2}.
            };
            Frame.TryParse(testData, out var frame);
        }

        [Fact]
        public void RealFrame_3()
        {
            var testData = new byte[] {
                0xaa, 0xa0, 0xaa, 0xb2, 0xa8, 0xa2, 0x60, 0x64,  // ......`d
                0x8a, 0x60, 0x92, 0x9e, 0xaa, 0xf2, 0x9a, 0x84,  // .`......
                0x6e, 0xaa, 0xa0, 0x40, 0xe0, 0x9a, 0x84, 0x6e,  // n..@...n
                0xaa, 0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88, 0x8a,  // ..@.....
                0x64, 0x40, 0xe1, 0x03, 0xf0, 0x60, 0x77, 0x30,  // d@...`w0
                0x6f, 0x6c, 0x5f, 0x3f, 0x6a, 0x2f, 0x60, 0x22,  // ol_?j/`"
                0x34, 0x36, 0x7d, 0x5f, 0x25, 0x0d,  // 46}_%.
            };
            Frame.TryParse(testData, out var frame);
        }

        [Fact]
        public void RealFrame_4()
        {
            var testData = new byte[] {
                0xaa, 0x62, 0xa6, 0xaa, 0xa8, 0xa8, 0x60, 0x9a,  // .b....`.
                0x62, 0xa4, 0x94, 0x98, 0x40, 0xea, 0x9a, 0x84,  // b...@...
                0x6e, 0xaa, 0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88,  // n..@....
                0x8a, 0x62, 0x40, 0xe0, 0xae, 0x92, 0x88, 0x8a,  // .b@.....
                0x64, 0x40, 0x63, 0x03, 0xf0, 0x60, 0x77, 0x36,  // d@c..`w6
                0x46, 0x6c, 0x20, 0x1c, 0x3e, 0x2f, 0x60, 0x22,  // Fl .>/`"
                0x35, 0x27, 0x7d, 0x57, 0x61, 0x6e, 0x74, 0x61,  // 5'}Wanta
                0x67, 0x65, 0x2c, 0x20, 0x4f, 0x78, 0x66, 0x6f,  // ge, Oxfo
                0x72, 0x64, 0x73, 0x68, 0x69, 0x72, 0x65, 0x5f,  // rdshire_
                0x25, 0x0d,  // %.
            };
            Frame.TryParse(testData, out var frame);
        }

        [Fact]
        public void RealFrame_5()
        {
            var testData = new byte[] {
                0x82, 0xa0, 0x9a, 0x92, 0x60, 0x6c, 0x60, 0x9a,  // ....`l`.
                0x60, 0x8e, 0xa2, 0xa6, 0x40, 0x60, 0xae, 0x92,  // `...@`..
                0x88, 0x8a, 0x64, 0x40, 0x65, 0x03, 0xf0, 0x40,  // ..d@e..@
                0x32, 0x32, 0x31, 0x34, 0x33, 0x35, 0x7a, 0x35,  // 221435z5
                0x31, 0x32, 0x36, 0x2e, 0x32, 0x37, 0x4e, 0x2f,  // 126.27N/
                0x30, 0x30, 0x30, 0x35, 0x37, 0x2e, 0x38, 0x33,  // 00057.83
                0x57, 0x2d, 0x57, 0x58, 0x33, 0x69, 0x6e, 0x31,  // W-WX3in1
                0x50, 0x6c, 0x75, 0x73, 0x32, 0x2e, 0x30, 0x20,  // Plus2.0 
                0x55, 0x3d, 0x31, 0x34, 0x2e, 0x31, 0x56, 0x2c,  // U=14.1V,
                0x20, 0x54, 0x3d, 0x33, 0x32, 0x2e, 0x35, 0x43,  //  T=32.5C
            };
            Frame.TryParse(testData, out var frame);
        }

        [Fact]
        public void RealFrame_6()
        {
            var testData = new byte[] {
                0x82, 0xa0, 0x9a, 0x92, 0x60, 0x6c, 0x60, 0x9a,  // ....`l`.
                0x60, 0x8e, 0xa2, 0xa6, 0x40, 0x60, 0x9a, 0x84,  // `...@`..
                0x6e, 0xaa, 0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88,  // n..@....
                0x8a, 0x64, 0x40, 0x63, 0x03, 0xf0, 0x40, 0x32,  // .d@c..@2
                0x32, 0x31, 0x34, 0x33, 0x35, 0x7a, 0x35, 0x31,  // 21435z51
                0x32, 0x36, 0x2e, 0x32, 0x37, 0x4e, 0x2f, 0x30,  // 26.27N/0
                0x30, 0x30, 0x35, 0x37, 0x2e, 0x38, 0x33, 0x57,  // 0057.83W
                0x2d, 0x57, 0x58, 0x33, 0x69, 0x6e, 0x31, 0x50,  // -WX3in1P
                0x6c, 0x75, 0x73, 0x32, 0x2e, 0x30, 0x20, 0x55,  // lus2.0 U
                0x3d, 0x31, 0x34, 0x2e, 0x31, 0x56, 0x2c, 0x20,  // =14.1V, 
                0x54, 0x3d, 0x33, 0x32, 0x2e, 0x35, 0x43,  // T=32.5C
            };
            Frame.TryParse(testData, out var frame);
        }

        [Fact]
        public void RealFrame_7()
        {
            var testData = new byte[] {
                0xaa, 0xa0, 0xaa, 0xb2, 0xb0, 0xa2, 0x60, 0x64,  // ......`d
                0x8a, 0x60, 0x92, 0x9e, 0xaa, 0xf2, 0x9a, 0x84,  // .`......
                0x6e, 0xaa, 0xa0, 0x40, 0xe0, 0x9a, 0x84, 0x6e,  // n..@...n
                0xaa, 0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88, 0x8a,  // ..@.....
                0x64, 0x40, 0xe1, 0x03, 0xf0, 0x60, 0x77, 0x31,  // d@...`w1
                0x2a, 0x6d, 0x2d, 0x4b, 0x6a, 0x2f, 0x60, 0x22,  // *m-Kj/`"
                0x34, 0x58, 0x7d, 0x5f, 0x25, 0x0d,  // 4X}_%.
            };
            Frame.TryParse(testData, out var frame);
        }

        [Fact]
        public void RealFrame_8()
        {
            var testData = new byte[] {
                0xaa, 0xa2, 0xaa, 0xa2, 0xa0, 0xa6, 0x60, 0x9a,  // ......`.
                0x60, 0xae, 0x84, 0x82, 0x40, 0xe0, 0x8e, 0x68,  // `...@..h
                0x8e, 0xac, 0xb4, 0x40, 0xe0, 0x9a, 0x84, 0x6e,  // ...@...n
                0xaa, 0xa6, 0xae, 0xe0, 0x9a, 0x84, 0x6e, 0xaa,  // ......n.
                0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88, 0x8a, 0x64,  // .@.....d
                0x40, 0xe1, 0x03, 0xf0, 0x27, 0x78, 0x2b, 0x35,  // @...'x+5
                0x6c, 0x20, 0x1c, 0x2d, 0x2f, 0x5d, 0x22, 0x33,  // l .-/]"3
                0x72, 0x7d, 0x47, 0x6c, 0x6f, 0x73, 0x20, 0x43,  // r}Glos C
                0x6f, 0x75, 0x6e, 0x74, 0x79, 0x20, 0x52, 0x61,  // ounty Ra
                0x79, 0x6e, 0x65, 0x74, 0x0d,  // ynet.
            };
            Frame.TryParse(testData, out var frame);
        }

        [Fact]
        public void RealFrame_9()
        {
            var testData = new byte[] {
                0xaa, 0xa0, 0xaa, 0xb2, 0xb2, 0xa2, 0x60, 0x64,  // ......`d
                0x8a, 0x60, 0x92, 0x9e, 0xaa, 0xf2, 0x9a, 0x84,  // .`......
                0x6e, 0xaa, 0xa0, 0x40, 0xe0, 0x9a, 0x84, 0x6e,  // n..@...n
                0xaa, 0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88, 0x8a,  // ..@.....
                0x64, 0x40, 0xe1, 0x03, 0xf0, 0x60, 0x77, 0x31,  // d@...`w1
                0x33, 0x6e, 0x2c, 0x68, 0x6a, 0x2f, 0x60, 0x22,  // 3n,hj/`"
                0x34, 0x5c, 0x7d, 0x5f, 0x25, 0x0d,  // 4\}_%.
            };
            Frame.TryParse(testData, out var frame);
        }

        [Fact]
        public void RealFrame_10()
        {
            var testData = new byte[] {
                0x82, 0xa0, 0x88, 0xa4, 0x62, 0x6a, 0xe0, 0x9a,  // ....bj..
                0x6c, 0x9a, 0xa8, 0x9c, 0x40, 0x7e, 0x9a, 0x84,  // l...@~..
                0x6e, 0xaa, 0xa0, 0x40, 0xe0, 0x9a, 0x84, 0x6e,  // n..@...n
                0xaa, 0x8e, 0x40, 0xe0, 0xae, 0x92, 0x88, 0x8a,  // ..@.....
                0x64, 0x40, 0x63, 0x03, 0xf0, 0x3d, 0x35, 0x30,  // d@c..=50
                0x34, 0x38, 0x2e, 0x36, 0x30, 0x4e, 0x2f, 0x30,  // 48.60N/0
                0x30, 0x31, 0x30, 0x35, 0x2e, 0x32, 0x32, 0x57,  // 0105.22W
                0x76, 0x20, 0x53, 0x74, 0x61, 0x79, 0x20, 0x53,  // v Stay S
                0x61, 0x66, 0x65, 0x20, 0x50, 0x65, 0x6f, 0x70,  // afe Peop
                0x6c, 0x65,  // le
            };
            Frame.TryParse(testData, out var frame);
        }

    }
}
