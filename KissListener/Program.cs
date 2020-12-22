using MoreLinq;
using NAx25;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace KissListener
{
    class Program
    {
        static void Main(string[] args)
        {
            var sp = new SerialPort("COM16", 57600, Parity.None, 8, StopBits.One);
            sp.Open();

            string testClass = $"RealFrames_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            var fs = File.Open(@"C:\Users\tomandels\Source\Repos\NAx25\NAx25.Tests\" + testClass + ".cs", FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
            streamWriter = new StreamWriter(fs);

            streamWriter.WriteLine(@$"using FluentAssertions;
using System.Linq;
using Xunit;

namespace NAx25.Tests
{{
    public class {testClass}
    {{");

            var buffer = new List<byte>();
            int frames = 0;
            while (true)
            {
                var b = (byte)sp.ReadByte();

                buffer.Add(b);

                var (isKissFrame, ax25Frame) = ProcessBuffer(buffer.ToArray());

                if (isKissFrame)
                {
                    frames++;
                    Process(ax25Frame, frames);
                    streamWriter.Flush();
                    
                    buffer.Clear();
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }

        static StreamWriter streamWriter;

        private static void Process(byte[] ax25Frame, int frameNumber)
        {
            //Frame frame = new Frame(ax25Frame);

            string testName = $"RealFrame_{frameNumber}";

            streamWriter.WriteLine("        [Fact]");
            streamWriter.WriteLine($"        public void {testName}()");
            streamWriter.WriteLine("        {");
            streamWriter.WriteLine("            var testData = new byte[] {");

            foreach (var batch in ax25Frame.Batch(8))
            {
                streamWriter.Write("                ");
                foreach (var b in batch)
                {
                    streamWriter.Write($"0x{b.ToHexByte()}, ");
                }

                streamWriter.Write(" // ");
                foreach (var b in batch)
                {
                    if (b >= ' ' && b <= '~' )
                    {
                        streamWriter.Write((char)b);
                    }
                    else
                    {
                        streamWriter.Write(".");
                    }
                }

                streamWriter.WriteLine();
            }
            streamWriter.WriteLine("            };");
            streamWriter.WriteLine("            Frame.TryParse(testData, out var frame);");
            streamWriter.WriteLine("            Asserts.GenericAprs(frame);");

            streamWriter.WriteLine("        }");

            streamWriter.WriteLine();

        }

        private static (bool isKissFrame, byte[] ax25Data) ProcessBuffer(byte[] buffer)
        {
            if (buffer.Length > 2 && buffer[0] == 0xc0 && buffer[1] == 0x00 && buffer[^1] == 0xc0)
            {
                var (ax25Data, fromModemPort, commandCode) = Kiss.Unkiss(buffer);

                if (commandCode != Kiss.CommandCode.DataFrame)
                {
                    throw new Exception($"Received a {commandCode} from the modem which is not valid");
                }

                int col = 0;

                foreach (var b in ax25Data)
                {
                    var digit = b.ToHexByte();

                    int groupingSpacing = col < 8 ? 0 : 2;
                    Console.SetCursorPosition(col * 3 + groupingSpacing, Console.CursorTop);
                    Console.BackgroundColor = b == 0xc0 ? ConsoleColor.Red : ConsoleColor.Black;
                    Console.Write(digit);

                    Console.SetCursorPosition(col + 16 * 3 + 3, Console.CursorTop);

                    if (b >= 32 && b <= 126)
                    {
                        Console.Write((char)b);
                    }
                    else
                    {
                        Console.Write('.');
                    }

                    if (col == 15)
                    {
                        col = 0;
                        Console.WriteLine();
                    }
                    else
                    {
                        col++;
                    }
                }

                return (true, ax25Data);
            }

            return (false, default);
        }
    }
}