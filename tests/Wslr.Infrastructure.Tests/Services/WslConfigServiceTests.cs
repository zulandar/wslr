using Wslr.Core.Models;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class WslConfigServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testConfigPath;

    public WslConfigServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WslConfigServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _testConfigPath = Path.Combine(_testDirectory, ".wslconfig");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithCustomPath_SetsConfigPath()
    {
        var service = new WslConfigService(_testConfigPath);

        service.ConfigPath.Should().Be(_testConfigPath);
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentException()
    {
        var act = () => new WslConfigService(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsArgumentException()
    {
        var act = () => new WslConfigService("");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region ConfigExists Tests

    [Fact]
    public void ConfigExists_WhenFileDoesNotExist_ReturnsFalse()
    {
        var service = new WslConfigService(_testConfigPath);

        service.ConfigExists.Should().BeFalse();
    }

    [Fact]
    public void ConfigExists_WhenFileExists_ReturnsTrue()
    {
        File.WriteAllText(_testConfigPath, "[wsl2]\nmemory=8GB");
        var service = new WslConfigService(_testConfigPath);

        service.ConfigExists.Should().BeTrue();
    }

    #endregion

    #region ReadConfigAsync Tests

    [Fact]
    public async Task ReadConfigAsync_WhenFileDoesNotExist_ReturnsDefaultConfig()
    {
        var service = new WslConfigService(_testConfigPath);

        var result = await service.ReadConfigAsync();

        result.Should().NotBeNull();
        result.Wsl2.Memory.Should().BeNull();
        result.Experimental.AutoMemoryReclaim.Should().BeNull();
    }

    [Fact]
    public async Task ReadConfigAsync_WithValidFile_ParsesCorrectly()
    {
        var content = """
            [wsl2]
            memory=8GB
            processors=4

            [experimental]
            autoMemoryReclaim=gradual
            """;
        File.WriteAllText(_testConfigPath, content);
        var service = new WslConfigService(_testConfigPath);

        var result = await service.ReadConfigAsync();

        result.Wsl2.Memory.Should().Be("8GB");
        result.Wsl2.Processors.Should().Be(4);
        result.Experimental.AutoMemoryReclaim.Should().Be("gradual");
    }

    [Fact]
    public async Task ReadConfigAsync_SupportsCancellation()
    {
        File.WriteAllText(_testConfigPath, "[wsl2]\nmemory=8GB");
        var service = new WslConfigService(_testConfigPath);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => service.ReadConfigAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReadConfigAsync_CachesDocument()
    {
        var content = "[wsl2]\nmemory=8GB";
        File.WriteAllText(_testConfigPath, content);
        var service = new WslConfigService(_testConfigPath);

        // First read
        var result1 = await service.ReadConfigAsync();

        // Wait to ensure file timestamp will be different (Windows file time resolution is ~15ms)
        await Task.Delay(100);

        // Modify file and explicitly set a new timestamp
        File.WriteAllText(_testConfigPath, "[wsl2]\nmemory=16GB");
        File.SetLastWriteTimeUtc(_testConfigPath, DateTime.UtcNow.AddSeconds(1));

        // Second read should detect change
        var result2 = await service.ReadConfigAsync();

        result1.Wsl2.Memory.Should().Be("8GB");
        result2.Wsl2.Memory.Should().Be("16GB");
    }

    #endregion

    #region WriteConfigAsync Tests

    [Fact]
    public async Task WriteConfigAsync_CreatesNewFile()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = "8GB",
                Processors = 4
            }
        };

        await service.WriteConfigAsync(config);

        File.Exists(_testConfigPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(_testConfigPath);
        content.Should().Contain("[wsl2]");
        content.Should().Contain("memory=8GB");
        content.Should().Contain("processors=4");
    }

    [Fact]
    public async Task WriteConfigAsync_PreservesComments()
    {
        var originalContent = """
            # My custom config
            [wsl2]
            memory=4GB
            # Comment about processors
            processors=2
            """;
        File.WriteAllText(_testConfigPath, originalContent);
        var service = new WslConfigService(_testConfigPath);

        // Read to cache the document
        await service.ReadConfigAsync();

        // Write updated config
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = "8GB"
            }
        };
        await service.WriteConfigAsync(config);

        var result = await File.ReadAllTextAsync(_testConfigPath);
        result.Should().Contain("# My custom config");
        result.Should().Contain("# Comment about processors");
        result.Should().Contain("memory=8GB");
    }

    [Fact]
    public async Task WriteConfigAsync_SupportsCancellation()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => service.WriteConfigAsync(config, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WriteConfigAsync_WithNullConfig_ThrowsArgumentNullException()
    {
        var service = new WslConfigService(_testConfigPath);

        var act = () => service.WriteConfigAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WriteConfigAsync_UpdatesCacheAfterWrite()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings { Memory = "8GB" }
        };

        await service.WriteConfigAsync(config);
        var result = await service.ReadConfigAsync();

        result.Wsl2.Memory.Should().Be("8GB");
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithValidConfig_ReturnsSuccess()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = "8GB",
                Processors = 2
            }
        };

        var result = service.Validate(config);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidMemoryFormat_ReturnsError()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = "invalid"
            }
        };

        var result = service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<WslConfigValidationError>(e =>
                e.Section == "wsl2" &&
                e.Key == "memory" &&
                e.Code == WslConfigErrorCode.InvalidMemoryFormat);
    }

    [Fact]
    public void Validate_WithValidMemoryFormats_ReturnsSuccess()
    {
        var service = new WslConfigService(_testConfigPath);

        var validFormats = new[] { "8GB", "8G", "8192MB", "8192M", "4096", "16TB", "1024KB" };

        foreach (var format in validFormats)
        {
            var config = new WslConfig
            {
                Wsl2 = new Wsl2Settings { Memory = format }
            };

            var result = service.Validate(config);
            result.IsValid.Should().BeTrue($"'{format}' should be valid");
        }
    }

    [Fact]
    public void Validate_WithZeroProcessors_ReturnsError()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings { Processors = 0 }
        };

        var result = service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Key.Should().Be("processors");
    }

    [Fact]
    public void Validate_WithNegativeVmIdleTimeout_ReturnsError()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings { VmIdleTimeout = -1 }
        };

        var result = service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Key.Should().Be("vmIdleTimeout");
    }

    [Fact]
    public void Validate_WithInvalidAutoMemoryReclaim_ReturnsError()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Experimental = new ExperimentalSettings { AutoMemoryReclaim = "invalid" }
        };

        var result = service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<WslConfigValidationError>(e =>
                e.Section == "experimental" &&
                e.Key == "autoMemoryReclaim" &&
                e.Code == WslConfigErrorCode.InvalidValue);
    }

    [Fact]
    public void Validate_WithValidAutoMemoryReclaimValues_ReturnsSuccess()
    {
        var service = new WslConfigService(_testConfigPath);
        var validValues = new[] { "disabled", "gradual", "dropcache", "DISABLED", "Gradual" };

        foreach (var value in validValues)
        {
            var config = new WslConfig
            {
                Experimental = new ExperimentalSettings { AutoMemoryReclaim = value }
            };

            var result = service.Validate(config);
            result.IsValid.Should().BeTrue($"'{value}' should be valid");
        }
    }

    [Fact]
    public void Validate_WithInvalidNetworkingMode_ReturnsError()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings { NetworkingMode = "invalid" }
        };

        var result = service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<WslConfigValidationError>(e =>
                e.Section == "wsl2" &&
                e.Key == "networkingMode");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var service = new WslConfigService(_testConfigPath);
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = "invalid",
                Processors = 0,
                VmIdleTimeout = -1
            }
        };

        var result = service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Validate_WithNullConfig_ThrowsArgumentNullException()
    {
        var service = new WslConfigService(_testConfigPath);

        var act = () => service.Validate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CreateBackupAsync Tests

    [Fact]
    public async Task CreateBackupAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var service = new WslConfigService(_testConfigPath);

        var result = await service.CreateBackupAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateBackupAsync_WhenFileExists_CreatesBackup()
    {
        var content = "[wsl2]\nmemory=8GB";
        File.WriteAllText(_testConfigPath, content);
        var service = new WslConfigService(_testConfigPath);

        var backupPath = await service.CreateBackupAsync();

        backupPath.Should().NotBeNull();
        File.Exists(backupPath).Should().BeTrue();
        File.ReadAllText(backupPath!).Should().Be(content);
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesUniqueBackupNames()
    {
        var content = "[wsl2]\nmemory=8GB";
        File.WriteAllText(_testConfigPath, content);
        var service = new WslConfigService(_testConfigPath);

        var backup1 = await service.CreateBackupAsync();
        await Task.Delay(1100); // Wait for timestamp to change
        var backup2 = await service.CreateBackupAsync();

        backup1.Should().NotBe(backup2);
        File.Exists(backup1).Should().BeTrue();
        File.Exists(backup2).Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task RoundTrip_PreservesAllSettings()
    {
        var service = new WslConfigService(_testConfigPath);
        var original = new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = "8GB",
                Processors = 4,
                LocalhostForwarding = true,
                Swap = "4GB",
                NestedVirtualization = false,
                NetworkingMode = "mirrored"
            },
            Experimental = new ExperimentalSettings
            {
                AutoMemoryReclaim = "gradual",
                SparseVhd = true
            }
        };

        await service.WriteConfigAsync(original);
        var loaded = await service.ReadConfigAsync();

        loaded.Wsl2.Memory.Should().Be(original.Wsl2.Memory);
        loaded.Wsl2.Processors.Should().Be(original.Wsl2.Processors);
        loaded.Wsl2.LocalhostForwarding.Should().Be(original.Wsl2.LocalhostForwarding);
        loaded.Wsl2.Swap.Should().Be(original.Wsl2.Swap);
        loaded.Wsl2.NestedVirtualization.Should().Be(original.Wsl2.NestedVirtualization);
        loaded.Wsl2.NetworkingMode.Should().Be(original.Wsl2.NetworkingMode);
        loaded.Experimental.AutoMemoryReclaim.Should().Be(original.Experimental.AutoMemoryReclaim);
        loaded.Experimental.SparseVhd.Should().Be(original.Experimental.SparseVhd);
    }

    [Fact]
    public async Task RoundTrip_WithComments_PreservesComments()
    {
        var originalContent = """
            # WSL Configuration File
            # Last updated: 2024-01-15

            [wsl2]
            # Memory limit
            memory=4GB

            # CPU settings
            processors=2

            [experimental]
            # Enable auto memory reclaim
            autoMemoryReclaim=gradual
            """;

        File.WriteAllText(_testConfigPath, originalContent);
        var service = new WslConfigService(_testConfigPath);

        // Read, modify, and write
        var config = await service.ReadConfigAsync();
        var modified = config with
        {
            Wsl2 = config.Wsl2 with { Memory = "8GB" }
        };
        await service.WriteConfigAsync(modified);

        // Read back and verify
        var content = await File.ReadAllTextAsync(_testConfigPath);
        content.Should().Contain("# WSL Configuration File");
        content.Should().Contain("# Memory limit");
        content.Should().Contain("memory=8GB");
        content.Should().Contain("# CPU settings");
    }

    #endregion
}
