using MediaForge.Core.Importing;

namespace MediaForge.Core.Tests;

public sealed class FileNameFilterTests
{
    [Fact]
    public void Keyword_filter_ignores_case_and_only_reads_the_file_name()
    {
        var filter = FileNameFilter.Create(FileNameFilterMode.Keyword, "holiday");

        Assert.True(filter.IsMatch(@"C:\archive\HOLIDAY-final.MP4"));
        Assert.False(filter.IsMatch(@"C:\holiday\unrelated.mp4"));
    }

    [Theory]
    [InlineData("*.mp4", "clip.mp4", true)]
    [InlineData("*.mp4", "clip.mp4.bak", false)]
    [InlineData("take-??.mkv", "take-07.mkv", true)]
    [InlineData("take-??.mkv", "take-7.mkv", false)]
    public void Wildcard_filter_matches_the_whole_file_name(string expression, string fileName, bool expected)
    {
        var filter = FileNameFilter.Create(FileNameFilterMode.Wildcard, expression);

        Assert.Equal(expected, filter.IsMatch(fileName));
    }

    [Fact]
    public void Invalid_regular_expression_is_rejected_before_scanning()
    {
        var error = Assert.Throws<ArgumentException>(
            () => FileNameFilter.Create(FileNameFilterMode.RegularExpression, "[unfinished"));

        Assert.Contains("regular expression", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
