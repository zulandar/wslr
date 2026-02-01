using Wslr.Core.Interfaces;

namespace Wslr.Core.Tests.Interfaces;

public class TemplatePreviewResultTests
{
    #region HasChanges Computed Property Tests

    [Fact]
    public void HasChanges_WithNoChanges_ReturnsFalse()
    {
        var result = new TemplatePreviewResult();

        result.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void HasChanges_WithGlobalChangesOnly_ReturnsTrue()
    {
        var result = new TemplatePreviewResult
        {
            GlobalChanges =
            [
                new SettingChange
                {
                    Section = "wsl2",
                    Setting = "memory",
                    CurrentValue = "4GB",
                    NewValue = "8GB",
                    ChangeType = SettingChangeType.Modify
                }
            ]
        };

        result.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void HasChanges_WithDistroChangesOnly_ReturnsTrue()
    {
        var result = new TemplatePreviewResult
        {
            DistroChanges =
            [
                new SettingChange
                {
                    Section = "automount",
                    Setting = "enabled",
                    NewValue = "true",
                    ChangeType = SettingChangeType.Add
                }
            ]
        };

        result.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void HasChanges_WithBothTypes_ReturnsTrue()
    {
        var result = new TemplatePreviewResult
        {
            GlobalChanges =
            [
                new SettingChange { Section = "wsl2", Setting = "memory", ChangeType = SettingChangeType.Modify }
            ],
            DistroChanges =
            [
                new SettingChange { Section = "boot", Setting = "systemd", ChangeType = SettingChangeType.Add }
            ]
        };

        result.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void HasChanges_WithEmptyLists_ReturnsFalse()
    {
        var result = new TemplatePreviewResult
        {
            GlobalChanges = [],
            DistroChanges = []
        };

        result.HasChanges.Should().BeFalse();
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void DefaultConstructor_GlobalChangesIsEmptyList()
    {
        var result = new TemplatePreviewResult();

        result.GlobalChanges.Should().BeEmpty();
    }

    [Fact]
    public void DefaultConstructor_DistroChangesIsEmptyList()
    {
        var result = new TemplatePreviewResult();

        result.DistroChanges.Should().BeEmpty();
    }

    #endregion

    #region Multiple Changes Tests

    [Fact]
    public void GlobalChanges_CanContainMultipleChanges()
    {
        var result = new TemplatePreviewResult
        {
            GlobalChanges =
            [
                new SettingChange { Section = "wsl2", Setting = "memory", ChangeType = SettingChangeType.Modify },
                new SettingChange { Section = "wsl2", Setting = "processors", ChangeType = SettingChangeType.Add },
                new SettingChange { Section = "experimental", Setting = "sparseVhd", ChangeType = SettingChangeType.Add }
            ]
        };

        result.GlobalChanges.Should().HaveCount(3);
        result.GlobalChanges.Should().Contain(c => c.Setting == "memory");
        result.GlobalChanges.Should().Contain(c => c.Setting == "processors");
        result.GlobalChanges.Should().Contain(c => c.Setting == "sparseVhd");
    }

    [Fact]
    public void DistroChanges_CanContainMultipleChanges()
    {
        var result = new TemplatePreviewResult
        {
            DistroChanges =
            [
                new SettingChange { Section = "automount", Setting = "enabled", ChangeType = SettingChangeType.Modify },
                new SettingChange { Section = "network", Setting = "hostname", ChangeType = SettingChangeType.Add },
                new SettingChange { Section = "interop", Setting = "enabled", ChangeType = SettingChangeType.Remove }
            ]
        };

        result.DistroChanges.Should().HaveCount(3);
    }

    #endregion
}

public class SettingChangeTests
{
    #region Default Value Tests

    [Fact]
    public void DefaultConstructor_SectionIsEmptyString()
    {
        var change = new SettingChange();

        change.Section.Should().BeEmpty();
    }

    [Fact]
    public void DefaultConstructor_SettingIsEmptyString()
    {
        var change = new SettingChange();

        change.Setting.Should().BeEmpty();
    }

    [Fact]
    public void DefaultConstructor_CurrentValueIsNull()
    {
        var change = new SettingChange();

        change.CurrentValue.Should().BeNull();
    }

    [Fact]
    public void DefaultConstructor_NewValueIsNull()
    {
        var change = new SettingChange();

        change.NewValue.Should().BeNull();
    }

    [Fact]
    public void DefaultConstructor_ChangeTypeIsAdd()
    {
        var change = new SettingChange();

        // Default enum value is 0 which is Add
        change.ChangeType.Should().Be(SettingChangeType.Add);
    }

    #endregion

    #region Change Type Scenarios

    [Fact]
    public void Add_RepresentsNewSetting()
    {
        var change = new SettingChange
        {
            Section = "wsl2",
            Setting = "memory",
            CurrentValue = null,
            NewValue = "8GB",
            ChangeType = SettingChangeType.Add
        };

        change.ChangeType.Should().Be(SettingChangeType.Add);
        change.CurrentValue.Should().BeNull();
        change.NewValue.Should().NotBeNull();
    }

    [Fact]
    public void Modify_RepresentsValueChange()
    {
        var change = new SettingChange
        {
            Section = "wsl2",
            Setting = "memory",
            CurrentValue = "4GB",
            NewValue = "8GB",
            ChangeType = SettingChangeType.Modify
        };

        change.ChangeType.Should().Be(SettingChangeType.Modify);
        change.CurrentValue.Should().NotBe(change.NewValue);
    }

    [Fact]
    public void Remove_RepresentsSettingDeletion()
    {
        var change = new SettingChange
        {
            Section = "wsl2",
            Setting = "memory",
            CurrentValue = "8GB",
            NewValue = null,
            ChangeType = SettingChangeType.Remove
        };

        change.ChangeType.Should().Be(SettingChangeType.Remove);
        change.CurrentValue.Should().NotBeNull();
        change.NewValue.Should().BeNull();
    }

    [Fact]
    public void Unchanged_RepresentsNoChange()
    {
        var change = new SettingChange
        {
            Section = "wsl2",
            Setting = "memory",
            CurrentValue = "8GB",
            NewValue = "8GB",
            ChangeType = SettingChangeType.Unchanged
        };

        change.ChangeType.Should().Be(SettingChangeType.Unchanged);
        change.CurrentValue.Should().Be(change.NewValue);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var change1 = new SettingChange
        {
            Section = "wsl2",
            Setting = "memory",
            CurrentValue = "4GB",
            NewValue = "8GB",
            ChangeType = SettingChangeType.Modify
        };
        var change2 = new SettingChange
        {
            Section = "wsl2",
            Setting = "memory",
            CurrentValue = "4GB",
            NewValue = "8GB",
            ChangeType = SettingChangeType.Modify
        };

        change1.Should().Be(change2);
    }

    [Fact]
    public void Equals_WithDifferentChangeType_ReturnsFalse()
    {
        var change1 = new SettingChange
        {
            Section = "wsl2",
            Setting = "memory",
            ChangeType = SettingChangeType.Add
        };
        var change2 = new SettingChange
        {
            Section = "wsl2",
            Setting = "memory",
            ChangeType = SettingChangeType.Modify
        };

        change1.Should().NotBe(change2);
    }

    #endregion
}

public class SettingChangeTypeTests
{
    [Fact]
    public void Add_HasValueZero()
    {
        ((int)SettingChangeType.Add).Should().Be(0);
    }

    [Theory]
    [InlineData(SettingChangeType.Add, "Add")]
    [InlineData(SettingChangeType.Modify, "Modify")]
    [InlineData(SettingChangeType.Remove, "Remove")]
    [InlineData(SettingChangeType.Unchanged, "Unchanged")]
    public void ToString_ReturnsExpectedName(SettingChangeType type, string expected)
    {
        type.ToString().Should().Be(expected);
    }

    [Fact]
    public void AllChangeTypes_HaveUniqueValues()
    {
        var values = Enum.GetValues<SettingChangeType>().Cast<int>().ToList();

        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllChangeTypes_AreDefined()
    {
        var types = Enum.GetValues<SettingChangeType>();

        types.Should().HaveCount(4);
        types.Should().Contain(SettingChangeType.Add);
        types.Should().Contain(SettingChangeType.Modify);
        types.Should().Contain(SettingChangeType.Remove);
        types.Should().Contain(SettingChangeType.Unchanged);
    }
}
