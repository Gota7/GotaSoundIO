using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VGAudio.Codecs.GcAdpcm;

namespace GotaSoundIO.Sound {

    /// <summary>
    /// DspAdpcm Encoding.
    /// </summary>
    public class DspAdpcm : AudioEncoding {
        
        /// <summary>
        /// Sample data.
        /// </summary>
        private byte[] samples;

        /// <summary>
        /// PCM16 samples for retrieving SEEK info.
        /// </summary>
        private List<short[]> pcm16History = new List<short[]>();

        /// <summary>
        /// Samples per block when using seek.
        /// </summary>
        private uint seekBlockSamples;

        /// <summary>
        /// DspAdpcm context.
        /// </summary>
        public DspAdpcmContext Context;

        /// <summary>
        /// Loop start.
        /// </summary>
        /// <returns>Loop start info.</returns>
        public uint LoopStart;

        /// <summary>
        /// Samples to nibbles.
        /// </summary>
        /// <param name="samples">Number of samples.</param>
        /// <returns>The number of nibbles.</returns>
        public static uint Samples2Nibbles(uint samples) {
            uint numFrames = samples / 14;
            uint remainingSamples = samples % 14;
            return numFrames * 16 + remainingSamples + 2;
        }

        /// <summary>
        /// Nibbles to samples.
        /// </summary>
        /// <param name="nibbles">The amount of nibbles.</param>
        /// <returns>The number of samples.</returns>
        public static uint Nibbles2Samples(uint nibbles) {
            uint numFrames = nibbles / 16;
            uint remainingNibbles = nibbles % 16;
            return numFrames * 14 + (remainingNibbles - 2);
        }

        /// <summary>
        /// Get the PCM16 samples.
        /// </summary>
        /// <returns>The PCM16 samples.</returns>
        public override short[] ToPCM16() {
            short[] pcm16 = new short[NumSamples];
            DspAdpcmDecoder.Decode(samples, ref pcm16, ref Context, (uint)NumSamples);
            return pcm16;
        }

        /// <summary>
        /// Set the PCM16 samples.
        /// </summary>
        public override void FromPCM16(short[] samples) {
            this.samples = DspAdpcmEncoder.EncodeSamples(samples, out Context, LoopStart);
            DataSize = this.samples.Length;
            NumSamples = samples.Length;
        }

        /// <summary>
        /// Read the encoding.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {
            if (NumSamples == -1) {
                samples = r.ReadBytes(DataSize);
                NumSamples = DataSize;
            } else {
                samples = r.ReadBytes(NumSamples * 8 / 7);
                DataSize = NumSamples * 8 / 7;
            }
        }

        /// <summary>
        /// Write the encoding.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {
            w.Write(samples);
        }

        /// <summary>
        /// Init samples from a number of blocks.
        /// </summary>
        /// <param name="blockCount">The number of blocks.</param>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <param name="lastBlockSize">The size of the last block.</param>
        /// <param name="lastBlockSamples">Samples in the last block.</param>
        public override void InitFromBlocks(uint blockCount, uint blockSize, uint blockSamples, uint lastBlockSize, uint lastBlockSamples) {
            samples = new byte[(blockCount - 1) * blockSize + lastBlockSize];
            NumSamples = (int)((blockCount - 1) * blockSamples + blockSize);
            DataSize = samples.Length;
        }

        /// <summary>
        /// Get the number of blocks.
        /// </summary>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The number of blocks.</returns>
        public override uint NumBlocks(uint blockSize, uint blockSamples) {
            uint num = (uint)samples.Length / blockSize;
            if (samples.Length % blockSize != 0) { num++; }
            return num;
        }

        /// <summary>
        /// Get the size of the last block.
        /// </summary>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The size of the last block.</returns>
        public override uint LastBlockSize(uint blockSize, uint blockSamples) {
            uint size = (uint)samples.Length % blockSize;
            if (size == 0) { size = blockSize; }
            return size;
        }

        /// <summary>
        /// Get the samples in the last block.
        /// </summary>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The amount of samples in the last block.</returns>
        public override uint LastBlockSamples(uint blockSize, uint blockSamples) {
            uint samples = (uint)NumSamples % blockSamples;
            if (samples == 0) { samples = blockSamples; }
            return samples;
        }

        /// <summary>
        /// Read a block.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="blockNum"></param>
        /// <param name="blockSize"></param>
        /// <param name="blockSamples"></param>
        public override void ReadBlock(FileReader r, int blockNum, uint blockSize, uint blockSamples) {
            byte[] data = r.ReadBytes(blockNum == NumBlocks(blockSize, blockSamples) - 1 ? (int)LastBlockSize(blockSize, blockSamples) : (int)blockSize);
            data.CopyTo(samples, blockNum * blockSize);
        }

        /// <summary>
        /// Write a block.
        /// </summary>
        /// <param name="w">The writer.</param>
        /// <param name="blockNum">The number of blocks.</param>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        public override void WriteBlock(FileWriter w, int blockNum, uint blockSize, uint blockSamples) {
            int size = blockNum == NumBlocks(blockSize, blockSamples) - 1 ? (int)LastBlockSize(blockSize, blockSamples) : (int)blockSize;
            w.Write(samples.SubArray(blockNum * NumSamples, size));
        }

    }

    public static class DspAdpcmDecoder {

        static sbyte[] NibbleToSbyte = { 0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1 };

        static uint DivideByRoundUp(uint dividend, uint divisor) {
            return (dividend + divisor - 1) / divisor;
        }

        static sbyte GetHighNibble(byte value) {
            return NibbleToSbyte[(value >> 4) & 0xF];
        }

        static sbyte GetLowNibble(byte value) {
            return NibbleToSbyte[value & 0xF];
        }

        static short Clamp16(int value) {
            if (value > 32767) {
                return 32767;
            }
            if (value < -32678) {
                return -32678;
            }
            return (short)value;
        }


        /// <summary>
        /// Decode DSP-ADPCM data.
        /// </summary>
        /// <param name="src">DSP-ADPCM source.</param>
        /// <param name="dst">Destination array of samples.</param>
        /// <param name="cxt">DSP-APCM context.</param>
        /// <param name="samples">Number of samples.</param>
        public static void Decode(byte[] src, ref Int16[] dst, ref DspAdpcmContext cxt, UInt32 samples) {

            //Each DSP-APCM frame is 8 bytes long. It contains 1 header byte, and 7 sample bytes.

            //Set initial values.
            short hist1 = cxt.yn1;
            short hist2 = cxt.yn2;
            int dstIndex = 0;
            int srcIndex = 0;

            //Until all samples decoded.
            while (dstIndex < samples) {

                //Get the header.
                byte header = src[srcIndex++];

                //Get scale and co-efficient index.
                UInt16 scale = (UInt16)(1 << (header & 0xF));
                byte coef_index = (byte)(header >> 4);
                short coef1 = cxt.coefs[coef_index][0];
                short coef2 = cxt.coefs[coef_index][1];

                //7 sample bytes per frame.
                for (UInt32 b = 0; b < 7; b++) {

                    //Get byte.
                    byte byt = src[srcIndex++];

                    //2 samples per byte.
                    for (UInt32 s = 0; s < 2; s++) {
                        sbyte adpcm_nibble = ((s == 0) ? GetHighNibble(byt) : GetLowNibble(byt));
                        short sample = Clamp16(((adpcm_nibble * scale) << 11) + 1024 + ((coef1 * hist1) + (coef2 * hist2)) >> 11);

                        hist2 = hist1;
                        hist1 = sample;
                        dst[dstIndex++] = sample;

                        if (dstIndex >= samples) break;
                    }
                    if (dstIndex >= samples) break;

                }

            }

        }

    }

    /// <summary>
    /// The encoder.
    /// </summary>
    public class DspAdpcmEncoder {

        /// <summary>
        /// Encodes the samples.
        /// </summary>
        /// <returns>The samples.</returns>
        /// <param name="samples">Samples.</param>
		public static byte[] EncodeSamples(short[] samples, out DspAdpcmContext info, uint loopStart) {

            //Encode data.
            short[] coeffs = GcAdpcmCoefficients.CalculateCoefficients(samples);
            byte[] dspAdpcm = GcAdpcmEncoder.Encode(samples, coeffs);

            info = new DspAdpcmContext();
            info.LoadCoeffs(coeffs);

            //Loop stuff.
            if (loopStart > 0) info.loop_yn1 = samples[loopStart - 1];
            if (loopStart > 1) info.loop_yn2 = samples[loopStart - 2];

            return dspAdpcm;

        }

    }

    /// <summary>
    /// DspAdpcm context
    /// </summary>
    public class DspAdpcmContext : IReadable, IWritable {

        /// <summary>
        /// Get the coeffecients.
        /// </summary>
        /// <returns>The coefficients.</returns>
        public short[] GetCoeffs() {
            List<short> c = new List<short>();
            foreach (var a in coefs) {
                c.AddRange(a);
            }
            return c.ToArray();
        }

        /// <summary>
        /// Load the coefficients.
        /// </summary>
        /// <param name="c">The coefficients.</param>
        public void LoadCoeffs(short[] c) {
            coefs = new short[8][];
            coefs[0] = new short[] { c[0], c[1] };
            coefs[1] = new short[] { c[2], c[3] };
            coefs[2] = new short[] { c[4], c[5] };
            coefs[3] = new short[] { c[6], c[7] };
            coefs[4] = new short[] { c[8], c[9] };
            coefs[5] = new short[] { c[10], c[11] };
            coefs[6] = new short[] { c[12], c[13] };
            coefs[7] = new short[] { c[14], c[15] };
        }

        /// <summary>
        /// Read the info.
        /// </summary>
        /// <param name="r">The reader.</param>
        public void Read(FileReader r) {
            LoadCoeffs(r.ReadInt16s(16));
            gain = r.ReadUInt16();
            pred_scale = r.ReadUInt16();
            yn1 = r.ReadInt16();
            yn2 = r.ReadInt16();
            loop_pred_scale = r.ReadUInt16();
            loop_yn1 = r.ReadInt16();
            loop_yn2 = r.ReadInt16();
        }

        /// <summary>
        /// Write the info.
        /// </summary>
        /// <param name="w">The writer.</param>
        public void Write(FileWriter w) {
            w.Write(GetCoeffs());
            w.Write(gain);
            w.Write(pred_scale);
            w.Write(yn1);
            w.Write(yn2);
            w.Write(loop_pred_scale);
            w.Write(loop_yn1);
            w.Write(loop_yn2);
        }

        /// <summary>
        /// [8][2] array of coefficients.
        /// </summary>
        public Int16[][] coefs;

        /// <summary>
        /// Gain.
        /// </summary>
        public UInt16 gain;

        /// <summary>
        /// Predictor scale.
        /// </summary>
        public UInt16 pred_scale;

        /// <summary>
        /// History 1.
        /// </summary>
        public Int16 yn1;

        /// <summary>
        /// History 2.
        /// </summary>
        public Int16 yn2;

        /// <summary>
        /// Loop predictor scale.
        /// </summary>
        public UInt16 loop_pred_scale;

        /// <summary>
        /// Loop history 1.
        /// </summary>
        public Int16 loop_yn1;

        /// <summary>
        /// Loop history 2.
        /// </summary>
        public Int16 loop_yn2;

    }

}
