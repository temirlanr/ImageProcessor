using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ImageDiff
{
    public enum ComparisonAlgorithm
    {
        /// <summary>
        /// Compares pixels by 4 values: A - transparency, R - red, G - green, B - blue.
        /// </summary>
        ARGB,
        /// <summary>
        /// Compares pixels by 3 values: R - red, G - green, B - blue.
        /// </summary>
        RGB
    }

    /// <summary>
    /// Implementation of <see cref="IImageProcessor"/> that compares two equally sized images,
    /// groups the differing pixels into <see cref="PixelCluster"/> regions and highlights them.
    /// </summary>
    /// <remarks>
    /// Both images must have the same dimensions. Pixel access uses
    /// <see cref="Bitmap.LockBits(Rectangle, ImageLockMode, PixelFormat)"/>, which is backed by GDI+,
    /// so this type runs on Windows only.
    /// </remarks>
    public class ImageProcessor : IImageProcessor
    {
        /// <summary>Extra breathing room (in pixels) drawn around each detected region.</summary>
        private const int RectanglePadding = 5;

        /// <summary>Width of the red highlight rectangle.</summary>
        private const float HighlightPenWidth = 5f;

        /// <summary>Number of bytes per pixel when a bitmap is locked as <see cref="PixelFormat.Format32bppArgb"/>.</summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// Specifies which formula to use when comparing pixels.
        /// </summary>
        public ComparisonAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Maximum number of difference regions to detect.
        /// </summary>
        /// <remarks>
        /// Use 0 or a negative value for no maximum.
        /// </remarks>
        public int MaxNumberOfDiff { get; set; }

        /// <summary>
        /// Per-pixel error tolerance when comparing images.
        /// </summary>
        /// <remarks>
        /// The lower it is, the more differences it will detect. The recursive stack-overflow caveat of
        /// earlier versions no longer applies: clustering is now iterative, so any non-negative value is safe.
        /// </remarks>
        public int ErrorTolerance { get; set; }

        /// <summary>
        /// Step size of each move during the difference search (8-connectivity stride).
        /// </summary>
        public int DepthOfDFS { get; set; }

        /// <summary>
        /// Minimum size (in pixels) of a region for it to be reported.
        /// </summary>
        public int SizeOfDiff { get; set; }

        /// <summary>
        /// Creates a processor. Defaults are tuned for typical photographic comparisons; tweak them if needed.
        /// </summary>
        /// <param name="algorithm">Pixel comparison formula.</param>
        /// <param name="maxNumberOfDiff">Maximum number of regions to report (0 or less = unlimited).</param>
        /// <param name="errorTolerance">Per-pixel tolerance before two pixels count as different.</param>
        /// <param name="depthOfDFS">Search stride used while clustering differing pixels.</param>
        /// <param name="sizeOfDiff">Minimum region size (in pixels) to report.</param>
        public ImageProcessor(
            ComparisonAlgorithm algorithm = ComparisonAlgorithm.ARGB,
            int maxNumberOfDiff = 0,
            int errorTolerance = 15,
            int depthOfDFS = 1,
            int sizeOfDiff = 25)
        {
            Algorithm = algorithm;
            MaxNumberOfDiff = maxNumberOfDiff;
            ErrorTolerance = errorTolerance;
            DepthOfDFS = depthOfDFS;
            SizeOfDiff = sizeOfDiff;
        }

        private delegate int CalculateDiff(Color pixel1, Color pixel2);

        // Shared, immutable lookup of comparison formulas. Add a new ComparisonAlgorithm value and a
        // matching entry here to plug in your own metric.
        private static readonly Dictionary<ComparisonAlgorithm, CalculateDiff> diffFormula = new()
        {
            [ComparisonAlgorithm.ARGB] = (pixel1, pixel2) =>
                (Math.Abs(pixel1.A - pixel2.A) + Math.Abs(pixel1.R - pixel2.R) +
                 Math.Abs(pixel1.G - pixel2.G) + Math.Abs(pixel1.B - pixel2.B)) / 4,

            [ComparisonAlgorithm.RGB] = (pixel1, pixel2) =>
                (Math.Abs(pixel1.R - pixel2.R) + Math.Abs(pixel1.G - pixel2.G) +
                 Math.Abs(pixel1.B - pixel2.B)) / 3,
        };

        /// <summary>
        /// Compares two image files, highlights the differences and saves the results to disk.
        /// </summary>
        /// <param name="fileName1">Path to the first source image.</param>
        /// <param name="fileName2">Path to the second source image.</param>
        /// <param name="resultFileName1">Path where the annotated first image is saved.</param>
        /// <param name="resultFileName2">Path where the annotated second image is saved.</param>
        /// <returns><see cref="Task"/></returns>
        /// <exception cref="ArgumentException">The images are not the same size.</exception>
        public async Task StartAsync(string fileName1, string fileName2, string resultFileName1, string resultFileName2)
        {
            using Bitmap img1 = new(fileName1);
            using Bitmap img2 = new(fileName2);
            await StartAsync(img1, img2, resultFileName1, resultFileName2);
        }

        /// <summary>
        /// Compares two bitmaps, highlights the differences and saves the results to disk.
        /// </summary>
        /// <param name="img1">First source image.</param>
        /// <param name="img2">Second source image.</param>
        /// <param name="resultFileName1">Path where the annotated first image is saved.</param>
        /// <param name="resultFileName2">Path where the annotated second image is saved.</param>
        /// <returns><see cref="Task"/></returns>
        /// <exception cref="ArgumentException">The images are not the same size.</exception>
        public async Task StartAsync(Bitmap img1, Bitmap img2, string resultFileName1, string resultFileName2)
        {
            using Pen pen = new(Color.Red, HighlightPenWidth);

            await foreach (var cluster in CompareTwoImagesAsync(img1, img2))
            {
                Console.WriteLine(cluster.ToString());
                Highlight(img1, img2, pen, cluster);
            }

            img1.Save(resultFileName1);
            img2.Save(resultFileName2);
        }

        /// <summary>
        /// Compares two image files, highlights the differences and saves the results to disk.
        /// </summary>
        /// <param name="fileName1">Path to the first source image.</param>
        /// <param name="fileName2">Path to the second source image.</param>
        /// <param name="resultFileName1">Path where the annotated first image is saved.</param>
        /// <param name="resultFileName2">Path where the annotated second image is saved.</param>
        /// <exception cref="ArgumentException">The images are not the same size.</exception>
        public void Start(string fileName1, string fileName2, string resultFileName1, string resultFileName2)
        {
            using Bitmap img1 = new(fileName1);
            using Bitmap img2 = new(fileName2);
            Start(img1, img2, resultFileName1, resultFileName2);
        }

        /// <summary>
        /// Compares two bitmaps, highlights the differences and saves the results to disk.
        /// </summary>
        /// <param name="img1">First source image.</param>
        /// <param name="img2">Second source image.</param>
        /// <param name="resultFileName1">Path where the annotated first image is saved.</param>
        /// <param name="resultFileName2">Path where the annotated second image is saved.</param>
        /// <exception cref="ArgumentException">The images are not the same size.</exception>
        public void Start(Bitmap img1, Bitmap img2, string resultFileName1, string resultFileName2)
        {
            using Pen pen = new(Color.Red, HighlightPenWidth);

            foreach (var cluster in CompareTwoImages(img1, img2))
            {
                Console.WriteLine(cluster.ToString());
                Highlight(img1, img2, pen, cluster);
            }

            img1.Save(resultFileName1);
            img2.Save(resultFileName2);
        }

        /// <summary>
        /// Compares two images and streams the difference regions as they are found.
        /// </summary>
        /// <param name="img1">First source image.</param>
        /// <param name="img2">Second source image.</param>
        /// <returns><see cref="IAsyncEnumerable{PixelCluster}"/></returns>
        /// <exception cref="ArgumentException">The images are not the same size.</exception>
        public async IAsyncEnumerable<PixelCluster> CompareTwoImagesAsync(Bitmap img1, Bitmap img2)
        {
            // The expensive per-pixel scan is offloaded so callers (e.g. a UI thread) stay responsive;
            // clusters are then yielded lazily as the search walks the difference map.
            bool[,] isDifferent = await Task.Run(() => DetectDifferences(img1, img2));

            foreach (var cluster in FindClusters(isDifferent))
            {
                yield return cluster;
            }
        }

        /// <summary>
        /// Compares two images and returns the difference regions.
        /// </summary>
        /// <param name="img1">First source image.</param>
        /// <param name="img2">Second source image.</param>
        /// <returns><see cref="IEnumerable{PixelCluster}"/></returns>
        /// <exception cref="ArgumentException">The images are not the same size.</exception>
        public IEnumerable<PixelCluster> CompareTwoImages(Bitmap img1, Bitmap img2)
        {
            bool[,] isDifferent = DetectDifferences(img1, img2);
            return FindClusters(isDifferent);
        }

        /// <summary>
        /// Builds a 2D map (indexed <c>[x, y]</c>) flagging every pixel whose difference exceeds
        /// <see cref="ErrorTolerance"/> under the chosen <see cref="Algorithm"/>.
        /// </summary>
        /// <param name="img1">First source image.</param>
        /// <param name="img2">Second source image.</param>
        /// <returns>A <see cref="bool"/> array where <c>true</c> marks a differing pixel.</returns>
        /// <exception cref="ArgumentException">The images are not the same size.</exception>
        public bool[,] DetectDifferences(Bitmap img1, Bitmap img2)
        {
            EnsureSameSize(img1, img2);

            int width = img1.Width;
            int height = img1.Height;
            var isDifferent = new bool[width, height];

            var rect = new Rectangle(0, 0, width, height);
            BitmapData data1 = img1.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData data2 = img2.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride1 = data1.Stride;
                int stride2 = data2.Stride;

                byte[] buffer1 = new byte[stride1 * height];
                byte[] buffer2 = new byte[stride2 * height];
                Marshal.Copy(data1.Scan0, buffer1, 0, buffer1.Length);
                Marshal.Copy(data2.Scan0, buffer2, 0, buffer2.Length);

                CalculateDiff diff = diffFormula[Algorithm];

                for (int y = 0; y < height; y++)
                {
                    int rowStart1 = y * stride1;
                    int rowStart2 = y * stride2;

                    for (int x = 0; x < width; x++)
                    {
                        int offset1 = rowStart1 + x * BytesPerPixel;
                        int offset2 = rowStart2 + x * BytesPerPixel;

                        // Locked bytes are laid out as B, G, R, A.
                        Color pixel1 = Color.FromArgb(buffer1[offset1 + 3], buffer1[offset1 + 2], buffer1[offset1 + 1], buffer1[offset1]);
                        Color pixel2 = Color.FromArgb(buffer2[offset2 + 3], buffer2[offset2 + 2], buffer2[offset2 + 1], buffer2[offset2]);

                        if (diff(pixel1, pixel2) > ErrorTolerance)
                        {
                            isDifferent[x, y] = true;
                        }
                    }
                }
            }
            finally
            {
                img1.UnlockBits(data1);
                img2.UnlockBits(data2);
            }

            return isDifferent;
        }

        // Walks the difference map, clustering connected differing pixels and yielding the regions
        // that are larger than SizeOfDiff, stopping once MaxNumberOfDiff regions have been reported.
        private IEnumerable<PixelCluster> FindClusters(bool[,] isDifferent)
        {
            int width = isDifferent.GetLength(0);
            int height = isDifferent.GetLength(1);
            var isVisited = new bool[width, height];
            int found = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (MaxNumberOfDiff > 0 && found >= MaxNumberOfDiff)
                    {
                        yield break;
                    }

                    if (isDifferent[x, y] && !isVisited[x, y])
                    {
                        PixelCluster cluster = Flood(isDifferent, isVisited, x, y);
                        if (cluster.Size > SizeOfDiff)
                        {
                            found++;
                            yield return cluster;
                        }
                    }
                }
            }
        }

        // Iterative flood fill (8-connectivity, stride = DepthOfDFS). Replaces the old recursion so large
        // difference regions can no longer overflow the call stack.
        private PixelCluster Flood(bool[,] isDifferent, bool[,] isVisited, int startX, int startY)
        {
            int width = isDifferent.GetLength(0);
            int height = isDifferent.GetLength(1);
            int step = DepthOfDFS;

            int[] dx = { -step, -step, -step, 0, 0, step, step, step };
            int[] dy = { -step, 0, step, -step, step, -step, 0, step };

            var cluster = new PixelCluster();
            var stack = new Stack<(int X, int Y)>();

            isVisited[startX, startY] = true;
            stack.Push((startX, startY));

            while (stack.Count > 0)
            {
                (int x, int y) = stack.Pop();
                cluster.Add(x, y);

                for (int k = 0; k < dx.Length; k++)
                {
                    int nx = x + dx[k];
                    int ny = y + dy[k];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && isDifferent[nx, ny] && !isVisited[nx, ny])
                    {
                        isVisited[nx, ny] = true;
                        stack.Push((nx, ny));
                    }
                }
            }

            return cluster;
        }

        // Draws the same red rectangle around a region on both images, clamped to the image bounds.
        private static void Highlight(Bitmap img1, Bitmap img2, Pen pen, PixelCluster cluster)
        {
            int left = Math.Max(0, cluster.LeftPoint - RectanglePadding);
            int top = Math.Max(0, cluster.UpperPoint - RectanglePadding);
            int right = Math.Min(img1.Width - 1, cluster.RightPoint + RectanglePadding);
            int bottom = Math.Min(img1.Height - 1, cluster.LowerPoint + RectanglePadding);

            var rectangle = new Rectangle(left, top, right - left, bottom - top);

            using (Graphics g = Graphics.FromImage(img1))
            {
                g.DrawRectangle(pen, rectangle);
            }

            using (Graphics g = Graphics.FromImage(img2))
            {
                g.DrawRectangle(pen, rectangle);
            }
        }

        private static void EnsureSameSize(Bitmap img1, Bitmap img2)
        {
            if (img1.Width != img2.Width || img1.Height != img2.Height)
            {
                throw new ArgumentException("Images should be of the same size pixel-wise.");
            }
        }
    }
}
