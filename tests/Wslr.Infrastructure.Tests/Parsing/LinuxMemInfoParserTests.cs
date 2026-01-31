using Wslr.Core.Parsing;

namespace Wslr.Infrastructure.Tests.Parsing;

public class LinuxMemInfoParserTests
{
    private const string SampleMemInfo = """
        MemTotal:       16323740 kB
        MemFree:        13707600 kB
        MemAvailable:   14771396 kB
        Buffers:           67364 kB
        Cached:           623228 kB
        SwapCached:        18416 kB
        Active:           550984 kB
        Inactive:         844952 kB
        Active(anon):     132532 kB
        Inactive(anon):   576828 kB
        Active(file):     418452 kB
        Inactive(file):   268124 kB
        Unevictable:           0 kB
        Mlocked:               0 kB
        SwapTotal:       4194304 kB
        SwapFree:        4015096 kB
        Dirty:                16 kB
        Writeback:             0 kB
        AnonPages:        691916 kB
        Mapped:           415664 kB
        """;

    #region Parse Tests

    [Fact]
    public void Parse_WithValidInput_ReturnsLinuxMemInfo()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Parse_WithValidInput_ParsesMemTotal()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        result!.MemTotalKb.Should().Be(16323740);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesMemFree()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        result!.MemFreeKb.Should().Be(13707600);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesMemAvailable()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        result!.MemAvailableKb.Should().Be(14771396);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesBuffers()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        result!.BuffersKb.Should().Be(67364);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesCached()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        result!.CachedKb.Should().Be(623228);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesSwapTotal()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        result!.SwapTotalKb.Should().Be(4194304);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesSwapFree()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        result!.SwapFreeKb.Should().Be(4015096);
    }

    [Fact]
    public void Parse_WithNullInput_ReturnsNull()
    {
        var result = LinuxMemInfoParser.Parse(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithEmptyInput_ReturnsNull()
    {
        var result = LinuxMemInfoParser.Parse("");

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithWhitespaceInput_ReturnsNull()
    {
        var result = LinuxMemInfoParser.Parse("   \n   ");

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMissingMemTotal_ReturnsNull()
    {
        var input = """
            MemFree:        13707600 kB
            MemAvailable:   14771396 kB
            """;

        var result = LinuxMemInfoParser.Parse(input);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMissingMemFree_ReturnsNull()
    {
        var input = """
            MemTotal:       16323740 kB
            MemAvailable:   14771396 kB
            """;

        var result = LinuxMemInfoParser.Parse(input);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMissingMemAvailable_CalculatesFromFreeBuffersCached()
    {
        var input = """
            MemTotal:       16323740 kB
            MemFree:        13707600 kB
            Buffers:           67364 kB
            Cached:           623228 kB
            """;

        var result = LinuxMemInfoParser.Parse(input);

        result.Should().NotBeNull();
        // MemAvailable = MemFree + Buffers + Cached
        result!.MemAvailableKb.Should().Be(13707600 + 67364 + 623228);
    }

    [Fact]
    public void Parse_WithMinimalInput_Works()
    {
        var input = """
            MemTotal:       16323740 kB
            MemFree:        13707600 kB
            """;

        var result = LinuxMemInfoParser.Parse(input);

        result.Should().NotBeNull();
        result!.MemTotalKb.Should().Be(16323740);
        result.MemFreeKb.Should().Be(13707600);
        result.MemAvailableKb.Should().Be(13707600); // Falls back to MemFree when nothing else available
    }

    [Fact]
    public void Parse_WithMalformedLines_SkipsThemGracefully()
    {
        var input = """
            MemTotal:       16323740 kB
            This is not a valid line
            MemFree:        13707600 kB
            AnotherBadLine
            MemAvailable:   14771396 kB
            """;

        var result = LinuxMemInfoParser.Parse(input);

        result.Should().NotBeNull();
        result!.MemTotalKb.Should().Be(16323740);
        result.MemFreeKb.Should().Be(13707600);
        result.MemAvailableKb.Should().Be(14771396);
    }

    [Fact]
    public void Parse_IsCaseInsensitive()
    {
        var input = """
            MEMTOTAL:       16323740 KB
            memfree:        13707600 kb
            MemAvailable:   14771396 kB
            """;

        var result = LinuxMemInfoParser.Parse(input);

        result.Should().NotBeNull();
        result!.MemTotalKb.Should().Be(16323740);
        result.MemFreeKb.Should().Be(13707600);
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void UsedMemoryKb_CalculatesCorrectly()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        // UsedMemory = MemTotal - MemAvailable = 16323740 - 14771396 = 1552344
        result!.UsedMemoryKb.Should().Be(16323740 - 14771396);
    }

    [Fact]
    public void UsedMemoryGb_CalculatesCorrectly()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        // UsedMemoryGb = UsedMemoryKb / 1024 / 1024
        var expectedGb = (16323740 - 14771396) / 1024.0 / 1024.0;
        result!.UsedMemoryGb.Should().BeApproximately(expectedGb, 0.001);
    }

    [Fact]
    public void TotalMemoryGb_CalculatesCorrectly()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        var expectedGb = 16323740 / 1024.0 / 1024.0;
        result!.TotalMemoryGb.Should().BeApproximately(expectedGb, 0.001);
    }

    [Fact]
    public void AvailableMemoryGb_CalculatesCorrectly()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        var expectedGb = 14771396 / 1024.0 / 1024.0;
        result!.AvailableMemoryGb.Should().BeApproximately(expectedGb, 0.001);
    }

    [Fact]
    public void UsagePercent_CalculatesCorrectly()
    {
        var result = LinuxMemInfoParser.Parse(SampleMemInfo);

        // UsagePercent = UsedMemoryKb / MemTotalKb * 100
        var expectedPercent = (double)(16323740 - 14771396) / 16323740 * 100.0;
        result!.UsagePercent.Should().BeApproximately(expectedPercent, 0.001);
    }

    [Fact]
    public void UsagePercent_WithZeroMemTotal_ReturnsZero()
    {
        // This is a defensive test - shouldn't happen in practice
        var memInfo = new LinuxMemInfo
        {
            MemTotalKb = 0,
            MemFreeKb = 0,
            MemAvailableKb = 0,
            BuffersKb = 0,
            CachedKb = 0
        };

        memInfo.UsagePercent.Should().Be(0);
    }

    #endregion
}
