# ImageProcessor

A small Windows/.NET tool that compares two equally sized images, finds the regions where
they differ, and writes out both images with the differences boxed in red.

## What it does

Given two images of the same dimensions, it:

1. Compares them pixel by pixel and builds a map of where they differ.
2. Groups the differing pixels into rectangular **clusters** (connected regions).
3. Prints the bounding box and size of each cluster.
4. Saves copies of both images with every cluster highlighted by a red rectangle.

## Project layout

| Path | Description |
| --- | --- |
| `ImageDiff/` | The library and a small console demo (`Program.cs`). Namespace `ImageDiff`. |
| `ImageDiff/Samples/` | Sample image pairs used by the demo and the tests. |
| `ImageProcessorTest/` | NUnit test project. |
| `ImageDiff.sln` | Solution containing both projects. |

## Requirements

- .NET 10 SDK
- Windows — pixel access and drawing use `System.Drawing` (GDI+), which is Windows-only.

## Getting started

Run the bundled demo (compares `Samples/Sample_2_A.jpg` and `Sample_2_B.jpg` and writes
`result2A.jpg` / `result2B.jpg`):

```bash
dotnet run --project ImageDiff
```

Run the tests:

```bash
dotnet test
```

## Using it from code

```csharp
using ImageDiff;

IImageProcessor processor = new ImageProcessor(
    algorithm: ComparisonAlgorithm.ARGB,
    maxNumberOfDiff: 0,   // 0 = no limit
    errorTolerance: 15,
    depthOfDFS: 1,
    sizeOfDiff: 25);

// Compare two files and save the highlighted results.
await processor.StartAsync("a.jpg", "b.jpg", "out_a.jpg", "out_b.jpg");

// Or stream the difference regions yourself.
using var img1 = new Bitmap("a.jpg");
using var img2 = new Bitmap("b.jpg");
await foreach (PixelCluster cluster in processor.CompareTwoImagesAsync(img1, img2))
{
    Console.WriteLine(cluster);
}
```

A synchronous `Start` / `CompareTwoImages` pair is available as well.

## Configuration

The behaviour is controlled by properties on `ImageProcessor`:

| Property | Meaning |
| --- | --- |
| `Algorithm` | Pixel comparison formula: `ARGB` (4 channels) or `RGB` (3 channels). |
| `ErrorTolerance` | How different two pixels must be before they count as different. Lower = more sensitive. |
| `SizeOfDiff` | Minimum region size, in pixels, for a cluster to be reported. Filters out noise. |
| `DepthOfDFS` | Stride used when connecting differing pixels into a region. |
| `MaxNumberOfDiff` | Maximum number of regions to report; `0` or less means unlimited. |

## How it works

- **Comparison.** `DetectDifferences` locks both bitmaps with `Bitmap.LockBits` and walks the raw
  pixel bytes, so it never pays the per-call cost of `GetPixel`. Each pixel pair is scored by the
  selected formula and flagged when the score exceeds `ErrorTolerance`.
- **Pluggable formulas.** The comparison formulas live in a single dictionary keyed by
  `ComparisonAlgorithm`. To add your own metric, add a value to the enum and a matching entry.
- **Clustering.** The flagged pixels form a "number of islands" problem. Each island is grown with
  an **iterative** flood fill (8-connectivity) and summarised as a `PixelCluster`, which keeps only
  its bounding box and pixel count rather than every coordinate.
- **Streaming.** `CompareTwoImagesAsync` offloads the pixel scan to a background thread and yields
  clusters through `IAsyncEnumerable`, so a caller sees each region as soon as it is found.

## Notes and limitations

- Windows-only, because of the `System.Drawing` dependency.
- The two images must have identical dimensions; otherwise an `ArgumentException` is thrown.
- JPEG is lossy, so comparing re-encoded photos may need a higher `ErrorTolerance`.

## Possible improvements

- Swap `System.Drawing` for a cross-platform library (e.g. ImageSharp) to drop the Windows requirement.
- Parallelise the pixel scan across rows for large images.
- Package the library on NuGet.
- Expand test coverage (RGB mode, tolerance edge cases, large images).
