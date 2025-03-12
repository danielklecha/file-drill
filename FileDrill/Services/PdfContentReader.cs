using FileDrill.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Rendering.Skia;

namespace FileDrill.Services;
internal class PdfContentReader(
    IChatClientFactory chatClientFactory,
    System.IO.Abstractions.IFileSystem fileSystem,
    IOptions<WritableOptions> options,
    ILogger<ImageContentReader> loggerForImageContentExtractor) : IContentReader
{
    public string[] Extensions { get; } = [".pdf"];

    public async Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(path);
        var plainText = ExtractPlainText(document);
        if (!string.IsNullOrEmpty(plainText))
            return plainText;
        document.AddSkiaPageFactory();
        StringBuilder sb = new();
        for (int p = 1; p <= document.NumberOfPages; p++)
        {
            using var stream = document.GetPageAsPng(p);
            ImageContentReader imageContentExtractor = new(chatClientFactory, fileSystem, options, loggerForImageContentExtractor);
            sb.AppendLine(await imageContentExtractor.GetContentAsync(stream.ToArray(), $"page_{p}.png", cancellationToken));
        }
        return sb.ToString();
    }

    private static string ExtractPlainText(PdfDocument pdfDocument)
    {
        var outputBuilder = new StringBuilder();
        foreach (var page in pdfDocument.GetPages())
            outputBuilder.Append(ExtractPlainText(page));
        return outputBuilder.ToString();
    }

    private static string ExtractPlainText(Page page)
    {
        var outputBuilder = new StringBuilder();
        var words = page.GetWords();
        double pageWidth = page.Width;

        // Group words by their Y position (approximation of lines)
        var lines = GroupWordsByLine(words);

        foreach (var line in lines)
        {
            // Sort words in the line by their X position (left to right)
            line.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));

            double previousRight = 0;
            foreach (var word in line)
            {
                // Calculate the number of spaces to insert based on the gap between words
                double gap = word.BoundingBox.Left - previousRight;

                if (gap > 0)
                {
                    int spaces = (int)(gap / (pageWidth / 80)); // Approximate spaces based on the gap and page width
                    outputBuilder.Append(new string(' ', spaces));
                }

                outputBuilder.Append(word.Text);
                previousRight = word.BoundingBox.Right;
            }

            outputBuilder.AppendLine(); // Add a new line at the end of each line
        }
        return outputBuilder.ToString();
    }

    /// <summary>
    /// Groups words by their Y position to approximate lines.
    /// </summary>
    /// <param name="words">A collection of words to group into lines.</param>
    /// <returns>A list of lines, each containing a list of words.</returns>
    private static List<List<Word>> GroupWordsByLine(IEnumerable<Word> words)
    {
        const float lineThreshold = 5; // Adjust this threshold as needed
        var lines = new List<List<Word>>();

        foreach (var word in words)
        {
            bool addedToLine = false;

            foreach (var line in lines)
            {
                // Check if the word belongs to the current line
                if (Math.Abs(line[0].BoundingBox.Top - word.BoundingBox.Top) < lineThreshold)
                {
                    line.Add(word);
                    addedToLine = true;
                    break;
                }
            }

            if (!addedToLine)
                lines.Add([word]);
        }

        return lines;
    }
}
