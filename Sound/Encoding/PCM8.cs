using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSoundIO.Sound {

    /// <summary>
    /// A standard PCM audio.
    /// </summary>
    public class PCM8 : AudioEncoding {

        /// <summary>
        /// Sample data.
        /// </summary>
        private byte[] samples;

        /// <summary>
        /// Samples.
        /// </summary>
        public byte[] Samples => samples;

        /// <summary>
        /// Get the PCM16 samples.
        /// </summary>
        /// <returns>The PCM16 samples.</returns>
        public override short[] ToPCM16() {
            short[] pcm16 = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++) {
                pcm16[i] = (short)((samples[i] - 128) << 8);
            }
            return pcm16;
        }

        /// <summary>
        /// Set the PCM16 samples.
        /// </summary>
        public override void FromPCM16(short[] samples) {
            this.samples = new byte[samples.Length];
            for (int i = 0; i < samples.Length; i++) {
                this.samples[i] = (byte)((samples[i] >> 8) + 128);
            }
            DataSize = samples.Length;
            NumSamples = samples.Length;
        }

        /// <summary>
        /// Convert to signed PCM8.
        /// </summary>
        /// <returns>The audio as signed PCM8.</returns>
        public sbyte[] ToSignedPCM8() {
            sbyte[] pcm8 = new sbyte[samples.Length];
            for (int i = 0; i < samples.Length; i++) {
                pcm8[i] = (sbyte)(samples[i] - 128);
            }
            return pcm8;
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
            	samples = r.ReadBytes(NumSamples);
                DataSize = NumSamples;
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
            samples = new byte[(blockCount - 1) * blockSamples + lastBlockSamples];
            NumSamples = (int)((blockCount - 1) * blockSamples + lastBlockSamples);
            DataSize = NumSamples;
        }

        /// <summary>
        /// Get the number of blocks.
        /// </summary>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The number of blocks.</returns>
        public override uint NumBlocks(uint blockSize, uint blockSamples) {
            uint num = (uint)samples.Length / blockSamples;
            if (samples.Length % blockSamples != 0) { num++; }
            return num;
        }

        /// <summary>
        /// Get the size of the last block.
        /// </summary>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The size of the last block.</returns>
        public override uint LastBlockSize(uint blockSize, uint blockSamples) {
            uint num = (uint)samples.Length % blockSamples;
            if (num == 0) { num = blockSize; }
            return num;
        }

        /// <summary>
        /// Get the samples in the last block.
        /// </summary>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The amount of samples in the last block.</returns>
        public override uint LastBlockSamples(uint blockSize, uint blockSamples) {
            uint num = (uint)samples.Length % blockSamples;
            if (num == 0) { num = blockSamples; }
            return num;
        }

        /// <summary>
        /// Read a block.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="blockNum"></param>
        /// <param name="blockSize"></param>
        /// <param name="blockSamples"></param>
        public override void ReadBlock(FileReader r, int blockNum, uint blockSize, uint blockSamples) {
            byte[] arr = r.ReadBytes(blockNum == NumBlocks(blockSize, blockSamples) - 1 ? (int)LastBlockSamples(blockSize, blockSamples) : (int)blockSamples);
            arr.CopyTo(samples, blockSize * blockNum);
        }

        /// <summary>
        /// Write a block.
        /// </summary>
        /// <param name="w">The writer.</param>
        /// <param name="blockNum">The number of blocks.</param>
        /// <param name="blockSize">Size of each block.</param>
        /// <param name="blockSamples">Samples per block.</param>
        public override void WriteBlock(FileWriter w, int blockNum, uint blockSize, uint blockSamples) {
            w.Write(samples.SubArray(blockNum * (int)blockSize, blockNum == NumBlocks(blockSize, blockSamples) - 1 ? (int)LastBlockSize(blockSize, blockSamples) : (int)blockSize));
        }

    }

}