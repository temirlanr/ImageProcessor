using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsposeTask
{
    public enum ComparisonAlgorithm
    {
        /// <summary>
        /// Compares Pixels by 4 values: A - Transparency value, R - Red value, G - Green value, B - Blue value
        /// </summary>
        ARGB,
        /// <summary>
        /// Compares Pixels by 3 values: R - Red value, G - Green value, B - Blue value
        /// </summary>
        RGB
    }

    /// <summary>
    /// Implementation of IImageProcessor interface that compares two same images for differences
    /// </summary>
    /// <remarks>
    /// Images must be of the same size
    /// </remarks>
    public class ImageProcessor : IImageProcessor
    {
        /// <summary>
        /// Specifies which Formula to use when comparing images
        /// </summary>
        public ComparisonAlgorithm Algorithm { get; set; }
        /// <summary>
        /// Specifies what is the maximum number of differences to detect
        /// </summary>
        /// <remarks>
        /// Choose 0 or negative value for no maximum
        /// </remarks>
        public int MaxNumberOfDiff { get; set; }
        /// <summary>
        /// Specifies the Error Tolerance when comparing images
        /// </summary>
        /// <remarks>
        /// The lower it is the more differences it will detect. Preferred values are >5, otherwise there is a chance of Stack Overflow.
        /// </remarks>
        public int ErrorTolerance { get; set; }
        /// <summary>
        /// Specifies on how far should be every step of Depth First Search
        /// </summary>
        public int DepthOfDFS { get; set; }
        /// <summary>
        /// Specifies the minimum size of regions that should be detected
        /// </summary>
        public int SizeOfDiff { get; set; }

        /// <summary>
        /// Regular Constructor where you need to specify the configuration by yourself
        /// </summary>
        /// <remarks>
        /// For better performance it is recommended to use default parameters, but feel free to change them if you are not satisfied with a result
        /// </remarks>
        /// <param name="algorithm"></param>
        /// <param name="maxNumberOfDiff"></param>
        /// <param name="errorTolerance"></param>
        /// <param name="depthOfDFS"></param>
        /// <param name="sizeOfDiff"></param>
        public ImageProcessor
            (ComparisonAlgorithm algorithm = ComparisonAlgorithm.ARGB,
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

        private Dictionary<ComparisonAlgorithm, CalculateDiff> diffFormula = new Dictionary<ComparisonAlgorithm, CalculateDiff>
        {
            [ComparisonAlgorithm.ARGB] = (pixel1, pixel2) => 
            {
                return (Math.Abs(pixel1.A - pixel2.A) + Math.Abs(pixel1.R - pixel2.R) + Math.Abs(pixel1.G - pixel2.G) + Math.Abs(pixel1.B - pixel2.B)) / 4;
            },
            [ComparisonAlgorithm.RGB] = (pixel1, pixel2) =>
            {
                return (Math.Abs(pixel1.R - pixel2.R) + Math.Abs(pixel1.G - pixel2.G) + Math.Abs(pixel1.B - pixel2.B)) / 3;
            }
        };

        /// <summary>
        /// Starts the comparison algorithm and saves resultant files into local drive
        /// </summary>
        /// <remarks>
        /// You might want to use this method for better performance
        /// </remarks>
        /// <param name="fileName1"></param>
        /// <param name="fileName2"></param>
        /// <param name="resultFileName1"></param>
        /// <param name="resultFileName2"></param>
        /// <returns><see cref="Task"></see></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task StartAsync(string fileName1, string fileName2, string resultFileName1, string resultFileName2)
        {
            Pen pen = new Pen(Color.Red, 5);
            Bitmap img1 = new Bitmap(fileName1);
            Bitmap img2 = new Bitmap(fileName2);

            if (img1.Width != img2.Width || img1.Height != img2.Height)
            {
                throw new ArgumentException("Images should be of the same size pixel-wise.");
            }

            //ValidateAutoAdjustment(img1, img2);

            await foreach (var cluster in CompareTwoImagesAsync(img1, img2))
            {
                Console.WriteLine(cluster.ToString());

                Rectangle rectangle = new Rectangle(cluster.LeftPoint - 5, cluster.UpperPoint - 5, cluster.RightPoint - cluster.LeftPoint + 10, cluster.LowerPoint - cluster.UpperPoint + 10);

                using (Graphics g = Graphics.FromImage(img1))
                {
                    g.DrawRectangle(pen, rectangle);
                }

                using (Graphics g = Graphics.FromImage(img2))
                {
                    g.DrawRectangle(pen, rectangle);
                }
            }

            img1.Save(resultFileName1);
            img2.Save(resultFileName2);
        }

        /// <summary>
        /// Starts the comparison algorithm and saves resultant files into local drive
        /// </summary>
        /// <remarks>
        /// You might want to use this method for better performance
        /// </remarks>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <param name="resultFileName1"></param>
        /// <param name="resultFileName2"></param>
        /// <returns><see cref="Task"></see></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task StartAsync(Bitmap img1, Bitmap img2, string resultFileName1, string resultFileName2)
        {
            if (img1.Width != img2.Width || img1.Height != img2.Height)
            {
                throw new ArgumentException("Images should be of the same size pixel-wise.");
            }

            Pen pen = new Pen(Color.Red, 5);

            await foreach (var cluster in CompareTwoImagesAsync(img1, img2))
            {
                Console.WriteLine(cluster.ToString());

                Rectangle rectangle = new Rectangle(cluster.LeftPoint - 5, cluster.UpperPoint - 5, cluster.RightPoint - cluster.LeftPoint + 10, cluster.LowerPoint - cluster.UpperPoint + 10);

                using (Graphics g = Graphics.FromImage(img1))
                {
                    g.DrawRectangle(pen, rectangle);
                }

                using (Graphics g = Graphics.FromImage(img2))
                {
                    g.DrawRectangle(pen, rectangle);
                }
            }

            img1.Save(resultFileName1);
            img2.Save(resultFileName2);
        }

        /// <summary>
        /// Starts the comparison algorithm and saves resultant files into local drive
        /// </summary>
        /// <param name="fileName1"></param>
        /// <param name="fileName2"></param>
        /// <param name="resultFileName1"></param>
        /// <param name="resultFileName2"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Start(string fileName1, string fileName2, string resultFileName1, string resultFileName2)
        {
            Pen pen = new Pen(Color.Red, 5);
            Bitmap img1 = new Bitmap(fileName1);
            Bitmap img2 = new Bitmap(fileName2);

            if (img1.Width != img2.Width || img1.Height != img2.Height)
            {
                throw new ArgumentException("Images should be of the same size pixel-wise.");
            }

            var clusters = CompareTwoImages(img1, img2);

            foreach (var cluster in clusters)
            {
                Console.WriteLine(cluster.ToString());

                Rectangle rectangle = new Rectangle(cluster.LeftPoint - 5, cluster.UpperPoint - 5, cluster.RightPoint - cluster.LeftPoint + 10, cluster.LowerPoint - cluster.UpperPoint + 10);

                using (Graphics g = Graphics.FromImage(img1))
                {
                    g.DrawRectangle(pen, rectangle);
                }

                using (Graphics g = Graphics.FromImage(img2))
                {
                    g.DrawRectangle(pen, rectangle);
                }
            }

            img1.Save(resultFileName1);
            img2.Save(resultFileName2);
        }

        /// <summary>
        /// Starts the comparison algorithm and saves resultant files into local drive
        /// </summary>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <param name="resultFileName1"></param>
        /// <param name="resultFileName2"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Start(Bitmap img1, Bitmap img2, string resultFileName1, string resultFileName2)
        {
            if (img1.Width != img2.Width || img1.Height != img2.Height)
            {
                throw new ArgumentException("Images should be of the same size pixel-wise.");
            }

            Pen pen = new Pen(Color.Red, 5);

            foreach (var cluster in CompareTwoImages(img1, img2))
            {
                Console.WriteLine(cluster.ToString());

                Rectangle rectangle = new Rectangle(cluster.LeftPoint - 5, cluster.UpperPoint - 5, cluster.RightPoint - cluster.LeftPoint + 10, cluster.LowerPoint - cluster.UpperPoint + 10);

                using (Graphics g = Graphics.FromImage(img1))
                {
                    g.DrawRectangle(pen, rectangle);
                }

                using (Graphics g = Graphics.FromImage(img2))
                {
                    g.DrawRectangle(pen, rectangle);
                }
            }

            img1.Save(resultFileName1);
            img2.Save(resultFileName2);
        }

        /// <summary>
        /// Compares images based on chosen comparison algorithm
        /// </summary>
        /// <remarks>
        /// You might want to use this method for better performance
        /// </remarks>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <returns><see cref="IAsyncEnumerable{PixelCluster}"/></returns>
        public async IAsyncEnumerable<PixelCluster> CompareTwoImagesAsync(Bitmap img1, Bitmap img2)
        {
            bool[,] isVisited = new bool[img1.Width, img1.Height];
            bool[,] isDifferent =  await Task.Run(() => DetectDifferences(img1, img2));

            int size = 0;

            for(int i = 5; i < img1.Width - 5; i++)
            {
                for(int j = 5; j < img1.Height - 5; j++)
                {
                    if(MaxNumberOfDiff > 0)
                    {
                        if(size >= MaxNumberOfDiff)
                        {
                            continue;
                        }
                    }

                    if (isDifferent[i, j] && !isVisited[i, j])
                    {
                        PixelCluster cluster = new PixelCluster();
                        await Task.Run(() => DFS(cluster, isDifferent, i, j, isVisited));
                        cluster.SetBorders();
                        if (cluster.Size > SizeOfDiff)
                        {
                            size += 1;
                            yield return cluster;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compares images based on chosen comparison algorithm
        /// </summary>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <returns><see cref="IEnumerable{PixelCluster}"/></returns>
        public IEnumerable<PixelCluster> CompareTwoImages(Bitmap img1, Bitmap img2)
        {
            List<PixelCluster> result = new List<PixelCluster>();

            bool[,] isVisited = new bool[img1.Width, img1.Height];
            bool[,] isDifferent = DetectDifferences(img1, img2);

            for (int i = 5; i < img1.Width - 5; i++)
            {
                for (int j = 5; j < img1.Height - 5; j++)
                {
                    if (MaxNumberOfDiff > 0)
                    {
                        if (result.Count >= MaxNumberOfDiff)
                        {
                            continue;
                        }
                    }

                    if (isDifferent[i, j] && !isVisited[i, j])
                    {
                        PixelCluster cluster = new PixelCluster();
                        DFS(cluster, isDifferent, i, j, isVisited);
                        cluster.SetBorders();
                        if (cluster.Size > SizeOfDiff)
                        {
                            result.Add(cluster);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// This method is used to detect the differences and return 2D array of booleans representing each pixel
        /// </summary>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <returns><see cref="Array"/> of type <see cref="bool"/></returns>
        public bool[,] DetectDifferences(Bitmap img1, Bitmap img2)
        {
            bool[,] isDifferent = new bool[img1.Width, img1.Height];

            for (int i = 5; i < img1.Width - 5; i++)
            {
                for (int j = 5; j < img1.Height - 5; j++)
                {
                    if (diffFormula[Algorithm](img1.GetPixel(i, j), img2.GetPixel(i, j)) > ErrorTolerance)
                    {
                        isDifferent[i, j] = true;
                    }
                }
            }

            return isDifferent;
        }

        // Checker for DFS
        private bool IsSafe(bool[,] isDifferent, int row, int col, bool[,] visited)
        {
            return (row >= 0) && (row < isDifferent.GetLength(0)) && (col >= 0) && (col < isDifferent.GetLength(1)) && (isDifferent[row, col] && !visited[row, col]);
        }

        // Depth First Search
        private void DFS(PixelCluster cluster, bool[,] isDifferent, int row, int col, bool[,] isVisited)
        {
            int[] rowNbr = new int[] { -DepthOfDFS, -DepthOfDFS, -DepthOfDFS, 0, 0, DepthOfDFS, DepthOfDFS, DepthOfDFS };
            int[] colNbr = new int[] { -DepthOfDFS, 0, DepthOfDFS, -DepthOfDFS, DepthOfDFS, -DepthOfDFS, 0, DepthOfDFS };

            isVisited[row, col] = true;

            cluster.XCoordinates.Add(row);
            cluster.YCoordinates.Add(col);

            for (int k = 0; k < 8; ++k)
            {
                if (IsSafe(isDifferent, row + rowNbr[k], col + colNbr[k], isVisited))
                {
                    DFS(cluster, isDifferent, row + rowNbr[k], col + colNbr[k], isVisited);
                }
            }
        }
    }
}
