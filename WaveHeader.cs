using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace SplitWavFile
{
    public class WaveHeader:ICloneable
    {
        //ChunkID
        //"RIFF"（4个字节）
        public string RIFF { get; set; }

        //ChunkSize 
        //录音数据长度 +（44 -8） （4个字节）
        //文件长度减去ChunkID和ChunkSize之后的长度
        public Int32 DataLength { get; set; }
        
        //Format
        //"WAVE "（4个字节）
        public string WAVE { get; set; }
        
        //SubChunkID
        // "fmt "  （4个字节）
        public string Fmt { get; set; }
        
        //SubChunkSize
        //如果采用PCM编码，值为16
        public Int32 Size1 { get; set; }

        //AudioFormat 音讯格式
        //如果采用PCM编码（线性量化）值为1
        public Int16 FormatTag { get; set; }

        //NumChannels
        // channel（2个字节）声道数，1为单声道，2为多声道
        public Int16 Channel { get; set; }

        //SampleRate
        //sampleRate（4个字节）采样率，值为8000，16000等
        public Int32 SampelRate { get; set; }

        //ByteRate 传输速率，单位Byte/s，value=SampleRate*NumChannels*BitPerSample/8
        //每秒所需的字节数
        public Int32 BytePerSecond { get; set; }

        //一个样点（包含所有声道）的字节数。value=NumChannels*BitPerSample/8
        //每个采样需要的字节数，计算公式：声道数 * 每个采样需要的bit  / 8
        public Int16 BlockSize { get; set; }

        //BitPerSaple
        //每个采样需要的bit数，一般为8或16
        public Int16 BitPerSamples { get; set; }


        //"data"（4个字节）
        public string Data { get; set; }

        //录音数据的长度，不包括头部长度
        public Int32 RecordDataWithoutHeaderLength { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
    /// <summary>
    /// 用于描述某个时间点的振幅
    /// </summary>
    public class AmplitudeWithTime
    {
        public Int16 Amplitude { get; set; }
        public double TimeStamp { get; set; }
    }

    public class SilenceTime
    {
        public double Start { get; set; }
        public double End { get; set; }
        public double Duration { get; set; }
    }


    public class TemplateFile
    {
        //模板文件路径
        public string File { get; set; }
        //模板文件所有的插入点位置。以秒为单位。
        public double[] InsertTimePoints { get; set; }
        //根据插入时间点换算出来的数据插入位置
        public long[] InsertPositions { get; set; }
    }
    public class AppendWaveData
    {
        //模板文件
        public TemplateFile T_File { get; set; }

        //所有待插入
        public IList<string> WaveFiles { get; set; }
        public byte[] Data { get; set; }
        public int Length { get; set; }
    }
}
