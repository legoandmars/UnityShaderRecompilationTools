using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityShaderRecompilationTools.Interfaces
{
    // Decompiles the assetbundle *and* turns the shader bytecode into a usable .shader file
    // TODO: split these into seperate services if need be. AssetRipper does both in one pass, so it's been unnecessary so far.
    public interface IDecompiler
    {
        /// <summary>
        /// Returns a list of .shader file paths
        /// </summary>
        public List<string> DecompileAssetBundleIntoShaderFiles(string file, string exportPath, bool keepDecompileArtifacts);
    }
}
