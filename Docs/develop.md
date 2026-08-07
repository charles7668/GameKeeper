# Development

## Requirements

- Visual Studio 2026
- .NET 8 SDK
- Visual Studio workloads:
  - Desktop development with C++
  - .NET desktop development

## Build

Run from the repository root:

```bat
build.bat
```

The build output is written to `output`.

## Package

Run from the repository root:

```bat
package.bat
```

The package script reads the version from `GameKeeper/GameKeeper.csproj` and creates:

```text
GameKeeper-v<version>.zip
```

Before creating the archive, `package.bat` removes `.pdb` files from the output folder.

## Project Layout

- `GameKeeper/`: WPF desktop application.
- `GameKeeperCore/`: Native helper DLL injected into the target process.
- `Injector/`: Managed injector launcher.
- `DllLoader/`: Native loader helper.
- `Docs/`: User and development documentation.

## Notes

- Build both x86 and x64 helper binaries before packaging.
- Administrator privileges may be required when attaching to elevated target processes.
