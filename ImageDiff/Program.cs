namespace AsposeTask
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IImageProcessor processor = new ImageProcessor();
            await processor.StartAsync("./Samples/Sample_2_A.jpg", "./Samples/Sample_2_B.jpg", "result2A.jpg", "result2B.jpg");
        }
    }
}