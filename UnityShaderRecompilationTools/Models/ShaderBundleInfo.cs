using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityShaderRecompilationTools.Models
{
    [Serializable]
    public class ShaderBundleInfo
    {
        public string AssetbundlePath;
        public string AssetbundleName; 

        public List<string> DecompiledShaderPaths = new();

        public ShaderBundleState State;

        public List<string>? WorkingShaders;
        public List<string>? BrokenShaders;
        public string? RecompiledAssetbundlePath;

        public ShaderBundleInfo(string assetbundlePath, List<string> decompiledShaderPaths)
        {
            AssetbundlePath = assetbundlePath;
            DecompiledShaderPaths = decompiledShaderPaths;

            State = ShaderBundleState.Decompiled;
            AssetbundleName = Path.GetFileNameWithoutExtension(AssetbundlePath);
        }
    }
}
