using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jepun.Core.Pdf.Model
{
    public class PdfAttachFile
    {
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        /// <summary>
        /// 加入檔案
        /// </summary>
        /// <param name="fileName">檔名</param>
        /// <param name="file">檔案</param>
        /// <returns>true:成功</returns>
        public bool AddFile(string fileName,byte[] file)
        {
            return files.TryAdd(fileName, file);
        }
        /// <summary>
        /// 移除檔案
        /// </summary>
        /// <param name="fileName">檔名</param>
        /// <returns>true:成功</returns>
        public bool RemoveFile(string fileName)
        {
            return files.Remove(fileName);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public byte[] GetFile(string fileName) 
        { 
            return files[fileName]; 
        }
        /// <summary>
        /// 取得檔案集合
        /// </summary>
        public Dictionary<string, byte[]> Files
        {
            set
            {                
                this.files = value;
			}
            get { return files; }  
        }
    }
}
