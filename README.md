# GameKeeper

[繁體中文](Docs/README-ZH.md) | [简体中文](Docs/README-CN.md) | [Development](Docs/develop.md)

GameKeeper is a lightweight Windows game utility that can keep selected games or applications behaving as if they are still active while they run in the background. It can also capture a target game window into an independent scalable window.

## Features

- **Foreground and focus spoofing**: Helps background-sensitive games or applications keep running when they are not the real foreground window.
- **Scalable capture window**: Captures the target window and scales the image to match the GameKeeper capture window size.
- **Input forwarding**: Forwards mouse movement, hover/leave transitions, left/right/middle clicks, and keyboard down/up events to the target window.
- **Child control targeting**: Sends mouse messages to the child window/control under the cursor when possible.
- **Captured title bar control**: Uses the captured title bar area to move, minimize, maximize, restore, or close the capture window.
- **Process list with icons**: Shows available target windows with application icons and process IDs.
- **x86/x64 support**: Loads the matching helper binaries for 32-bit and 64-bit targets.

## Usage

1. Run `GameKeeper.exe`.
2. Click **Refresh** to update the process list.
3. Select a target process and click **Attach**.
4. Click **Capture** from the attached process list to open the capture window.
5. Resize the capture window as needed. The captured game image scales with the window.
6. Interact with the game through the capture window.
7. Click **Detach** when finished, or close GameKeeper to detach all attached processes.

## Notes

- Run GameKeeper as administrator when targeting games or applications that also run as administrator.
- Some games use DirectInput, Raw Input, or polling APIs such as `GetAsyncKeyState`; standard window-message forwarding may not trigger every input path.
- Use this tool only for legitimate personal, educational, or research purposes. Do not use it to violate game terms of service or anti-cheat policies.
