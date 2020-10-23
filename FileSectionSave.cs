using System;
using System.IO;
namespace FileSectionUpload
{
    /// <summary>
    /// 文件分段保存类
    /// </summary>
    public class FileSectionSave
    {
        /// <summary>
        /// 屏蔽私有初始化方法
        /// </summary>
        private FileSectionSave()
        {
        }

        /// <summary>
        /// 保存的文件夹
        /// </summary>
        private string Dir { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        private string FileName { get; set; }

        /// <summary>
        /// 临时文件后缀名
        /// </summary>
        private string TempSuffix { get; set; }

        /// <summary>
        /// 返回保存实例对象
        /// </summary>
        /// <param name="saveDir">保存目录</param>
        /// <param name="fileName">保存文件名</param>
        /// <param name="tempSuffix">临时文件后缀名 默认 fstp </param>
        /// <returns></returns>
        public static FileSectionSave InstanceForSave(string saveDir,string fileName="",string tempSuffix="fstp")
        {
            if (string.IsNullOrEmpty(saveDir)) return null;
            if (!System.IO.Directory.Exists(saveDir))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(saveDir);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            FileSectionSave instance = new FileSectionSave();
            instance.Dir = saveDir;
            instance.FileName = fileName;
            instance.TempSuffix = tempSuffix;
            return instance;
        }


        /// <summary>
        /// 保存分块
        /// </summary>
        /// <param name="saveDir">保存目录</param>
        /// <param name="fileName">保存文件名</param>
        /// <param name="tempSuffix"></param>
        public FileSectionSave(string saveDir, string fileName = "", string tempSuffix = "fstp")
        {
            if (string.IsNullOrEmpty(saveDir)) throw new Exception(" saveDir is null");
            if (!System.IO.Directory.Exists(saveDir))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(saveDir);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            this.Dir = saveDir;
            this.FileName = fileName;
            this.TempSuffix = tempSuffix;
        }


        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public SaveResult SaveSection(SectionData  section)
        {
            SaveResult result = new SaveResult();

            if (section == null || (section.Data == null && string.IsNullOrEmpty(section.Base64String)))
            {
                result.Message = "没有数据!";
                return result;
            }
            if (string.IsNullOrEmpty(this.FileName))
            {
                this.FileName = section.FileName;
            }
            string savePath = this.Dir + System.IO.Path.DirectorySeparatorChar + this.FileName;
            //已经存在
            if (System.IO.File.Exists(savePath))
            {
                if (section.CurrentPageIndex == 0)
                {
                    System.IO.File.Delete(savePath);
                }
                else
                {
                    result.Success = true;
                    result.Message = "文件已存在!";
                    return result;
                }
            }
            //查找特殊后缀名
            savePath += ".fstp";
            if(!System.IO.File.Exists(savePath))
            {
                //不存在则说明是首次请求进行创建， 创建不一定是 CurrentPageIndex==0 的请求
                this.CreateSaveFile(savePath, section.FileSize,section.Pages);
            }
            if (string.IsNullOrEmpty(section.MD5))
            {
                result.Success = true;
                result.Message = "MD5缺失!";
                return result;
            }
            //验证MD5
            if (section.Data != null && section.Data.Length > 0)
            {
                string MD5 = FileSectionUtils.GetMD5Hash(section.Data);
                if (!MD5.Equals(section.MD5))
                {
                    result.Success = false;
                    result.Message = "MD5错误!";
                    return result;
                }
                else
                {
                    this.WriteSection(savePath, section);
                }
            }
            else if (!string.IsNullOrEmpty(section.Base64String))
            {
                byte[] buffer = Convert.FromBase64String(section.Base64String);
                section.Data = buffer;
                string MD5 = FileSectionUtils.GetMD5Hash(buffer);
                if (!MD5.Equals(section.MD5))
                {
                    result.Success = false;
                    result.Message = "MD5错误!";
                    return result;
                }
                else
                {
                    this.WriteSection(savePath, section);
                }
            }
            result.Success = true;
            //检查是否全部写完
           result.FileFinished = CheckSaveOver(savePath, section);
           return result;
        }

        /// <summary>
        /// 检查文件所有分块保存是否完成
        /// </summary>
        /// <param name="path"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private bool CheckSaveOver(string path, SectionData section)
        {
            bool isOver = false;
            byte[] data =new byte[section.Pages];
            // 检测该块是否已经被写入 避免重复写
            using (FileStream fileReader = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileReader.Seek(section.FileSize, SeekOrigin.Begin);
                fileReader.Read(data, 0, section.Pages);
            }
            if (data != null && data.Length > 0)
            {
                int sum = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    sum += data[i];
                }
                isOver = sum == data.Length;
            }
            //改变大小和该名称
            if (isOver)
            {
                using (BinaryWriter bw = new BinaryWriter(new FileStream(path, FileMode.Open)))
                {
                    bw.BaseStream.SetLength(bw.BaseStream.Length - section.Pages);
                }
                //改名
                Directory.Move(path, path.Replace(".fstp", ""));
            }
            return isOver;
        }

        /// <summary>
        /// 写文件块
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="section"></param>
        private void WriteSection(string savePath, SectionData section)
        {
            long startPosition = section.CurrentPageIndex * section.SectionSize;
            byte[] data = section.Data;
            byte value = 1;
            // 检测该块是否已经被写入 避免重复写
            using (FileStream fileWrite = new FileStream(savePath, FileMode.Open, FileAccess.Write))
            {
                fileWrite.Seek(startPosition, SeekOrigin.Begin);
                fileWrite.Write(data, 0, data.Length);

                long updatePosition = section.FileSize + section.CurrentPageIndex;
                fileWrite.Seek(updatePosition, SeekOrigin.Begin);
                fileWrite.WriteByte(value);

                fileWrite.Close();
                fileWrite.Dispose();
            }
        }

        /// <summary>
        /// 创建指定大小的文件
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="size"></param>
        private  void CreateSaveFile(string savePath, long size,int sectionLength)
        {
            using (FileStream fileWrite = new FileStream(savePath, FileMode.Create, FileAccess.Write))
            {
                var data = new byte[size+ sectionLength];
                fileWrite.Seek(0, SeekOrigin.Begin);
                fileWrite.Write(data, 0, (int)size);
                fileWrite.Close();
                fileWrite.Dispose();
            }
        }
    }

    /// <summary>
    /// 保存结果返回数据结构
    /// </summary>
    public class SaveResult
    {
        /// <summary>
        ///当前块是否保存成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 保存成功的块序号
        /// </summary>
        public int SaveIndex { get; set; }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 文件保存全部完成
        /// </summary>
        public bool FileFinished { get; set; }
    }

     
}
