using iText.Kernel.Geom;
namespace Jepun.Core.Pdf.Model
{
	/// <summary>
	/// 匹配的文字訊息
	/// </summary>
	internal class MatchedTextInfo : IComparable<MatchedTextInfo>
	{
		/// <summary>
		/// 文字
		/// </summary>
		public string Text { get; set; }
		/// <summary>
		/// 長方形
		/// </summary>
		public Rectangle Rectangle { get; }
		/// <summary>
		/// 建構子
		/// </summary>
		/// <param name="text">文字</param>
		/// <param name="rectangle">長方形</param>
		public MatchedTextInfo(string text, Rectangle rectangle)
		{
			Text = text;
			Rectangle = rectangle;
		}

		public int CompareTo(MatchedTextInfo rhs)
		{
			if (rhs == null) return 1;
			if (this == rhs) return 0;

			float rslt;

			//y  由上而下
			rslt = rhs.Rectangle.GetY() - this.Rectangle.GetY();
			if (Math.Abs(rslt) < 1.5)
			{//誤差值 眼睛分不出,但的確  Top  不同,但要視做 同一行
				rslt = 0;
			}
			if (rslt != 0) return (int)rslt;
			//x 由左而右

			rslt = this.Rectangle.GetX() - rhs.Rectangle.GetX();
			if (rslt != 0) return (int)rslt;



			return 0;


		}
	}
}
