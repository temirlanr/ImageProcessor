namespace ImageDiff
{
    /// <summary>
    /// A rectangular region of differing pixels. Bounds and size are accumulated as pixels are added,
    /// so a cluster keeps only five integers regardless of how many pixels it covers.
    /// </summary>
    public class PixelCluster
    {
        /// <summary>Smallest Y coordinate (top edge) of the region.</summary>
        public int UpperPoint { get; private set; } = int.MaxValue;

        /// <summary>Largest Y coordinate (bottom edge) of the region.</summary>
        public int LowerPoint { get; private set; } = int.MinValue;

        /// <summary>Smallest X coordinate (left edge) of the region.</summary>
        public int LeftPoint { get; private set; } = int.MaxValue;

        /// <summary>Largest X coordinate (right edge) of the region.</summary>
        public int RightPoint { get; private set; } = int.MinValue;

        /// <summary>Number of pixels in the region.</summary>
        public int Size { get; private set; }

        /// <summary>
        /// Adds a differing pixel, expanding the bounding box and increasing <see cref="Size"/>.
        /// </summary>
        /// <param name="x">Pixel X coordinate.</param>
        /// <param name="y">Pixel Y coordinate.</param>
        public void Add(int x, int y)
        {
            if (x < LeftPoint) LeftPoint = x;
            if (x > RightPoint) RightPoint = x;
            if (y < UpperPoint) UpperPoint = y;
            if (y > LowerPoint) LowerPoint = y;
            Size++;
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
