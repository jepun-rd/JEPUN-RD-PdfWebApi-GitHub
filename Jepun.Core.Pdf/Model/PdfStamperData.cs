//using System.Text.Json.Serialization;

namespace Jepun.Core.Pdf.Model
{
    public class PdfStamperData
    {
		private Dictionary<int, List<PdfText>> pdfTexts = new Dictionary<int, List<PdfText>>();
        private Dictionary<int, List<PdfImg>> pdfImgs = new Dictionary<int, List<PdfImg>>();
		private Dictionary<int, List<PdfImgWaterMark>> pdfwaterimgs = new Dictionary<int, List<PdfImgWaterMark>>();
		private Dictionary<int, List<PdfTextWaterMark>> pdfwatertxts = new Dictionary<int, List<PdfTextWaterMark>>();
		/// <summary>
		/// 必須有,反序列化 
		/// </summary>
		public PdfStamperData() { }
		//[JsonRequired]
		public Dictionary<int, List<PdfText>> Texts
        {

			get
			{
				return pdfTexts;
			}
			set
			{
				pdfTexts = value;
			}
		}
		//[JsonRequired]
		public Dictionary<int, List<PdfImg>> Imgs
        {
			set
			{
				pdfImgs = value;
			}
			get
            {
                return pdfImgs;
            }
        }
		//[JsonRequired]
		public Dictionary<int, List<PdfImgWaterMark>> WMImgs
		{
			set
			{
				pdfwaterimgs = value;
			}
			get
			{
				return pdfwaterimgs;
			}
		}
		//[JsonRequired]
		public Dictionary<int, List<PdfTextWaterMark>> WMTexts
		{
			set
			{
				pdfwatertxts = value;
			}
			get
			{
				return pdfwatertxts;
			}
		}
		/// <summary>
		/// 加入圖片
		/// </summary>
		/// <param name="pageNum"></param>
		/// <param name="imggb"></param>
		/// <param name="newWidth"></param>
		/// <param name="newHeight"></param>
		/// <param name="absoluteX"></param>
		/// <param name="absoluteY"></param>
		/// <param name="rotationDegrees"></param>
		public void AddImg(int pageNum, byte[] imggb, float newWidth, float newHeight, float absoluteX, float absoluteY, float rotationDegrees = 0, float opacity = 1f)
        {
            if (pdfImgs.TryGetValue(pageNum, out var imgs))
            {
                imgs.Add(new PdfImg(imggb, newWidth, newHeight, absoluteX, absoluteY, rotationDegrees, opacity));
            }
            else
            {
				var list = new List<PdfImg>
				{
					new PdfImg(imggb, newWidth, newHeight, absoluteX, absoluteY, rotationDegrees, opacity)
				};
				pdfImgs.Add(pageNum, list);
            }
        }

		/// <summary>
		/// 加入文字
		/// </summary>
		/// <param name="pageNum"></param>
		/// <param name="text"></param>
		/// <param name="size"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="font"></param>
		/// <param name="alignment"></param>
		/// <param name="rotation"></param>
		/// <param name="fillOpacity"></param>
		/// <param name="strokeOpacity"></param>
		public void AddText(int pageNum, string text, float size, float x, float y, string font = "DFKai-SB", int alignment = 0, float rotation = 0, float fillOpacity = 1f, float strokeOpacity = 1f, string color = "#000000")
        {
            if (pdfTexts.TryGetValue(pageNum, out var texts))
            {
                texts.Add(new PdfText(text, size, x, y, font, alignment, rotation, "", fillOpacity, strokeOpacity, color));
            }
            else
            {
				var list = new List<PdfText>
				{
					new PdfText(text, size, x, y, font, alignment, rotation, "", fillOpacity, strokeOpacity, color)
				};
				pdfTexts.Add(pageNum, list);
            }
        }
        /// <summary>
        /// 加入連結
        /// </summary>
        /// <param name="pageNum">頁碼</param>
        /// <param name="uri">連結</param>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="width">寬</param>
        /// <param name="height">高</param>
        public void AddUri(int pageNum, string uri, float x, float y, float width,float height )
        {
            if (pdfTexts.TryGetValue(pageNum, out var texts))
            {
                texts.Add(new PdfText("", 0, x, y, "", 0,0, uri, width, height));
            }
            else
            {
                var list = new List<PdfText>
                {
                    new PdfText("", 0, x, y, "", 0, 0, uri, width, height)
                };
                pdfTexts.Add(pageNum, list);
            }
        }

        /// <summary>
        /// 加入有顏色的文字，不影響舊事件額外擴充
        /// </summary>
        /// <param name="pageNum"></param>
        /// <param name="text"></param>
        /// <param name="size"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="font"></param>
        /// <param name="color"></param>
		public void AddTextColor(int pageNum, string text, float size, float x, float y, string font = "DFKai-SB", string color = "#000000",float fillOpacity = 1f)
		{
			if (pdfTexts.TryGetValue(pageNum, out var texts))
			{
				texts.Add(new (text, size, x, y, font, color, fillOpacity));
			}
			else
			{
				var list = new List<PdfText>
				{
					new (text, size, x, y, font, color,fillOpacity)
				};
				pdfTexts.Add(pageNum, list);
			}
		}

		/// <summary>
		/// 增加浮水印(圖片)
		/// </summary>
		/// <param name="pageNum">0代表全部頁面都加入</param>
		/// <param name="imggb"></param>
		/// <param name="size"></param>
		/// <param name="rotationDegrees"></param>
		/// <param name="opacity"></param>
		public void AddWMImgs(int pageNum, byte[] imggb, float size, float rotationDegrees = 0, float opacity = 1.0f
			, float startX = 0, float startY = 0, float stepX = 100f, float stepY = 100f, float endX = 0f, float endY = 0f)
		{
			if (pdfwaterimgs.TryGetValue(pageNum, out var imgs))
			{
				imgs.Add(new PdfImgWaterMark(imggb, size, rotationDegrees, opacity, startX, startY, stepX, stepY, endX, endY));
			}
			else
			{
				var list = new List<PdfImgWaterMark>
				{
					new PdfImgWaterMark(imggb, size, rotationDegrees, opacity, startX,startY,stepX,stepY,endX,endY)
				};
				pdfwaterimgs.Add(pageNum, list);
			}
		}


		/// <summary>
		/// 增加浮水印(文字)
		/// </summary>
		/// <param name="pageNum">0代表全部頁面都加入</param>
		/// <param name="text"></param>
		/// <param name="size"></param>
		/// <param name="font"></param>
		/// <param name="color"></param>
		/// <param name="rotation"></param>
		/// <param name="strokeOpacity"></param>
		public void AddWMTxts(int pageNum, string text,float size, string font = "DFKai-SB", string color = "#000000", float rotation = 0, float strokeOpacity = 0.5f
			, float startX = 0, float startY = 0, float stepX = 100f, float stepY = 100f, float endX = 0f, float endY = 0f)
		{
			if (pdfwatertxts.TryGetValue(pageNum, out var txts))
			{
				txts.Add(new PdfTextWaterMark(text, size, font, color, rotation, strokeOpacity, startX, startY, stepX, stepY, endX, endY));
			}
			else
			{
				var list = new List<PdfTextWaterMark>
				{
					new PdfTextWaterMark(text, size, font, color, rotation, strokeOpacity, startX,startY,stepX,stepY,endX,endY)
				};
				pdfwatertxts.Add(pageNum, list);
			}
		}

	}

}
