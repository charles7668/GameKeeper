#pragma once
#include <windows.h>

#define EXPORT __declspec(dllexport)

extern "C" {
EXPORT bool GK_TryAttach(DWORD dwProcessId, const char* dllPath);
EXPORT bool GK_TryDetach(DWORD dwProcessId, const char* dllPath);
EXPORT bool GK_InjectCoreDll(DWORD dwProcessId, const char* dllPath);
}
