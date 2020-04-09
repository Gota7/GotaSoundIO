using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSoundIO.Sound {

    /// <summary>
    /// Blocked Interactive Multimedia Association ADPCM.
    /// </summary>
    public class BlockedImaAdpcm : AudioEncoding {

        /// <summary>
        /// Sample data.
        /// </summary>
        private byte[] samples;

        /// <summary>
        /// Block size.
        /// </summary>
        public uint BlockSize = 0x200;

        /// <summary>
        /// Block samples.
        /// </summary>
        public uint BlockSamples = 0x3F8;

        /// <summary>
        /// Get the PCM16 samples.
        /// </summary>
        /// <returns>The PCM16 samples.</returns>
        public override short[] ToPCM16() {
            List<short> s = new List<short>();
            for (int i = 0; i < NumBlocks(BlockSize, BlockSamples); i++) {
                ImaAdpcmDecoder d = new ImaAdpcmDecoder(samples, (int)BlockSize * i);
                for (int j = 0; j < BlockSamples; j++) {
                    try {
                        s.Add(d.GetSample());
                    } catch { }
                }
            }
            return s.ToArray();
        }

        /// <summary>
        /// Set the PCM16 samples.
        /// </summary>
        public override void FromPCM16(short[] samples) {
            NumSamples = samples.Length;
            List<byte[]> blocks = new List<byte[]>();
            int numBlocks = samples.Length / (int)BlockSamples;
            for (int i = 0; i < numBlocks; i++) {
                ImaAdpcmEncoder e = new ImaAdpcmEncoder();
                blocks.Add(e.Encode(samples.SubArray(i * (int)BlockSamples, (int)BlockSamples)));
            }
            if (samples.Length % BlockSamples != 0) {
                ImaAdpcmEncoder e = new ImaAdpcmEncoder();
                blocks.Add(e.Encode(samples.SubArray(numBlocks * (int)BlockSamples, samples.Length % (int)BlockSamples)));
            }
            List<byte> data = new List<byte>();
            foreach (var b in blocks) {
                foreach (var j in b) {
                    data.Add(j);
                }
            }
            this.samples = data.ToArray();
            DataSize = this.samples.Length;
        }

        /// <summary>
        /// Read the encoding.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {
            samples = r.ReadBytes(DataSize);
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
            NumSamples = (int)((blockCount - 1) * blockSamples + lastBlockSamples);
            DataSize = samples.Length;
            BlockSize = blockSize;
            BlockSamples = blockSamples;
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
            uint num = (uint)samples.Length % blockSize;
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
            uint num = (uint)NumSamples % blockSamples;
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
            byte[] arr = r.ReadBytes(blockNum == NumBlocks(blockSize, blockSamples) - 1 ? (int)LastBlockSize(blockSize, blockSamples) : (int)blockSize);
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
