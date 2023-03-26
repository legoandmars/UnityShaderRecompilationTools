using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityShaderRecompilationTools.Interfaces
{
    // For "modifying" a shader text file
    // This can be useful for adding things necessary for instancing
    public interface IShaderModifier
    {
        public string ModifyShader(string shader);
    }
}
