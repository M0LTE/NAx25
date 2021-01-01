using FluentAssertions;
using System;
using Xunit;

namespace NAx25.Tests
{
    /// <summary>
    /// Some test cases from https://en.wikipedia.org/wiki/KISS_(TNC)#Packet_format
    /// </summary>
    public class KissFramingTests
    {
        [Fact]
        public void Unframe_SendTESTOutOfTncPort0()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0x00, 0x54, 0x45, 0x53, 0x54, 0xc0 },
                expectedOutput: new[] { (byte)'T', (byte)'E', (byte)'S', (byte)'T' },
                expectedPortId: 0,
                expectedCommandCode: KissCommandCode.DataFrame);
        }

        [Fact]
        public void Unframe_SendHelloOutOfTncPort5()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0x50, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0xc0 },
                expectedOutput: new[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' },
                expectedPortId: 5,
                expectedCommandCode: KissCommandCode.DataFrame);
        }

        [Fact]
        public void Unframe_SendTheBytes_0xc0_0xdb_OutOfTncPort0()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0x00, 0xdb, 0xdc, 0xdb, 0xdd, 0xc0 },
                expectedOutput: new byte[] { 0xc0, 0xdb },
                expectedPortId: 0,
                expectedCommandCode: KissCommandCode.DataFrame);
        }

        [Fact]
        public void Unframe_SetTxDelay()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0x01, 99, 0xc0 },
                expectedOutput: new byte[] { 99 },
                expectedPortId: 0x00,
                expectedCommandCode: KissCommandCode.TxDelay);
        }

        [Fact]
        public void Unframe_ExitKissMode()
        {
            AssertDecodedKissFrame(
                input: new byte[] { 0xc0, 0xff, 0xc0 },
                expectedOutput: Array.Empty<byte>(),
                expectedPortId: 0x0f,
                expectedCommandCode: KissCommandCode.ExitKissMode);
        }

        [Fact]
        public void Unframe_InvalidExitKissModeFrame_throws()
        {
            Action a = () => AssertDecodedKissFrame(input: new byte[] { 0xc0, 0xff, 0x00, 0xc0 }, default, default, default);
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Unframe_InvalidPersisenceCommand_throws()
        {
            Action a = () => AssertDecodedKissFrame(input: new byte[] { 0xc0, 0x02, 0x00, 0x01, 0xc0 }, default, default, default);
            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Unframe_SetHardware_doesnt_have_one_byte_limit()
        {
            Action a = () => AssertDecodedKissFrame(input: new byte[] { 0xc0, 0x06, 0x00, 0xc0 }, default, default, default);
            a.Should().NotThrow<ArgumentException>();
        }

        private void AssertDecodedKissFrame(byte[] input, byte[] expectedOutput, int expectedPortId, KissCommandCode expectedCommandCode)
        {
            var (data, portId, commandCode) = KissFraming.Unkiss(input);

            data.Should().BeEquivalentTo(expectedOutput);
            portId.Should().Be(expectedPortId);
            commandCode.Should().Be(expectedCommandCode);
        }

        [Fact]
        public void Frame_KissFrame_TestOnTncPort0()
        {
            var rawFrame = new byte[] { (byte)'T', (byte)'E', (byte)'S', (byte)'T' };
            var expectedOutput = new byte[] { 0xc0, 0x00, 0x54, 0x45, 0x53, 0x54, 0xc0 };

            AssertEncodedKissFrame(rawFrame, 0, KissCommandCode.DataFrame, expectedOutput);
        }

        [Fact]
        public void Frame_KissFrame_HelloOnTncPort5()
        {
            var rawFrame = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
            var expectedOutput = new byte[] { 0xc0, 0x50, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0xc0 };

            AssertEncodedKissFrame(rawFrame, 5, KissCommandCode.DataFrame, expectedOutput);
        }

        [Fact]
        public void Frame_KissFrame_0xc0_0xdb_OnTncPort0()
        {
            var rawFrame = new byte[] { 0xc0, 0xdb };
            var expectedOutput = new byte[] { 0xc0, 0x00, 0xdb, 0xdc, 0xdb, 0xdd, 0xc0 };

            AssertEncodedKissFrame(rawFrame, 0, KissCommandCode.DataFrame, expectedOutput);
        }

        [Fact]
        public void Frame_KissFrame_TxDelayTo500ms_OnTncPort6()
        {
            var rawFrame = new byte[] { 50 };
            var expectedOutput = new byte[] { 0xc0, 0x61, 50, 0xc0 };

            AssertEncodedKissFrame(rawFrame, 6, KissCommandCode.TxDelay, expectedOutput);
        }

        private static void AssertEncodedKissFrame(byte[] rawFrame, uint portId, KissCommandCode kissCommandCode, byte[] expectedOutput)
        {
            var kissFramedOutput = KissFraming.Kiss(rawFrame, portId, kissCommandCode);
            kissFramedOutput.Should().BeEquivalentTo(expectedOutput);
        }
    }
}