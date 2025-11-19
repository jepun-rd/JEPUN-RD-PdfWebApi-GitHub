using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

 

namespace Jepun.Core.Pdf.Model
{
	/// <summary>
	/// 區塊文字擷取
	/// </summary>
	public class ChunkTextExtractionStrategy : IEventListener
	{
        private List<JepunTextChunk> m_locationChunks = new List<JepunTextChunk>();
        private List<JepunTextInfo> m_TextLocationInfos = new List<JepunTextInfo>();
        public List<JepunTextChunk> LocationResult
        {
            get { return m_locationChunks; }
        }
        public List<JepunTextInfo> TextLocationInfos
        {
            get 
            {				
				return m_TextLocationInfos; 
            }
        }
		/// <summary>
		/// Creates a new LocationTextExtractionStrategyEx
		/// 建立一個新的 LocationTextExtractionStrategyEx
		/// </summary>
		public ChunkTextExtractionStrategy()
        {
        }
		/// <summary>
		/// Returns the result so far
		/// 傳回目前為止的結果
		/// </summary>
		/// <returns>a String with the resulting text</returns>
		public String GetResultantText()
        {
            //由上下 在 由左至右 排序
			m_locationChunks.Sort();
			StringBuilder sb = new StringBuilder();
            JepunTextChunk lastChunk = null;
            JepunTextInfo lastTextInfo = null;
            foreach (JepunTextChunk chunk in m_locationChunks)
            {
                if (lastChunk == null)
                {
                    sb.Append(chunk.Text);
                    lastTextInfo = new JepunTextInfo(chunk);
                    m_TextLocationInfos.Add(lastTextInfo);
                }
                else  if (chunk.IsSameLine(lastChunk))
                {//同行
					if (!StartsWithSpace(chunk.Text) && !EndsWithSpace(chunk.Text))
					{
						sb.Append(' ');
					}
					sb.Append(chunk.Text);
					lastTextInfo.AddSpace("@@");
					lastTextInfo.AppendText(chunk);

					//float dist = chunk.DistanceFromEndOf(lastChunk);
					//if (dist < -chunk.CharSpaceWidth)
					//{
					//    sb.Append("@@");
					//    lastTextInfo.AddSpace("@@");
					//}
					//else if (dist > chunk.CharSpaceWidth / 2.0f && chunk.Text[0] != ' ' && lastChunk.Text[lastChunk.Text.Length - 1] != ' ')
					//{//append a space if the trailing char of the prev string wasn't a space && the 1st char of the current string isn't a space
					//    //如果前一個字串的尾隨字元不是空格 && 目前字串的第一個字元不是空格  ，則追加一個空格
					//    sb.Append("**");
					//    lastTextInfo.AddSpace("**");
					//}
					//sb.Append("--");
					//lastTextInfo.AddSpace("--");
					//sb.Append(chunk.Text);
					//lastTextInfo.AppendText(chunk);
				}
				else
                {//不同行
                    sb.Append('\n');
                    sb.Append(chunk.Text);
                    lastTextInfo = new JepunTextInfo(chunk);
                    m_TextLocationInfos.Add(lastTextInfo);
                }
                
                lastChunk = chunk;
            }			
			return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dara"></param>
        /// <param name="type"></param>
        public void EventOccurred(IEventData dara, EventType type)
        {
            if (dara is TextRenderInfo textRenderInfo)
            {
                LineSegment segment = textRenderInfo.GetBaseline();
                JepunTextChunk location = new JepunTextChunk(textRenderInfo.GetText(), segment.GetStartPoint(), segment.GetEndPoint(), textRenderInfo.GetSingleSpaceWidth(), textRenderInfo.GetAscentLine(), textRenderInfo.GetDescentLine());
                m_locationChunks.Add(location);
            }
        }
		public ICollection<EventType> GetSupportedEvents()
		{
			return new List<EventType> { EventType.RENDER_TEXT };
		}
		private bool StartsWithSpace(string str)
		{
			if (str.Length != 0)
			{
				return str[0] == ' ';
			}

			return false;
		}

		//
		// 摘要:
		//     Checks if the string ends with a space character, false if the string is empty
		//     or ends with a non-space character
		//
		// 參數:
		//   str:
		//     the string to be checked
		//
		// 傳回:
		//     true if the string ends with a space character, false if the string is empty
		//     or ends with a non-space character
		private bool EndsWithSpace(string str)
		{
			if (str.Length != 0)
			{
				return str[str.Length - 1] == ' ';
			}

			return false;
		}
	}

}
