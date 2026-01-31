using Wslr.Core.Parsing;

namespace Wslr.Infrastructure.Tests.Parsing;

public class LinuxCpuStatParserTests
{
    private const string SampleProcStat = """
        cpu  10132153 290696 3084719 46828483 16683 0 25195 0 0 0
        cpu0 1393280 32966 572056 13343292 6130 0 17875 0 0 0
        cpu1 1353964 32075 579333 13308656 3499 0 3781 0 0 0
        intr 1234567890 0 0 0 0 0 0 0 0 0
        ctxt 9876543210
        btime 1704067200
        processes 123456
        procs_running 1
        procs_blocked 0
        """;

    #region Parse Tests

    [Fact]
    public void Parse_WithValidInput_ReturnsLinuxCpuStat()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Parse_WithValidInput_ParsesUser()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result!.User.Should().Be(10132153);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesNice()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result!.Nice.Should().Be(290696);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesSystem()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result!.System.Should().Be(3084719);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesIdle()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result!.Idle.Should().Be(46828483);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesIoWait()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result!.IoWait.Should().Be(16683);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesIrq()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result!.Irq.Should().Be(0);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesSoftIrq()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result!.SoftIrq.Should().Be(25195);
    }

    [Fact]
    public void Parse_WithValidInput_ParsesSteal()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        result!.Steal.Should().Be(0);
    }

    [Fact]
    public void Parse_WithNullInput_ReturnsNull()
    {
        var result = LinuxCpuStatParser.Parse(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithEmptyInput_ReturnsNull()
    {
        var result = LinuxCpuStatParser.Parse("");

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithWhitespaceInput_ReturnsNull()
    {
        var result = LinuxCpuStatParser.Parse("   \n   ");

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNoCpuLine_ReturnsNull()
    {
        var input = """
            intr 1234567890 0 0 0 0 0 0 0 0 0
            ctxt 9876543210
            btime 1704067200
            """;

        var result = LinuxCpuStatParser.Parse(input);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMinimalFields_Works()
    {
        // Older kernels may only have user, nice, system, idle
        var input = "cpu  10132153 290696 3084719 46828483";

        var result = LinuxCpuStatParser.Parse(input);

        result.Should().NotBeNull();
        result!.User.Should().Be(10132153);
        result.Nice.Should().Be(290696);
        result.System.Should().Be(3084719);
        result.Idle.Should().Be(46828483);
        result.IoWait.Should().Be(0);
        result.Irq.Should().Be(0);
        result.SoftIrq.Should().Be(0);
        result.Steal.Should().Be(0);
    }

    [Fact]
    public void Parse_WithTooFewFields_ReturnsNull()
    {
        var input = "cpu  10132153 290696 3084719";

        var result = LinuxCpuStatParser.Parse(input);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMalformedNumbers_ReturnsNull()
    {
        var input = "cpu  abc 290696 3084719 46828483";

        var result = LinuxCpuStatParser.Parse(input);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_WithExtraSpaces_Works()
    {
        var input = "cpu   10132153  290696   3084719  46828483  16683   0  25195  0";

        var result = LinuxCpuStatParser.Parse(input);

        result.Should().NotBeNull();
        result!.User.Should().Be(10132153);
    }

    [Fact]
    public void Parse_WithLeadingWhitespace_Works()
    {
        var input = "   cpu  10132153 290696 3084719 46828483 16683 0 25195 0";

        var result = LinuxCpuStatParser.Parse(input);

        result.Should().NotBeNull();
        result!.User.Should().Be(10132153);
    }

    [Fact]
    public void Parse_SelectsAggregateCpuLine()
    {
        // Should select "cpu " not "cpu0 "
        var input = """
            cpu0 1393280 32966 572056 13343292 6130 0 17875 0
            cpu  10132153 290696 3084719 46828483 16683 0 25195 0
            cpu1 1353964 32075 579333 13308656 3499 0 3781 0
            """;

        var result = LinuxCpuStatParser.Parse(input);

        result.Should().NotBeNull();
        result!.User.Should().Be(10132153);
    }

    [Fact]
    public void Parse_IsCaseInsensitiveForCpuPrefix()
    {
        var input = "CPU  10132153 290696 3084719 46828483 16683 0 25195 0";

        var result = LinuxCpuStatParser.Parse(input);

        result.Should().NotBeNull();
        result!.User.Should().Be(10132153);
    }

    #endregion

    #region Computed Property Tests

    [Fact]
    public void TotalTime_CalculatesCorrectly()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        // TotalTime = user + nice + system + idle + iowait + irq + softirq + steal
        var expected = 10132153L + 290696 + 3084719 + 46828483 + 16683 + 0 + 25195 + 0;
        result!.TotalTime.Should().Be(expected);
    }

    [Fact]
    public void IdleTime_CalculatesCorrectly()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        // IdleTime = idle + iowait
        var expected = 46828483L + 16683;
        result!.IdleTime.Should().Be(expected);
    }

    [Fact]
    public void ActiveTime_CalculatesCorrectly()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        // ActiveTime = TotalTime - IdleTime
        var totalTime = 10132153L + 290696 + 3084719 + 46828483 + 16683 + 0 + 25195 + 0;
        var idleTime = 46828483L + 16683;
        result!.ActiveTime.Should().Be(totalTime - idleTime);
    }

    [Fact]
    public void ActiveTime_EqualsUserPlusNicePlusSystemPlusIrqPlusSoftIrqPlusSteal()
    {
        var result = LinuxCpuStatParser.Parse(SampleProcStat);

        // ActiveTime should equal user + nice + system + irq + softirq + steal
        var expected = 10132153L + 290696 + 3084719 + 0 + 25195 + 0;
        result!.ActiveTime.Should().Be(expected);
    }

    #endregion
}
