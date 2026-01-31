using System.Text.RegularExpressions;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.Infrastructure.Parsing;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IWslConfigService"/> that reads and writes the .wslconfig file.
/// </summary>
public sealed partial class WslConfigService : IWslConfigService
{
    private const string ConfigFileName = ".wslconfig";
    private readonly string _configPath;
    private readonly object _lock = new();
    private IniDocument? _cachedDocument;
    private DateTime _cacheTimestamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="WslConfigService"/> class.
    /// </summary>
    public WslConfigService()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ConfigFileName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WslConfigService"/> class with a custom path.
    /// </summary>
    /// <param name="configPath">The path to the .wslconfig file.</param>
    public WslConfigService(string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        _configPath = configPath;
    }

    /// <inheritdoc />
    public string ConfigPath => _configPath;

    /// <inheritdoc />
    public bool ConfigExists => File.Exists(_configPath);

    /// <inheritdoc />
    public async Task<WslConfig> ReadConfigAsync(CancellationToken cancellationToken = default)
    {
        if (!ConfigExists)
        {
            return new WslConfig();
        }

        string content;
        lock (_lock)
        {
            // Check if we have a valid cached document
            if (_cachedDocument is not null && File.Exists(_configPath))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(_configPath);
                if (lastWriteTime <= _cacheTimestamp)
                {
                    return WslConfigParser.ParseFromDocument(_cachedDocument);
                }
            }
        }

        content = await File.ReadAllTextAsync(_configPath, cancellationToken);

        lock (_lock)
        {
            _cachedDocument = IniDocument.Parse(content);
            _cacheTimestamp = File.GetLastWriteTimeUtc(_configPath);
            return WslConfigParser.ParseFromDocument(_cachedDocument);
        }
    }

    /// <inheritdoc />
    public async Task WriteConfigAsync(WslConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        string content;

        lock (_lock)
        {
            // Start with cached document or existing file content to preserve comments
            IniDocument document;

            if (_cachedDocument is not null)
            {
                document = _cachedDocument.Clone();
            }
            else if (ConfigExists)
            {
                var existingContent = File.ReadAllText(_configPath);
                document = IniDocument.Parse(existingContent);
            }
            else
            {
                document = new IniDocument();
            }

            WslConfigParser.MergeIntoDocument(document, config);
            content = document.ToString();

            // Update cache
            _cachedDocument = document;
        }

        await File.WriteAllTextAsync(_configPath, content, cancellationToken);

        lock (_lock)
        {
            _cacheTimestamp = File.GetLastWriteTimeUtc(_configPath);
        }
    }

    /// <inheritdoc />
    public WslConfigValidationResult Validate(WslConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var errors = new List<WslConfigValidationError>();

        // Validate memory format
        if (config.Wsl2.Memory is not null)
        {
            if (!IsValidMemoryFormat(config.Wsl2.Memory))
            {
                errors.Add(new WslConfigValidationError
                {
                    Section = "wsl2",
                    Key = "memory",
                    Message = $"Invalid memory format: '{config.Wsl2.Memory}'. Expected format: '8GB', '4096MB', etc.",
                    Code = WslConfigErrorCode.InvalidMemoryFormat
                });
            }
        }

        // Validate swap format
        if (config.Wsl2.Swap is not null)
        {
            if (!IsValidMemoryFormat(config.Wsl2.Swap))
            {
                errors.Add(new WslConfigValidationError
                {
                    Section = "wsl2",
                    Key = "swap",
                    Message = $"Invalid swap format: '{config.Wsl2.Swap}'. Expected format: '8GB', '4096MB', etc.",
                    Code = WslConfigErrorCode.InvalidMemoryFormat
                });
            }
        }

        // Validate processors
        if (config.Wsl2.Processors.HasValue)
        {
            if (config.Wsl2.Processors.Value < 1)
            {
                errors.Add(new WslConfigValidationError
                {
                    Section = "wsl2",
                    Key = "processors",
                    Message = "Processors must be at least 1.",
                    Code = WslConfigErrorCode.ValueOutOfRange
                });
            }
            else if (config.Wsl2.Processors.Value > Environment.ProcessorCount)
            {
                errors.Add(new WslConfigValidationError
                {
                    Section = "wsl2",
                    Key = "processors",
                    Message = $"Processors ({config.Wsl2.Processors.Value}) exceeds available logical processors ({Environment.ProcessorCount}).",
                    Code = WslConfigErrorCode.ValueOutOfRange
                });
            }
        }

        // Validate vmIdleTimeout
        if (config.Wsl2.VmIdleTimeout.HasValue && config.Wsl2.VmIdleTimeout.Value < 0)
        {
            errors.Add(new WslConfigValidationError
            {
                Section = "wsl2",
                Key = "vmIdleTimeout",
                Message = "VM idle timeout cannot be negative.",
                Code = WslConfigErrorCode.ValueOutOfRange
            });
        }

        // Validate kernel path exists if specified
        if (config.Wsl2.Kernel is not null && !File.Exists(config.Wsl2.Kernel))
        {
            errors.Add(new WslConfigValidationError
            {
                Section = "wsl2",
                Key = "kernel",
                Message = $"Kernel file not found: '{config.Wsl2.Kernel}'.",
                Code = WslConfigErrorCode.FileNotFound
            });
        }

        // Validate autoMemoryReclaim
        if (config.Experimental.AutoMemoryReclaim is not null)
        {
            var validValues = new[] { "disabled", "gradual", "dropcache" };
            if (!validValues.Contains(config.Experimental.AutoMemoryReclaim, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(new WslConfigValidationError
                {
                    Section = "experimental",
                    Key = "autoMemoryReclaim",
                    Message = $"Invalid autoMemoryReclaim value: '{config.Experimental.AutoMemoryReclaim}'. Valid values: disabled, gradual, dropcache.",
                    Code = WslConfigErrorCode.InvalidValue
                });
            }
        }

        // Validate networkingMode
        if (config.Wsl2.NetworkingMode is not null)
        {
            var validModes = new[] { "nat", "mirrored" };
            if (!validModes.Contains(config.Wsl2.NetworkingMode, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(new WslConfigValidationError
                {
                    Section = "wsl2",
                    Key = "networkingMode",
                    Message = $"Invalid networkingMode value: '{config.Wsl2.NetworkingMode}'. Valid values: nat, mirrored.",
                    Code = WslConfigErrorCode.InvalidValue
                });
            }
        }

        return errors.Count > 0
            ? WslConfigValidationResult.Failure(errors)
            : WslConfigValidationResult.Success;
    }

    /// <inheritdoc />
    public async Task<string?> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        if (!ConfigExists)
        {
            return null;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(
            Path.GetDirectoryName(_configPath)!,
            $".wslconfig.backup.{timestamp}");

        await CopyFileAsync(_configPath, backupPath, cancellationToken);
        return backupPath;
    }

    private static bool IsValidMemoryFormat(string value)
    {
        return MemoryFormatRegex().IsMatch(value);
    }

    [GeneratedRegex(@"^\d+\s*(KB|MB|GB|TB|K|M|G|T)?$", RegexOptions.IgnoreCase)]
    private static partial Regex MemoryFormatRegex();

    private static async Task CopyFileAsync(string sourceFileName, string destFileName, CancellationToken cancellationToken = default)
    {
        await using var source = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        await using var dest = new FileStream(destFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
        await source.CopyToAsync(dest, cancellationToken);
    }
}
