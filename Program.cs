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
            //var file = @"f:\suite-espanola-op-47-leyenda.wav";
            //var t=await WaveFileHelper.SplitWaveFileBySecondsAsync(file, 50, @"F:\Temp", "new_file_");

            /*测试分析Wave数据的振幅*/
            //var file = @"F:\Temp\newtrain_4_1\answer_26.wav";
            //var result = await WaveFileHelper.AnalysisWave(file);
            //foreach (var r in result)
            //    Console.Write("[{0}] ", r.Amplitude);

            /*测试wave的静默查询*/
            var file = @"F:\Temp\newtrain_4_1\answer_27.wav";
            var result = await WaveFileHelper.QueryWaveSilences(file);
            if(result!=null)
            {
                foreach (var r in result)
                    Console.WriteLine("静默时间{0} - {1}，间隔{2}秒", r.Start, r.End, r.Duration);
            }


            Console.Read();
        }
    }
}
