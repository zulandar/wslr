# WSLR Troubleshooting Guide

This guide covers common issues and their solutions for WSLR v0.0.5.

## Table of Contents

- [Application Won't Start](#application-wont-start)
- [No Distributions Showing](#no-distributions-showing)
- [Resource Monitoring Issues](#resource-monitoring-issues)
- [Distribution Operations Fail](#distribution-operations-fail)
- [System Tray Issues](#system-tray-issues)
- [Update Notifications Not Working](#update-notifications-not-working)
- [Viewing Logs](#viewing-logs)
- [Reporting Issues](#reporting-issues)

---

## Application Won't Start

### .NET 8.0 Runtime Not Installed

**Symptom**: Application fails to launch or shows a .NET runtime error dialog.

**Solution**:
1. Download the [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Choose the **Windows x64** installer under ".NET Desktop Runtime"
3. Run the installer and restart WSLR

### Missing Visual C++ Redistributable

**Symptom**: Application crashes immediately on startup with no error message.

**Solution**:
1. Download the [Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)
2. Install the x64 version
3. Restart your computer

### Antivirus Blocking

**Symptom**: Application is deleted or quarantined after extraction.

**Solution**:
1. Add an exception for the WSLR folder in your antivirus software
2. Re-extract the release zip file

---

## No Distributions Showing

### WSL Not Installed or Enabled

**Symptom**: Distribution list is empty even though you have WSL distributions.

**Verification**:
```powershell
wsl --list --verbose
```

If this command fails or shows an error, WSL is not properly installed.

**Solution**:
1. Open PowerShell as Administrator
2. Run: `wsl --install`
3. Restart your computer
4. Reopen WSLR

### WSL Version 1 Only

**Symptom**: Distributions show but resource monitoring doesn't work.

**Solution**: WSLR works best with WSL 2. Convert your distributions:
```powershell
wsl --set-version <DistributionName> 2
```

### wsl.exe Not in PATH

**Symptom**: "Failed to load distributions" error.

**Solution**:
1. Verify `wsl.exe` exists in `C:\Windows\System32\`
2. If missing, repair your Windows installation or reinstall WSL

---

## Resource Monitoring Issues

### CPU/Memory Shows 0% or N/A

**Symptom**: Resource usage displays zero or not available for running distributions.

**Possible Causes**:
- Distribution is running but idle (0% is accurate)
- WSL 2 kernel is not running
- Distribution is WSL 1 (only WSL 2 supports per-distro monitoring)

**Solution**:
1. Ensure the distribution is WSL 2: `wsl --list --verbose`
2. Start a process in the distribution to verify monitoring works
3. Check that `vmmem` or `vmmemWSL` process is running in Task Manager

### Disk Usage Not Updating

**Symptom**: Disk usage shows stale values or doesn't change.

**Solution**:
- Disk usage is calculated from the VHDX file size
- It updates on refresh (click the refresh button or wait for auto-refresh)
- Large file operations may take time to reflect in the virtual disk size

---

## Distribution Operations Fail

### "Failed to start distribution"

**Possible Causes**:
- Corrupted distribution
- Out of disk space
- WSL service crashed

**Solutions**:
1. Try starting from command line: `wsl -d <DistributionName>`
2. Check available disk space on the drive containing your distributions
3. Restart the WSL service:
   ```powershell
   wsl --shutdown
   ```
4. If the distribution is corrupted, you may need to unregister and reinstall it

### "Failed to export distribution"

**Possible Causes**:
- Insufficient disk space for the export
- No write permission to the target folder
- Distribution is in an inconsistent state

**Solutions**:
1. Ensure you have enough free space (exports can be large)
2. Try exporting to a different location
3. Stop the distribution before exporting: `wsl --terminate <DistributionName>`

### "Failed to import distribution"

**Possible Causes**:
- Invalid or corrupted tar file
- Target folder already exists
- Insufficient disk space

**Solutions**:
1. Verify the tar file is valid: `tar -tf <filename.tar>`
2. Choose an empty folder for the import location
3. Ensure sufficient disk space for the extracted distribution

### "Failed to clone distribution"

**Solution**: Clone requires exporting then importing. Follow the solutions above for both operations.

---

## System Tray Issues

### Tray Icon Not Appearing

**Symptom**: WSLR runs but no icon appears in the system tray.

**Solutions**:
1. Check the system tray overflow (click the ^ arrow in the taskbar)
2. Right-click the taskbar > Taskbar settings > Turn on "System tray icons"
3. Restart Windows Explorer:
   ```powershell
   Stop-Process -Name explorer -Force; Start-Process explorer
   ```

### App Disappears When Minimized

**Symptom**: Clicking minimize closes the app completely.

**Explanation**: This is expected behavior - WSLR minimizes to the system tray. Look for the icon in the system tray area.

**To restore**: Click or double-click the WSLR icon in the system tray.

---

## Update Notifications Not Working

### No Update Notification on Startup

**Possible Causes**:
- Network connectivity issues
- GitHub API rate limiting
- Firewall blocking GitHub access

**Solution**:
1. Check your internet connection
2. Manually check for updates at [Releases](https://github.com/zulandar/wslr/releases)
3. Update notifications are non-blocking - the app will work even if the check fails

---

## Viewing Logs

WSLR maintains application logs that can help diagnose issues.

### Log Location

```
%LOCALAPPDATA%\WSLR\Logs\
```

To open this folder:
1. Press `Win+R`
2. Type: `%LOCALAPPDATA%\WSLR\Logs`
3. Press Enter

### Log Files

- Logs are named `wslr-YYYYMMDD.log`
- Logs rotate daily and are kept for 7 days
- Maximum total size is approximately 50MB

### Enabling Debug Logging

For more detailed logs:
1. Open WSLR settings
2. Enable "Debug Logging"
3. Reproduce the issue
4. Check the log file for detailed information

### What to Look For

- **Error** or **Fatal** entries indicate problems
- Look for entries near the time the issue occurred
- Stack traces provide technical details about crashes

---

## Reporting Issues

If you can't resolve your issue:

1. **Check existing issues**: [GitHub Issues](https://github.com/zulandar/wslr/issues)

2. **Gather information**:
   - WSLR version (shown in the status bar)
   - Windows version (`winver` command)
   - WSL version: `wsl --version`
   - Relevant log entries

3. **Create a new issue**: [New Issue](https://github.com/zulandar/wslr/issues/new)

Include:
- Steps to reproduce the problem
- Expected vs actual behavior
- Log excerpts (remove any sensitive information)
- Screenshots if applicable

---

## Quick Reference: WSL Commands

These commands can help troubleshoot WSL issues outside of WSLR:

| Command | Description |
|---------|-------------|
| `wsl --list --verbose` | List all distributions with version and state |
| `wsl --shutdown` | Shut down all distributions and the WSL 2 VM |
| `wsl --status` | Show WSL status and kernel version |
| `wsl --update` | Update WSL to the latest version |
| `wsl --set-version <distro> 2` | Convert a distribution to WSL 2 |
| `wsl -d <distro>` | Start a specific distribution |
| `wsl --terminate <distro>` | Stop a specific distribution |
