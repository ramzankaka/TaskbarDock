# TaskbarDock Uninstaller
Write-Host "Restoring Windows 11 Taskbar..." -ForegroundColor Yellow

$env:DOTNET_ROOT = "C:\Users\SSC\.dotnet"
$bin = "$env:LOCALAPPDATA\TaskbarDock\bin\TaskbarDock.exe"
if (Test-Path $bin) {
    & $bin --restore-taskbar
}

# Stop any running process
Get-Process TaskbarDock -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove Registry Startup
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Remove-ItemProperty -Path $runKey -Name "TaskbarDock" -ErrorAction SilentlyContinue

# Remove Start Menu shortcut
$startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\TaskbarDock"
if (Test-Path $startMenuDir) {
    Remove-Item -Path $startMenuDir -Recurse -Force
}

Write-Host "TaskbarDock uninstalled cleanly. Windows Taskbar restored." -ForegroundColor Green
