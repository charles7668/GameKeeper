# GameKeeper

[繁體中文](Docs/README-ZH.md) | [简体中文](Docs/README-CN.md)

GameKeeper is a user-friendly, general-purpose game utility tool designed to **fake the foreground window status**. This allows certain games or applications that automatically pause when running in the background to continue operating actively.

## Features

- **Intuitive Interface**: A simple windowed interface lets you quickly find and manage your running games or applications.
- **Instant Toggle**: Enable (Attach) or disable (Detach) the foreground spoofer at any time without restarting the game.
- **Broad Support**: Supports both x86 and x64 applications.

## Build Instructions

This project is developed using **Visual Studio 2026** and **.NET 8**. Please follow the steps below to build:

1.  **Install Development Environment**:
    - Install **Visual Studio 2026**.
    - In the Visual Studio Installer, select the following workloads and components:
      - **Desktop development with C++**
      - **.NET desktop development** (ensure **.NET 8** SDK is included)

2.  **Run Automated Build**:
    - Double-click the `build.bat` file in the project root directory.
    - Wait for the script to finish and display "Build and deployment complete".
    - The built executable will be located in the `output` folder.

## Usage

1.  **Launch Program**: Run `GameKeeper.exe` in the `output` folder (Recommended to run as Administrator).
2.  **Refresh List**: Click the **Refresh** button on the interface to update the list of currently running processes.
3.  **Enable Function**: Select the target game or application from the dropdown menu, then click the **Attach** button.
    - _The game will now be treated as if it were in the foreground, and will not pause even if you switch windows._
4.  **Disable Function**: To turn off the feature, simply click the **Detach** button next to the process in the attached list.

---

_This tool is for educational and research purposes only. Do not use it for actions that violate game terms of service._
