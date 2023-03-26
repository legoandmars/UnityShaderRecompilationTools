using AssetRipper.Export.UnityProjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityShaderRecompilationTools.Interfaces;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Utils;
using AssetRipper.IO.Files.Streams.MultiFile;
using System.IO;

namespace UnityShaderRecompilationTools.Decompilers
{
    // This decompiler has problem with shader variants. It will only pick the first shader variant to decompile, and thus can be unreliable for some shaders
    // NOTE: The first variant will always be a non-VR version, so going from Single Pass Instanced -> Single Pass *should be the same* as vice versa
    public class AssetRipperDecompiler : IDecompiler
    {
        public List<string> DecompileAssetBundleIntoShaderFiles(string file, string exportPath, bool keepDecompileArtifacts)
        {
            var shaderPaths = new List<string>();
            // do stuff!;
            var filesToExport = new List<string>() { file };

            Ripper ripper = new();
            ripper.Settings.ShaderExportMode = AssetRipper.Export.UnityProjects.Configuration.ShaderExportMode.Decompile;
            // ripper.Settings.LogConfigurationValues();
            ripper.Load(filesToExport);
            ripper.ExportProject(exportPath);

            if (Path.Exists(Path.Combine(exportPath, "ExportedProject", "Assets", "Shader")))
            {
                // redo project
                var shaderFolder = Directory.GetFiles(Path.Combine(exportPath, "ExportedProject", "Assets", "Shader"));
                for (int i = 0; i < shaderFolder.Length; i++)
                {
                    var shader = shaderFolder[i];
                    var newShaderpath = Path.Combine(exportPath, Path.GetFileName(shader));

                    if (!keepDecompileArtifacts) File.Move(shader, newShaderpath);
                    else File.Copy(shader, newShaderpath);

                    if(!newShaderpath.EndsWith(".meta")) shaderPaths.Add(newShaderpath);
                }

                if(!keepDecompileArtifacts) Directory.Delete(Path.Combine(exportPath, "ExportedProject"), true);
                if (!keepDecompileArtifacts) Directory.Delete(Path.Combine(exportPath, "AuxiliaryFiles"), true);
            }

            return shaderPaths;
        }

    }
}
