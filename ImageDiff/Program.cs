namespace ImageDiff
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Demo: compare the bundled sample pair and write the highlighted results next to the executable.
            IImageProcessor processor = new ImageProcessor();
            await processor.StartAsync(
                "./Samples/Sample_2_A.jpg",
                "./Samples/Sample_2_B.jpg",
                "result2A.jpg",
                "result2B.jpg");

            Console.WriteLine("Done. Wrote result2A.jpg and result2B.jpg.");
        }
    }
}
