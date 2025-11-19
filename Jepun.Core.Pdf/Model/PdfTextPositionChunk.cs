using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jepun.Core.Pdf.Model
{
	public class PdfTextPositionChunk
	{
		public string Text { get; set; }
		public float StartX { get; set; }
		public float StartY { get; set; }
		public float EndX { get; set; }
		public float EndY { get; set; }
		public int Page { get; set; }
		public float CharSpaceWidth { get; set; }
		public string SearchText { get; set; }
	}
}
