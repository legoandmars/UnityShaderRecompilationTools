using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityShaderRecompilationTools.Interfaces;
using UnityShaderRecompilationTools.Models;

namespace UnityShaderRecompilationTools.Recompilers
{
    // This is NOT an ideal recompiler. Ideally, an external library could be used for building shaders instead of directly building inside the unity editor.
    // Doing things in unity adds a LOT of bloat and overhead.
    public class UnityEditorRecompiler : IRecompiler
    {
        public const string UnityVersion = "2019.4.40f1";

        public UnityEditorRecompiler()
        {
            _unityInstall = LocateUnityInstall();
        }

        public List<ShaderBundleInfo> RecompileShaderFilesIntoAssetbundle(
            List<ShaderBundleInfo> shaderBundleInfos,
            string exportPath,
            bool keepRecompileArtifacts,
            bool keepDecompileArtifacts,
            VRRenderingMode renderingMode
        )
        {
            if (String.IsNullOrWhiteSpace(_unityInstall) || !File.Exists(_unityInstall)) throw new FileNotFoundException($"Unity {UnityVersion} editor could not be found! Please pass a valid executable path with --unity-editor-path");

            var unityProjectBaseLocation = Path.Combine(AppContext.BaseDirectory, "../../../../", "UnityProject");
            if (!Directory.Exists(unityProjectBaseLocation)) throw new DirectoryNotFoundException($"Could not find the UnityProject folder! Make sure it's in the same folder as the {typeof(UnityEditorRecompiler).Namespace} executable");

            // copy unity project
            // I would like to remove this eventually, unfortunately you cannot run the same project multiple times in batch mode
            // for now, you will have to suffer with the R/W consequences.
            // TODO: benchmark to find out if removing the Library folder and letting unity regenerate is faster because of all the 4k files
            var newUnityProjectLocation = Path.Combine(exportPath, "UnityProject");

            if (!CopyEntireDirectory(new DirectoryInfo(unityProjectBaseLocation), new DirectoryInfo(newUnityProjectLocation))) throw new IOException("Failed to copy UnityProject folder.");

            // copy all shaders into unity project (with meta files, if exist)
            foreach(var shaderBundleInfo in shaderBundleInfos)
            {
                var baseDirectory = Path.Combine(newUnityProjectLocation, "Assets", "Resources", shaderBundleInfo.AssetbundleName);
                if (!Directory.Exists(baseDirectory)) Directory.CreateDirectory(baseDirectory);

                foreach(var shader in shaderBundleInfo.DecompiledShaderPaths)
                {
                    var shaderMetaPath = Path.ChangeExtension(shader, ".shader.meta");
                    var newShaderPath = Path.Combine(baseDirectory, Path.GetFileName(shader));
                    var newShaderMetaPath = Path.ChangeExtension(newShaderPath, ".shader.meta");

                    if (!File.Exists(newShaderMetaPath)) File.Copy(shader, newShaderPath);
                    if (File.Exists(shaderMetaPath) && !File.Exists(newShaderMetaPath)) File.Copy(shaderMetaPath, newShaderMetaPath);
                }
            }


            Console.WriteLine($"Unity Install: {_unityInstall}");
            // Start Unity Process
            StartUnityProject(newUnityProjectLocation, renderingMode);

            // Really need a better way of cross-communication between Unity and this program
            // or, really, i just shouldn't be needing to use unity like this at all..

            List<string> brokenShaders = new();
            var brokenShadersPath = Path.Combine(exportPath, "broken-shaders.txt");
            if (File.Exists(brokenShadersPath)) brokenShaders = File.ReadAllLines(brokenShadersPath).ToList();

            // actually parse results and see if anything broke
            foreach(var shaderBundleInfo in shaderBundleInfos)
            {
                shaderBundleInfo.WorkingShaders = new();
                shaderBundleInfo.BrokenShaders = new();

                if (!File.Exists(Path.Combine(exportPath, $"{shaderBundleInfo.AssetbundleName}.shaderbundle"))){
                    // shader bundle does not exist..

                    foreach(var decompiledShader in shaderBundleInfo.DecompiledShaderPaths)
                    {
                        shaderBundleInfo.BrokenShaders.Add(decompiledShader);
                    }

                    continue;
                }

                foreach(var decompiledShader in shaderBundleInfo.DecompiledShaderPaths)
                {
                    var shaderName = Path.GetFileNameWithoutExtension(decompiledShader);
                    var broken = false;
                    foreach(var brokenShader in brokenShaders)
                    {
                        if(brokenShader.StartsWith(shaderBundleInfo.AssetbundleName) && brokenShader.EndsWith(shaderName)){
                            broken = true;
                            break;
                        }
                    }

                    if (broken) shaderBundleInfo.BrokenShaders.Add(shaderName);
                    else shaderBundleInfo.WorkingShaders.Add(shaderName);
                }

                shaderBundleInfo.State = shaderBundleInfo.BrokenShaders.Count == 0 ? ShaderBundleState.RecompiledSuccesfully : ShaderBundleState.RecompiledWithErrors;
            }

            if (!keepRecompileArtifacts)
            {
                Directory.Delete(newUnityProjectLocation, true);
                if (File.Exists(brokenShadersPath)) File.Delete(brokenShadersPath);
            }

            if (!keepDecompileArtifacts)
            {
                foreach(var shaderBundleInfo in shaderBundleInfos)
                {
                    var temporaryOutputShaderDirectory = Path.Combine(exportPath, shaderBundleInfo.AssetbundleName);
                    if (Directory.Exists(temporaryOutputShaderDirectory)) Directory.Delete(temporaryOutputShaderDirectory, true);
                }
            }

            // TODO: maybe put shaderbundles in subfolder? unnecessary for now

            return shaderBundleInfos;
        }

        private string _unityInstall;

        private bool StartUnityProject(string newUnityProjectLocation, VRRenderingMode renderingMode)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = _unityInstall,
                Arguments = $"-projectPath \"{newUnityProjectLocation}\" -batchmode -nographics -executeMethod CreateShaderBundle.CreateShaderBundles -logFile unity-log.txt -quit -vrrenderingmode {renderingMode}",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                CreateNoWindow = false,
            };

            Process unityProcess = new Process { StartInfo = startInfo };

            try
            {
                unityProcess.Start();
                unityProcess.WaitForExit();
                Console.WriteLine("Process finished.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public void ForceSetUnityInstall(string unityInstallPath)
        {
            _unityInstall = unityInstallPath;
        }

        private static bool CopyEntireDirectory(DirectoryInfo source, DirectoryInfo target, bool overwiteFiles = true)
        {
            if (!source.Exists) return false;
            if (!target.Exists) target.Create();

            try
            {
                Parallel.ForEach(source.GetDirectories(), (sourceChildDirectory) =>
                    CopyEntireDirectory(sourceChildDirectory, new DirectoryInfo(Path.Combine(target.FullName, sourceChildDirectory.Name))));

                Parallel.ForEach(source.GetFiles(), sourceFile =>
                    sourceFile.CopyTo(Path.Combine(target.FullName, sourceFile.Name), overwiteFiles));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string LocateUnityInstall()
        {
            List<string> possibleUnityPaths;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // not sure if they stopped supporting 32bit builds at this version, don't want to check right now
                possibleUnityPaths = new(){
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", UnityVersion, "Editor", "Unity.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Unity", UnityVersion, "Editor", "Unity.exe")
                };
            }
            else
            {
                // TODO: check if either unity hub path is valid
                possibleUnityPaths = new()
                {
                    Path.Combine($"~/Unity-{UnityVersion}", "Editor", "Unity"),
                    Path.Combine($"~/Unity", "Hub", "Editor", UnityVersion, "Editor", "Unity"),
                    Path.Combine($"~/Unity", "Hub", "Editor", $"Unity-{UnityVersion}", "Editor", "Unity"),
                };
            }

            foreach(var possibleUnityPath in possibleUnityPaths)
            {
                if (File.Exists(possibleUnityPath)) return possibleUnityPath;
            }

            // nothing exists
            return "";
        }
    }
}
