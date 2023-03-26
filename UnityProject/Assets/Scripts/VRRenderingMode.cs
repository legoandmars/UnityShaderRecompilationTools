using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityShaderRecompilationTools.Models
{
    [Serializable]
    public enum VRRenderingMode
    {
        None,
        SinglePass,
        SinglePassInstanced,
        MultiPass, // should almost never be used, but there for completeness
    }
}