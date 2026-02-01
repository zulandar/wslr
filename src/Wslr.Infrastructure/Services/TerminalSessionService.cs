using System.Collections.Concurrent;
using Wslr.Core.Interfaces;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="ITerminalSessionService"/> that creates and manages
/// terminal sessions connected to WSL distributions.
/// </summary>
public sealed class TerminalSessionService : ITerminalSessionService, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ITerminalSession> _sessions = new();
    private bool _disposed;

    /// <inheritdoc />
    public IReadOnlyList<ITerminalSession> ActiveSessions =>
        _sessions.Values.Where(s => s.IsRunning).ToList();

    /// <inheritdoc />
    public Task<ITerminalSession> CreateSessionAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var session = new WslTerminalSession(distributionName);

        // Track the session
        _sessions.TryAdd(session.Id, session);

        // Remove from tracking when session exits
        session.Exited += exitCode => _sessions.TryRemove(session.Id, out _);

        return Task.FromResult<ITerminalSession>(session);
    }

    /// <inheritdoc />
    public async Task TerminateAllAsync()
    {
        var sessions = _sessions.Values.ToList();

        foreach (var session in sessions)
        {
            try
            {
                session.Terminate();
                await session.DisposeAsync();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        _sessions.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await TerminateAllAsync();
    }
}
