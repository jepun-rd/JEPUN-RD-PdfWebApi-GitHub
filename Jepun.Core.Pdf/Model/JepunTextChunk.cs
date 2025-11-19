
using iText.Kernel.Geom;

namespace Jepun.Core.Pdf.Model
{
    /// <summary>
    /// 文字區塊
    /// </summary>
    public class JepunTextChunk : IComparable<JepunTextChunk>

	{
        string m_text;
        Vector m_startLocation;
        Vector m_endLocation;
        Vector m_orientationVector;
        int m_orientationMagnitude;
        int m_distPerpendicular;
        float m_distParallelStart;
        float m_distParallelEnd;
        float m_charSpaceWidth;

        public LineSegment AscentLine;
        public LineSegment DecentLine;

        
        /// <summary>
        /// 文字
        /// </summary>
        public string Text
        {
            get { return m_text; }
            set { m_text = value; }
        }
        /// <summary>
        /// 字寬
        /// </summary>
        public float CharSpaceWidth
        {
            get { return m_charSpaceWidth; }
            set { m_charSpaceWidth = value; }
        }
        /// <summary>
        /// 開始位置
        /// </summary>
        public Vector StartLocation
        {
            get { return m_startLocation; }
            set { m_startLocation = value; }
        }
        /// <summary>
        /// 結束位置
        /// </summary>
        public Vector EndLocation
        {
            get { return m_endLocation; }
            set { m_endLocation = value; }
        }

		/// <summary>
		/// 表示一段文字、它的方向以及相對於方向向量的位置
		/// </summary>
		/// <param name="txt">文字</param>
		/// <param name="startLoc">起始位置</param>
		/// <param name="endLoc">結束位置</param>
		/// <param name="charSpaceWidth">字元空間寬度</param>
		/// <param name="ascentLine">上升線</param>
		/// <param name="decentLine">下降線</param>
		public JepunTextChunk(string txt, Vector startLoc, Vector endLoc, float charSpaceWidth, LineSegment ascentLine, LineSegment decentLine)
        {
            //文字
            m_text = txt;
            m_startLocation = startLoc;
            m_endLocation = endLoc;
            //字寬
            m_charSpaceWidth = charSpaceWidth;
			//上升線
			AscentLine = ascentLine;
			//下降線
			DecentLine = decentLine;
            //向量
            m_orientationVector = m_endLocation.Subtract(m_startLocation).Normalize();
			//向量幅度
			m_orientationMagnitude = (int)(Math.Atan2(m_orientationVector.Get(Vector.I2), m_orientationVector.Get(Vector.I1)) * 1000);

			//see http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
			// the two vectors we are crossing are in the same plane, so the result will be purely
			// in the z-axis(out of plane) direction, so we just take the I3 component of the result
			//我們相交的兩個向量在同一平面上，因此結果將純粹是在 z 軸（平面外）方向，所以我們只取結果的 I3 分量
		    Vector origin = new Vector(0, 0, 1);
            //垂直距離
            m_distPerpendicular = (int)(m_startLocation.Subtract(origin)).Cross(m_orientationVector).Get(Vector.I3);
            //平行開始
            m_distParallelStart = m_orientationVector.Dot(m_startLocation);
			//平行結束
			m_distParallelEnd = m_orientationVector.Dot(m_endLocation);
        }

		/// <summary>
		/// true if this location is on the the same line as the other text chunk
		/// 如果此位置與其他文字區塊在同一行，則為 true
		/// </summary>
		/// <param name="textChunkToCompare">the location to compare to 要比較的位置</param>
		/// <returns>true if this location is on the the same line as the other 如果此位置與其他位置在同一行，則為 true</returns>
		public bool IsSameLine(JepunTextChunk textChunkToCompare)
        {

			if( Math.Abs(textChunkToCompare.StartLocation.Get(1) - this.m_startLocation.Get(1)) < 1.5)
			{//誤差值 眼睛分不出,但的確  Top  不同,但要視做 同一行
				return true;
            }

			return false;

			//if (m_orientationMagnitude != textChunkToCompare.m_orientationMagnitude) return false;
   //         if (m_distPerpendicular != textChunkToCompare.m_distPerpendicular) return false;
   //         return true;
        }
		/// <summary>
		/// Computes the distance between the end of 'other' and the beginning of this chunk
		/// in the direction of this chunk's orientation vector.  Note that it's a bad idea
		/// to call this for chunks that aren't on the same line and orientation, but we don't
		/// explicitly check for that condition for performance reasons.
		/// 計算“other”的末尾與該區塊的開頭之間的距離
		// 沿著該塊的方向向量的方向。 請注意，這是一個壞主意
		// 為不在同一行和方向上的區塊呼叫此方法，但我們不這樣做
		// 出於性能原因明確檢查該條件。
		/// </summary>
		/// <param name="other"></param>
		/// <returns>the number of spaces between the end of 'other' and the beginning of this chunk    
        /// 「other」結尾與該區塊開頭之間的空格數
        /// </returns>
		public float DistanceFromEndOf(JepunTextChunk other)
        {//水平距離
            float distance = m_distParallelStart - other.m_distParallelEnd;

			

			return distance;
        }
		/// <summary>
		/// Compares based on orientation, perpendicular distance, then parallel distance
		/// 基於方向、垂直距離、然後平行距離進行比較
		/// 由上下 在 由左至右 排序
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(JepunTextChunk rhs)
        {
			if (rhs == null) return 1;
			if (this == rhs) return 0;

            float rslt;

			//y  由上而下
			rslt = rhs.StartLocation.Get(1) - this.m_startLocation.Get(1);
            if (Math.Abs(rslt) < 1.5)
            {//誤差值 眼睛分不出,但的確  Top  不同,但要視做 同一行
                rslt = 0;
            }
            if (rslt != 0) return (int)rslt;
            //x 由左而右

			rslt = this.m_startLocation.Get(0) - rhs.StartLocation.Get(0);
			if (rslt != 0) return (int)rslt;


			return 0;



			//int rslt;
			//         rslt = m_orientationMagnitude - rhs.m_orientationMagnitude;  //由上而下
			//         if (rslt != 0) return rslt;

			//         rslt = m_distPerpendicular - rhs.m_distPerpendicular;   //由左至右
			//         if (rslt != 0) return rslt;

			////note: it's never safe to check floating point numbers for equality, and if two chunks
			//// are truly right on top of each other, which one comes first or second just doesn't matter
			//// so we arbitrarily choose this way.
			////注意：檢查浮點數是否相等是不安全的，並且如果兩個區塊
			//// 確實是互相重疊的，哪個是第一或第二並不重要
			//// 所以我們隨意選擇這種方式。
			//rslt = m_distParallelStart < rhs.m_distParallelStart ? -1 : 1;
			//return rslt;             
        }
    }
}
