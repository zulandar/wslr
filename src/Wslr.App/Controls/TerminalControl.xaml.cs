using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace Wslr.App.Controls;

/// <summary>
/// A terminal control using WebView2 and xterm.js for rendering.
/// </summary>
public partial class TerminalControl : UserControl
{
    private bool _isInitialized;
    private bool _isReady;
    private readonly Queue<string> _pendingOutput = new();

    /// <summary>
    /// Raised when user input is received from the terminal.
    /// </summary>
    public event EventHandler<string>? InputReceived;

    /// <summary>
    /// Raised when the terminal is resized.
    /// </summary>
    public event EventHandler<TerminalSizeEventArgs>? Resized;

    /// <summary>
    /// Raised when the terminal is ready to receive input.
    /// </summary>
    public event EventHandler? Ready;

    /// <summary>
    /// Gets whether the terminal is ready to receive input.
    /// </summary>
    public bool IsReady => _isReady;

    public TerminalControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        try
        {
            await InitializeWebViewAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to initialize WebView2: {ex.Message}");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        TerminalWebView.CoreWebView2?.Stop();
    }

    private async Task InitializeWebViewAsync()
    {
        // Initialize WebView2
        var env = await CoreWebView2Environment.CreateAsync();
        await TerminalWebView.EnsureCoreWebView2Async(env);

        var coreWebView = TerminalWebView.CoreWebView2;

        // Configure WebView2 settings
        coreWebView.Settings.IsScriptEnabled = true;
        coreWebView.Settings.AreDefaultScriptDialogsEnabled = false;
        coreWebView.Settings.IsStatusBarEnabled = false;
        coreWebView.Settings.AreDevToolsEnabled = false;
        coreWebView.Settings.IsZoomControlEnabled = false;
        coreWebView.Settings.AreDefaultContextMenusEnabled = false;

        // Set up virtual host for local assets (use AppContext.BaseDirectory for single-file compatibility)
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Terminal");
        coreWebView.SetVirtualHostNameToFolderMapping("wslr.local", assetsPath, CoreWebView2HostResourceAccessKind.Allow);

        // Handle messages from JavaScript
        coreWebView.WebMessageReceived += OnWebMessageReceived;

        // Navigate to local terminal.html via virtual host
        coreWebView.Navigate("https://wslr.local/terminal.html");

        // Show the WebView
        LoadingPanel.Visibility = Visibility.Collapsed;
        TerminalWebView.Visibility = Visibility.Visible;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var type = root.GetProperty("type").GetString();
            var data = root.TryGetProperty("data", out var dataElement) ? dataElement : default;

            switch (type)
            {
                case "ready":
                    _isReady = true;
                    FlushPendingOutput();
                    var cols = data.TryGetProperty("cols", out var colsEl) ? colsEl.GetInt32() : 80;
                    var rows = data.TryGetProperty("rows", out var rowsEl) ? rowsEl.GetInt32() : 24;
                    Resized?.Invoke(this, new TerminalSizeEventArgs(cols, rows));
                    Ready?.Invoke(this, EventArgs.Empty);
                    break;

                case "input":
                    var input = data.GetString();
                    if (!string.IsNullOrEmpty(input))
                    {
                        InputReceived?.Invoke(this, input);
                    }
                    break;

                case "binary":
                    var binaryData = data.GetString();
                    if (!string.IsNullOrEmpty(binaryData))
                    {
                        InputReceived?.Invoke(this, binaryData);
                    }
                    break;

                case "resize":
                    var newCols = data.TryGetProperty("cols", out var c) ? c.GetInt32() : 80;
                    var newRows = data.TryGetProperty("rows", out var r) ? r.GetInt32() : 24;
                    Resized?.Invoke(this, new TerminalSizeEventArgs(newCols, newRows));
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing terminal message: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes output to the terminal.
    /// </summary>
    /// <param name="output">The text to write.</param>
    public void WriteOutput(string output)
    {
        if (!_isReady)
        {
            _pendingOutput.Enqueue(output);
            return;
        }

        SendMessage("output", output);
    }

    /// <summary>
    /// Clears the terminal screen.
    /// </summary>
    public void Clear()
    {
        SendMessage("clear", null);
    }

    /// <summary>
    /// Focuses the terminal.
    /// </summary>
    public new void Focus()
    {
        TerminalWebView.Focus();
        SendMessage("focus", null);
    }

    /// <summary>
    /// Resets the terminal to its initial state.
    /// </summary>
    public void Reset()
    {
        SendMessage("reset", null);
    }

    /// <summary>
    /// Scrolls the terminal to the bottom.
    /// </summary>
    public void ScrollToBottom()
    {
        SendMessage("scrollToBottom", null);
    }

    /// <summary>
    /// Sets the terminal font size.
    /// </summary>
    /// <param name="size">Font size in pixels.</param>
    public void SetFontSize(int size)
    {
        SendMessage("setFontSize", size);
    }

    private void FlushPendingOutput()
    {
        while (_pendingOutput.Count > 0)
        {
            var output = _pendingOutput.Dequeue();
            SendMessage("output", output);
        }
    }

    private void SendMessage(string type, object? data)
    {
        if (TerminalWebView.CoreWebView2 == null)
        {
            return;
        }

        var message = data != null
            ? JsonSerializer.Serialize(new { type, data })
            : JsonSerializer.Serialize(new { type });

        TerminalWebView.CoreWebView2.PostWebMessageAsJson(message);
    }

    private void ShowError(string message)
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        TerminalWebView.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ErrorMessage.Text = message;
    }
}

/// <summary>
/// Event arguments for terminal resize events.
/// </summary>
public class TerminalSizeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Rows { get; }

    public TerminalSizeEventArgs(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }
}
