using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

using Rectangle = iText.Kernel.Geom.Rectangle;

namespace Jepun.Core.Pdf.Model
{
	class FullTextExtractionStrategy : IEventListener
	{
        private readonly string searchKeyword;
        private string tmpKeyword = "";
        private readonly List<MatchedTextInfo> matchedTexts;

        public FullTextExtractionStrategy(string keyword)
        {
            searchKeyword = keyword;
            matchedTexts = new List<MatchedTextInfo>();
        }

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT)
            {                
                return;
            }
            TextRenderInfo renderInfo = (TextRenderInfo)data;
            string text = renderInfo.GetText();
            Rectangle rectangle = renderInfo.GetDescentLine().GetBoundingRectangle();
            //Console.WriteLine(text);
			//if(text == "覆")
			//{
			//    Console.WriteLine("find");
			//}

			// 關鍵字為空白 直接返回,取得完整 表示要全部取回
			if (string.IsNullOrEmpty(searchKeyword))
            {
				MatchedTextInfo matchedText = new MatchedTextInfo(text, rectangle);
                if (!string.IsNullOrEmpty(text.Trim()))
                {//不為空白
					matchedTexts.Add(matchedText);
				}			
				tmpKeyword = "";				
				return;
			}
            // 搜尋關鍵字
            if (text.Contains(searchKeyword))
            {  // 找到關鍵字資訊，儲存起來                   
                if (!searchKeyword.Equals(text))
                {//返回   EX:  有權人簽樣 :                           覆核：                        經辦：
                    float count2Byte = 0;
                    foreach(char s in text.Substring(0, text.IndexOf(searchKeyword)))
                    {
                        if(s == ' ') //空白
                        {
                            count2Byte += 0.5f;
                        }
                        else
                        {
                            count2Byte += 1f;
                        }
                    }
                    rectangle.SetX(rectangle.GetLeft() + (searchKeyword.Length + count2Byte) * renderInfo.GetFontSize());                    
                }
                else
                {//完全相同
                    rectangle.SetX(rectangle.GetLeft() + rectangle.GetWidth());
                }                  
                MatchedTextInfo matchedText = new MatchedTextInfo(text, rectangle);
                matchedTexts.Add(matchedText);
                tmpKeyword = "";
               
                return;
            }

            if (searchKeyword.Contains(text))
            {//一個字返回,要組回來
                tmpKeyword += text;
                //確認順序一致
                if (searchKeyword.IndexOf(tmpKeyword) > 0)
                {//順序不一致
                    tmpKeyword = "";
                   
                    return;
                }               
                if (searchKeyword == tmpKeyword)
                {                             
                    rectangle.SetX(rectangle.GetLeft() + rectangle.GetWidth());
                    MatchedTextInfo matchedText = new MatchedTextInfo(searchKeyword, rectangle);
                    matchedTexts.Add(matchedText);
                    tmpKeyword = "";
                }
               
                return;
            }             
            tmpKeyword = "";           
           
        }

        public List<MatchedTextInfo> GetMatchedTexts()
        {
            matchedTexts.Sort();

			return matchedTexts;
        }
		public ICollection<EventType> GetSupportedEvents()
		{
			return new List<EventType> { EventType.RENDER_TEXT };
		}
	}

    
}
