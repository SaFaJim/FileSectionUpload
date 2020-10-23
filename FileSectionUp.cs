using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileSectionUpload
{
    /// <summary>
    /// 文件分段上传类
    /// </summary>
    public class FileSectionUp
    {
        /// <summary>
        /// 屏蔽私有初始化方法
        /// </summary>
        private FileSectionUp()
        {

        }
        /// <summary>
        /// 分块上传设置
        /// </summary>
        private SectionSetting Setting { get; set; }
        /// <summary>
        /// 文件大小
        /// </summary>
        private long FileSize { get; set; }
        /// <summary>
        /// 分块大小
        /// </summary>
        private long SectionSize { get; set; }
        /// <summary>
        /// 总分块数量
        /// </summary>
        public int TotalSectionCount { get; set; }
        /// <summary>
        /// 文件名称 不含路径
        /// </summary>
        private string FileName { get; set; }
        /// <summary>
        /// 初始化上传类
        /// </summary>
        /// <param name="setting"></param>
        public   FileSectionUp(SectionSetting setting)
        {
            if (setting == null)  throw new Exception("setting is null");
            if (string.IsNullOrEmpty(setting.FileFullName)) throw new Exception("setting.FileFullName is null"); ;
            this.Setting = setting;
            try
            {
                System.IO.FileInfo fileInfo = null;
                fileInfo = new System.IO.FileInfo(setting.FileFullName);
                this.FileSize = fileInfo.Length;
                this.FileName = fileInfo.Name;
                switch (setting.SectionSize)
                {
                    case FileSectionUpload.SectionSize._1MB:
                        this.SectionSize = 1048576;
                        break;
                    case FileSectionUpload.SectionSize._2MB:
                        this.SectionSize = 2097152;
                        break;
                    case FileSectionUpload.SectionSize._512KB:
                        this.SectionSize = 524288;
                        break;
                    default:
                        this.SectionSize = 524288;
                        break;
                }
                this.TotalSectionCount = (int)Math.Ceiling((double)this.FileSize / this.SectionSize);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 获取分块数据
        /// </summary>
        /// <param name="sectionIndex"></param>
        /// <returns></returns>
        public SectionData GetFileData(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= this.TotalSectionCount) return null;
            SectionData section = new SectionData();
            section.FileName = this.FileName;
            section.FileSize = this.FileSize;
            section.SectionSize = this.SectionSize;
            section.Pages = this.TotalSectionCount;
            section.CurrentPageIndex = sectionIndex;
            section.Data = this.GetSection(sectionIndex);
            section.MD5 = FileSectionUtils.GetMD5Hash(section.Data);
            if (this.Setting.DataType == SectionDataType.Base64)
            {
                section.Base64String = Convert.ToBase64String(section.Data);
                section.Data = null;
            }
            return section;
        }
        /// <summary>
        /// 分块读取数据
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="startPosition"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private byte[] GetSection(int index)
        {
            string fileName = this.Setting.FileFullName;
            long start = this.SectionSize * index;
            long size = this.SectionSize;
            if (index == this.TotalSectionCount - 1)
            {
                size = this.FileSize - start;
            }
            byte[] data;
            using (FileStream fileRead = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                data = new byte[size];
                fileRead.Seek(start, SeekOrigin.Begin);
                fileRead.Read(data, 0, (int)size);
                fileRead.Close();
                fileRead.Dispose();
            }
            return data;
        }
    }

    /// <summary>
    /// 设置类
    /// </summary>
    public class SectionSetting
    {
        /// <summary>
        /// 要分开的文件全名
        /// </summary>
        public string FileFullName { get; set; }

        /// <summary>
        /// 分块大小
        /// </summary>
        public SectionSize SectionSize { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public SectionDataType DataType { get; set; }
    }

    /// <summary>
    /// 数据存储格式
    /// </summary>
    public enum SectionDataType
    {
        Byte,
        Base64,
    }

    /// <summary>
    /// 分块数据大小
    /// </summary>
    public enum SectionSize
    {
        _1MB,
        _2MB,
        _512KB,
    }

    public class SectionData
    {
        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 总分块
        /// </summary>
        public int Pages { get; set; }

        /// <summary>
        /// 当前块序号 0 开始
        /// </summary>
        public int CurrentPageIndex { get; set; }

        /// <summary>
        /// 块大小
        /// </summary>
        public long SectionSize { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件块 二进制
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Base64文件字符串
        /// </summary>
        public string Base64String { get; set; }

        /// <summary>
        /// MD5值用来验证当前块和传输过去的块MD5值是否一致
        /// </summary>
        public string MD5 { get; set; }

    }

    /// <summary>
    /// 工具类
    /// </summary>
    public class FileSectionUtils
    {
        /// <summary>
        /// 计算数组MD5
        /// </summary>
        /// <param name="bytedata"></param>
        /// <returns></returns>
        public static string GetMD5Hash(byte[] bytedata)
        {
            try
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(bytedata);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5Hash() fail,error:" + ex.Message);
            }
        }
        /// <summary>
        /// 字符串转Bas464编码
        /// </summary>
        /// <param name="str"></param>
        public static string StringToBase64(string str)
        {
            string value = "";
            if (string.IsNullOrEmpty(str))
            {
                return value;
            }
            try
            {
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(str);
                //先转成byte[];
                value = Convert.ToBase64String(byteArray);
            }
            catch (Exception ex)
            {
                value = "";
            }
            return value;
        }
        /// <summary>
        ///Bas464转字符串
        /// </summary>
        /// <param name="str"></param>
        public static string Base64ToString(string str)
        {
            string value = "";
            if (string.IsNullOrEmpty(str))
            {
                return value;
            }
            try
            {
                byte[] bytes = Convert.FromBase64String(str);
                value = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                value = "";
            }
            return value;
        }
    }

}
