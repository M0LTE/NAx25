using FluentAssertions;
using System.Collections;
using Xunit;

namespace NAx25.Tests
{
    public class ExtensionMethodsTests
    {
        [Fact] public void BoolArrayConversion_0() => (new[] { false, false, false, false, false, false, false, false }).ConvertToByte().Should().Be(0);
        [Fact] public void BoolArrayConversion_5() => (new[] { false, false, false, false, false, true, false, true }).ConvertToByte().Should().Be(5);
        [Fact] public void BoolArrayConversion_255() => (new[] { true, true, true, true, true, true, true, true }).ConvertToByte().Should().Be(255);
        [Fact] public void BitArrayConversion_0() => new BitArray(new[] { false, false, false, false, false, false, false, false }).ConvertToByte().Should().Be(0);
        [Fact] public void BitArrayConversion_5() => new BitArray(new[] { true, false, true, false, false, false, false, false }).ConvertToByte().Should().Be(5);
        [Fact] public void BitArrayConversion_255() => new BitArray(new[] { true, true, true, true, true, true, true, true }).ConvertToByte().Should().Be(255);
    }
}