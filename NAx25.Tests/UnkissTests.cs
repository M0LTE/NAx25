using FluentAssertions;
using System;
using Xunit;

namespace NAx25.Tests
{
    /// <summary>
    /// Some test cases from https://en.wikipedia.org/wiki/KISS_(TNC)#Packet_format
    /// </summary>
    public class UnkissTests
    {
        [Fact]
        public void SendTESTOutOfTncPort0()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0x00, 0x54, 0x45, 0x53, 0x54, 0xc0 },
                expectedOutput: new[] { (byte)'T', (byte)'E', (byte)'S', (byte)'T' },
                expectedPortId: 0,
                expectedCommandCode: Kiss.CommandCode.DataFrame);
        }

        [Fact]
        public void SendHelloOutOfTncPort5()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0x50, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0xc0 },
                expectedOutput: new[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' },
                expectedPortId: 5,
                expectedCommandCode: Kiss.CommandCode.DataFrame);
        }

        [Fact]
        public void SendTheBytes_0xc0_0xdb_OutOfTncPort0()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0x00, 0xdb, 0xdc, 0xdb, 0xdd, 0xc0 },
                expectedOutput: new byte[] { 0xc0, 0xdb },
                expectedPortId: 0,
                expectedCommandCode: Kiss.CommandCode.DataFrame);
        }

        [Fact]
        public void SetTxDelay()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0x01, 99, 0xc0 },
                expectedOutput: new byte[] { 99 },
                expectedPortId: 0x00,
                expectedCommandCode: Kiss.CommandCode.TxDelay);
        }

        [Fact]
        public void ExitKissMode()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0xff, 0xc0 },
                expectedOutput: Array.Empty<byte>(),
                expectedPortId: 0x0f,
                expectedCommandCode: Kiss.CommandCode.ExitKissMode);
        }

        [Fact]
        public void InvalidExitKissModeFrame_throws()
        {
            Action a = () => AssertDecodedKissFrame(input: new byte[] { 0xc0, 0xff, 0x00, 0xc0 }, default, default, default);
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void InvalidPersisenceCommand_throws()
        {
            Action a = () => AssertDecodedKissFrame(input: new byte[] { 0xc0, 0x02, 0x00, 0x01, 0xc0 }, default, default, default);
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetHardware_doesnt_have_one_byte_limit()
        {
            Action a = () => AssertDecodedKissFrame(input: new byte[] { 0xc0, 0x06, 0x00, 0xc0 }, default, default, default);
            a.Should().NotThrow<ArgumentException>();
        }

        private void AssertDecodedKissFrame(byte[] input, byte[] expectedOutput, int expectedPortId, Kiss.CommandCode expectedCommandCode)
        {
            var (data, portId, commandCode) = Kiss.Unkiss(input);

            data.Should().BeEquivalentTo(expectedOutput);
            portId.Should().Be(expectedPortId);
            commandCode.Should().Be(expectedCommandCode);
        }
    }
}