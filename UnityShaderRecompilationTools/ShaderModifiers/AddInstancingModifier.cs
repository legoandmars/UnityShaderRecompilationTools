using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityShaderRecompilationTools.Interfaces;
using UnityShaderRecompilationTools.Models;

namespace UnityShaderRecompilationTools.ShaderModifiers
{
    // Abandon all hope, ye who enter here
    // desperately needs to be rewritten so it works with things besides the decompiled AssetRipper shaders
    public class AddInstancingModifier : IShaderModifier
    {
        public AddInstancingModifier(VRRenderingMode renderingMode)
        {
            _renderingMode = renderingMode;
        }

        private VRRenderingMode _renderingMode;
        public string ModifyShader(string shader)
        {
            if (_renderingMode != VRRenderingMode.SinglePassInstanced) return shader;

            List<string> shaderPasses = ExtractShaderPassesWorking(shader);

            if (shaderPasses.Count == 0)
            {
                // broken
            }
            else if (shaderPasses.Count != Regex.Matches(shader, @"Pass {").Count)
            {
                return "";
            }

            bool failBool = false;

            for (int i = 0; i < shaderPasses.Count; i++)
            {
                // vertex shader
                if (shaderPasses[i].Contains("v2f vert"))
                {
                    Regex vertRegex = new Regex(@"v2f\s+vert\(appdata_full\s+v\)\s*{\s*v2f\s+o;", RegexOptions.Multiline);
                    MatchCollection shaderPassVert = vertRegex.Matches(shaderPasses[i]);

                    if (shaderPassVert == null || shaderPassVert.Count != 1)
                    {
                        failBool = true;
                        continue;
                    }

                    // really should check for the following. but insert anyways!
                    string[] vertLines = shaderPassVert[0].Value.Split('\n');

                    string v2fo = vertLines[vertLines.Length - 1];
                    string tabs = shaderPassVert[0].Value.Split('{')[1].Split('\n')[1].Split('v')[0]; // jesus christ.
                    string v2foReplacement = $"{v2fo}\n{tabs}UNITY_SETUP_INSTANCE_ID(v);\n{tabs}UNITY_INITIALIZE_OUTPUT(v2f, o);\n{tabs}UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);";
                    string shaderPassvertNew = shaderPassVert[0].Value.Replace(v2fo, v2foReplacement);

                    // actually replace full pass
                    shader = shader.Replace(shaderPasses[i], shaderPasses[i].Replace(shaderPassVert[0].Value, shaderPassvertNew));

                }
                else failBool = true;

                if (shaderPasses[i].Contains("struct v2f"))
                {
                    Regex v2fRegex = new Regex(@"struct v2f\s*{[^}]*};", RegexOptions.Multiline);
                    MatchCollection shaderPassv2f = v2fRegex.Matches(shaderPasses[i]);
                    if (shaderPassv2f == null || shaderPassv2f.Count != 1)
                    {
                        failBool = true;
                        continue;
                    }

                    if (shaderPassv2f.ToString().Contains("UNITY_VERTEX_OUTPUT_STEREO"))
                    {
                        continue;
                    }

                    string[] v2fLines = shaderPassv2f[0].Value.Split('\n');
                    string finalBracket = v2fLines[v2fLines.Length - 1];
                    string finalBracketReplacement = Repeat("\t", finalBracket.Split('\t').Length) + "UNITY_VERTEX_OUTPUT_STEREO\n" + finalBracket;
                    string shaderPassv2fNew = shaderPassv2f[0].Value.Replace(finalBracket, finalBracketReplacement);

                    // *pretty* sure this shouldn't mess up anything; worth double checking!
                    shader = shader.Replace(shaderPassv2f[0].Value, shaderPassv2fNew);
                }
                else failBool = true;
            }

            if (failBool) return "";
            return shader;
        }

        private static string Repeat(string value, int count)
        {
            return new StringBuilder(value.Length * count).Insert(0, value, count).ToString();
        }

        private static List<string> ExtractShaderPassesWorking(string shaderCode)
        {
            List<string> passes = new List<string>();
            Regex re = new Regex(@"Pass {");
            MatchCollection matches = re.Matches(shaderCode);

            foreach (Match match in matches)
            {
                int finalIndex = -1;
                // start at index, go until we have reached The Final ENDCG

                List<int> endcgIndex = new List<int>();

                Regex endcgRegex = new Regex("ENDCG", RegexOptions.Multiline);
                MatchCollection endcgMatches = endcgRegex.Matches(shaderCode);
                foreach (Match endcgMatch in endcgMatches)
                {
                    endcgIndex.Add(endcgMatch.Index);
                }

                for (int i = 0; i < endcgIndex.Count; i++)
                {
                    if (endcgIndex[i] > match.Index)
                    {
                        finalIndex = endcgIndex[i];
                        break;
                    }
                }

                if (finalIndex == -1) throw new InvalidDataException();
                passes.Add(shaderCode.Substring(match.Index, finalIndex - match.Index));
            }

            return passes;
        }
    }
}
