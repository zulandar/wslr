using Wslr.Core.Models;
using Wslr.Infrastructure.Parsing;

namespace Wslr.Infrastructure.Tests.Parsing;

public class WslOutputParserTests
{
    #region ParseListVerbose Tests

    [Fact]
    public void ParseListVerbose_WithEmptyString_ReturnsEmptyList()
    {
        var result = WslOutputParser.ParseListVerbose("");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseListVerbose_WithNullString_ReturnsEmptyList()
    {
        var result = WslOutputParser.ParseListVerbose(null!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseListVerbose_WithWhitespaceOnly_ReturnsEmptyList()
    {
        var result = WslOutputParser.ParseListVerbose("   \n   \n   ");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseListVerbose_WithHeaderOnly_ReturnsEmptyList()
    {
        var output = "  NAME      STATE           VERSION\n";

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseListVerbose_WithSingleRunningDistribution_ParsesCorrectly()
    {
        var output = """
              NAME      STATE           VERSION
            * Ubuntu    Running         2
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ubuntu");
        result[0].State.Should().Be(DistributionState.Running);
        result[0].Version.Should().Be(2);
        result[0].IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ParseListVerbose_WithSingleStoppedDistribution_ParsesCorrectly()
    {
        var output = """
              NAME      STATE           VERSION
              Debian    Stopped         2
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Debian");
        result[0].State.Should().Be(DistributionState.Stopped);
        result[0].Version.Should().Be(2);
        result[0].IsDefault.Should().BeFalse();
    }

    [Fact]
    public void ParseListVerbose_WithMultipleDistributions_ParsesAllCorrectly()
    {
        var output = """
              NAME            STATE           VERSION
            * Ubuntu          Running         2
              Debian          Stopped         2
              Alpine          Stopped         1
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(3);

        result[0].Name.Should().Be("Ubuntu");
        result[0].State.Should().Be(DistributionState.Running);
        result[0].Version.Should().Be(2);
        result[0].IsDefault.Should().BeTrue();

        result[1].Name.Should().Be("Debian");
        result[1].State.Should().Be(DistributionState.Stopped);
        result[1].Version.Should().Be(2);
        result[1].IsDefault.Should().BeFalse();

        result[2].Name.Should().Be("Alpine");
        result[2].State.Should().Be(DistributionState.Stopped);
        result[2].Version.Should().Be(1);
        result[2].IsDefault.Should().BeFalse();
    }

    [Fact]
    public void ParseListVerbose_WithInstallingState_ParsesCorrectly()
    {
        var output = """
              NAME      STATE           VERSION
              Ubuntu    Installing      2
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(1);
        result[0].State.Should().Be(DistributionState.Installing);
    }

    [Fact]
    public void ParseListVerbose_WithUnknownState_ReturnsUnknown()
    {
        var output = """
              NAME      STATE           VERSION
              Ubuntu    Converting      2
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(1);
        result[0].State.Should().Be(DistributionState.Unknown);
    }

    [Fact]
    public void ParseListVerbose_WithDistributionNameContainingHyphen_ParsesCorrectly()
    {
        var output = """
              NAME              STATE           VERSION
            * Ubuntu-22.04     Running         2
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ubuntu-22.04");
    }

    [Theory]
    [InlineData("running")]
    [InlineData("RUNNING")]
    [InlineData("Running")]
    public void ParseListVerbose_WithDifferentStateCasing_ParsesCorrectly(string state)
    {
        var output = $"""
              NAME      STATE           VERSION
              Ubuntu    {state}         2
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(1);
        result[0].State.Should().Be(DistributionState.Running);
    }

    [Fact]
    public void ParseListVerbose_WithInvalidVersionNumber_DefaultsToVersion2()
    {
        var output = """
              NAME      STATE           VERSION
              Ubuntu    Running         invalid
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(1);
        result[0].Version.Should().Be(2);
    }

    [Fact]
    public void ParseListVerbose_WithExtraWhitespace_ParsesCorrectly()
    {
        var output = """
              NAME            STATE              VERSION
            *    Ubuntu          Running              2
            """;

        var result = WslOutputParser.ParseListVerbose(output);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ubuntu");
        result[0].IsDefault.Should().BeTrue();
    }

    #endregion

    #region ParseListOnline Tests

    [Fact]
    public void ParseListOnline_WithEmptyString_ReturnsEmptyList()
    {
        var result = WslOutputParser.ParseListOnline("");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseListOnline_WithNullString_ReturnsEmptyList()
    {
        var result = WslOutputParser.ParseListOnline(null!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseListOnline_WithHeaderOnly_ReturnsEmptyList()
    {
        var output = """
            The following is a list of valid distributions that can be installed.
            Install using 'wsl --install -d <Distro>'.

            NAME                            FRIENDLY NAME
            """;

        var result = WslOutputParser.ParseListOnline(output);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseListOnline_WithSingleDistribution_ParsesCorrectly()
    {
        var output = """
            The following is a list of valid distributions that can be installed.
            Install using 'wsl --install -d <Distro>'.

            NAME                            FRIENDLY NAME
            Ubuntu                          Ubuntu
            """;

        var result = WslOutputParser.ParseListOnline(output);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ubuntu");
        result[0].FriendlyName.Should().Be("Ubuntu");
    }

    [Fact]
    public void ParseListOnline_WithMultipleDistributions_ParsesAllCorrectly()
    {
        var output = """
            The following is a list of valid distributions that can be installed.
            Install using 'wsl --install -d <Distro>'.

            NAME                            FRIENDLY NAME
            Ubuntu                          Ubuntu
            Debian                          Debian GNU/Linux
            kali-linux                      Kali Linux Rolling
            Ubuntu-22.04                    Ubuntu 22.04 LTS
            """;

        var result = WslOutputParser.ParseListOnline(output);

        result.Should().HaveCount(4);

        result[0].Name.Should().Be("Ubuntu");
        result[0].FriendlyName.Should().Be("Ubuntu");

        result[1].Name.Should().Be("Debian");
        result[1].FriendlyName.Should().Be("Debian GNU/Linux");

        result[2].Name.Should().Be("kali-linux");
        result[2].FriendlyName.Should().Be("Kali Linux Rolling");

        result[3].Name.Should().Be("Ubuntu-22.04");
        result[3].FriendlyName.Should().Be("Ubuntu 22.04 LTS");
    }

    [Fact]
    public void ParseListOnline_WithSeparatorLine_SkipsSeparator()
    {
        var output = """
            The following is a list of valid distributions that can be installed.

            NAME                            FRIENDLY NAME
            ----------------------------------------
            Ubuntu                          Ubuntu
            """;

        var result = WslOutputParser.ParseListOnline(output);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ubuntu");
    }

    [Fact]
    public void ParseListOnline_WithLongFriendlyName_CapturesFullName()
    {
        var output = """
            NAME                            FRIENDLY NAME
            OracleLinux_9_1                 Oracle Linux 9.1
            openSUSE-Tumbleweed             openSUSE Tumbleweed
            """;

        var result = WslOutputParser.ParseListOnline(output);

        result.Should().HaveCount(2);
        result[0].FriendlyName.Should().Be("Oracle Linux 9.1");
        result[1].FriendlyName.Should().Be("openSUSE Tumbleweed");
    }

    [Fact]
    public void ParseListOnline_WithNoHeaderFound_ReturnsEmptyList()
    {
        var output = """
            Some random text without the expected header format
            Ubuntu    Ubuntu
            """;

        var result = WslOutputParser.ParseListOnline(output);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseListOnline_WithEmptyLinesAfterHeader_SkipsEmptyLines()
    {
        var output = """
            NAME                            FRIENDLY NAME


            Ubuntu                          Ubuntu
            """;

        var result = WslOutputParser.ParseListOnline(output);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ubuntu");
    }

    #endregion
}
