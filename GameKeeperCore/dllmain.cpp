#include "pch.h"
#include <windows.h>
#include "GameKeeperCore.h"

#include <detours.h>

WNDPROC g_OriginalWndProc = nullptr;
HWND g_hMainWindow = nullptr;
static const wchar_t* OriginalWndProcProperty = L"GameKeeper.OriginalWndProc";
static LONG g_HookedCursorPosX = 0;
static LONG g_HookedCursorPosY = 0;
static LONG g_HasHookedCursorPos = FALSE;
static LONG g_EnableHookedCursorPos = FALSE;
static UINT g_SetCursorOverrideMessage = 0;

// Function pointer for the original GetForegroundWindow
static HWND (WINAPI*RealGetForegroundWindow)(void) = GetForegroundWindow;
static HWND (WINAPI*RealGetActiveWindow)(void) = GetActiveWindow;
static HWND (WINAPI*RealGetFocus)(void) = GetFocus;
static BOOL (WINAPI*RealGetCursorPos)(LPPOINT lpPoint) = GetCursorPos;

HWND GetMainWindow();
LRESULT CALLBACK NewWndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

WNDPROC GetOriginalWndProc(HWND hWnd)
{
	WNDPROC originalWndProc = (WNDPROC)GetPropW(hWnd, OriginalWndProcProperty);
	if (originalWndProc)
	{
		return originalWndProc;
	}

	return g_OriginalWndProc;
}

bool SubclassWindow(HWND hWnd)
{
	if (!hWnd || GetPropW(hWnd, OriginalWndProcProperty))
	{
		return false;
	}

	SetLastError(0);
	WNDPROC originalWndProc = (WNDPROC)SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)NewWndProc);
	if (!originalWndProc && GetLastError() != 0)
	{
		return false;
	}

	if (!SetPropW(hWnd, OriginalWndProcProperty, (HANDLE)originalWndProc))
	{
		SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)originalWndProc);
		return false;
	}

	return true;
}

void RestoreWindowSubclass(HWND hWnd)
{
	WNDPROC originalWndProc = (WNDPROC)GetPropW(hWnd, OriginalWndProcProperty);
	if (!originalWndProc)
	{
		return;
	}

	SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR)originalWndProc);
	RemovePropW(hWnd, OriginalWndProcProperty);
}

BOOL CALLBACK SubclassChildWindowsProc(HWND hWnd, LPARAM lParam)
{
	SubclassWindow(hWnd);
	return TRUE;
}

BOOL CALLBACK RestoreChildWindowsProc(HWND hWnd, LPARAM lParam)
{
	RestoreWindowSubclass(hWnd);
	return TRUE;
}

void UpdateHookedCursorPos(HWND hWnd, LPARAM lParam)
{
	if (InterlockedCompareExchange(&g_EnableHookedCursorPos, TRUE, TRUE) != TRUE)
	{
		return;
	}

	POINT point = {
		(int)(short)LOWORD(lParam),
		(int)(short)HIWORD(lParam)
	};

	if (!ClientToScreen(hWnd, &point))
	{
		return;
	}

	InterlockedExchange(&g_HookedCursorPosX, point.x);
	InterlockedExchange(&g_HookedCursorPosY, point.y);
	InterlockedExchange(&g_HasHookedCursorPos, TRUE);
}

bool IsHookedCursorInsideClient(HWND hWnd)
{
	if (InterlockedCompareExchange(&g_EnableHookedCursorPos, TRUE, TRUE) != TRUE ||
		InterlockedCompareExchange(&g_HasHookedCursorPos, TRUE, TRUE) != TRUE)
	{
		return false;
	}

	POINT point = {
		InterlockedCompareExchange(&g_HookedCursorPosX, 0, 0),
		InterlockedCompareExchange(&g_HookedCursorPosY, 0, 0)
	};

	if (!ScreenToClient(hWnd, &point))
	{
		return false;
	}

	RECT clientRect = {};
	if (!GetClientRect(hWnd, &clientRect))
	{
		return false;
	}

	return point.x >= clientRect.left &&
		point.y >= clientRect.top &&
		point.x < clientRect.right &&
		point.y < clientRect.bottom;
}

void SetHookedCursorPosEnabled(BOOL enabled)
{
	InterlockedExchange(&g_EnableHookedCursorPos, enabled ? TRUE : FALSE);
	if (!enabled)
	{
		InterlockedExchange(&g_HasHookedCursorPos, FALSE);
	}
}

// Detour function
HWND WINAPI HookedGetForegroundWindow(void)
{
	if (g_hMainWindow)
	{
		return g_hMainWindow;
	}
	return RealGetForegroundWindow();
}

HWND WINAPI HookedGetActiveWindow(void)
{
	if (g_hMainWindow)
	{
		return g_hMainWindow;
	}
	return RealGetActiveWindow();
}

HWND WINAPI HookedGetFocus(void)
{
	if (g_hMainWindow)
	{
		return g_hMainWindow;
	}
	return RealGetFocus();
}

BOOL WINAPI HookedGetCursorPos(LPPOINT lpPoint)
{
	BOOL result = RealGetCursorPos(lpPoint);
	if (result &&
		lpPoint &&
		InterlockedCompareExchange(&g_EnableHookedCursorPos, TRUE, TRUE) == TRUE &&
		InterlockedCompareExchange(&g_HasHookedCursorPos, TRUE, TRUE) == TRUE)
	{
		lpPoint->x = InterlockedCompareExchange(&g_HookedCursorPosX, 0, 0);
		lpPoint->y = InterlockedCompareExchange(&g_HookedCursorPosY, 0, 0);
	}

	return result;
}

LRESULT CALLBACK NewWndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	if (g_SetCursorOverrideMessage != 0 && uMsg == g_SetCursorOverrideMessage)
	{
		SetHookedCursorPosEnabled(wParam != FALSE);
		return 0;
	}

	if (uMsg == WM_MOUSEMOVE)
	{
		UpdateHookedCursorPos(hWnd, lParam);
	}

	if (uMsg == WM_MOUSELEAVE && IsHookedCursorInsideClient(hWnd))
	{
		return 0;
	}

	WNDPROC originalWndProc = GetOriginalWndProc(hWnd);
	if (!originalWndProc)
	{
		return DefWindowProc(hWnd, uMsg, wParam, lParam);
	}

	if (uMsg == WM_ACTIVATE)
	{
		if (LOWORD(wParam) == WA_INACTIVE) return 0;
	}
	else if (uMsg == WM_KILLFOCUS)
	{
		return 0;
	}
	else if (uMsg == WM_ACTIVATEAPP)
	{
		if (wParam == FALSE) return 0;
	}
	else if (uMsg == WM_NCACTIVATE)
	{
		if (wParam == FALSE) return CallWindowProc(originalWndProc, hWnd, uMsg, TRUE, lParam);
	}

	return CallWindowProc(originalWndProc, hWnd, uMsg, wParam, lParam);
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
	g_SetCursorOverrideMessage = RegisterWindowMessageW(L"GameKeeper.SetCursorOverride");

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourAttach(&(PVOID&)RealGetForegroundWindow, HookedGetForegroundWindow);
	DetourAttach(&(PVOID&)RealGetActiveWindow, HookedGetActiveWindow);
	DetourAttach(&(PVOID&)RealGetFocus, HookedGetFocus);
	DetourAttach(&(PVOID&)RealGetCursorPos, HookedGetCursorPos);
	DetourTransactionCommit();

	g_hMainWindow = GetMainWindow();
	if (g_hMainWindow)
	{
		SubclassWindow(g_hMainWindow);
		g_OriginalWndProc = GetOriginalWndProc(g_hMainWindow);
		EnumChildWindows(g_hMainWindow, SubclassChildWindowsProc, 0);
	}

	return 0;
}

DWORD WINAPI Detach(LPVOID lpParam)
{
	// Remove Detour
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourDetach(&(PVOID&)RealGetForegroundWindow, HookedGetForegroundWindow);
	DetourDetach(&(PVOID&)RealGetActiveWindow, HookedGetActiveWindow);
	DetourDetach(&(PVOID&)RealGetFocus, HookedGetFocus);
	DetourDetach(&(PVOID&)RealGetCursorPos, HookedGetCursorPos);
	DetourTransactionCommit();

	if (g_hMainWindow && g_OriginalWndProc)
	{
		EnumChildWindows(g_hMainWindow, RestoreChildWindowsProc, 0);
		RestoreWindowSubclass(g_hMainWindow);
		g_OriginalWndProc = nullptr;
		g_hMainWindow = nullptr;
		SetHookedCursorPosEnabled(FALSE);
	}

	return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	return TRUE;
}
