using Wslr.Infrastructure.Parsing;

namespace Wslr.Infrastructure.Tests.Parsing;

public class WslConfigParserTests
{
    private const string SampleWslConfig = """
        # This is a sample .wslconfig file
        # Comments should be preserved

        [wsl2]
        memory=8GB
        processors=4
        localhostForwarding=true
        swap=4GB
        nestedVirtualization=false

        [experimental]
        autoMemoryReclaim=gradual
        sparseVhd=true
        """;

    #region Parse Tests

    [Fact]
    public void Parse_WithEmptyString_ReturnsDefaultConfig()
    {
        var result = WslConfigParser.Parse("");

        result.Should().NotBeNull();
        result.Wsl2.Memory.Should().BeNull();
        result.Experimental.AutoMemoryReclaim.Should().BeNull();
    }

    [Fact]
    public void Parse_WithValidConfig_ParsesWsl2Section()
    {
        var result = WslConfigParser.Parse(SampleWslConfig);

        result.Wsl2.Memory.Should().Be("8GB");
        result.Wsl2.Processors.Should().Be(4);
        result.Wsl2.LocalhostForwarding.Should().BeTrue();
        result.Wsl2.Swap.Should().Be("4GB");
        result.Wsl2.NestedVirtualization.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithValidConfig_ParsesExperimentalSection()
    {
        var result = WslConfigParser.Parse(SampleWslConfig);

        result.Experimental.AutoMemoryReclaim.Should().Be("gradual");
        result.Experimental.SparseVhd.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithUnknownSettings_CapturesAdditionalSettings()
    {
        var config = """
            [wsl2]
            memory=8GB
            unknownSetting=value

            [experimental]
            futureFeature=enabled
            """;

        var result = WslConfigParser.Parse(config);

        result.Wsl2.AdditionalSettings.Should().ContainKey("unknownSetting");
        result.Wsl2.AdditionalSettings["unknownSetting"].Should().Be("value");
        result.Experimental.AdditionalSettings.Should().ContainKey("futureFeature");
        result.Experimental.AdditionalSettings["futureFeature"].Should().Be("enabled");
    }

    [Fact]
    public void Parse_WithUnknownSections_CapturesAdditionalSections()
    {
        var config = """
            [wsl2]
            memory=8GB

            [custom]
            setting1=value1
            setting2=value2
            """;

        var result = WslConfigParser.Parse(config);

        result.AdditionalSections.Should().ContainKey("custom");
        result.AdditionalSections["custom"].Should().ContainKey("setting1");
        result.AdditionalSections["custom"]["setting1"].Should().Be("value1");
    }

    [Fact]
    public void Parse_WithBooleanValues_ParsesCorrectly()
    {
        var config = """
            [wsl2]
            localhostForwarding=true
            nestedVirtualization=false
            guiApplications=TRUE
            debugConsole=False
            """;

        var result = WslConfigParser.Parse(config);

        result.Wsl2.LocalhostForwarding.Should().BeTrue();
        result.Wsl2.NestedVirtualization.Should().BeFalse();
        result.Wsl2.GuiApplications.Should().BeTrue();
        result.Wsl2.DebugConsole.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithNumericValues_ParsesCorrectly()
    {
        var config = """
            [wsl2]
            processors=8
            vmIdleTimeout=60000
            """;

        var result = WslConfigParser.Parse(config);

        result.Wsl2.Processors.Should().Be(8);
        result.Wsl2.VmIdleTimeout.Should().Be(60000);
    }

    [Fact]
    public void Parse_WithInvalidNumericValue_ReturnsNull()
    {
        var config = """
            [wsl2]
            processors=invalid
            """;

        var result = WslConfigParser.Parse(config);

        result.Wsl2.Processors.Should().BeNull();
    }

    [Fact]
    public void Parse_WithCaseInsensitiveKeys_ParsesCorrectly()
    {
        var config = """
            [wsl2]
            MEMORY=8GB
            Processors=4
            localhostforwarding=true
            """;

        var result = WslConfigParser.Parse(config);

        result.Wsl2.Memory.Should().Be("8GB");
        result.Wsl2.Processors.Should().Be(4);
        result.Wsl2.LocalhostForwarding.Should().BeTrue();
    }

    #endregion

    #region Serialize Tests

    [Fact]
    public void Serialize_WithDefaultConfig_ReturnsEmptyString()
    {
        var config = new Wslr.Core.Models.WslConfig();

        var result = WslConfigParser.Serialize(config);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_WithWsl2Settings_ProducesValidIni()
    {
        var config = new Wslr.Core.Models.WslConfig
        {
            Wsl2 = new Wslr.Core.Models.Wsl2Settings
            {
                Memory = "8GB",
                Processors = 4,
                LocalhostForwarding = true
            }
        };

        var result = WslConfigParser.Serialize(config);

        result.Should().Contain("[wsl2]");
        result.Should().Contain("memory=8GB");
        result.Should().Contain("processors=4");
        result.Should().Contain("localhostForwarding=true");
    }

    [Fact]
    public void Serialize_WithExperimentalSettings_ProducesValidIni()
    {
        var config = new Wslr.Core.Models.WslConfig
        {
            Experimental = new Wslr.Core.Models.ExperimentalSettings
            {
                AutoMemoryReclaim = "gradual",
                SparseVhd = true
            }
        };

        var result = WslConfigParser.Serialize(config);

        result.Should().Contain("[experimental]");
        result.Should().Contain("autoMemoryReclaim=gradual");
        result.Should().Contain("sparseVhd=true");
    }

    [Fact]
    public void Serialize_WithBooleanFalse_SerializesAsFalse()
    {
        var config = new Wslr.Core.Models.WslConfig
        {
            Wsl2 = new Wslr.Core.Models.Wsl2Settings
            {
                NestedVirtualization = false
            }
        };

        var result = WslConfigParser.Serialize(config);

        result.Should().Contain("nestedVirtualization=false");
    }

    #endregion

    #region MergeIntoDocument Tests (Comment Preservation)

    [Fact]
    public void MergeIntoDocument_PreservesComments()
    {
        var originalContent = """
            # This is a comment
            [wsl2]
            memory=4GB  # inline comment

            # Another comment
            processors=2
            """;

        var document = IniDocument.Parse(originalContent);
        var config = new Wslr.Core.Models.WslConfig
        {
            Wsl2 = new Wslr.Core.Models.Wsl2Settings
            {
                Memory = "8GB",
                Processors = 4
            }
        };

        WslConfigParser.MergeIntoDocument(document, config);
        var result = document.ToString();

        result.Should().Contain("# This is a comment");
        result.Should().Contain("# Another comment");
        result.Should().Contain("memory=8GB");
        result.Should().Contain("processors=4");
    }

    [Fact]
    public void MergeIntoDocument_PreservesEmptyLines()
    {
        var originalContent = """
            [wsl2]
            memory=4GB

            processors=2
            """;

        var document = IniDocument.Parse(originalContent);
        var config = new Wslr.Core.Models.WslConfig
        {
            Wsl2 = new Wslr.Core.Models.Wsl2Settings
            {
                Memory = "8GB"
            }
        };

        WslConfigParser.MergeIntoDocument(document, config);
        var result = document.ToString();

        // Should preserve the empty line between settings
        var lines = result.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        lines.Should().Contain("");
    }

    [Fact]
    public void MergeIntoDocument_UpdatesExistingValues()
    {
        var originalContent = """
            [wsl2]
            memory=4GB
            processors=2
            """;

        var document = IniDocument.Parse(originalContent);
        var config = new Wslr.Core.Models.WslConfig
        {
            Wsl2 = new Wslr.Core.Models.Wsl2Settings
            {
                Memory = "16GB"
            }
        };

        WslConfigParser.MergeIntoDocument(document, config);
        var result = document.ToString();

        result.Should().Contain("memory=16GB");
        result.Should().NotContain("memory=4GB");
    }

    [Fact]
    public void MergeIntoDocument_AddsNewValues()
    {
        var originalContent = """
            [wsl2]
            memory=4GB
            """;

        var document = IniDocument.Parse(originalContent);
        var config = new Wslr.Core.Models.WslConfig
        {
            Wsl2 = new Wslr.Core.Models.Wsl2Settings
            {
                Memory = "4GB",
                Processors = 8
            }
        };

        WslConfigParser.MergeIntoDocument(document, config);
        var result = document.ToString();

        result.Should().Contain("processors=8");
    }

    [Fact]
    public void MergeIntoDocument_CreatesNewSections()
    {
        var originalContent = """
            [wsl2]
            memory=4GB
            """;

        var document = IniDocument.Parse(originalContent);
        var config = new Wslr.Core.Models.WslConfig
        {
            Experimental = new Wslr.Core.Models.ExperimentalSettings
            {
                AutoMemoryReclaim = "gradual"
            }
        };

        WslConfigParser.MergeIntoDocument(document, config);
        var result = document.ToString();

        result.Should().Contain("[experimental]");
        result.Should().Contain("autoMemoryReclaim=gradual");
    }

    #endregion

    #region IniDocument Tests

    [Fact]
    public void IniDocument_Parse_HandlesInlineComments()
    {
        var content = """
            [wsl2]
            memory=8GB  ; this is a comment
            """;

        var document = IniDocument.Parse(content);
        var value = document.GetValue("wsl2", "memory");

        value.Should().Be("8GB");
    }

    [Fact]
    public void IniDocument_Parse_HandlesSemicolonComments()
    {
        var content = """
            ; Header comment
            [wsl2]
            memory=8GB
            """;

        var document = IniDocument.Parse(content);

        document.Lines[0].Type.Should().Be(IniLineType.Comment);
        document.GetValue("wsl2", "memory").Should().Be("8GB");
    }

    [Fact]
    public void IniDocument_Parse_HandlesHashComments()
    {
        var content = """
            # Header comment
            [wsl2]
            memory=8GB
            """;

        var document = IniDocument.Parse(content);

        document.Lines[0].Type.Should().Be(IniLineType.Comment);
        document.GetValue("wsl2", "memory").Should().Be("8GB");
    }

    [Fact]
    public void IniDocument_SetValue_UpdatesExistingValue()
    {
        var content = """
            [wsl2]
            memory=4GB
            """;

        var document = IniDocument.Parse(content);
        document.SetValue("wsl2", "memory", "8GB");

        document.GetValue("wsl2", "memory").Should().Be("8GB");
    }

    [Fact]
    public void IniDocument_SetValue_AddsNewKey()
    {
        var content = """
            [wsl2]
            memory=4GB
            """;

        var document = IniDocument.Parse(content);
        document.SetValue("wsl2", "processors", "4");

        document.GetValue("wsl2", "processors").Should().Be("4");
    }

    [Fact]
    public void IniDocument_SetValue_CreatesNewSection()
    {
        var content = """
            [wsl2]
            memory=4GB
            """;

        var document = IniDocument.Parse(content);
        document.SetValue("experimental", "sparseVhd", "true");

        document.GetValue("experimental", "sparseVhd").Should().Be("true");
    }

    [Fact]
    public void IniDocument_RemoveKey_RemovesExistingKey()
    {
        var content = """
            [wsl2]
            memory=4GB
            processors=4
            """;

        var document = IniDocument.Parse(content);
        var removed = document.RemoveKey("wsl2", "memory");

        removed.Should().BeTrue();
        document.GetValue("wsl2", "memory").Should().BeNull();
        document.GetValue("wsl2", "processors").Should().Be("4");
    }

    [Fact]
    public void IniDocument_RemoveKey_ReturnsFalseForNonexistentKey()
    {
        var content = """
            [wsl2]
            memory=4GB
            """;

        var document = IniDocument.Parse(content);
        var removed = document.RemoveKey("wsl2", "nonexistent");

        removed.Should().BeFalse();
    }

    [Fact]
    public void IniDocument_GetSection_ReturnsCaseInsensitiveKeys()
    {
        var content = """
            [wsl2]
            Memory=4GB
            PROCESSORS=4
            """;

        var document = IniDocument.Parse(content);
        var section = document.GetSection("wsl2");

        section.Should().ContainKey("memory");
        section.Should().ContainKey("processors");
    }

    [Fact]
    public void IniDocument_Sections_ReturnsAllSectionNames()
    {
        var content = """
            [wsl2]
            memory=4GB

            [experimental]
            sparseVhd=true

            [custom]
            setting=value
            """;

        var document = IniDocument.Parse(content);

        document.Sections.Should().BeEquivalentTo(new[] { "wsl2", "experimental", "custom" });
    }

    [Fact]
    public void IniDocument_Clone_CreatesIndependentCopy()
    {
        var content = """
            [wsl2]
            memory=4GB
            """;

        var document = IniDocument.Parse(content);
        var clone = document.Clone();

        clone.SetValue("wsl2", "memory", "8GB");

        document.GetValue("wsl2", "memory").Should().Be("4GB");
        clone.GetValue("wsl2", "memory").Should().Be("8GB");
    }

    [Fact]
    public void IniDocument_ToString_ReconstructsDocument()
    {
        var content = "[wsl2]\nmemory=4GB";

        var document = IniDocument.Parse(content);
        var result = document.ToString();

        // Normalize line endings for comparison
        result.Replace("\r\n", "\n").Should().Be(content.Replace("\r\n", "\n"));
    }

    #endregion
}
