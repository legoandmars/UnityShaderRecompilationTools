using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityShaderRecompilationTools.Models;

namespace UnityShaderRecompilationTools.Interfaces
{
    // Recompiles the .shader file into a standalone .shaderbundle file.
    // TODO: Create a way to repackage the assetbundle completely
    public interface IRecompiler
    {
        /// <summary>
        /// Returns a list of .shaderbundle file paths
        /// </summary>
        public List<ShaderBundleInfo> RecompileShaderFilesIntoAssetbundle(
            List<ShaderBundleInfo> shaderBundleInfos, 
            string exportPath, 
            bool keepRecompileArtifacts, 
            bool keepDecompileArtifacts,
            VRRenderingMode renderingMode
        );
    }
}
