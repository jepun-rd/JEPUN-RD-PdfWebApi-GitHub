using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jepun.Core.Pdf.Model
{
    public class PdfImg
    {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="imggb"></param>
		/// <param name="newWidth">新寬</param>
		/// <param name="newHeight">新高</param>
		/// <param name="absoluteX">X</param>
		/// <param name="absoluteY">Y</param>
		/// <param name="rotationDegrees">旋轉角度</param>
		/// <param name="opacity">透明度</param>
		public PdfImg(byte[] imggb, float newWidth, float newHeight, float absoluteX, float absoluteY, float rotationDegrees = 0,float opacity = 1f)
        {
            Imggb = imggb;
            NewWidth = newWidth;
            NewHeight = newHeight;
            AbsoluteX = absoluteX;
            AbsoluteY = absoluteY;
            RotationDegrees = rotationDegrees;
            Opacity = opacity;
		}
		/// <summary>
		/// 必須有,反序列化 
		/// </summary>
		public PdfImg() { }

		public byte[] Imggb { get; set; }
        public float NewWidth { get; set; }
        public float NewHeight { get; set; }
        public float AbsoluteX { get; set; }
        public float AbsoluteY { get; set; }
        public float RotationDegrees { get; set; }

        public float Opacity { get; set; }

	}
}
