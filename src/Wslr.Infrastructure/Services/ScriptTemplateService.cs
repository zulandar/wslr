using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Service for managing setup script templates.
/// </summary>
public sealed class ScriptTemplateService : IScriptTemplateService
{
    private readonly ILogger<ScriptTemplateService> _logger;
    private readonly string _templatesPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ScriptTemplate> _builtInTemplates;

    public ScriptTemplateService(ILogger<ScriptTemplateService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _templatesPath = Path.Combine(appDataPath, "WSLR", "ScriptTemplates");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _builtInTemplates = CreateBuiltInTemplates();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScriptTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = new List<ScriptTemplate>(_builtInTemplates);

        if (Directory.Exists(_templatesPath))
        {
            foreach (var file in Directory.GetFiles(_templatesPath, "*.json"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var template = JsonSerializer.Deserialize<ScriptTemplate>(json, _jsonOptions);
                    if (template is not null)
                    {
                        templates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load template from {File}", file);
                }
            }
        }

        return templates
            .OrderBy(t => t.IsBuiltIn ? 0 : 1)
            .ThenBy(t => t.Category ?? string.Empty)
            .ThenBy(t => t.Name)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScriptTemplate>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var allTemplates = await GetAllTemplatesAsync(cancellationToken);
        return allTemplates
            .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ScriptTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        // Check built-in templates first
        var builtIn = _builtInTemplates.FirstOrDefault(t => t.Id == templateId);
        if (builtIn is not null)
        {
            return builtIn;
        }

        // Check user templates
        var filePath = GetTemplateFilePath(templateId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<ScriptTemplate>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load template {TemplateId}", templateId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ScriptTemplate> CreateTemplateAsync(ScriptTemplate template, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        // Ensure template is not marked as built-in
        var newTemplate = template with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await SaveTemplateAsync(newTemplate, cancellationToken);

        _logger.LogInformation("Created script template '{Name}' with ID {Id}", newTemplate.Name, newTemplate.Id);

        return newTemplate;
    }

    /// <inheritdoc />
    public async Task<ScriptTemplate> UpdateTemplateAsync(ScriptTemplate template, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        // Check if it's a built-in template
        if (_builtInTemplates.Any(t => t.Id == template.Id))
        {
            throw new InvalidOperationException("Cannot modify built-in templates. Duplicate the template first.");
        }

        // Check if template exists
        var filePath = GetTemplateFilePath(template.Id);
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Template with ID '{template.Id}' not found.");
        }

        var updatedTemplate = template with
        {
            IsBuiltIn = false,
            ModifiedAt = DateTime.UtcNow
        };

        await SaveTemplateAsync(updatedTemplate, cancellationToken);

        _logger.LogInformation("Updated script template '{Name}' (ID: {Id})", updatedTemplate.Name, updatedTemplate.Id);

        return updatedTemplate;
    }

    /// <inheritdoc />
    public Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        // Check if it's a built-in template
        if (_builtInTemplates.Any(t => t.Id == templateId))
        {
            throw new InvalidOperationException("Cannot delete built-in templates.");
        }

        var filePath = GetTemplateFilePath(templateId);
        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted script template with ID {Id}", templateId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {TemplateId}", templateId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ScriptTemplate> DuplicateTemplateAsync(string templateId, string? newName = null, CancellationToken cancellationToken = default)
    {
        var original = await GetTemplateAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Template with ID '{templateId}' not found.");

        var duplicateName = newName ?? $"{original.Name} (Copy)";

        var duplicate = original with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = duplicateName,
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await SaveTemplateAsync(duplicate, cancellationToken);

        _logger.LogInformation(
            "Duplicated template '{OriginalName}' as '{NewName}' (ID: {Id})",
            original.Name,
            duplicate.Name,
            duplicate.Id);

        return duplicate;
    }

    /// <inheritdoc />
    public async Task<string> ExportTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Template with ID '{templateId}' not found.");

        // Export without the IsBuiltIn flag so it can be imported as user template
        var exportTemplate = template with { IsBuiltIn = false };

        return JsonSerializer.Serialize(exportTemplate, _jsonOptions);
    }

    /// <inheritdoc />
    public async Task<ScriptTemplate> ImportTemplateAsync(string json, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        ScriptTemplate? imported;
        try
        {
            imported = JsonSerializer.Deserialize<ScriptTemplate>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid template JSON format.", ex);
        }

        if (imported is null)
        {
            throw new InvalidOperationException("Failed to parse template from JSON.");
        }

        // Create as new template with fresh ID
        var newTemplate = imported with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await SaveTemplateAsync(newTemplate, cancellationToken);

        _logger.LogInformation("Imported script template '{Name}' with ID {Id}", newTemplate.Name, newTemplate.Id);

        return newTemplate;
    }

    private async Task SaveTemplateAsync(ScriptTemplate template, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_templatesPath);

        var filePath = GetTemplateFilePath(template.Id);
        var json = JsonSerializer.Serialize(template, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private string GetTemplateFilePath(string templateId) =>
        Path.Combine(_templatesPath, $"{templateId}.json");

    private static List<ScriptTemplate> CreateBuiltInTemplates() =>
    [
        new ScriptTemplate
        {
            Id = "builtin01",
            Name = "Development Environment",
            Description = "Sets up a complete development environment with common tools, Git, and build essentials.",
            Category = "Development",
            IsBuiltIn = true,
            ScriptContent = """
                #!/bin/bash
                set -e

                echo "=== Setting up Development Environment ==="

                # Update package lists
                echo "Updating package lists..."
                sudo apt-get update

                # Install build essentials
                echo "Installing build essentials..."
                sudo apt-get install -y build-essential

                # Install common development tools
                echo "Installing common development tools..."
                sudo apt-get install -y \
                    git \
                    curl \
                    wget \
                    vim \
                    jq \
                    unzip \
                    htop \
                    tree

                # Configure Git if variables provided
                if [ -n "${GIT_USER_NAME}" ]; then
                    git config --global user.name "${GIT_USER_NAME}"
                    echo "Git user.name set to: ${GIT_USER_NAME}"
                fi

                if [ -n "${GIT_USER_EMAIL}" ]; then
                    git config --global user.email "${GIT_USER_EMAIL}"
                    echo "Git user.email set to: ${GIT_USER_EMAIL}"
                fi

                echo "=== Development Environment Setup Complete ==="
                """,
            Variables = new Dictionary<string, string>
            {
                ["GIT_USER_NAME"] = "",
                ["GIT_USER_EMAIL"] = ""
            }
        },
        new ScriptTemplate
        {
            Id = "builtin02",
            Name = "Node.js Development",
            Description = "Installs Node.js LTS via nvm with npm and common global packages.",
            Category = "Development",
            IsBuiltIn = true,
            ScriptContent = """
                #!/bin/bash
                set -e

                echo "=== Setting up Node.js Development Environment ==="

                # Install nvm
                echo "Installing nvm..."
                curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.7/install.sh | bash

                # Load nvm
                export NVM_DIR="$HOME/.nvm"
                [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"

                # Install Node.js LTS
                echo "Installing Node.js LTS..."
                nvm install --lts
                nvm use --lts
                nvm alias default 'lts/*'

                # Install common global packages
                echo "Installing global npm packages..."
                npm install -g yarn pnpm typescript ts-node

                echo "Node.js version: $(node --version)"
                echo "npm version: $(npm --version)"

                echo "=== Node.js Development Setup Complete ==="
                """
        },
        new ScriptTemplate
        {
            Id = "builtin03",
            Name = "Python Development",
            Description = "Installs Python 3 with pip, venv, and common development packages.",
            Category = "Development",
            IsBuiltIn = true,
            ScriptContent = """
                #!/bin/bash
                set -e

                echo "=== Setting up Python Development Environment ==="

                # Update and install Python
                echo "Installing Python 3 and pip..."
                sudo apt-get update
                sudo apt-get install -y \
                    python3 \
                    python3-pip \
                    python3-venv \
                    python3-dev

                # Install common Python tools
                echo "Installing Python development tools..."
                pip3 install --user --upgrade pip
                pip3 install --user \
                    virtualenv \
                    black \
                    flake8 \
                    mypy \
                    pytest

                echo "Python version: $(python3 --version)"
                echo "pip version: $(pip3 --version)"

                echo "=== Python Development Setup Complete ==="
                """
        },
        new ScriptTemplate
        {
            Id = "builtin04",
            Name = "Docker Setup",
            Description = "Installs Docker Engine and configures it for non-root usage.",
            Category = "Development",
            IsBuiltIn = true,
            ScriptContent = """
                #!/bin/bash
                set -e

                echo "=== Setting up Docker ==="

                # Install prerequisites
                echo "Installing prerequisites..."
                sudo apt-get update
                sudo apt-get install -y \
                    ca-certificates \
                    curl \
                    gnupg \
                    lsb-release

                # Add Docker's official GPG key
                echo "Adding Docker GPG key..."
                sudo install -m 0755 -d /etc/apt/keyrings
                curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
                sudo chmod a+r /etc/apt/keyrings/docker.gpg

                # Set up the repository
                echo "Setting up Docker repository..."
                echo \
                    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
                    $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
                    sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

                # Install Docker
                echo "Installing Docker..."
                sudo apt-get update
                sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

                # Add current user to docker group
                echo "Adding user to docker group..."
                sudo usermod -aG docker $USER

                echo "=== Docker Setup Complete ==="
                echo "NOTE: Log out and back in for group changes to take effect."
                """
        },
        new ScriptTemplate
        {
            Id = "builtin05",
            Name = "Minimal Server",
            Description = "Minimal server setup with essential utilities only.",
            Category = "Server",
            IsBuiltIn = true,
            ScriptContent = """
                #!/bin/bash
                set -e

                echo "=== Setting up Minimal Server ==="

                # Update system
                echo "Updating system..."
                sudo apt-get update
                sudo apt-get upgrade -y

                # Install minimal utilities
                echo "Installing minimal utilities..."
                sudo apt-get install -y \
                    curl \
                    wget \
                    vim-tiny \
                    htop \
                    net-tools

                # Clean up
                echo "Cleaning up..."
                sudo apt-get autoremove -y
                sudo apt-get clean

                echo "=== Minimal Server Setup Complete ==="
                """
        },
        new ScriptTemplate
        {
            Id = "builtin06",
            Name = "Create User",
            Description = "Creates a new user with sudo access and optional SSH key.",
            Category = "System",
            IsBuiltIn = true,
            ScriptContent = """
                #!/bin/bash
                set -e

                if [ -z "${NEW_USERNAME}" ]; then
                    echo "Error: NEW_USERNAME variable is required"
                    exit 1
                fi

                echo "=== Creating User: ${NEW_USERNAME} ==="

                # Create user
                echo "Creating user..."
                sudo useradd -m -s /bin/bash "${NEW_USERNAME}"

                # Add to sudo group
                echo "Adding to sudo group..."
                sudo usermod -aG sudo "${NEW_USERNAME}"

                # Set up SSH key if provided
                if [ -n "${SSH_PUBLIC_KEY}" ]; then
                    echo "Setting up SSH key..."
                    sudo -u "${NEW_USERNAME}" mkdir -p "/home/${NEW_USERNAME}/.ssh"
                    echo "${SSH_PUBLIC_KEY}" | sudo -u "${NEW_USERNAME}" tee "/home/${NEW_USERNAME}/.ssh/authorized_keys" > /dev/null
                    sudo chmod 700 "/home/${NEW_USERNAME}/.ssh"
                    sudo chmod 600 "/home/${NEW_USERNAME}/.ssh/authorized_keys"
                fi

                echo "=== User ${NEW_USERNAME} Created Successfully ==="
                echo "Set password with: sudo passwd ${NEW_USERNAME}"
                """,
            Variables = new Dictionary<string, string>
            {
                ["NEW_USERNAME"] = "",
                ["SSH_PUBLIC_KEY"] = ""
            }
        }
    ];
}
