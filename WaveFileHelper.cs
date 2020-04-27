using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SplitWavFile
{
    public class WaveFileHelper
    {
        const int Wave_Header_Length = 44;
        const int Wave_Header_Block_Length = 4;
        const int Wave_Header_Sub_Block_Length = 2;
        public static async Task<WaveHeader> GetWaveHeaders (string filePath)
        {
            
            byte[] buf;
            byte[] buf1;
            var result = new WaveHeader();
            using (var fs = File.Open(filePath, FileMode.Open))
            {
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
                            Console.WriteLine("固定字符：{0}", result.Fmt);
                            break;
                        case 4:
                            result.Size1 = BitConverter.ToInt32(buf);
                            Console.WriteLine("Size1：{0}", result.Size1);
                            break;
                        case 5:
                            buf1 = new byte[Wave_Header_Sub_Block_Length];
                            buf1[0] = buf[0];
                            buf1[1] = buf[1];
                            result.FormatTag = BitConverter.ToInt16(buf1);
                            Console.WriteLine("format tag：{0}", result.FormatTag);
                            buf1[0] = buf[2];
                            buf1[1] = buf[3];
                            result.Channel = BitConverter.ToInt16(buf1);
                            Console.WriteLine("通道数为：{0}", result.Channel);
                            break;
                        case 6:
                            result.SampelRate = BitConverter.ToInt32(buf);
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
                            Console.WriteLine("blockAlign：{0}", result.BlockSize);
                            buf1[0] = buf[2];
                            buf1[1] = buf[3];
                            result.BitPerSamples = BitConverter.ToInt16(buf1);
                            Console.WriteLine("bitPerSample：{0}", result.BitPerSamples);
                            break;
                        case 9:
                            result.Data = Encoding.ASCII.GetString(buf);
                            Console.WriteLine("固定字符：{0}", result.Data);
                            break;
                        case 10:
                            result.RecordDataWithHeaderLength = BitConverter.ToInt32(buf);
                            Console.WriteLine("录音数据的长度，不包括头部长度：{0}", result.RecordDataWithHeaderLength);
                            break;

                    }
                }
            }
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
            if (maxSize >= heads.RecordDataWithHeaderLength)
            {
                var saveFile = string.Format(@"{0}\{1}.wav", savePath, afterSplitName);
                File.Copy(file, saveFile);
                result.Add(saveFile);
                return result;
            }
            else
            {
                int standard_block_size = 512;
                var total = Math.Ceiling((double)heads.RecordDataWithHeaderLength / (double)maxSize);
                using (var fs = File.Open(file, FileMode.Open))
                {
                    fs.Seek(Wave_Header_Length, SeekOrigin.Begin);

                    byte[] buf = new byte[standard_block_size];

                    for (var i = 0; i < total; i++)
                    {
                        var newfileHead = heads.Clone() as WaveHeader;
                        newfileHead.RecordDataWithHeaderLength = 0;
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
                                newfileHead.RecordDataWithHeaderLength += buf.Length;
                                await newFs.WriteAsync(buf);
                            } while (len > 0 && maxSize_copy > 0);
                            newfileHead.DataLength = newfileHead.RecordDataWithHeaderLength + 44 - 8;
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

        private static async Task WriteWaveHeads(FileStream fs, WaveHeader heads)
        {
            fs.Seek(0, SeekOrigin.Begin);

            byte[] buf = new byte[Wave_Header_Block_Length];
            Encoding.ASCII.GetBytes(heads.RIFF).CopyTo(buf,0);
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
            BitConverter.GetBytes(heads.RecordDataWithHeaderLength).CopyTo(buf, 0);
            await fs.WriteAsync(buf);

            return;
        }

        public static int CalculateWaveTotalSeconds(WaveHeader heads)
        {
            var seconds = heads.RecordDataWithHeaderLength / heads.BytePerSecond;
            return seconds;
        }
    
    }
}
