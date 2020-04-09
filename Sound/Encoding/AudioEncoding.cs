using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSoundIO.Sound {

    /// <summary>
    /// Audio encoding.
    /// </summary>
    public abstract class AudioEncoding : IReadable, IWritable {
        
        /// <summary>
        /// Number of samples.
        /// </summary>
        public int NumSamples = -1;    

        /// <summary>
        /// Size of the data in bytes.
        /// </summary>
        public int DataSize = -1; 

        /// <summary>
        /// Load data from another encoding.
        /// </summary>
        /// <parm name="other">The other encoding to convert from.</param>
        public void FromOtherEncoding(AudioEncoding other) {

            //Simply just load the converted PCM16 data.
            FromPCM16(other.ToPCM16());

        }

        /// <summary>
        /// Convert channels.
        /// </summary>
        /// <param name="type">Type to convert channels to.</param>
        /// <param name="channels">Channels to convert.</param>
        /// <returns>A list of converted channels.</returns>
        public static List<AudioEncoding> ConvertChannels(Type type, List<AudioEncoding> channels) {
            List<AudioEncoding> chan = new List<AudioEncoding>();
            foreach (var c in channels) {
                var e = Activator.CreateInstance(type);
                (e as AudioEncoding).FromOtherEncoding(c);
                chan.Add(e as AudioEncoding);
            }
            return chan;
        }

        /// <summary>
        /// Convert the encoding to PCM16.
        /// </summary>
        /// <returns>The data as PCM16.</returns>
        public abstract short[] ToPCM16();

        /// <summary>
        /// Load data from PCM16 encoding.
        /// </summary>
        /// <param name="samples">PCM16 samples to load from.</param>
        public abstract void FromPCM16(short[] samples);

        /// <summary>
        /// Read the encoding.
        /// </summary>
        /// <param name="r">The reader.</param>
        public abstract void Read(FileReader r);

        /// <summary>
        /// Write the encoding.
        /// </summary>
        /// <param name="w">The writer.</param>
        public abstract void Write(FileWriter w);

        /// <summary>
        /// Init the arrays from blocks.
        /// </summary>
        /// <param name="blockCount">Block count.</param>
        /// <param name="blockSize">Block size.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <param name="lastBlockSize">Last block size.</param>
        /// <param name="lastBlockSamples">Samples in the last block.</param>
        public abstract void InitFromBlocks(uint blockCount, uint blockSize, uint blockSamples, uint lastBlockSize, uint lastBlockSamples);

        /// <summary>
        /// Number of blocks.
        /// </summary>
        /// <param name="blockSize">Block size.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The number of blocks.</returns>
        public abstract uint NumBlocks(uint blockSize, uint blockSamples);

        /// <summary>
        /// Get the size of the last block.
        /// </summary>
        /// <param name="blockSize">Block size.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The last block size.</returns>
        public abstract uint LastBlockSize(uint blockSize, uint blockSamples);

        /// <summary>
        /// Get the amount of samples in the last block.
        /// </summary>
        /// <param name="blockSize">Block size.</param>
        /// <param name="blockSamples">Samples per block.</param>
        /// <returns>The number of samples in the last block</param>
        public abstract uint LastBlockSamples(uint blockSize, uint blockSamples);

        /// <summary>
        /// Read a block.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="blockNum">The block number.</param>
        /// <param name="blockSize">The block size.</param>
        /// <param name="blockSamples">Samples per block.</param>
        public abstract void ReadBlock(FileReader r, int blockNum, uint blockSize, uint blockSamples);

        /// <summary>
        /// Write a block.
        /// </summary>
        /// <param name="w">The writer.</param>
        /// <param name="blockNum">Block number.</param>
        /// <param name="blockSize">Block size.</param>
        /// <param name="blockSamples">Samples per block.</param>
        public abstract void WriteBlock(FileWriter w, int blockNum, uint blockSize, uint blockSamples);

    }

}
