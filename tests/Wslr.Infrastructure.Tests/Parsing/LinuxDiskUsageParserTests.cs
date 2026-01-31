using Wslr.Core.Parsing;

namespace Wslr.Infrastructure.Tests.Parsing;

public class LinuxDiskUsageParserTests
{
    private const string SampleDfOutput = """
        Filesystem     1B-blocks        Used   Available Use% Mounted on
        /dev/sdc       269490393088 8547123456 247181266944   4% /
        """;

    private const string MultiFilesystemOutput = """
        Filesystem     1B-blocks        Used   Available Use% Mounted on
        none           269490393088          0 269490393088   0% /mnt/wsl
        /dev/sdc       269490393088 8547123456 247181266944   4% /
        none           269490393088          0 269490393088   0% /usr/lib/wsl/drivers
        """;

    #region Parse Tests

    [Fact]
    public void Parse_WithValidInput_ReturnsLinuxDiskUsage()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Parse_WithValidInput_ParsesFilesystem()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        result!.Filesystem.Should().Be("/dev/sdc");
    }

    [Fact]
    public void Parse_WithValidInput_ParsesTotalBytes()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        result!.TotalBytes.Should().Be(269490393088);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesUsedBytes()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        result!.UsedBytes.Should().Be(8547123456);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesAvailableBytes()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        result!.AvailableBytes.Should().Be(247181266944);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesMountPoint()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        result!.MountPoint.Should().Be("/");
    }

    [Fact]
    public void Parse_WithNullInput_ReturnsNull()
    {
        var result = LinuxDiskUsageParser.Parse(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithEmptyInput_ReturnsNull()
    {
        var result = LinuxDiskUsageParser.Parse("");

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithWhitespaceInput_ReturnsNull()
    {
        var result = LinuxDiskUsageParser.Parse("   \n   ");

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithHeaderOnly_ReturnsNull()
    {
        var input = "Filesystem     1B-blocks        Used   Available Use% Mounted on";

        var result = LinuxDiskUsageParser.Parse(input);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMalformedDataLine_ReturnsNull()
    {
        var input = """
            Filesystem     1B-blocks        Used   Available Use% Mounted on
            this is not a valid line
            """;

        var result = LinuxDiskUsageParser.Parse(input);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNonNumericValues_ReturnsNull()
    {
        var input = """
            Filesystem     1B-blocks        Used   Available Use% Mounted on
            /dev/sdc       abc 8547123456 247181266944   4% /
            """;

        var result = LinuxDiskUsageParser.Parse(input);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithExtraWhitespace_Works()
    {
        var input = """
            Filesystem     1B-blocks        Used   Available Use% Mounted on
            /dev/sdc        269490393088   8547123456   247181266944    4%   /
            """;

        var result = LinuxDiskUsageParser.Parse(input);

        result.Should().NotBeNull();
        result!.TotalBytes.Should().Be(269490393088);
    }

    [Fact]
    public void Parse_ReturnsFirstDataLine()
    {
        var result = LinuxDiskUsageParser.Parse(MultiFilesystemOutput);

        result.Should().NotBeNull();
        result!.MountPoint.Should().Be("/mnt/wsl");
    }

    #endregion

    #region ParseRootFilesystem Tests

    [Fact]
    public void ParseRootFilesystem_WithMultipleFilesystems_ReturnsRoot()
    {
        var result = LinuxDiskUsageParser.ParseRootFilesystem(MultiFilesystemOutput);

        result.Should().NotBeNull();
        result!.MountPoint.Should().Be("/");
        result.Filesystem.Should().Be("/dev/sdc");
    }

    [Fact]
    public void ParseRootFilesystem_WithNoRootMount_ReturnsNull()
    {
        var input = """
            Filesystem     1B-blocks        Used   Available Use% Mounted on
            none           269490393088          0 269490393088   0% /mnt/wsl
            none           269490393088          0 269490393088   0% /usr/lib/wsl/drivers
            """;

        var result = LinuxDiskUsageParser.ParseRootFilesystem(input);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseRootFilesystem_WithSingleLine_ReturnsRoot()
    {
        var result = LinuxDiskUsageParser.ParseRootFilesystem(SampleDfOutput);

        result.Should().NotBeNull();
        result!.MountPoint.Should().Be("/");
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void TotalGb_CalculatesCorrectly()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        var expectedGb = 269490393088 / 1024.0 / 1024.0 / 1024.0;
        result!.TotalGb.Should().BeApproximately(expectedGb, 0.001);
    }

    [Fact]
    public void UsedGb_CalculatesCorrectly()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        var expectedGb = 8547123456 / 1024.0 / 1024.0 / 1024.0;
        result!.UsedGb.Should().BeApproximately(expectedGb, 0.001);
    }

    [Fact]
    public void AvailableGb_CalculatesCorrectly()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        var expectedGb = 247181266944 / 1024.0 / 1024.0 / 1024.0;
        result!.AvailableGb.Should().BeApproximately(expectedGb, 0.001);
    }

    [Fact]
    public void UsagePercent_CalculatesCorrectly()
    {
        var result = LinuxDiskUsageParser.Parse(SampleDfOutput);

        var expectedPercent = (double)8547123456 / 269490393088 * 100.0;
        result!.UsagePercent.Should().BeApproximately(expectedPercent, 0.001);
    }

    [Fact]
    public void UsagePercent_WithZeroTotal_ReturnsZero()
    {
        var diskUsage = new LinuxDiskUsage
        {
            Filesystem = "/dev/sdc",
            TotalBytes = 0,
            UsedBytes = 0,
            AvailableBytes = 0,
            MountPoint = "/"
        };

        diskUsage.UsagePercent.Should().Be(0);
    }

    #endregion
}
