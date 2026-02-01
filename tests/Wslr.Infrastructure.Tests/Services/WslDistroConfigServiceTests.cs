using Wslr.Core.Models;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class WslDistroConfigServiceTests
{
    private readonly WslDistroConfigService _service;

    public WslDistroConfigServiceTests()
    {
        _service = new WslDistroConfigService();
    }

    #region GetConfigPath Tests

    [Fact]
    public void GetConfigPath_WithValidDistroName_ReturnsUncPath()
    {
        var result = _service.GetConfigPath("Ubuntu");

        result.Should().Be(@"\\wsl$\Ubuntu\etc\wsl.conf");
    }

    [Fact]
    public void GetConfigPath_WithDistroContainingSpaces_ReturnsCorrectPath()
    {
        var result = _service.GetConfigPath("Ubuntu 22.04");

        result.Should().Be(@"\\wsl$\Ubuntu 22.04\etc\wsl.conf");
    }

    [Fact]
    public void GetConfigPath_WithNullName_ThrowsArgumentException()
    {
        var act = () => _service.GetConfigPath(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetConfigPath_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => _service.GetConfigPath("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetConfigPath_WithWhitespaceName_ThrowsArgumentException()
    {
        var act = () => _service.GetConfigPath("   ");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithDefaultConfig_ReturnsSuccess()
    {
        var config = new WslDistroConfig();

        var result = _service.Validate(config);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidRootPath_ReturnsSuccess()
    {
        var config = new WslDistroConfig
        {
            Automount = new AutomountSettings
            {
                Root = "/mnt/"
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidRootPath_ReturnsError()
    {
        var config = new WslDistroConfig
        {
            Automount = new AutomountSettings
            {
                Root = "C:\\mnt"  // Windows path, not Unix
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<WslDistroConfigValidationError>(e =>
                e.Section == "automount" &&
                e.Key == "root" &&
                e.Code == WslDistroConfigErrorCode.InvalidPath);
    }

    [Fact]
    public void Validate_WithValidHostname_ReturnsSuccess()
    {
        var config = new WslDistroConfig
        {
            Network = new NetworkSettings
            {
                Hostname = "my-wsl-host"
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("-invalid")]      // Starts with hyphen
    [InlineData("invalid-")]      // Ends with hyphen
    [InlineData("in valid")]      // Contains space
    [InlineData("in@valid")]      // Contains special char
    public void Validate_WithInvalidHostname_ReturnsError(string hostname)
    {
        var config = new WslDistroConfig
        {
            Network = new NetworkSettings
            {
                Hostname = hostname
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<WslDistroConfigValidationError>(e =>
                e.Section == "network" &&
                e.Key == "hostname" &&
                e.Code == WslDistroConfigErrorCode.InvalidHostname);
    }

    [Theory]
    [InlineData("myhost")]
    [InlineData("my-host")]
    [InlineData("host123")]
    [InlineData("a")]
    public void Validate_WithValidHostnames_ReturnsSuccess(string hostname)
    {
        var config = new WslDistroConfig
        {
            Network = new NetworkSettings
            {
                Hostname = hostname
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidUsername_ReturnsSuccess()
    {
        var config = new WslDistroConfig
        {
            User = new UserSettings
            {
                Default = "myuser"
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("MyUser")]        // Uppercase not allowed
    [InlineData("1user")]         // Can't start with number
    [InlineData("-user")]         // Can't start with hyphen
    [InlineData("user@domain")]   // Special chars not allowed
    public void Validate_WithInvalidUsername_ReturnsError(string username)
    {
        var config = new WslDistroConfig
        {
            User = new UserSettings
            {
                Default = username
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<WslDistroConfigValidationError>(e =>
                e.Section == "user" &&
                e.Key == "default" &&
                e.Code == WslDistroConfigErrorCode.InvalidUsername);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("_user")]
    [InlineData("user123")]
    [InlineData("user-name")]
    [InlineData("user_name")]
    public void Validate_WithValidUsernames_ReturnsSuccess(string username)
    {
        var config = new WslDistroConfig
        {
            User = new UserSettings
            {
                Default = username
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidMountOptions_ReturnsSuccess()
    {
        var config = new WslDistroConfig
        {
            Automount = new AutomountSettings
            {
                Options = "metadata,umask=22,fmask=11"
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("metadata umask=22")]   // Space instead of comma
    [InlineData("metadata;umask=22")]   // Semicolon
    [InlineData("option with spaces")]  // Spaces in value
    public void Validate_WithInvalidMountOptions_ReturnsError(string options)
    {
        var config = new WslDistroConfig
        {
            Automount = new AutomountSettings
            {
                Options = options
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match<WslDistroConfigValidationError>(e =>
                e.Section == "automount" &&
                e.Key == "options" &&
                e.Code == WslDistroConfigErrorCode.InvalidMountOptions);
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var config = new WslDistroConfig
        {
            Automount = new AutomountSettings
            {
                Root = "C:\\invalid",
                Options = "invalid options"
            },
            Network = new NetworkSettings
            {
                Hostname = "-invalid"
            },
            User = new UserSettings
            {
                Default = "INVALID"
            }
        };

        var result = _service.Validate(config);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(4);
        result.Errors.Should().Contain(e => e.Key == "root");
        result.Errors.Should().Contain(e => e.Key == "options");
        result.Errors.Should().Contain(e => e.Key == "hostname");
        result.Errors.Should().Contain(e => e.Key == "default");
    }

    [Fact]
    public void Validate_WithNullConfig_ThrowsArgumentNullException()
    {
        var act = () => _service.Validate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ConfigExists Tests (Note: These require actual WSL which may not be available in CI)

    [Fact]
    public void ConfigExists_WithNonExistentDistro_ReturnsFalse()
    {
        // Using a distro name that definitely doesn't exist
        var result = _service.ConfigExists("NonExistentDistro_12345");

        result.Should().BeFalse();
    }

    #endregion
}
