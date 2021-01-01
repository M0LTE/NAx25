# NAx25
Cleanroom .NET Standard implementation of AX.25. Very much work-in-progress.

# Aims
- learning experience for the author
- to provide a reusable building block for AX.25 packet decoding and encoding .NET / .NET Core
- to prioritise code readability/understandability over ultimate performance
- to decouple frame encoding/decoding from application logic, and from KISS framing
- to reduce the difficulty of building AX.25 applications in .NET Core
- ultimately to enable the development of a modern connected-mode AX.25 client or server built in a higher-level language

# Status
- frame encoding and decoding basically works now, and the public API is fairly clean
- FCS calculation isn't implemented :cry:
- a larger test suite needs assembling which covers a larger variety of the possible valid types of frame
- issues and contributions very welcome, please go ahead and use the GitHub tooling

# Examples
## Frame decoding
```
byte[] testData = new byte[] {
    0x7e, // 0  flag
    0x96, // 1  A1 = K
    0x70, // 2  A2 = 8
    0x9a, // 3  A3 = M
    0x9a, // 4  A4 = M
    0x9e, // 5  A5 = O
    0x40, // 6  A6 = space
    0xe0, // 7  A7 ssid = 11100000 = 0
    0xae, // 8  A8 = W
    0x84, // 9  A9 = B
    0x68, // 10 A10 = 4
    0x94, // 11 A11 = J
    0x8c, // 12 A12 = F
    0x92, // 13 A13 = I
    0x61, // 14 A14 ssid = 01100001 = 0 (last address)
    0x3e, // 15 Control = 00111110 = I
    0xf0, // 16 PID = 11110000 = none
    0x00, // 17 FCS1
    0x00, // 18 FCS2
    0x7e, // 19 flag
};

if (Frame.TryParse(frameData, out var frame))
{
    frame.FrameType.Should().Be(FrameType.Information);
    frame.DestinationAddress.Callsign.Should().Be("K8MMO");
    frame.DestinationAddress.Ssid.Should().Be(0);
    frame.DestinationAddress.CommandResponseBit.Should().Be(true);
    frame.SourceAddresses.First().Callsign.Should().Be("WB4JFI");
    frame.SourceAddresses.First().Ssid.Should().Be(0);
    // etc...
}
```

## Frame generation
```
var frame = new Frame(
    frameType: FrameType.Information,
    destinationAddress: new AddressField("K8MMO", commandResponseBit: true),
    sourceAddresses: new[] { new AddressField("WB4JFI") },
    pollFinalBit: true,
    receiveSequenceNumber: 1,
    sendSequenceNumber: 7,
    protocolId: Protocol.NoLayer3Protocol);

byte[] ax25Frame = frame.ToBytes();
```

## KISS
```
[Fact]
public void Frame_SendHelloOnTncPort5()
{
    var input = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
    var expectedOutput = new byte[] { 0xc0, 0x50, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0xc0 };

    var kissFramedOutput = KissFraming.Kiss(input, portIndex: 5, KissCommandCode.DataFrame);
    kissFramedOutput.Should().BeEquivalentTo(expectedOutput);
}

[Fact]
public void Unframe_SendHelloOnTncPort5()
{
    var input = new byte[] { 0xc0, 0x50, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0xc0 };
            
    var (data, portId, commandCode) = KissFraming.Unkiss(input);

    data.Should().BeEquivalentTo(new[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' });
    portId.Should().Be(5);
    commandCode.Should().Be(KissCommandCode.DataFrame);
}
```
