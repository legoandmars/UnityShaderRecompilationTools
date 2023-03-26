# Unity Shader Recompilation Tools

The purpose of this project is to serve as a ***last ditch effort*** to fix the shaders of compiled assetbundles without needing to re-export from the Unity Editor.

Example use cases:
- You have an existing library of modded content for a VR game, but it suddenly changes rendering mode from Single Pass to Single Pass Instanced.
- You have an existing library of modded content for one platform (eg: pc), and suddenly the game comes out on mac.

**Requirements:**

- ***MUST*** have Unity 2019.4.40f1 installed

**Usage:**

```bash
UnityShaderRecompilationTools [List<string>...] [options]

# Example: 
UnityShaderRecompilationTools "C:/Users/YourName/Bundles/fizz.bundle" "C:/Users/YourName/Bundles/buzz.bundle"

# Also supported: drag-and-dropping files onto the .exe on windows
```

## Progress
### Definitions:

*Custom Assetbundle Dependencies*: A custom, unity-less solution for loading/modifying assetbundles such as AssetRipper or AssetTools.NET. Not ideal to distribute to a client's PC directly if possible.

### Essential Features:
- ~~Extract Shader bytecode from Assetbundle~~ ✅

- ~~Decompile shader bytecode into usable .shader file~~ ✅
    - TODO: Both this step and the step above are currently done in one swoop by AsstRipper. These steps will probably need to be seperated if a new custom solution is added for shader decompilation.

- ~~`.shader` file code injection for adding new features.~~ ✅ Currently implemented for [making shaders support SinglePassInstancing](https://docs.unity3d.com/Manual/SinglePassInstancing.html).
    - TODO: Make this significantly more robust so it works with non-decompiled shaders or other types of decompilation.

- ~~Re-compiling the shader and exporting it as a new, standalone .`shaderbundle`~~ ✅
    - TODO: Look into ways of doing this without using the Unity Editor, as it adds a *significant* amount of overhead.
    - TODO: Add support for different unity versions.

- More robust shader platform detection/compilation, so you can convert DirectX -> OpenGL and vice versa

- Module to detect if shaders need to be recompiled/decompiled in the first place.
    - Current goal: detect if shader was compiled for Single Pass or Single Pass Instanced.
    - Ideally, this module should work from within a .NET framework 4 environment without Custom Assetbundle Dependencies so it can be integrated directly into mods.
    - **This module should be made available in the MIT license for ease of use**

- Module/tests to automatically detect if shader recompilation failed.
    - This is kind of abstract, but is probably partially automatable. Things such as a model only showing up in one eye for VR when changing the VR rendering mode would be an obvious fail that can be detected programmatically. 

- Definitive `.shaderbundle` spec that supports multiple graphics APIs (Metal, DirectX, etc..) and multiple compilation types (Single Pass, Single Pass Instanced) in one file
    - This file should be easy to load in a .NET framework 4 environment without Custom Assetbundle Dependencies.
    - This format will probably just be multiple assetbundles or shader variants combined into a single file somehow.
    - A way to directly combine this `.shaderbundle` with other (normal) assetbundles in a single file would be beneficial for custom content going forward.
    - **This format spec should be made available in the MIT license for ease of use**

- Refactor for Web API / Service support

- Caching for web API
    - Once a bundle is uploaded, it should have its `.shaderbundle` stored and returned instead of reprocessing if hash matches
    - A way to add "custom" caches in case somebody has the original shader source would be great, too

## For Developers
### Initializing repo:
```bash
git clone https://github.com/legoandmars/UnityShaderRecompilationTools
cd UnityShaderRecompilationTools
git submodule init
git submodule update --remote
```
Make sure to manually open the AssetRipper .sln file and build that before opening the UnityShaderRecompilationTools project!

## Legal Disclaimers

UnityShaderRecompilationTools is licensed under the [GNU General Public License v3.0](License.md).

Please be aware that using or distributing the output from this software may be against copyright legislation in your jurisdiction. You are responsible for ensuring that you're not breaking any laws.

This software is not sponsored by or affiliated with Unity Technologies or its affiliates. "Unity" is a registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.