# TaskbarDock Installer
$installDir = "$env:LOCALAPPDATA\TaskbarDock"
$binDir = "$installDir\bin"
New-Item -ItemType Directory -Path $binDir -Force | Out-Null

$srcBin = "$PSScriptRoot\self_contained_dist"
if (-not (Test-Path "$srcBin\TaskbarDock.exe")) {
    $srcBin = "$PSScriptRoot\dist"
}

Copy-Item -Path "$srcBin\*" -Destination $binDir -Recurse -Force

# Create Start Menu Shortcut
$wshShell = New-Object -ComObject WScript.Shell
$startMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\TaskbarDock"
New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
$shortcut = $wshShell.CreateShortcut("$startMenuDir\TaskbarDock.lnk")
$shortcut.TargetPath = "$binDir\TaskbarDock.exe"
$shortcut.Description = "Windows 11 Taskbar to macOS Dock"
$shortcut.WorkingDirectory = $binDir
$shortcut.Save()

Write-Host "TaskbarDock installed successfully to $installDir" -ForegroundColor Green
