using System.Globalization;
using Wslr.Core.Models;

namespace Wslr.Infrastructure.Parsing;

/// <summary>
/// Parses and serializes .wslconfig files.
/// </summary>
public static class WslConfigParser
{
    private const string Wsl2Section = "wsl2";
    private const string ExperimentalSection = "experimental";

    /// <summary>
    /// Parses a .wslconfig file content into a <see cref="WslConfig"/> object.
    /// </summary>
    /// <param name="content">The file content.</param>
    /// <returns>The parsed configuration.</returns>
    public static WslConfig Parse(string content)
    {
        var document = IniDocument.Parse(content);
        return ParseFromDocument(document);
    }

    /// <summary>
    /// Parses an <see cref="IniDocument"/> into a <see cref="WslConfig"/> object.
    /// </summary>
    /// <param name="document">The INI document.</param>
    /// <returns>The parsed configuration.</returns>
    public static WslConfig ParseFromDocument(IniDocument document)
    {
        var wsl2Section = document.GetSection(Wsl2Section);
        var experimentalSection = document.GetSection(ExperimentalSection);

        var wsl2 = ParseWsl2Settings(wsl2Section);
        var experimental = ParseExperimentalSettings(experimentalSection);

        // Collect additional sections
        var additionalSections = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        foreach (var sectionName in document.Sections)
        {
            if (!string.Equals(sectionName, Wsl2Section, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sectionName, ExperimentalSection, StringComparison.OrdinalIgnoreCase))
            {
                additionalSections[sectionName] = document.GetSection(sectionName);
            }
        }

        return new WslConfig
        {
            Wsl2 = wsl2,
            Experimental = experimental,
            AdditionalSections = additionalSections
        };
    }

    /// <summary>
    /// Merges a <see cref="WslConfig"/> into an existing <see cref="IniDocument"/>, preserving structure.
    /// </summary>
    /// <param name="document">The existing document to update.</param>
    /// <param name="config">The configuration to merge.</param>
    public static void MergeIntoDocument(IniDocument document, WslConfig config)
    {
        MergeWsl2Settings(document, config.Wsl2);
        MergeExperimentalSettings(document, config.Experimental);

        // Merge additional sections
        foreach (var (sectionName, values) in config.AdditionalSections)
        {
            foreach (var (key, value) in values)
            {
                document.SetValue(sectionName, key, value);
            }
        }
    }

    /// <summary>
    /// Serializes a <see cref="WslConfig"/> to a string.
    /// </summary>
    /// <param name="config">The configuration to serialize.</param>
    /// <returns>The serialized content.</returns>
    public static string Serialize(WslConfig config)
    {
        var document = new IniDocument();
        MergeIntoDocument(document, config);
        return document.ToString();
    }

    private static Wsl2Settings ParseWsl2Settings(IReadOnlyDictionary<string, string> section)
    {
        var additionalSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "kernel", "memory", "processors", "localhostForwarding", "kernelCommandLine",
            "safeMode", "swap", "swapFile", "pageReporting", "guiApplications",
            "debugConsole", "nestedVirtualization", "vmIdleTimeout", "dnsTunneling",
            "firewall", "networkingMode"
        };

        foreach (var (key, value) in section)
        {
            if (!knownKeys.Contains(key))
            {
                additionalSettings[key] = value;
            }
        }

        return new Wsl2Settings
        {
            Kernel = GetStringValue(section, "kernel"),
            Memory = GetStringValue(section, "memory"),
            Processors = GetIntValue(section, "processors"),
            LocalhostForwarding = GetBoolValue(section, "localhostForwarding"),
            KernelCommandLine = GetStringValue(section, "kernelCommandLine"),
            SafeMode = GetBoolValue(section, "safeMode"),
            Swap = GetStringValue(section, "swap"),
            SwapFile = GetStringValue(section, "swapFile"),
            PageReporting = GetBoolValue(section, "pageReporting"),
            GuiApplications = GetBoolValue(section, "guiApplications"),
            DebugConsole = GetBoolValue(section, "debugConsole"),
            NestedVirtualization = GetBoolValue(section, "nestedVirtualization"),
            VmIdleTimeout = GetIntValue(section, "vmIdleTimeout"),
            DnsTunneling = GetBoolValue(section, "dnsTunneling"),
            Firewall = GetBoolValue(section, "firewall"),
            NetworkingMode = GetStringValue(section, "networkingMode"),
            AdditionalSettings = additionalSettings
        };
    }

    private static ExperimentalSettings ParseExperimentalSettings(IReadOnlyDictionary<string, string> section)
    {
        var additionalSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "autoMemoryReclaim", "sparseVhd", "useWindowsDnsCache", "bestEffortDnsParsing",
            "initialAutoProxyTimeout", "ignoredPorts", "hostAddressLoopback"
        };

        foreach (var (key, value) in section)
        {
            if (!knownKeys.Contains(key))
            {
                additionalSettings[key] = value;
            }
        }

        return new ExperimentalSettings
        {
            AutoMemoryReclaim = GetStringValue(section, "autoMemoryReclaim"),
            SparseVhd = GetBoolValue(section, "sparseVhd"),
            UseWindowsDnsCache = GetBoolValue(section, "useWindowsDnsCache"),
            BestEffortDnsParsing = GetBoolValue(section, "bestEffortDnsParsing"),
            InitialAutoProxyTimeout = GetIntValue(section, "initialAutoProxyTimeout"),
            IgnoredPorts = GetBoolValue(section, "ignoredPorts"),
            HostAddressLoopback = GetBoolValue(section, "hostAddressLoopback"),
            AdditionalSettings = additionalSettings
        };
    }

    private static void MergeWsl2Settings(IniDocument document, Wsl2Settings settings)
    {
        SetIfNotNull(document, Wsl2Section, "kernel", settings.Kernel);
        SetIfNotNull(document, Wsl2Section, "memory", settings.Memory);
        SetIfNotNull(document, Wsl2Section, "processors", settings.Processors?.ToString(CultureInfo.InvariantCulture));
        SetIfNotNull(document, Wsl2Section, "localhostForwarding", FormatBool(settings.LocalhostForwarding));
        SetIfNotNull(document, Wsl2Section, "kernelCommandLine", settings.KernelCommandLine);
        SetIfNotNull(document, Wsl2Section, "safeMode", FormatBool(settings.SafeMode));
        SetIfNotNull(document, Wsl2Section, "swap", settings.Swap);
        SetIfNotNull(document, Wsl2Section, "swapFile", settings.SwapFile);
        SetIfNotNull(document, Wsl2Section, "pageReporting", FormatBool(settings.PageReporting));
        SetIfNotNull(document, Wsl2Section, "guiApplications", FormatBool(settings.GuiApplications));
        SetIfNotNull(document, Wsl2Section, "debugConsole", FormatBool(settings.DebugConsole));
        SetIfNotNull(document, Wsl2Section, "nestedVirtualization", FormatBool(settings.NestedVirtualization));
        SetIfNotNull(document, Wsl2Section, "vmIdleTimeout", settings.VmIdleTimeout?.ToString(CultureInfo.InvariantCulture));
        SetIfNotNull(document, Wsl2Section, "dnsTunneling", FormatBool(settings.DnsTunneling));
        SetIfNotNull(document, Wsl2Section, "firewall", FormatBool(settings.Firewall));
        SetIfNotNull(document, Wsl2Section, "networkingMode", settings.NetworkingMode);

        foreach (var (key, value) in settings.AdditionalSettings)
        {
            document.SetValue(Wsl2Section, key, value);
        }
    }

    private static void MergeExperimentalSettings(IniDocument document, ExperimentalSettings settings)
    {
        SetIfNotNull(document, ExperimentalSection, "autoMemoryReclaim", settings.AutoMemoryReclaim);
        SetIfNotNull(document, ExperimentalSection, "sparseVhd", FormatBool(settings.SparseVhd));
        SetIfNotNull(document, ExperimentalSection, "useWindowsDnsCache", FormatBool(settings.UseWindowsDnsCache));
        SetIfNotNull(document, ExperimentalSection, "bestEffortDnsParsing", FormatBool(settings.BestEffortDnsParsing));
        SetIfNotNull(document, ExperimentalSection, "initialAutoProxyTimeout", settings.InitialAutoProxyTimeout?.ToString(CultureInfo.InvariantCulture));
        SetIfNotNull(document, ExperimentalSection, "ignoredPorts", FormatBool(settings.IgnoredPorts));
        SetIfNotNull(document, ExperimentalSection, "hostAddressLoopback", FormatBool(settings.HostAddressLoopback));

        foreach (var (key, value) in settings.AdditionalSettings)
        {
            document.SetValue(ExperimentalSection, key, value);
        }
    }

    private static void SetIfNotNull(IniDocument document, string section, string key, string? value)
    {
        if (value is not null)
        {
            document.SetValue(section, key, value);
        }
    }

    private static string? GetStringValue(IReadOnlyDictionary<string, string> section, string key)
    {
        return section.TryGetValue(key, out var value) ? value : null;
    }

    private static int? GetIntValue(IReadOnlyDictionary<string, string> section, string key)
    {
        if (section.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return null;
    }

    private static bool? GetBoolValue(IReadOnlyDictionary<string, string> section, string key)
    {
        if (section.TryGetValue(key, out var value))
        {
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return null;
    }

    private static string? FormatBool(bool? value)
    {
        return value.HasValue ? (value.Value ? "true" : "false") : null;
    }
}
