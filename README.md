# TaskbarDock: Windows 11 Taskbar to macOS-Style Dock

**TaskbarDock** is a lightweight, high-performance Windows 11 desktop utility whose **only purpose is to transform the Windows 11 taskbar experience into a macOS-inspired dock**.

The rest of Windows remains 100% untouched.

---

## Features

- **Reversible Mode Switching**: Toggle between normal Windows 11 Taskbar and macOS-Style Dock at any time via `Ctrl + Alt + D` or System Tray.
- **macOS Floating Dock Design**:
  - Translucent glass/acrylic backdrop with rounded corners and drop shadows.
  - Smooth parabolic hover magnification with customizable scale, range, and speed.
  - Running application indicators (glowing LED dots under active apps).
  - Launch bounce animation.
- **Fail-Safe Taskbar Restoration**:
  - Heartbeat-backed crash detector ensures your Windows taskbar is never left hidden.
  - Emergency command line recovery: `TaskbarDock.exe --restore-taskbar`.
  - System tray quick recovery action.
- **Customizable Applications**:
  - Add, remove, and reorder apps.
  - Built-in shortcuts for Start Menu, File Explorer, Windows Terminal, Notepad, Calculator, Microsoft Store, Settings, and Recycle Bin.
- **Auto-Hide & Proximity Reveal**:
  - Auto-hides when idle, smoothly slides in when mouse touches bottom screen edge.
- **Multi-DPI & Multi-Monitor Support**:
  - Full Per-Monitor V2 DPI awareness (100% to 200%+ scaling).

---

## Global Keyboard Shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl + Alt + D` | Toggle between Windows Taskbar Mode & macOS Dock Mode |

---

## Command Line Usage

| Argument | Description |
| --- | --- |
| `TaskbarDock.exe` | Launch the dock utility in saved or configured mode |
| `TaskbarDock.exe --settings` | Launch directly into Settings window |
| `TaskbarDock.exe --restore-taskbar` | Emergency command: restores Windows taskbar immediately and exits |
