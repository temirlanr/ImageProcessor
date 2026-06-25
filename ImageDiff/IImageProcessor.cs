using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsposeTask
{
    /// <summary>
    /// Interface for Image Processor that compares two same images for differences
    /// </summary>
    /// <remarks>
    /// Images must be of the same size
    /// </remarks>
    public interface IImageProcessor
    {
        /// <summary>
        /// Specifies which Formula to use when comparing images
        /// </summary>
        ComparisonAlgorithm Algorithm { get; set; }
        /// <summary>
        /// Specifies how many differences to detect
        /// </summary>
        /// <remarks>
        /// Choose 0 or negative value for no maximum
        /// </remarks>
        int MaxNumberOfDiff { get; set; }
        /// <summary>
        /// Specifies the Error Tolerance when comparing images
        /// </summary>
        /// <remarks>
        /// The lower it is the more differences it will detect. Preferred values are >5, otherwise there is a chance of Stack Overflow.
        /// </remarks>
        int ErrorTolerance { get; set; }
        /// <summary>
        /// Specifies on how far should be every step of Depth First Search
        /// </summary>
        int DepthOfDFS { get; set; }
        /// <summary>
        /// Specifies the minimum size of regions that should be detected
        /// </summary>
        int SizeOfDiff { get; set; }
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
        Task StartAsync(string fileName1, string fileName2, string resultFileName1, string resultFileName2);
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
        Task StartAsync(Bitmap img1, Bitmap img2, string resultFileName1, string resultFileName2);
        /// <summary>
        /// Starts the comparison algorithm and saves resultant files into local drive
        /// </summary>
        /// <param name="fileName1"></param>
        /// <param name="fileName2"></param>
        /// <param name="resultFileName1"></param>
        /// <param name="resultFileName2"></param>
        /// <exception cref="ArgumentException"></exception>
        void Start(string fileName1, string fileName2, string resultFileName1, string resultFileName2);
        /// <summary>
        /// Starts the comparison algorithm and saves resultant files into local drive
        /// </summary>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <param name="resultFileName1"></param>
        /// <param name="resultFileName2"></param>
        /// <exception cref="ArgumentException"></exception>
        void Start(Bitmap img1, Bitmap img2, string resultFileName1, string resultFileName2);
        /// <summary>
        /// Compares images based on chosen comparison algorithm
        /// </summary>
        /// <remarks>
        /// You might want to use this method for better performance
        /// </remarks>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <returns><see cref="IAsyncEnumerable{PixelCluster}"/></returns>
        IAsyncEnumerable<PixelCluster> CompareTwoImagesAsync(Bitmap img1, Bitmap img2);
        /// <summary>
        /// Compares images based on chosen comparison algorithm
        /// </summary>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <returns><see cref="IEnumerable{PixelCluster}"/></returns>
        IEnumerable<PixelCluster> CompareTwoImages(Bitmap img1, Bitmap img2);
        /// <summary>
        /// This method is used to detect the differences and return 2D array of booleans representing each pixel
        /// </summary>
        /// <param name="img1"></param>
        /// <param name="img2"></param>
        /// <returns><see cref="Array"/> of type <see cref="bool"/></returns>
        bool[,] DetectDifferences(Bitmap img1, Bitmap img2);
    }
}
