using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSoundIO.Sound {

    /// <summary>
    /// A streamed audio file.
    /// </summary>
    public abstract class SoundFile : IOFile {

        /// <summary>
        /// Get the supported encodings.
        /// </summary>
        /// <returns>The supported encodings.</returns>
        public abstract Type[] SupportedEncodings();

        /// <summary>
        /// The name of the format.
        /// </summary>
        /// <returns>The name of the file.</returns>
        public abstract string Name();

        /// <summary>
        /// Extensions for the sound file.
        /// </summary>
        /// <returns>The extensions.</returns>
        public abstract string[] Extensions();

        /// <summary>
        /// Description of the sound file.
        /// </summary>
        /// <returns>The description.</returns>
        public abstract string Description();

        /// <summary>
        /// If the file supports tracks.
        /// </summary>
        /// <returns>If the file supports tracks.</returns>
        public abstract bool SupportsTracks();

        /// <summary>
        /// Get the preferred encoding.
        /// </summary>
        /// <returns>The preferred encoding.</returns>
        public abstract Type PreferredEncoding();

        /// <summary>
        /// Blank constructor.
        /// </summary>
        public SoundFile() { }

        /// <summary>
        /// Read an audio file from a file path.
        /// </summary>
        /// <param name="filePath">The file to read.</param>
        public SoundFile(string filePath) : base(filePath) { }

        /// <summary>
        /// Channels.
        /// </summary>
        public List<AudioEncoding> Channels = new List<AudioEncoding>();

        /// <summary>
        /// If the audio loops.
        /// </summary>
        [DisplayName("Loops")]
        [Category("Sound Info")]
        [Description("If a sound loops or not.")]
        public bool Loops { get; set; }

        /// <summary>
        /// Start loop.
        /// </summary>
        [DisplayName("Loop Start Sample")]
        [Category("Sound Info")]
        [Description("The loop start sample.")]
        public uint LoopStart { get; set; }

        /// <summary>
        /// Original loop start.
        /// </summary>
        [DisplayName("Original Loop Start Sample")]
        [Category("Sound Info")]
        [Description("The original loop start sample.")]
        public uint OriginalLoopStart { get; set; }

        /// <summary>
        /// Ending loop.
        /// </summary>
        [DisplayName("Loop End Sample")]
        [Category("Sound Info")]
        [Description("The loop end sample.")]
        public uint LoopEnd { get; set; }

        /// <summary>
        /// The sample rate of the audio file.
        /// </summary>
        [ReadOnly(true)]
        [DisplayName("Sample Rate")]
        [Category("Sound Info")]
        [Description("How many samples are played per second.")]
        public uint SampleRate { get; set; }

        /// <summary>
        /// The loop start, but in a human readable form.
        /// </summary>
        [Category("Sound Info")]
        [DisplayName("Loop Start")]
        [Description("The loop start.")]
        public TimeSpan ReadableLoopStart {
            get {
                return new TimeSpan(0, 0, 0, (int)(LoopStart / SampleRate), (int)((int)(LoopStart % SampleRate) / (float)SampleRate * 1000));
            }
            set {
                uint val = (uint)(value.TotalSeconds * SampleRate);
                if (val > Channels[0].NumSamples) {
                    val = (uint)Channels[0].NumSamples;
                }
                LoopStart = val;
            }
        }

        /// <summary>
        /// The loop end, but in a human readable form.
        /// </summary>
        [Category("Sound Info")]
        [DisplayName("Loop Start")]
        [Description("The loop start.")]
        public TimeSpan ReadableLoopEnd {
            get {
                return new TimeSpan(0, 0, 0, (int)(LoopEnd / SampleRate), (int)((int)(LoopEnd % SampleRate) / (float)SampleRate * 1000));
            }
            set {
                uint val = (uint)(value.TotalSeconds * SampleRate);
                if (val > Channels[0].NumSamples) {
                    val = (uint)Channels[0].NumSamples;
                }
                LoopEnd = val;
            }
        }

        public int LoopStartHours {
            get => ReadableLoopStart.Hours;
            set { ReadableLoopStart = new TimeSpan(0, value, ReadableLoopStart.Minutes, ReadableLoopStart.Seconds, ReadableLoopStart.Milliseconds); }
        }

        public int LoopStartSeconds {
            get => ReadableLoopStart.Seconds;
            set { ReadableLoopStart = new TimeSpan(0, ReadableLoopStart.Hours, ReadableLoopStart.Minutes, value, ReadableLoopStart.Milliseconds); }
        }

        /// <summary>
        /// Tracks.
        /// </summary>
        [EditableCategory]
        public List<TrackData> Tracks = new List<TrackData>();

        /// <summary>
        /// Track data.
        /// </summary>
        public abstract class TrackData {

            /// <summary>
            /// Channels.
            /// </summary>
            [Editable]
            public List<int> Channels = new List<int>();

        }

        /// <summary>
        /// When converting from another streamed audio file.
        /// </summary>
        public abstract void OnConversion();

        /// <summary>
        /// Convert from another streamed audio file.
        /// </summary>
        /// <param name="other">The other file converted from.</param>
        public void FromOtherStreamFile(SoundFile other) {

            //Set data.
            Loops = other.Loops;
            LoopStart = other.LoopStart;
            LoopEnd = other.LoopEnd;
            SampleRate = other.SampleRate;

            //Set other data. TODO.

            //Convert to preffered encoding.
            if (PreferredEncoding() != null && !other.Channels[0].GetType().Equals(PreferredEncoding())) {
                Channels = AudioEncoding.ConvertChannels(PreferredEncoding(), other.Channels);
                OnConversion();
                return;
            }

            //If any of the encodings match then that's good.
            var e = SupportedEncodings();
            foreach (var t in e) {
                if (t.Equals(other.Channels[0].GetType())) {
                    Channels = other.Channels;
                    OnConversion();
                    return;
                }
            }

            //By default convert to first type on list.
            Channels = AudioEncoding.ConvertChannels(SupportedEncodings()[0], other.Channels);

            //Activate on conversion hook.
            OnConversion();

        }

        /// <summary>
        /// Convert from another stream file.
        /// </summary>
        /// <param name="other">Other stream file.</param>
        /// <param name="audioEncoding">Audio encoding to use.</param>
        public void FromOtherStreamFile(SoundFile other, Type audioEncoding) {

            //Set data.
            Loops = other.Loops;
            LoopStart = other.LoopStart;
            LoopEnd = other.LoopEnd;
            SampleRate = other.SampleRate;

            //Set other data. TODO.

            //Set channel data.
            Channels = AudioEncoding.ConvertChannels(audioEncoding, other.Channels);

            //Activate on conversion hook.
            OnConversion();

        }

        /// <summary>
        /// Change the encoding.
        /// </summary>
        /// <param name="newEncoding">The new sound encoding.</param>
        public void ChangeEncoding(Type newEncoding) {

            //Don't do anything if type is the same.
            if (!newEncoding.Equals(Channels[0].GetType())) {

                //Do conversion.
                Channels = AudioEncoding.ConvertChannels(newEncoding, Channels);

            }

        }

        /// <summary>
        /// Mix the sound file to mono. This also converts the encoding to PCM16.
        /// </summary>
        public void MixToMono() {

            //Already mono.
            if (Channels.Count < 2) {
                return;
            }

            //Convert to PCM16.
            ChangeEncoding(typeof(PCM16));

            //Set new channel data.
            short[][] samples = Channels.Select(x => (x as PCM16).Samples).ToArray();
            short[] newSamples = new short[samples[0].Length];
            for (int i = 0; i < newSamples.Length; i++) {
                double val = 0;
                for (int j = 0; j < samples.Length; j++) {
                    val += samples[j][i] / 32768d;
                }
                val *= .707;
                if (val > .9999694824) { val = .9999694824; }
                if (val < -1) { val = -1; }
                newSamples[i] = (short)(val * 32768);
            }

            //Set data.
            Channels = new List<AudioEncoding>() { new PCM16(newSamples) };
            foreach (var t in Tracks) {
                t.Channels = new List<int>() { 0 };
            }

        }

        /// <summary>
        /// Mix the sound file to stereo. This also converts the encoding to PCM16.
        /// </summary>
        /// <param name="channelMutes">Channels to just mute.</param>
        public void MixToStereo(bool[] channelMutes = null) {

            //Already stereo.
            if (Channels.Count == 2) {
                return;
            }

            //Mono.
            if (Channels.Count < 2) {
                short[] samples = Channels[0].ToPCM16();
                short[][] newSamples = new short[2][];
                for (int i = 0; i < 2; i++) {
                    newSamples[i] = new short[samples.Length];
                }
                for (int i = 0; i < samples.Length; i++) {
                    for (int j = 0; j < 2; j++) {
                        newSamples[j][i] = samples[i];
                    }
                }
                Channels = new List<AudioEncoding>() { new PCM16(newSamples[0]), new PCM16(newSamples[1]) };
                foreach (var t in Tracks.Where(x => x.Channels.Contains(0))) {
                    t.Channels = new List<int>() { 0, 1 };
                }
            }

            //More than mono.
            else {

                //First get mutes.
                bool[] mutes = channelMutes;
                if (mutes == null) {
                    mutes = new bool[Channels.Count];
                }

                //Get samples to mix.
                short[][] samples = new short[mutes.Where(x => x == false).Count()][];
                int[] channelModes = new int[samples.Length];
                int chanPtr = 0;
                for (int i = 0; i < mutes.Length; i++) {
                    if (!mutes[i]) {
                        samples[chanPtr] = Channels[i].ToPCM16();
                        if (i % 2 == 1) { channelModes[chanPtr] = 1; }
                        chanPtr++;
                    }
                }

                //Last channel is mono.
                if (Channels.Count % 2 != 0 && !mutes.Last()) {
                    channelModes[channelModes.Length - 1] = 2;
                }

                //New samples.
                short[][] newSamples = new short[2][];
                for (int i = 0; i < 2; i++) {
                    newSamples[i] = new short[samples[0].Length];
                }

                //Mix the channels.
                for (int i = 0; i < newSamples[0].Length; i++) {

                    //Initial values.
                    double l = 0;
                    double r = 0;

                    //Switch the channel mode.
                    for (int j = 0; j < channelModes.Length; j++) {
                        switch (channelModes[j]) {

                            //Left.
                            case 0:
                                l += samples[j][i] / 32768d;
                                break;

                            //Right.
                            case 1:
                                r += samples[j][i] / 32768d;
                                break;

                            //Center.
                            case 2:
                                l += samples[j][i] / 32768d;
                                r += samples[j][i] / 32768d;
                                break;

                        }

                    }

                    //Try to stop clipping. This is actually 1/Sqrt(2) which should significantly decrease clipping chances without much audible volume change.
                    l *= .707;
                    r *= .707;

                    //Set limits.
                    if (l > .9999694824) { l = .9999694824; }
                    if (r > .9999694824) { r = .9999694824; }
                    if (l < -1) { l = -1; }
                    if (r < -1) { r = -1; }

                    //Set data.
                    newSamples[0][i] = (short)(l * 32768);
                    newSamples[1][i] = (short)(r * 32768);

                }

                //Set new channels.
                Channels = new List<AudioEncoding>() { new PCM16(newSamples[0]), new PCM16(newSamples[1]) };

            }

        }

    }

}
