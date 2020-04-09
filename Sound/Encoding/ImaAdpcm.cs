using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSoundIO.Sound {

    /// <summary>
    /// Interactive Multimedia Association ADPCM.
    /// </summary>
    public class ImaAdpcm : AudioEncoding {

        /// <summary>
        /// Sample data.
        /// </summary>
        private byte[] samples;

        /// <summary>
        /// Get the PCM16 samples.
        /// </summary>
        /// <returns>The PCM16 samples.</returns>
        public override short[] ToPCM16() {
            List<short> s = new List<short>();
            ImaAdpcmDecoder d = new ImaAdpcmDecoder(samples, 0);
            for (int i = 0; i < NumSamples; i++) {
                try {
                    s.Add(d.GetSample());
                } catch { }
            }
            return s.ToArray();
        }

        /// <summary>
        /// Set the PCM16 samples.
        /// </summary>
        public override void FromPCM16(short[] samples) {
            NumSamples = samples.Length;
            ImaAdpcmEncoder e = new ImaAdpcmEncoder();
            this.samples = e.Encode(samples);
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

    /// <summary>
    /// ImaAdpcm encoder.
    /// </summary>
    public class ImaAdpcmEncoder {
        private bool IsInit;
        private int Last;
        private int Index;

        public byte[] Encode(short[] WaveData) {
            List<byte> byteList = new List<byte>();
            int num = 0;
            if (!this.IsInit) {
                this.Last = (int)WaveData[0];
                this.Index = this.GetBestTableIndex(((int)WaveData[1] - (int)WaveData[0]) * 8);
                MemoryStream o = new MemoryStream();
                FileWriter bw = new FileWriter(o);
                bw.ByteOrder = ByteOrder.LittleEndian;
                bw.Write(WaveData[0]);
                bw.Write((short)this.Index);
                byte[] Data = o.ToArray();
                byteList.AddRange((IEnumerable<byte>)Data);
                ++num;
                this.IsInit = true;
            }
            byte[] numArray = new byte[WaveData.Length - num];
            for (int index = num; index < WaveData.Length; ++index) {
                int bestConfig = this.GetBestConfig(this.Index, (int)WaveData[index] - this.Last);
                numArray[index - num] = (byte)bestConfig;
                this.Last = (int)ImaAdpcmMath.ClampSample(this.Last + (ImaAdpcmMath.StepTable[this.Index] / 8 + ImaAdpcmMath.StepTable[this.Index] / 4 * (bestConfig & 1) + ImaAdpcmMath.StepTable[this.Index] / 2 * (bestConfig >> 1 & 1) + ImaAdpcmMath.StepTable[this.Index] * (bestConfig >> 2 & 1)) * ((bestConfig >> 3 & 1) == 1 ? -1 : 1));
                this.Index = ImaAdpcmMath.ClampIndex(this.Index + ImaAdpcmMath.IndexTable[bestConfig & 7]);
            }
            int index1 = 0;
            while (index1 < numArray.Length) {
                if (index1 == numArray.Length - 1)
                    byteList.Add(numArray[index1]);
                else
                    byteList.Add((byte)((uint)numArray[index1] | (uint)numArray[index1 + 1] << 4));
                index1 += 2;
            }
            return byteList.ToArray();
        }

        private int GetBestTableIndex(int Diff) {
            int num1 = int.MaxValue;
            int num2 = -1;
            for (int index = 0; index < ImaAdpcmMath.StepTable.Length; ++index) {
                int num3 = Math.Abs(Math.Abs(Diff) - ImaAdpcmMath.StepTable[index]);
                if (num3 < num1) {
                    num1 = num3;
                    num2 = index;
                }
            }
            return num2;
        }

        private int GetBestConfig(int Index, int Diff) {
            int num1 = 0;
            if (Diff < 0)
                num1 |= 8;
            Diff = Math.Abs(Diff);
            int num2 = ImaAdpcmMath.StepTable[Index] / 8;
            if (Math.Abs(num2 - Diff) >= ImaAdpcmMath.StepTable[Index]) {
                num1 |= 4;
                num2 += ImaAdpcmMath.StepTable[Index];
            }
            if (Math.Abs(num2 - Diff) >= ImaAdpcmMath.StepTable[Index] / 2) {
                num1 |= 2;
                num2 += ImaAdpcmMath.StepTable[Index] / 2;
            }
            if (Math.Abs(num2 - Diff) >= ImaAdpcmMath.StepTable[Index] / 4) {
                num1 |= 1;
                int num3 = num2 + ImaAdpcmMath.StepTable[Index] / 4;
            }
            return num1;
        }
    }

    /// <summary>
    /// ImaAdpcm decoder.
    /// </summary>
    public class ImaAdpcmDecoder {
        private byte[] _data;

        public int Last { get; set; }

        public int Index { get; set; }

        public int Offset { get; set; }

        public bool SecondNibble { get; set; }

        public ImaAdpcmDecoder(byte[] Data, int Offset) {
            this.Last = (int)(short)((int)Data[Offset] | (int)Data[Offset + 1] << 8);
            this.Index = (int)(short)((int)Data[Offset + 2] | (int)Data[Offset + 3] << 8) & (int)sbyte.MaxValue;
            Offset += 4;
            this.Offset = Offset;
            this._data = Data;
        }

        public short GetSample() {
            short sample = this.GetSample((byte)((int)this._data[this.Offset] >> (this.SecondNibble ? 4 : 0) & 15));
            if (this.SecondNibble)
                ++this.Offset;
            this.SecondNibble = !this.SecondNibble;
            return sample;
        }

        private short GetSample(byte nibble) {
            this.Last = (int)ImaAdpcmMath.ClampSample(this.Last + (ImaAdpcmMath.StepTable[this.Index] / 8 + ImaAdpcmMath.StepTable[this.Index] / 4 * ((int)nibble & 1) + ImaAdpcmMath.StepTable[this.Index] / 2 * ((int)nibble >> 1 & 1) + ImaAdpcmMath.StepTable[this.Index] * ((int)nibble >> 2 & 1)) * (((int)nibble >> 3 & 1) == 1 ? -1 : 1));
            this.Index = ImaAdpcmMath.ClampIndex(this.Index + ImaAdpcmMath.IndexTable[(int)nibble & 7]);
            return (short)this.Last;
        }
    }

    /// <summary>
    /// ImaAdpcm math.
    /// </summary>
    public class ImaAdpcmMath {
        public static readonly int[] IndexTable = new int[16]
        {
      -1,
      -1,
      -1,
      -1,
      2,
      4,
      6,
      8,
      -1,
      -1,
      -1,
      -1,
      2,
      4,
      6,
      8
        };
        public static readonly int[] StepTable = new int[89]
        {
      7,
      8,
      9,
      10,
      11,
      12,
      13,
      14,
      16,
      17,
      19,
      21,
      23,
      25,
      28,
      31,
      34,
      37,
      41,
      45,
      50,
      55,
      60,
      66,
      73,
      80,
      88,
      97,
      107,
      118,
      130,
      143,
      157,
      173,
      190,
      209,
      230,
      253,
      279,
      307,
      337,
      371,
      408,
      449,
      494,
      544,
      598,
      658,
      724,
      796,
      876,
      963,
      1060,
      1166,
      1282,
      1411,
      1552,
      1707,
      1878,
      2066,
      2272,
      2499,
      2749,
      3024,
      3327,
      3660,
      4026,
      4428,
      4871,
      5358,
      5894,
      6484,
      7132,
      7845,
      8630,
      9493,
      10442,
      11487,
      12635,
      13899,
      15289,
      16818,
      18500,
      20350,
      22385,
      24623,
      27086,
      29794,
      (int) short.MaxValue
        };

        public static short ClampSample(int value) {
            if (value < -32767)
                value = -32767;
            if (value > (int)short.MaxValue)
                value = (int)short.MaxValue;
            return (short)value;
        }

        public static int ClampIndex(int value) {
            if (value < 0)
                value = 0;
            if (value > 88)
                value = 88;
            return value;
        }
    }

}
