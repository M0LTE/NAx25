using FluentAssertions;
using System.Collections;
using System.Linq;

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
            //frame.ControlFields.ModifierBits.Should().BeEquivalentTo(new BitArray(new[] { false, false, false, false, false }));
            frame.ControlFields.M7.Should().BeFalse();
            frame.ControlFields.M6.Should().BeFalse();
            frame.ControlFields.M5.Should().BeFalse();
            frame.ControlFields.M3.Should().BeFalse();
            frame.ControlFields.M2.Should().BeFalse();
            frame.ControlFields.PollFinalBit.Should().BeFalse();
            frame.ControlFields.ReceiveSequenceNumber.Should().Be(0);
            frame.ControlFields.SendSequenceNumber.Should().Be(0);
            frame.ControlFields.SupervisoryControlFieldType.Should().BeNull();
            frame.InformationFrameFields.ProtocolId.Should().Be(Protocol.NoLayer3Protocol);
            frame.SourceAddresses.Take(frame.SourceAddresses.Count - 1).Select(f => f.IsLastAddress).Should().AllBeEquivalentTo(false);
            frame.SourceAddresses[^1].IsLastAddress.Should().BeTrue();
        }
    }
}