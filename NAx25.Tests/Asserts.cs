using FluentAssertions;
using System.Collections;

namespace NAx25.Tests
{
    internal class Asserts
    {
        internal static void GenericAprs(Frame frame)
        {
            frame.FrameType.Should().Be(FrameType.UnnumberedInformation);
            frame.InformationFrameFields.Should().NotBeNull();
            frame.SupervisoryFrameFields.Should().BeNull();
            frame.ControlFields.Should().NotBeNull();
            frame.ControlFields.ModifierBits.Should().BeEquivalentTo(new BitArray(new[] { false, false, false, false, false }));
            frame.ControlFields.PollFinalBit.Should().BeFalse();
            frame.ControlFields.ReceiveSequenceNumber.Should().Be(0);
            frame.ControlFields.SendSequenceNumber.Should().Be(0);
            frame.ControlFields.SupervisoryControlFieldType.Should().BeNull();
            frame.InformationFrameFields.ProtocolIdByte.Should().Be(0xf0);
        }
    }
}