using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jepun.Core.Pdf.Model
{
    /// <summary>
    /// 文字資訊
    /// </summary>
    public class JepunTextInfo  
    {
		private Vector TopLeft;
		private Vector BottomRight;
        private string m_Text;
        private float m_CharSpaceWidth;
		/// <summary>
		/// 文字
		/// </summary>
		public string Text
        {
            get { return m_Text; }
        }
        /// <summary>
        /// 頂  (由下面 算上來 )
        /// </summary>
        public float Top
        {
            get { return TopLeft.Get(1); }
        }
        /// <summary>
        /// 左  (由左邊算 )
        /// </summary>
        public float Left
        {
            get { return TopLeft.Get(0); }
        }
		/// <summary>
		///  底 (由下面 算上來 )
		/// </summary>
		public float Bottom
        {
            get { return BottomRight.Get(1); }
        }
		/// <summary>
		/// 右  (由左邊算 )
		/// </summary>
		public float Right
        {
            get { return BottomRight.Get(0); }
        }
        /// <summary>
        /// 字寬
        /// </summary>
        public float CharSpaceWidth
        {
            get { return m_CharSpaceWidth; }
        }
		/// <summary>
		/// Create a TextInfo.
		/// </summary>
		/// <param name="initialTextChunk">文字區塊</param>
		public JepunTextInfo(JepunTextChunk initialTextChunk)
        {			
			TopLeft = initialTextChunk.AscentLine.GetStartPoint();
            BottomRight = initialTextChunk.DecentLine.GetEndPoint();
            m_Text = initialTextChunk.Text;
            m_CharSpaceWidth = initialTextChunk.CharSpaceWidth;
        }
		/// <summary>
		/// Add more text to this TextInfo.
		/// 在此 TextInfo 中新增更多文字。
		/// </summary>
		/// <param name="additionalTextChunk"></param>
		public void AppendText(JepunTextChunk additionalTextChunk)
        {
            BottomRight = additionalTextChunk.DecentLine.GetEndPoint();
            m_Text += additionalTextChunk.Text;
        }
		/// <summary>
		/// Add a space to the TextInfo.  This will leave the endpoint out of sync with the text.
		/// The assumtion is that you will add more text after the space which will correct the endpoint.
		/// 在 TextInfo 中新增一個空格。 這將使端點與文字不同步。
		/// 假設您將在空格後添加更多文本，以糾正端點。
		/// </summary>
		/// <param name="separate">分隔</param>
		public void AddSpace(string separate = " ")
        {
			m_Text += separate;
		}
		///// <summary>
		///// 字元
		///// </summary>
		///// <param name="separate">分隔</param>
		//public void AddSpace(char separate = ' ')
		//{			 
		//	m_Text += separate;
		//}

	}
}
