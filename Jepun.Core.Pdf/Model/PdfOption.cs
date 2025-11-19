using iText.Commons.Utils;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jepun.Core.Pdf.Model
{
    /// <summary>
    /// Pdf壓縮狀態參數
    /// </summary>
    public class PdfOption
    {
        private bool hasFontDuplication;
        private bool hasFontSubsetting;
        private bool hasCompression;
        private readonly double imageQuality;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageQuality">圖片壓縮品質1 ~ 0.1 </param>
        /// <param name="hasFontDuplication">是否『字體複製優化』指的是刪除整個字體文件的重複實例，或 PDF 文檔中字體文件的重複嵌入子集。</param>
        /// <param name="hasFontSubsetting">是否『字體子集優化』允許僅嵌入文檔中文本實際使用的字形數據，而不是嵌入字體文件中的所有字形數據。</param>
        /// <param name="hasCompression">是否對每個PdfStream 執行最大壓縮，並對PdfDocument執行完全壓縮。</param>
        /// <exception cref="ArgumentException"></exception>
        public PdfOption(double imageQuality = 1,bool hasFontDuplication = false, bool hasFontSubsetting = false, bool hasCompression = false)
        {
            if (imageQuality > 1.0 || imageQuality < 0.0)
            {
                throw new ArgumentException(MessageFormatUtil.Format("Invalid compression parameter! Value {0} is out of range [0, 1]", imageQuality));
            }
            this.imageQuality = imageQuality;
            this.hasFontDuplication = hasFontDuplication;
            this.hasFontSubsetting = hasFontSubsetting;
            this.hasCompression = hasCompression;
        }


        public bool GethasFontDuplication()
        {
            return hasFontDuplication;
        }

        public void SethasFontDuplication(bool value)
        {
            hasFontDuplication = value;
        }

        public bool GethasFontSubsetting()
        {
            return hasFontSubsetting;
        }

        public void SethasFontSubsetting(bool value)
        {
            hasFontSubsetting = value;
        }



        public bool GethasCompression()
        {
            return hasCompression;
        }

        public void SethasCompression(bool value)
        {
            hasCompression = value;
        }

       

        public bool GethasImageQuality()
        {
            return imageQuality < 1;
        }

        public double GetImageQuality()
        {
            return imageQuality;
        }


    }
}
