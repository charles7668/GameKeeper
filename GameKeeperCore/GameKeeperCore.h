#pragma once
#include <windows.h>

#define EXPORT __declspec(dllexport)

extern "C" {
	EXPORT DWORD WINAPI Attach(LPVOID lpParam);
	EXPORT DWORD WINAPI Detach(LPVOID lpParam);
}
