using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jepun.Core.Pdf.Model
{
    public class PdfText
    {/// <summary>
	 /// 必須有,反序列化 
	 /// </summary>
		public PdfText() { }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="size"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="font">DFKai-SB old: KAIU.TTF</param>
		/// <param name="alignment"></param>
		/// <param name="rotation"></param>
		/// <param name="uri"></param>
		/// <param name="fillOpacity"></param>
		/// <param name="strokeOpacity"></param>
		public PdfText(string text, float size, float x, float y, string font = "DFKai-SB", int alignment = 0, float rotation = 0, string uri = "", float fillOpacity = 1f, float strokeOpacity = 1f, string color = "#000000")
        {
            Text = text;
            Size = size;
            X = x;
            Y = y;
            Alignment = alignment;
            Rotation = rotation;
            Uri = uri;
            FillOpacity = fillOpacity;
            StrokeOpacity = strokeOpacity;
            FontFamily = font;
			Color = color;
		}

		public PdfText(string text, float size, float x, float y, string font = "DFKai-SB", string color = "#000000", float fillOpacity = 1f)
		{
			Text = text;
			Size = size;
			X = x;
			Y = y;
			Alignment = 0;
			Rotation = 0;
			Uri = "";
			FillOpacity = fillOpacity;
			StrokeOpacity = 1f;
			FontFamily = font;
			Color = color;
		}

		public string Text { get; set; }
        public float Size { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float X { get; set; }
        public float Y { get; set; }
        public int Alignment { get; set; }

        public float Rotation { get; set; }
        public string Uri { get; set; }

        public float FillOpacity { get; set; }
        public float StrokeOpacity { get; set; }

        public string FontFamily { get; set; }

		public string Color { get; set; }


	}
}
