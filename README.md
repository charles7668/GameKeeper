# GameKeeper

[繁體中文](Docs/README-ZH.md) | [简体中文](Docs/README-CN.md)

GameKeeper is a Windows game utility that keeps selected games or applications behaving as if they are still active, even when you operate them through a separate capture window.

It injects a small helper DLL into the target process to spoof foreground, active, focus, and cursor state. It also provides a live capture window that can forward mouse and keyboard input back to the target window.

## Features

- **Process manager**: Lists capturable processes with application icons, PID, attach, detach, and capture controls.
- **Foreground spoofing**: Hooks foreground, active, focus, and cursor APIs so background-sensitive games can keep running.
- **Live capture window**: Shows a real-time capture of the target window in a separate, independent window.
- **Input forwarding**: Forwards mouse movement, left/right/middle clicks, hover transitions, key down, and key up messages to the target window.
- **Child control targeting**: Mouse messages are forwarded to the child control under the cursor instead of only the main window.
- **Captured title bar controls**: The capture window has no native title bar; the captured title bar can move, minimize, maximize, restore, or close the capture window.
- **Clean detach**: Detach restores hooks and unloads the injected DLL to avoid locking the DLL file.
- **x86/x64 support**: Builds and loads the correct helper binaries for 32-bit and 64-bit targets.

## Build Instructions

Requirements:

- Visual Studio 2026
- .NET 8 SDK
- Visual Studio workloads:
  - Desktop development with C++
  - .NET desktop development

Build:

1. Open a developer environment with Visual Studio/MSBuild available.
2. Run `build.bat` from the repository root.
3. The packaged output is written to the `output` folder.

## Usage

1. Run `GameKeeper.exe` from the `output` folder. Administrator mode is recommended when targeting elevated games or applications.
2. Click **Refresh** to update the process list.
3. Select a target process and click **Attach**.
4. Click **Capture** from the attached process list to open the live capture window.
5. Use the capture window to interact with the target process.
6. Click **Detach** when finished. This restores hooks and unloads the helper DLL from the target process.

## Notes

- Some games use DirectInput, Raw Input, or polling APIs such as `GetAsyncKeyState`. Standard window-message forwarding may not trigger every input path.
- This tool is intended for education and research. Do not use it to violate game terms of service or anti-cheat policies.
