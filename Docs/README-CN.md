# GameKeeper

[English](../README.md) | [繁體中文](README-ZH.md)

GameKeeper 是一个 Windows 游戏辅助工具，用于让指定游戏或应用在后台时仍表现得像处于活动状态，并可通过独立的实时捕获窗口进行操作。

它会向目标进程注入一个轻量 DLL，用来模拟前台窗口、活动窗口、焦点和光标状态；同时提供实时捕获窗口，把鼠标和键盘输入转发回目标窗口。

## 功能

- **进程管理**：显示可捕获进程的图标、PID，并提供 Attach、Detach、Capture 操作。
- **前台状态模拟**：Hook foreground、active、focus、cursor 相关 API，让对后台敏感的游戏继续运行。
- **实时捕获窗口**：以独立窗口显示目标窗口的实时画面。
- **输入转发**：转发鼠标移动、左键、右键、中键、hover/leave 变化，以及键盘按下和放开消息。
- **子控件命中**：鼠标消息会发送到光标下的 child control，而不是一律发送到主窗口。
- **使用捕获标题栏控制窗口**：CaptureWindow 没有原生标题栏，可直接用捕获画面中的标题栏移动、最小化、最大化、还原或关闭捕获窗口。
- **干净卸载**：Detach 会还原 hook，并从目标进程卸载注入 DLL，避免 DLL 文件被锁定。
- **x86/x64 支持**：会为 32 位和 64 位目标构建并加载对应的辅助文件。

## 构建

需求：

- Visual Studio 2026
- .NET 8 SDK
- Visual Studio workload：
  - Desktop development with C++
  - .NET desktop development

步骤：

1. 确认 Visual Studio/MSBuild 环境可用。
2. 在项目根目录运行 `build.bat`。
3. 构建结果会输出到 `output` 文件夹。

## 使用

1. 从 `output` 文件夹运行 `GameKeeper.exe`。如果目标程序以管理员权限运行，建议 GameKeeper 也以管理员权限运行。
2. 点击 **Refresh** 更新进程列表。
3. 选择目标进程并点击 **Attach**。
4. 在已附加进程列表中点击 **Capture** 打开实时捕获窗口。
5. 通过捕获窗口操作目标程序。
6. 使用完毕后点击 **Detach**，还原 hook 并卸载目标进程中的辅助 DLL。

## 注意

- 部分游戏使用 DirectInput、Raw Input 或 `GetAsyncKeyState` 等轮询式输入接口，普通窗口消息转发不一定能触发所有输入逻辑。
- 本工具仅用于学习和研究，请勿用于违反游戏服务条款或反作弊规则的行为。
