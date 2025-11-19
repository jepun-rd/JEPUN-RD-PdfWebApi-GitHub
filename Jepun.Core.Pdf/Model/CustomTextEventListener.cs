using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Geom;

namespace Jepun.Core.Pdf.Model
{
	/// <summary>
	/// 以區塊順序
	/// </summary>
	public class CustomTextEventListener : IEventListener
	{
		private StringBuilder sb;
		public CustomTextEventListener()
		{
			sb = new StringBuilder();
			 
		}

		public void EventOccurred(IEventData data, EventType type)
		{
			if (type == EventType.RENDER_TEXT)
			{
				TextRenderInfo renderInfo = (TextRenderInfo)data;
				string text = renderInfo.GetText();
				// Append the text to the current paragraph		
				sb.Append(text);				 
			}
		}

		public ICollection<EventType> GetSupportedEvents()
		{
			return new List<EventType> { EventType.RENDER_TEXT };
		}

		public string GetResultantText()
		{
			return sb.ToString();
		}		
	}
}
