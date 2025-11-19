using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jepun.Core.Pdf.Model
{
    public class PdfTextWaterMark
	{
		/// <summary>
		/// 必須有,反序列化 
		/// </summary>
		public PdfTextWaterMark() { }
		public PdfTextWaterMark(string text, float size, string font = "DFKai-SB", string color = "#000000", float rotationDegrees = 0, float opacity = 1.0f, float startX = 0, float startY = 0, float stepX = 100f, float stepY = 100f, float endX = 0f, float endY = 0f)
		{
			Text = text;
			Size = size;
            Color = color;
			FontFamily = font;
			Rotation = rotationDegrees;
			Opacity = opacity;
			StartX = startX;
			StartY = startY;
			EndX = endX;
			EndY = endY;
			StepX = stepX;
			StepY = stepY;
		}

		public string Text { get; set; }
        public float Size { get; set; }
        public float Rotation { get; set; }

        public string FontFamily { get; set; }

        public string Color {  get; set; }
		public float Opacity { get; set; }

		public float StartX { get; set; }
		public float StartY { get; set; }
		public float EndX { get; set; }
		public float EndY { get; set; }
		public float StepX { get; set; }
		public float StepY { get; set; }


	}
}
