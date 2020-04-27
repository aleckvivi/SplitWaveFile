using System;
using System.Collections.Generic;
using System.Text;

namespace SplitWavFile
{
    public class WaveHeader:ICloneable
    {
        //"RIFF"（4个字节）
        public string RIFF { get; set; }
        //录音数据长度 +（44 -8） （4个字节）
        public Int32 DataLength { get; set; }
        //"WAVE "（4个字节）
        public string WAVE { get; set; }
        // "fmt "  （4个字节）
        public string Fmt { get; set; }
        //值为16
        public Int32 Size1 { get; set; }

        //  值为1
        public Int16 FormatTag { get; set; }

        // channel（2个字节）声道数，1为单声道，2为多声道
        public Int16 Channel { get; set; }

        //sampleRate（4个字节）采样率，值为8000，16000等
        public Int32 SampelRate { get; set; }

        //每秒所需的字节数
        public Int32 BytePerSecond { get; set; }

        //每个采样需要的字节数，计算公式：声道数 * 每个采样需要的bit  / 8
        public Int16 BlockSize { get; set; }

        //每个采样需要的bit数，一般为8或16
        public Int16 BitPerSamples { get; set; }

        //"data"（4个字节）
        public string Data { get; set; }

        //录音数据的长度，不包括头部长度
        public Int32 RecordDataWithHeaderLength { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
