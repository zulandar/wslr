using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.App.Dialogs;

/// <summary>
/// Dialog for displaying script execution progress and output.
/// </summary>
public partial class ScriptExecutionDialog : Window, INotifyPropertyChanged
{
    private readonly IScriptExecutionService _scriptExecutionService;
    private CancellationTokenSource? _cts;
    private readonly StringBuilder _outputBuilder = new();

    private string _title = "Running Setup Script";
    private string _distributionName = string.Empty;
    private string _statusText = "Initializing...";
    private string _outputContent = string.Empty;
    private string _durationText = string.Empty;
    private bool _isRunning;
    private bool _isComplete;
    private bool _hasError;
    private int _exitCode;

    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public new string Title
    {
        get => _title;
        set { _title = value; base.Title = value; OnPropertyChanged(nameof(Title)); }
    }

    /// <summary>
    /// Gets or sets the distribution name.
    /// </summary>
    public string DistributionName
    {
        get => _distributionName;
        set { _distributionName = value; OnPropertyChanged(nameof(DistributionName)); }
    }

    /// <summary>
    /// Gets or sets the status text.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
    }

    /// <summary>
    /// Gets or sets the output content.
    /// </summary>
    public string OutputContent
    {
        get => _outputContent;
        set
        {
            _outputContent = value;
            OnPropertyChanged(nameof(OutputContent));
            // Auto-scroll to bottom
            Dispatcher.BeginInvoke(() =>
            {
                OutputScrollViewer?.ScrollToEnd();
            });
        }
    }

    /// <summary>
    /// Gets or sets the duration text.
    /// </summary>
    public string DurationText
    {
        get => _durationText;
        set { _durationText = value; OnPropertyChanged(nameof(DurationText)); }
    }

    /// <summary>
    /// Gets or sets whether the script is running.
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); }
    }

    /// <summary>
    /// Gets or sets whether execution is complete.
    /// </summary>
    public bool IsComplete
    {
        get => _isComplete;
        set { _isComplete = value; OnPropertyChanged(nameof(IsComplete)); }
    }

    /// <summary>
    /// Gets or sets whether there was an error.
    /// </summary>
    public bool HasError
    {
        get => _hasError;
        set { _hasError = value; OnPropertyChanged(nameof(HasError)); }
    }

    /// <summary>
    /// Gets or sets the exit code.
    /// </summary>
    public int ExitCode
    {
        get => _exitCode;
        set { _exitCode = value; OnPropertyChanged(nameof(ExitCode)); }
    }

    /// <summary>
    /// Gets the execution result.
    /// </summary>
    public ScriptExecutionResult? Result { get; private set; }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionDialog"/> class.
    /// </summary>
    /// <param name="scriptExecutionService">The script execution service.</param>
    public ScriptExecutionDialog(IScriptExecutionService scriptExecutionService)
    {
        _scriptExecutionService = scriptExecutionService ?? throw new ArgumentNullException(nameof(scriptExecutionService));
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// Executes a script and displays the output.
    /// </summary>
    /// <param name="distributionName">The distribution to run the script in.</param>
    /// <param name="scriptContent">The script content.</param>
    /// <param name="scriptName">Optional name for display.</param>
    public async Task ExecuteAsync(string distributionName, string scriptContent, string? scriptName = null)
    {
        DistributionName = distributionName;
        Title = string.IsNullOrEmpty(scriptName) ? "Running Setup Script" : $"Running: {scriptName}";
        IsRunning = true;
        StatusText = "Running...";

        _cts = new CancellationTokenSource();
        var startTime = DateTime.Now;

        try
        {
            var progress = new Progress<string>(line =>
            {
                _outputBuilder.AppendLine(line);
                OutputContent = _outputBuilder.ToString();
            });

            Result = await _scriptExecutionService.ExecuteScriptAsync(
                distributionName,
                scriptContent,
                progress: progress,
                cancellationToken: _cts.Token);

            ExitCode = Result.ExitCode;
            HasError = !Result.IsSuccess;

            if (Result.WasCancelled)
            {
                StatusText = "Cancelled";
                HasError = true;
            }
            else if (Result.IsSuccess)
            {
                StatusText = "Completed successfully";
            }
            else
            {
                StatusText = $"Failed (exit code {Result.ExitCode})";
                if (!string.IsNullOrWhiteSpace(Result.StandardError))
                {
                    _outputBuilder.AppendLine();
                    _outputBuilder.AppendLine("[stderr]");
                    _outputBuilder.AppendLine(Result.StandardError);
                    OutputContent = _outputBuilder.ToString();
                }
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
            HasError = true;
            ExitCode = -1;
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            HasError = true;
            ExitCode = -1;
            _outputBuilder.AppendLine();
            _outputBuilder.AppendLine($"[Error] {ex.Message}");
            OutputContent = _outputBuilder.ToString();
        }
        finally
        {
            IsRunning = false;
            IsComplete = true;
            DurationText = $"{(DateTime.Now - startTime).TotalSeconds:F1}s";
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = Result?.IsSuccess == true;
        Close();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (IsRunning)
        {
            // Prevent closing while running
            e.Cancel = true;
            _cts?.Cancel();
        }
    }
}
