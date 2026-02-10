#include "pch.h"
#include <windows.h>
#include "GameKeeperCore.h"

#include <detours.h>

WNDPROC g_OriginalWndProc = nullptr;
HWND g_hMainWindow = nullptr;

// Function pointer for the original GetForegroundWindow
static HWND (WINAPI*RealGetForegroundWindow)(void) = GetForegroundWindow;

HWND GetMainWindow();

// Detour function
HWND WINAPI HookedGetForegroundWindow(void)
{
	if (g_hMainWindow)
	{
		return g_hMainWindow;
	}
	return RealGetForegroundWindow();
}

LRESULT CALLBACK NewWndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	if (uMsg == WM_ACTIVATE)
	{
		if (LOWORD(wParam) == WA_INACTIVE) return 0;
	}
	else if (uMsg == WM_KILLFOCUS)
	{
		return 0;
	}
	else if (uMsg == WM_NCACTIVATE)
	{
		if (wParam == FALSE) return CallWindowProc(g_OriginalWndProc, hWnd, uMsg, TRUE, lParam);
	}

	return CallWindowProc(g_OriginalWndProc, hWnd, uMsg, wParam, lParam);
}

BOOL CALLBACK EnumWindowsProc(HWND hWnd, LPARAM lParam)
{
	DWORD dwProcessId = 0;
	GetWindowThreadProcessId(hWnd, &dwProcessId);

	if (dwProcessId == GetCurrentProcessId())
	{
		if (GetWindow(hWnd, GW_OWNER) == nullptr && IsWindowVisible(hWnd))
		{
			*(HWND*)lParam = hWnd;
			return FALSE;
		}
	}
	return TRUE;
}

HWND GetMainWindow()
{
	HWND hWnd = nullptr;
	EnumWindows(EnumWindowsProc, (LPARAM)&hWnd);
	return hWnd;
}

DWORD WINAPI Attach(LPVOID lpParam)
{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourAttach(&(PVOID&)RealGetForegroundWindow, HookedGetForegroundWindow);
	DetourTransactionCommit();

	g_hMainWindow = GetMainWindow();
	if (g_hMainWindow)
	{
#ifdef _WIN64
		g_OriginalWndProc = (WNDPROC)SetWindowLongPtr(g_hMainWindow, GWLP_WNDPROC, (LONG_PTR)NewWndProc);
#else
		g_OriginalWndProc = (WNDPROC)SetWindowLong(g_hMainWindow, GWL_WNDPROC, (LONG)NewWndProc);
#endif
	}

	return 0;
}

DWORD WINAPI Detach(LPVOID lpParam)
{
	// Remove Detour
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourDetach(&(PVOID&)RealGetForegroundWindow, HookedGetForegroundWindow);
	DetourTransactionCommit();

	if (g_hMainWindow && g_OriginalWndProc)
	{
#ifdef _WIN64
		SetWindowLongPtr(g_hMainWindow, GWLP_WNDPROC, (LONG_PTR)g_OriginalWndProc);
#else
		SetWindowLong(g_hMainWindow, GWL_WNDPROC, (LONG)g_OriginalWndProc);
#endif
		g_OriginalWndProc = nullptr;
		g_hMainWindow = nullptr;
	}

	return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	return TRUE;
}
