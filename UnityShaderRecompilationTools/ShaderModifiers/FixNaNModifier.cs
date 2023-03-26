using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityShaderRecompilationTools.Interfaces;

namespace UnityShaderRecompilationTools.ShaderModifiers
{
    // This could end up causing more problems than it's worth
    // AssetRipper sometimes puts out NaN for floats, and, in my opinion, it's better to at least *try* to fix that than let the shader fail to compile
    public class FixNaNModifier : IShaderModifier
    {
        public string ModifyShader(string shader)
        {
            return shader.Replace("NaN", "0");
        }
    }
}
