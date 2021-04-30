using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SplitWavFile
{
    public class WaveFileHelper
    {
        const int Wave_Header_Length = 44;
        //每4个字节为一个wave 头的数据块
        const int Wave_Header_Block_Length = 4;
        const int Wave_Header_Sub_Block_Length = 2;
        //静默振幅的阈值
        const int SILENCE_UP_VALUE = 400;
        //最小允许静默时间(毫秒）,小于此间隔忽略
        const int SILENCE_MINI_DURATION = 500;


        public static async Task<WaveHeader> GetWaveHeaders(string filePath,short channel=1, int sampleRate=8000, short bitPerSample =16)
        {
            using (var fs = File.Open(filePath, FileMode.Open))
            {
                return await GetWaveHeaders(fs);
            }
        }

        public static async Task<WaveHeader> GetWaveHeaders(Stream fs, short channel = 1, int sampleRate = 8000, short bitPerSample = 16)
        {
            byte[] buf;
            byte[] buf1;
            var result = new WaveHeader();
            Console.WriteLine("文件长度: {0}", fs.Length);
            for (var i = 0; i < Wave_Header_Length; i += Wave_Header_Block_Length)
            {
                var index = i / 4;
                buf = new byte[Wave_Header_Block_Length];
                //指定位移
                fs.Seek(index * Wave_Header_Block_Length, SeekOrigin.Begin);
                //读取指定位置的数值
                await fs.ReadAsync(buf, 0, Wave_Header_Block_Length);
                switch (index)
                {
                    case 0:
                        result.RIFF = Encoding.ASCII.GetString(buf);
                        Console.WriteLine("类型是：{0}", result.RIFF);
                        break;
                    case 1:
                        result.DataLength = BitConverter.ToInt32(buf);
                        Console.WriteLine("数据长度为：{0}", result.DataLength);
                        break;
                    case 2:
                        result.WAVE = Encoding.ASCII.GetString(buf);
                        Console.WriteLine("固定字符：{0}", result.WAVE);
                        break;
                    case 3:
                        result.Fmt = Encoding.ASCII.GetString(buf);
                        if (result.Fmt != "fmt ") result.Fmt = "fmt ";
                        Console.WriteLine("固定字符：{0}", result.Fmt);
                        break;
                    case 4:
                        result.Size1 = BitConverter.ToInt32(buf);
                        if (result.Size1 != 16) result.Size1 = 16;
                        Console.WriteLine("Size1：{0}", result.Size1);
                        break;
                    case 5:
                        buf1 = new byte[Wave_Header_Sub_Block_Length];
                        buf1[0] = buf[0];
                        buf1[1] = buf[1];
                        result.FormatTag = BitConverter.ToInt16(buf1);
                        if (result.FormatTag != 1) result.FormatTag = 1;
                        Console.WriteLine("format tag：{0}", result.FormatTag);
                        buf1[0] = buf[2];
                        buf1[1] = buf[3];
                        result.Channel = BitConverter.ToInt16(buf1);
                        if (result.Channel == 0) result.Channel = channel;
                        
                        Console.WriteLine("通道数为：{0}", result.Channel);
                        break;
                    case 6:
                        result.SampelRate = BitConverter.ToInt32(buf);
                        if (result.SampelRate == 0) result.SampelRate = sampleRate;
                        Console.WriteLine("采样率为：{0}", result.SampelRate);
                        break;
                    case 7:
                        result.BytePerSecond = BitConverter.ToInt32(buf);
                        Console.WriteLine("bytePerSec：{0}", result.BytePerSecond);
                        break;
                    case 8:
                        buf1 = new byte[Wave_Header_Sub_Block_Length];
                        buf1[0] = buf[0];
                        buf1[1] = buf[1];
                        result.BlockSize = BitConverter.ToInt16(buf1);
                        //if (result.BlockSize != 2) result.BlockSize = 2;
                        Console.WriteLine("blockAlign：{0}", result.BlockSize);
                        buf1[0] = buf[2];
                        buf1[1] = buf[3];
                        result.BitPerSamples = BitConverter.ToInt16(buf1);
                        if (result.BitPerSamples == 0) result.BitPerSamples = bitPerSample;
                        Console.WriteLine("bitPerSample：{0}", result.BitPerSamples);
                        break;
                    case 9:
                        result.Data = Encoding.ASCII.GetString(buf);
                        if (result.Data != "data") result.Data = "data";
                        Console.WriteLine("固定字符：{0}", result.Data);
                        break;
                    case 10:
                        result.RecordDataWithoutHeaderLength = BitConverter.ToInt32(buf);
                        if (result.RecordDataWithoutHeaderLength == 0) result.RecordDataWithoutHeaderLength = result.DataLength - 32;
                        Console.WriteLine("录音数据的长度，不包括头部长度：{0}", result.RecordDataWithoutHeaderLength);
                        break;

                }
            }
            if (result.BytePerSecond == 0) result.BytePerSecond = result.SampelRate * result.BitPerSamples * result.Channel / 8;
            if (result.BlockSize == 0) result.BlockSize =(short)( result.Channel * result.BitPerSamples / 8);
            return result;
        }


        /// <summary>
        /// 根据时间来分割Wave文件
        /// </summary>
        /// <param name="file"></param>
        /// <param name="seconds"></param>
        /// <param name="afterSplitName"></param>
        /// <returns></returns>
        public static async Task<IList<string>> SplitWaveFileBySecondsAsync(string file, int seconds, string savePath, string afterSplitName)
        {
            var waveHeads = await GetWaveHeaders(file);
            var splitFileSize = waveHeads.BytePerSecond * seconds;
            return await SplitWaveAsyncBySize(file, waveHeads, splitFileSize, savePath, afterSplitName);

        }

        private static async Task<IList<string>> SplitWaveAsyncBySize(string file, WaveHeader heads, Int32 maxSize, string savePath, string afterSplitName)
        {
            IList<string> result = new List<string>();
            if (maxSize >= heads.RecordDataWithoutHeaderLength)
            {
                var saveFile = string.Format(@"{0}\{1}.wav", savePath, afterSplitName);
                File.Copy(file, saveFile);
                result.Add(saveFile);
                return result;
            }
            else
            {
                int standard_block_size = 512;
                var total = Math.Ceiling((double)heads.RecordDataWithoutHeaderLength / (double)maxSize);
                using (var fs = File.Open(file, FileMode.Open))
                {
                    fs.Seek(Wave_Header_Length, SeekOrigin.Begin);

                    byte[] buf = new byte[standard_block_size];

                    for (var i = 0; i < total; i++)
                    {
                        var newfileHead = heads.Clone() as WaveHeader;
                        newfileHead.RecordDataWithoutHeaderLength = 0;
                        //设定的分割文件最大长度，读取文件数据的长度
                        int maxSize_copy = maxSize, len = 0;
                        var saveFile = string.Format(@"{0}\{1}{2}.wav", savePath, afterSplitName, i);
                        using (var newFs = File.Open(saveFile, FileMode.Create))
                        {
                            do
                            {
                                //获取读取数据块的大小
                                len = await fs.ReadAsync(buf, 0, standard_block_size);
                                //控制文件大小在指定的最大长度左右
                                maxSize_copy -= len;
                                //累计分割文件的实际大小
                                newfileHead.RecordDataWithoutHeaderLength += buf.Length;
                                await newFs.WriteAsync(buf);
                            } while (len > 0 && maxSize_copy > 0);
                            newfileHead.DataLength = newfileHead.RecordDataWithoutHeaderLength + 44 - 8;
                            await WriteWaveHeads(newFs, newfileHead);
                            await newFs.FlushAsync();
                        }
                        result.Add(saveFile);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// 根据文件大小来分割Wave文件
        /// </summary>
        /// <param name="file"></param>
        /// <param name="maxSize"></param>
        /// <param name="afterSplitName"></param>
        /// <returns></returns>
        public static async Task<IList<string>> SplitWaveAsyncBySize(string file, Int32 maxSize, string savePath, string afterSplitName)
        {
            var waveHeads = await GetWaveHeaders(file);
            return await SplitWaveAsyncBySize(file, waveHeads, maxSize, savePath, afterSplitName);
        }

        /// <summary>
        /// 返回文件各个采样点的振幅数据
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<IList<AmplitudeWithTime>> AnalysisWave(string file, bool compress=false)
        {
            using (var fs = File.Open(file, FileMode.Open))
            {
                return await AnalysisWave(fs, compress);
            }
        }

        /// <summary>
        /// 返回文件各个采样点的振幅数据
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        public static async Task<IList<AmplitudeWithTime>> AnalysisWave(Stream fs, bool compress = false)
        {
            IList<AmplitudeWithTime> Amplitudes = new List<AmplitudeWithTime>();
            var headers = await GetWaveHeaders(fs);
            var sampleBytes = headers.BitPerSamples / 8;

            fs.Seek(Wave_Header_Length, SeekOrigin.Begin);
            byte[] buf = new byte[sampleBytes];
            double index = 0;
            while (await fs.ReadAsync(buf, 0, sampleBytes) > 0)
            {
                try
                {
                    Amplitudes.Add(
                    new AmplitudeWithTime { Amplitude = BitConverter.ToInt16(buf), TimeStamp = sampleBytes * Math.Round(index / (double)headers.BytePerSecond, 3) }
                    );
                    index++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("分析文件数据时发生错误：{0}",ex.Message);
                }
            }
            if (compress)
                return CompressData(Amplitudes);
            else
             return Amplitudes;
        }


        private static IList<AmplitudeWithTime> CompressData(IList<AmplitudeWithTime> source)
        {
            double currentTimeStamp = -1;
            int currentTimeStampTimes = 0;
            int currentTimeStampAmplitudes = 0;
            IList<AmplitudeWithTime> result = new List<AmplitudeWithTime>();

            foreach (var am in source)
            {
                if (am.TimeStamp != currentTimeStamp)
                {
                    if (currentTimeStampTimes != 0)
                    {
                        //结束当前时间点的处理，将累计的数据平均化后压入结果列表。
                        result.Add(new AmplitudeWithTime { TimeStamp = currentTimeStamp, Amplitude =Convert.ToInt16(currentTimeStampAmplitudes / currentTimeStampTimes) });
                        currentTimeStampTimes = 0;
                        currentTimeStampAmplitudes = 0;
                    }
                    currentTimeStamp = am.TimeStamp;
                    
                }
                //累加振幅
                currentTimeStampAmplitudes += am.Amplitude;
                //累加次数
                currentTimeStampTimes++;
            }
            if (currentTimeStampTimes > 0)
            {
                //将剩余累计的数据平均化后压入结果列表。
                result.Add(new AmplitudeWithTime { TimeStamp = currentTimeStamp, Amplitude = Convert.ToInt16(currentTimeStampAmplitudes / currentTimeStampTimes) });
            }
            return result;
        }

        public static async Task<IList<SilenceTime>> QueryWaveSilences(string file,int silenceValue= SILENCE_UP_VALUE,int silenceDuration=SILENCE_MINI_DURATION)
        {
            using(var fs = File.OpenRead(file))
            {
                return await QueryWaveSilences(fs, silenceValue, silenceDuration);
            }
        }

        public static async Task<IList<SilenceTime>> QueryWaveSilences(Stream fs,int silenceValue= SILENCE_UP_VALUE, int silenceDuration = SILENCE_MINI_DURATION)
        {
            double duration = Math.Round((double)silenceDuration / (double)1000,3);
            bool isDialogBegin = false;
            var data = await AnalysisWave(fs, true);
            IList<SilenceTime> result = new List<SilenceTime>();
            SilenceTime current = null;
            foreach (var am in data)
            {
                am.Amplitude = Math.Abs(am.Amplitude);
                if (am.Amplitude <= silenceValue && isDialogBegin)
                {
                    //振幅小于阈值，对当前的静默对象进行处理
                    if (current != null)
                        current.End = am.TimeStamp;
                    else
                        current = new SilenceTime { Start = am.TimeStamp, End = am.TimeStamp };
                }
                else if (am.Amplitude > silenceValue)
                {
                    //振幅大于阈值，判断之前的静默是否符合静默时间的要求
                    if (!isDialogBegin) isDialogBegin = true;
                    if (current != null)
                    {
                        current.Duration = current.End - current.Start;
                        if (current.Duration > duration)
                            result.Add(current);
                        current = null;
                    }
                }
            }
            //最后的静默对象不用处理，忽略
            return result;

        }



        /// <summary>
        /// 在文件头上插入符合该wave文件的wav header
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="heads"></param>
        /// <returns></returns>
        private static async Task WriteWaveHeads(FileStream fs, WaveHeader heads)
        {
            fs.Seek(0, SeekOrigin.Begin);

            byte[] buf = new byte[Wave_Header_Block_Length];
            Encoding.ASCII.GetBytes(heads.RIFF).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Block_Length];
            BitConverter.GetBytes(heads.DataLength).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Block_Length];
            Encoding.ASCII.GetBytes(heads.WAVE).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Block_Length];
            Encoding.ASCII.GetBytes(heads.Fmt).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Block_Length];
            BitConverter.GetBytes(heads.Size1).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Sub_Block_Length];
            BitConverter.GetBytes(heads.FormatTag).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Sub_Block_Length];
            BitConverter.GetBytes(heads.Channel).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Block_Length];
            BitConverter.GetBytes(heads.SampelRate).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Block_Length];
            BitConverter.GetBytes(heads.BytePerSecond).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Sub_Block_Length];
            BitConverter.GetBytes(heads.BlockSize).CopyTo(buf, 0);
            await fs.WriteAsync(buf);


            buf = new byte[Wave_Header_Sub_Block_Length];
            BitConverter.GetBytes(heads.BitPerSamples).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Block_Length];
            Encoding.ASCII.GetBytes(heads.Data).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            buf = new byte[Wave_Header_Block_Length];
            BitConverter.GetBytes(heads.RecordDataWithoutHeaderLength).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            return;
        }

        /// <summary>
        /// 计算wave文件的长度
        /// </summary>
        /// <param name="heads"></param>
        /// <returns></returns>
        public static int CalculateWaveTotalSeconds(WaveHeader heads)
        {
            var seconds = heads.RecordDataWithoutHeaderLength / heads.BytePerSecond;
            return seconds;
        }



    }
}
