using System.Collections;

namespace NAx25
{
    public class ControlFields
    {
        /// <summary>
        /// The "M" bits are the unnumbered frame modifier bits and their encoding is discussed in 2.3.4.3.
        /// Only present in U frames, there will always be 5 bits.
        /// </summary>
        public BitArray ModifierBits { get; set; }

        /// <summary>
        /// The P/F bit is the Poll/Final bit. Its function is described in 2.3.3. The distinction between 
        /// command and response, and therefore the distinction between P bit and F bit, is made by 
        /// addressing rules discussed in 2.4.1.2.
        /// Present in U, I and S frames. In I frames, it is the P bit. In U and S frames, it is the P/F bit.
        /// </summary>
        public bool PollFinalBit { get; set; }

        /// <summary>
        /// N(R) is the receive sequence number (bit 5 is the LSB).
        /// Present in I and S frames.
        /// </summary>
        public byte ReceiveSequenceNumber { get; set; }
        
        /// <summary>
        /// N(S) is the send sequence number (bit 1 is the LSB).
        /// Present only in I frames.
        /// </summary>
        public byte SendSequenceNumber { get; set; }

        /// <summary>
        /// http://nic.vajn.icu/PDF/ham/AX25/ax25.html 2.3.4.2 Supervisory Frame Control Field
        /// </summary>
        public SupervisoryControlFieldType? SupervisoryControlFieldType { get; set; }
    }
}