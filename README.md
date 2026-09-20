# AU Assets Swapper

A BepInEx IL2CPP plugin for Among Us that lets you replace in-game assets at runtime by matching asset names to files in a swap folder.

This project is designed for modding and experimentation: you can swap sprites, textures, audio, fonts, materials, shaders, and prefabs without editing the game files directly.

## Features

- Replace game sprites and textures using ordinary image files
- Swap audio clips such as WAV and OGG files
- Override fonts with custom TTF/OTF files
- Replace shaders, materials, and prefabs from AssetBundle files
- Detect asset names live from the game and log them to the console
- Hot-reload replacements while playing with F5
- Inspect objects under the mouse in pick mode using F7
- Built on Harmony patches for low-level asset interception

## How it works

The plugin loads a swap root folder relative to the game install and scans directories such as:

- `Sprites/`
- `Textures/`
- `Audio/`
- `Fonts/`
- `Shaders/`
- `Materials/`
- `Prefabs/`

Each file is matched by its filename (without extension) to an in-game asset name. When a matching asset is loaded, the plugin intercepts the request and substitutes the custom asset.

The project uses Harmony patches to hook into Unity asset-loading flows and BepInEx to initialize the runtime scanner.

## Repository structure

```text
AU-Assets-Swapper/
├── .gitignore
├── AU-Assets-Swapper.sln
├── src/
│   └── AU-Assets-Swapper/
│       ├── AssetSwapManager.cs
│       ├── Plugin.cs
│       ├── SwapManagerComponent.cs
│       ├── AU-Assets-Swapper.csproj
│       ├── Patches/
│       │   ├── AddressablesPatch.cs
│       │   ├── AssetBundlePatch.cs
│       │   └── ResourcesLoadPatch.cs
│       ├── Swappers/
│       │   ├── AudioSwapper.cs
│       │   ├── FontSwapper.cs
│       │   ├── MaterialSwapper.cs
│       │   ├── PrefabSwapper.cs
│       │   ├── ShaderSwapper.cs
│       │   └── SpriteSwapper.cs
│       └── Utils/
│           ├── AssetLogger.cs
│           ├── AudioLoader.cs
│           └── ImageLoader.cs
└── README.md
```

## Requirements

- Among Us installed with BepInEx IL2CPP
- .NET 6 SDK for building the plugin
- Unity/IL2CPP references for the game install
- A Windows environment for local Windows game builds

## Installation

1. Build the project with `dotnet build`.
2. Copy the generated plugin files into your Among Us BepInEx plugins folder.
3. Launch the game.
4. The plugin will create an `AUAS_Data` directory near the game install and populate swap folders automatically.
5. Put replacement files in the correct category directory and ensure the filenames match the original asset names.

## Usage

After the plugin loads, press:

- `F5` to rescan and hot-reload replacements
- `F7` to toggle pick mode and inspect items under the cursor

The plugin can log all loaded asset names with the configuration entry:

- `DumpAllAssets = true`

This is useful for discovering the exact names of assets you want to replace.

## Supported asset types

| Category | Supported file types | Notes |
| --- | --- | --- |
| Sprites | PNG/JPG | Replaces Sprite objects |
| Textures | PNG/JPG | Replaces raw `Texture2D` assets |
| Audio | WAV/OGG | Replaces `AudioClip` assets |
| Fonts | TTF/OTF | Replaces `Font` assets |
| Shaders | AssetBundle files | Loaded by bundle name and asset name |
| Materials | AssetBundle files | Loaded by bundle name and asset name |
| Prefabs | AssetBundle files | Replaces `GameObject` prefabs |

## Build instructions

The project uses an `AmongUsDir` MSBuild property and falls back to a local default path:

```xml
<AmongUsDir Condition="'$(AmongUsDir)' == ''">D:\games\AU_BIE</AmongUsDir>
```

Set that path to your local Among Us installation and BepInEx directory before building:

```bash
dotnet build src/AU-Assets-Swapper/AU-Assets-Swapper.csproj
```

If your install is elsewhere, override the property:

```bash
dotnet build src/AU-Assets-Swapper/AU-Assets-Swapper.csproj -p:AmongUsDir="C:/path/to/AmongUs"
```

## Notes

This repository is focused on runtime asset replacement for Among Us. It is intended for modding workflows and asset experimentation rather than a general game engine toolkit.

Use the in-game asset dump output to determine the exact asset names you want to override, and place replacement files in the matching folder with the same name (without extension).

## License

Anyone can fork this repo and contribute. But anyone cant hard fork it.
