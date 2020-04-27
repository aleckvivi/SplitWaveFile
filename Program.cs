using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SplitWavFile
{
    class Program
    {
        static async Task Main(string[] args)
        {

            /*测试文件时间*/
            //var file = @"f:\ponce-preludio-in-e-major.wav";
            //var heads = await WaveFileHelper.GetWaveHeaders(file);
            //var r = WaveFileHelper.CalculateWaveTotalSeconds(heads);
            //Console.WriteLine("这首歌一共是{0}秒", r);


            /*测试读取文件头*/
            //var file = @"f:\temp\new_file_0.wav";
            //var r = await WaveFileHelper.GetWaveHeaders(file);


            /*测试分割Wave文件*/
            var file = @"f:\suite-espanola-op-47-leyenda.wav";
            var t=await WaveFileHelper.SplitWaveFileBySecondsAsync(file, 50, @"F:\Temp", "new_file_");


            Console.Read();
        }
    }
}
