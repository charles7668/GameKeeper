# GameKeeper

[English](../README.md) | [简体中文](README-CN.md)

GameKeeper 是一個 Windows 遊戲輔助工具，用於讓指定遊戲或應用程式在背景時仍表現得像處於作用中狀態，並可透過獨立的即時擷取視窗進行操作。

它會向目標行程注入一個輕量 DLL，用來模擬前景視窗、作用中視窗、焦點和游標狀態；同時提供即時擷取視窗，把滑鼠和鍵盤輸入轉發回目標視窗。

## 功能

- **行程管理**：顯示可擷取行程的圖示、PID，並提供 Attach、Detach、Capture 操作。
- **前景狀態模擬**：Hook foreground、active、focus、cursor 相關 API，讓對背景敏感的遊戲繼續執行。
- **即時擷取視窗**：以獨立視窗顯示目標視窗的即時畫面。
- **輸入轉發**：轉發滑鼠移動、左鍵、右鍵、中鍵、hover/leave 變化，以及鍵盤按下和放開訊息。
- **子控制項命中**：滑鼠訊息會送到游標下的 child control，而不是一律送到主視窗。
- **使用擷取標題列控制視窗**：CaptureWindow 沒有原生標題列，可直接用擷取畫面中的標題列移動、最小化、最大化、還原或關閉擷取視窗。
- **乾淨卸載**：Detach 會還原 hook，並從目標行程卸載注入 DLL，避免 DLL 檔案被鎖定。
- **x86/x64 支援**：會為 32 位元和 64 位元目標建置並載入對應的輔助檔案。

## 建置

需求：

- Visual Studio 2026
- .NET 8 SDK
- Visual Studio workload：
  - Desktop development with C++
  - .NET desktop development

步驟：

1. 確認 Visual Studio/MSBuild 環境可用。
2. 在專案根目錄執行 `build.bat`。
3. 建置結果會輸出到 `output` 資料夾。

## 使用

1. 從 `output` 資料夾執行 `GameKeeper.exe`。如果目標程式以系統管理員權限執行，建議 GameKeeper 也以系統管理員權限執行。
2. 點擊 **Refresh** 更新行程列表。
3. 選擇目標行程並點擊 **Attach**。
4. 在已附加行程列表中點擊 **Capture** 開啟即時擷取視窗。
5. 透過擷取視窗操作目標程式。
6. 使用完畢後點擊 **Detach**，還原 hook 並卸載目標行程中的輔助 DLL。

## 注意

- 部分遊戲使用 DirectInput、Raw Input 或 `GetAsyncKeyState` 等輪詢式輸入介面，普通視窗訊息轉發不一定能觸發所有輸入邏輯。
- 本工具僅用於學習和研究，請勿用於違反遊戲服務條款或反作弊規則的行為。
