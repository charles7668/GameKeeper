#include "pch.h"
#include <cstring>
#include <windows.h>
#include <tlhelp32.h>
#include <tchar.h>
#include "DllLoader.h"
#pragma comment(lib, "psapi.lib")

static const TCHAR* DLL_NAME = _T("GameKeeperCore.dll");

DWORD_PTR GetFunctionOffset(const char* dllPath, const char* funcName, const char* fallbackName)
{
	bool bLoadedLocally = false;
	HMODULE hLocal = GetModuleHandle(DLL_NAME);

	if (!hLocal)
	{
#ifdef UNICODE
		TCHAR wPath[MAX_PATH];
		MultiByteToWideChar(CP_ACP, 0, dllPath, -1, wPath, MAX_PATH);
		hLocal = LoadLibraryEx(wPath, nullptr, DONT_RESOLVE_DLL_REFERENCES);
#else
		hLocal = LoadLibraryEx(dllPath, nullptr, DONT_RESOLVE_DLL_REFERENCES);
#endif
		bLoadedLocally = true;
	}

	if (!hLocal) return 0;

	FARPROC pFunc = GetProcAddress(hLocal, funcName);
	if (!pFunc && fallbackName)
	{
		pFunc = GetProcAddress(hLocal, fallbackName);
	}

	DWORD_PTR offset = 0;
	if (pFunc)
	{
		offset = (DWORD_PTR)pFunc - (DWORD_PTR)hLocal;
	}

	if (bLoadedLocally) FreeLibrary(hLocal);
	return offset;
}

HMODULE GetRemoteModuleHandle(DWORD dwProcessId)
{
	HMODULE hRemoteModule = nullptr;
	HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, dwProcessId);
	if (hSnapshot != INVALID_HANDLE_VALUE)
	{
		MODULEENTRY32 me32;
		me32.dwSize = sizeof(MODULEENTRY32);
		if (Module32First(hSnapshot, &me32))
		{
			do
			{
				if (_tcsstr(me32.szModule, DLL_NAME) || _tcsstr(me32.szExePath, DLL_NAME))
				{
					hRemoteModule = (HMODULE)me32.modBaseAddr;
					break;
				}
			}
			while (Module32Next(hSnapshot, &me32));
		}
		CloseHandle(hSnapshot);
	}
	return hRemoteModule;
}

bool RunRemoteAction(DWORD dwProcessId, const char* dllPath, const char* funcName, const char* fallbackName)
{
	HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwProcessId);
	if (!hProcess) return false;

	DWORD_PTR dwOffset = GetFunctionOffset(dllPath, funcName, fallbackName);
	if (!dwOffset)
	{
		CloseHandle(hProcess);
		return false;
	}

	HMODULE hRemoteModule = GetRemoteModuleHandle(dwProcessId);
	if (!hRemoteModule)
	{
		CloseHandle(hProcess);
		return false;
	}

	LPTHREAD_START_ROUTINE pRemoteFunc = (LPTHREAD_START_ROUTINE)((DWORD_PTR)hRemoteModule + dwOffset);
	HANDLE hThread = CreateRemoteThread(hProcess, nullptr, 0, pRemoteFunc, nullptr, 0, nullptr);

	if (hThread)
	{
		WaitForSingleObject(hThread, INFINITE);
		CloseHandle(hThread);
	}

	CloseHandle(hProcess);
	return hThread != nullptr;
}

bool GK_TryAttach(DWORD dwProcessId, const char* dllPath)
{
	return RunRemoteAction(dwProcessId, dllPath, "Attach", "_Attach@4");
}

bool GK_TryDetach(DWORD dwProcessId, const char* dllPath)
{
	return RunRemoteAction(dwProcessId, dllPath, "Detach", "_Detach@4");
}

bool GK_InjectCoreDll(DWORD dwProcessId, const char* dllPath)
{
	HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwProcessId);
	if (!hProcess) return false;

	size_t pathLen = strlen(dllPath) + 1;
	LPVOID pRemoteBuf = VirtualAllocEx(hProcess, nullptr, pathLen, MEM_COMMIT, PAGE_READWRITE);
	if (!pRemoteBuf)
	{
		CloseHandle(hProcess);
		return false;
	}

	if (!WriteProcessMemory(hProcess, pRemoteBuf, dllPath, pathLen, nullptr))
	{
		VirtualFreeEx(hProcess, pRemoteBuf, 0, MEM_RELEASE);
		CloseHandle(hProcess);
		return false;
	}

	HMODULE hKernel32 = GetModuleHandleA("kernel32.dll");
	auto pLoadLibrary = reinterpret_cast<LPTHREAD_START_ROUTINE>(GetProcAddress(hKernel32, "LoadLibraryA"));

	HANDLE hThread = CreateRemoteThread(hProcess, nullptr, 0, pLoadLibrary, pRemoteBuf, 0, nullptr);
	if (hThread)
	{
		WaitForSingleObject(hThread, INFINITE);
		CloseHandle(hThread);
	}

	VirtualFreeEx(hProcess, pRemoteBuf, 0, MEM_RELEASE);
	CloseHandle(hProcess);
	return hThread != nullptr;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	return TRUE;
}
