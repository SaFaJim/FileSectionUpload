
using FileSectionUpload;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
//using System.Linq;
using System.Text;
using System.Threading;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            SectionSetting setting = new SectionSetting();
            setting.FileFullName = @"C:\Segments\python-3.7.0-amd64.exe";
            setting.SectionSize = SectionSize._512KB;
            FileSectionUp upInstance = new FileSectionUp(setting);
            FileSectionSave saveInstance = new FileSectionSave(@"C:\Segments\", "a.exe");
            for (int i = 0; i < upInstance.TotalSectionCount; i++)
            {
                var section = upInstance.GetFileData(i);
                var result = saveInstance.SaveSection(section);
                if (!result.Success)
                {
                    Console.WriteLine("失败:" + result.Message);
                }
                if (result.FileFinished)
                {
                    Console.WriteLine("全部上传完毕");
                    break;
                }
            }
        }
    }
}
