#include "pch.h"
#include <windows.h>
#include "GameKeeperCore.h"

WNDPROC g_OriginalWndProc = nullptr;

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
	HWND hWnd = GetMainWindow();
	if (hWnd)
	{
#ifdef _WIN64
		g_OriginalWndProc = (WNDPROC)SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)NewWndProc);
#else
		g_OriginalWndProc = (WNDPROC)SetWindowLong(hWnd, GWL_WNDPROC, (LONG)NewWndProc);
#endif
	}

	return 0;
}

DWORD WINAPI Detach(LPVOID lpParam)
{
	HWND hWnd = GetMainWindow();
	if (hWnd && g_OriginalWndProc)
	{
#ifdef _WIN64
		SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)g_OriginalWndProc);
#else
		SetWindowLong(hWnd, GWL_WNDPROC, (LONG)g_OriginalWndProc);
#endif
		g_OriginalWndProc = nullptr;
	}

	return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	return TRUE;
}
