# Unity Shader Recompilation Tools

The purpose of this project is to serve as a ***last ditch effort*** to fix the shaders of compiled assetbundles without needing to re-export from the Unity Editor.

Example use cases:
- You have an existing library of modded content for a VR game, but it suddenly changes rendering mode from Single Pass to Single Pass Instanced.
- You have an existing library of modded content for one platform (eg: pc), and suddenly the game comes out on mac.

## Progress
### Definitions:

*Custom Assetbundle Dependencies*: A custom, unity-less solution for loading/modifying assetbundles such as AssetRipper or AssetTools.NET. Not ideal to distribute to a client's PC directly if possible.

### Essential Features:
- ~~Extract Shader bytecode from Assetbundle~~ ✅

- ~~Decompile shader bytecode into usable .shader file~~ ✅
    - TODO: Both this step and the step above are currently done in one swoop by AssetRipper. These steps will probably need to be seperated if a new custom solution is added for shader decompilation.

- ~~`.shader` file code injection for adding new features.~~ ✅ Currently implemented for [making shaders support SinglePassInstancing](https://docs.unity3d.com/Manual/SinglePassInstancing.html).
    - TODO: Make this significantly more robust so it works with non-decompiled shaders or other types of decompilation.

- ~~Re-compiling the shader and exporting it as a new, standalone .`shaderbundle`~~ ✅
    - TODO: Look into ways of doing this without using the Unity Editor, as it adds a *significant* amount of overhead.
    - TODO: Add support for different unity versions.

- Module to detect if shaders need to be recompiled/decompiled in the first place.
    - Current goal: detect if shader was compiled for Single Pass or Single Pass Instanced.
    - Ideally, this module should work from within a .NET framework 4 environment without Custom Assetbundle Dependencies so it can be integrated directly into mods.
    - **This module should be made available in the MIT license for ease of use**

- Module/tests to automatically detect if shader recompilation failed.
    - This is kind of abstract, but is probably partially automatable. Things such as a model only showing up in one eye for VR when changing the VR rendering mode would be an obvious fail that can be detected programmatically. 

- Definitive `.shaderbundle` spec that supports multiple graphics APIs (Metal, DirectX, etc..) and multiple compilation types (Single Pass, Single Pass Instanced) in one file
    - This file should be easy to load in a .NET framework 4 environment without Custom Assetbundle Dependencies.
    - This format will probably just be multiple assetbundles combined into a single file somehow.
    - A way to directly combine this `.shaderbundle` with other (normal) assetbundles in a single file would be beneficial for custom content going forward.
    - **This format spec should be made available in the MIT license for ease of use**

- Web API / Service support