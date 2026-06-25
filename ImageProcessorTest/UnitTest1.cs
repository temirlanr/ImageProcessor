using ImageDiff;
using NUnit.Framework;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace ImageProcessorTest
{
    [TestFixture]
    public class Tests
    {
        IImageProcessor processor = null!;

        [SetUp]
        public void SetUp()
        {
            processor = new ImageProcessor();
        }

        [Test]
        public async Task CompareTwoImagesAsync_CheckPixelClusters()
        {
            processor.MaxNumberOfDiff = 4;
            processor.SizeOfDiff = 100;

            Bitmap img1A = new Bitmap(Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "Sample_1_A.jpg"));
            Bitmap img1B = new Bitmap(Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "Sample_1_B.jpg"));

            int count = 0;

            await foreach (var cluster in processor.CompareTwoImagesAsync(img1A, img1B))
            {
                Assert.IsInstanceOf<PixelCluster>(cluster);
                Assert.Less(100, cluster.Size);
                count++;
            }

            Assert.AreEqual(4, count);
        }

        [Test]
        public void CompareTwoImages_CheckPixelClusters()
        {
            processor.MaxNumberOfDiff = 4;
            processor.SizeOfDiff = 150;

            Bitmap img1A = new Bitmap(Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "Sample_1_A.jpg"));
            Bitmap img1B = new Bitmap(Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "Sample_1_B.jpg"));

            int count = 0;

            foreach(var cluster in processor.CompareTwoImages(img1A, img1B))
            {
                Assert.IsInstanceOf<PixelCluster>(cluster);
                Assert.Less(150, cluster.Size);
                count++;
            }

            Assert.AreEqual(4, count);
        }

        [Test]
        public void Start_NotEqualImages_ThrowsArgumentException()
        {
            Bitmap img1A = new Bitmap(Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "Sample_1_A.jpg"));
            Bitmap img2A = new Bitmap(Path.Combine(TestContext.CurrentContext.TestDirectory, "Samples", "Sample_2_A.jpg"));

            TestDelegate act = () => processor.Start(img1A, img2A, "result1a.jpg", "result2a.jpg");

            Assert.Throws<ArgumentException>(act);
        }
    }
}