using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsposeTask
{
    /// <summary>
    /// Pixel Clusters for better working with different pixels in two images
    /// </summary>
    public class PixelCluster
    {
        public int UpperPoint { get; private set; }
        public int LowerPoint { get; private set; }
        public int LeftPoint { get; private set; }
        public int RightPoint { get; private set; }
        public int Size { get; private set; }

        public List<int> XCoordinates { get; private set; }
        public List<int> YCoordinates { get; private set; }

        public PixelCluster()
        {
            XCoordinates = new List<int>();
            YCoordinates = new List<int>();
        }

        /// <summary>
        /// Sets borders for Clusters
        /// </summary>
        public void SetBorders()
        {
            UpperPoint = YCoordinates.Min();
            LowerPoint = YCoordinates.Max();
            RightPoint = XCoordinates.Max();
            LeftPoint = XCoordinates.Min();
            Size = XCoordinates.Count;
        }

        public override string ToString()
        {
            return $"Upper point: {UpperPoint}, \n" +
                $"Lower point: {LowerPoint}, \n" +
                $"Left point: {LeftPoint}, \n" +
                $"Right point: {RightPoint}, \n" +
                $"Size: {Size}. \n";
        }
    }
}
