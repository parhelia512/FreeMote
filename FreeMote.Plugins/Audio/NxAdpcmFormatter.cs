﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using FreeMote.Psb;
using VGAudio.Containers.Dsp;
using VGAudio.Containers.Wave;
using VGAudio.Utilities;

//REF: https://www.metroid2002.com/retromodding/wiki/DSP_(File_Format)

namespace FreeMote.Plugins.Audio
{
    [Export(typeof(IPsbAudioFormatter))]
    [ExportMetadata("Name", "FreeMote.NxAdpcm")]
    [ExportMetadata("Author", "Ulysses")]
    [ExportMetadata("Comment", "NX ADPCM support via VGAudio.")]
    class NxAdpcmFormatter : IPsbAudioFormatter
    {
        public List<string> Extensions { get; } = new List<string> { ".adpcm" };
        public bool CanToWave(IArchData archData, Dictionary<string, object> context = null)
        {
            return archData is NxArchData;
        }

        public bool CanToArchData(byte[] wave, Dictionary<string, object> context = null)
        {
            return wave != null;
        }

        public byte[] ToWave(IArchData archData, Dictionary<string, object> context = null)
        {
            DspReader reader = new DspReader();
            var data = reader.Read(archData.Data.Data);
            using MemoryStream oms = new MemoryStream();
            WaveWriter writer = new WaveWriter();
            writer.WriteToStream(data, oms, new WaveConfiguration { Codec = WaveCodec.Pcm16Bit }); //only 16Bit supported
            return oms.ToArray();
        }

        public IArchData ToArchData(AudioMetadata md, in byte[] wave, string fileName, string waveExt, Dictionary<string, object> context = null)
        {
            WaveReader reader = new WaveReader();
            var data = reader.Read(wave);
            using MemoryStream oms = new MemoryStream();
            DspWriter writer = new DspWriter();
            writer.WriteToStream(data, oms, new DspConfiguration {Endianness = Endianness.LittleEndian});
            NxArchData archData = new NxArchData { Data = new PsbResource { Data = oms.ToArray() } };
            var format = data.GetAllFormats().FirstOrDefault();
            if (format != null)
            {
                archData.SampleCount = format.SampleCount;
                archData.SampRate = format.SampleRate;
                archData.ChannelCount = format.ChannelCount;
            }

            return archData;
        }

        public bool TryGetArchData(PSB psb, PsbDictionary channel, out IArchData data, Dictionary<string, object> context = null)
        {
            data = null;
            //if (psb.Platform != PsbSpec.nx)
            //{
            //    return false;
            //}
            if (!(channel["archData"] is PsbDictionary archDic))
            {
                return false;
            }

            if (archDic["body"] is PsbDictionary body &&
                body["data"] is PsbResource aData && body["sampleCount"] is PsbNumber sampleCount
                && archDic["ext"] is PsbString ext && ext.Value == Extensions[0] &&
                archDic["samprate"] is PsbNumber sampRate)
            {
                var newData = new NxArchData
                {
                    Data = aData,
                    SampRate = sampRate.IntValue,
                    SampleCount = sampleCount.IntValue,
                    Format = PsbAudioFormat.ADPCM
                };

                if (channel["pan"] is PsbList panList)
                {
                    newData.Pan = panList;

                    if (panList.Count == 2)
                    {
                        var left = panList[0].GetFloat();
                        var right = panList[1].GetFloat();
                        if (left == 1.0f && right == 0.0f)
                        {
                            newData.ChannelPan = PsbAudioPan.Left;
                        }
                        else if (left == 0.0f && right == 1.0f)
                        {
                            newData.ChannelPan = PsbAudioPan.Right;
                        }
                    }
                }

                data = newData;
                return true;
            }

            return false;
        }
    }
}
