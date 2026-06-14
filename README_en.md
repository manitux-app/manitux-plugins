[Türkçe](README.md)

# Manitux Plugin Repository

This repository contains a collection of plugins for [Manitux](https://github.com/manitux-app/manitux)

Add the raw URL of the repo.json file to the Manitux app settings page:

https://raw.githubusercontent.com/manitux-app/manitux-plugins/main/repo.json

Short code (It needs to be created with [tiny](https://tinyurl.com/)): manitrepo

## Repository setup for plugin developers

Use [PluginTemplate](PluginTemplate/README_en.md) as the starter project for new plugin development. Its README summarizes the build, publish, and video source resolution flow with the built-in extractor classes.

This repository publishes from a single branch:

- The `main` branch contains the source code, the top-level repository manifest, and the published build outputs.
- The `builds/` directory contains compiled plugin DLLs and the plugin list read by Manitux.

`repo.json` is the main manifest added to the Manitux app. Its `pluginLists` field currently points to `builds/plugins.json` under the `main` branch:

```json
"pluginLists": [
  "https://raw.githubusercontent.com/manitux-app/manitux-plugins/main/builds/plugins.json"
]
```

Because of this, adding source code is not enough to publish a new plugin; the compiled DLL and metadata must also be added to the in-repository `builds/` directory.

The local workspace layout is:

- Source repository: `manitux-plugins`
- Publish/build directory: `builds/`
- Published plugin list: `builds/plugins.json`
- Published DLLs: `builds/*.dll`

General flow for preparing a new or updated plugin:

1. Add or update the plugin class under `Manitux.Plugins/`.
2. Keep the plugin `PluginManifest.Id` value unique. This value must match `internalName` in `plugins.json`.
3. Build the project:

```bash
dotnet build Manitux.Plugins/Manitux.Plugins.csproj -c Release
```

4. Copy the generated DLL to `builds/`. The shared package currently expects the file name `Manitux.Plugins.dll`.
5. Update the relevant DLL entry in `builds/plugins.json`.

Each DLL entry in `plugins.json` contains:

- `url`: Raw GitHub URL of the DLL under `builds/` on the `main` branch.
- `status`: Plugin publish status. Use `1` for active entries.
- `version`: DLL package version. Increment it when the DLL contents change.
- `apiVersion`: Manitux plugin API version.
- `authors`: Plugin developers.
- `repositoryUrl`: Source repository URL.
- `plugins`: Metadata list for the plugins inside the DLL as shown in Manitux.

Each plugin in the `plugins` list should define at least:

- `name`: Display name shown to the user.
- `internalName`: Manifest `Id` value from the code.
- `description`: Short description.
- `language`: Default language code.
- `iconUrl`: Icon URL.
- `isAdult`: Adult content flag.
- `tvTypes`: Supported content types.

Commit source code changes, DLL files under `builds/`, and `builds/plugins.json` changes to the same `main` branch. The Manitux app reaches `builds/plugins.json` through `repo.json`, then downloads plugins from the raw DLL URLs listed there.
