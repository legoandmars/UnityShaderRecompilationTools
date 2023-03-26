using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using UnityShaderRecompilationTools.Decompilers;
using UnityShaderRecompilationTools.Interfaces;
using UnityShaderRecompilationTools.Models;
using UnityShaderRecompilationTools.Recompilers;
using UnityShaderRecompilationTools.ShaderModifiers;

namespace UnityShaderRecompilationTools
{
    internal class Program
    {
        private static IDecompiler _decompiler = new AssetRipperDecompiler();
        private static IRecompiler _recompiler = new UnityEditorRecompiler();
        private static List<IShaderModifier> _modifiers = new List<IShaderModifier>() { new FixNaNModifier() };
        static async Task<int> Main(string[] args)
        {
            RootCommand rootCommand = new() { Description = "AssetRipper Console" };

            Argument<List<string>> inputFilesOption = new();
            rootCommand.AddArgument(inputFilesOption);

            var outputOption = new Option<DirectoryInfo?>(
                            aliases: new[] { "-o", "--output" },
                            description: "The directory to output the built .shaderbundle file.",
                            getDefaultValue: () => new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "output")));
            rootCommand.AddOption(outputOption);

            // TODO: make this work on the actual assetbundles in-unity-project
            var overwriteOutputOption = new Option<bool>(
                            aliases: new[] { "--overwrite-output" },
                            description: "Overwrite the output directory if it already exists.",
                            getDefaultValue: () => false);
            rootCommand.AddOption(overwriteOutputOption);

            var keepDecompileArtifactsOption = new Option<bool>(
                            aliases: new[] { "--keep-decompile-artifacts" },
                            description: "Keep any artifacts from decompilation such as extra assets (meshes, textures, etc)",
                            getDefaultValue: () => false);
            rootCommand.AddOption(keepDecompileArtifactsOption);

            var keepRecompileArtifactsOption = new Option<bool>(
                            aliases: new[] { "--keep-recompile-artifacts" },
                            description: "Keep any artifacts from recompilation such as .shader files and the generated unity project",
                            getDefaultValue: () => false);
            rootCommand.AddOption(keepRecompileArtifactsOption);

            // TODO: selectively remove this if recompiler is not unity.
            // probably unnecessary until another recompiler is actually found.
            var unityEditorPath = new Option<string?>(
                aliases: new[] { "-u", "--unity-editor-path" },
                description: $"The Unity {UnityEditorRecompiler.UnityVersion} editor executable to open the recompilation project with. Do NOT use this unless the automatic detection is failing!",
                getDefaultValue: () => null);
            rootCommand.AddOption(unityEditorPath);

            // TODO: support for compiling multiple VR rendering modes
            // and more "automatic" handling of this
            // for now this is set to "compile with single pass instanced" by default

            var renderingModeOption = new Option<VRRenderingMode>(
                aliases: new[] {"-r", "--vr-rendering-mode" },
                description: "Recompile for a specific vr rendering mode",
                getDefaultValue: () => VRRenderingMode.SinglePassInstanced);
            rootCommand.AddOption(renderingModeOption);


            // TODO: "export modes"
            // current export mode, add .shaderbundle next to existing bundle mode, and "repackage" bundle mode

            rootCommand.SetHandler(ProcessCommand, inputFilesOption, outputOption, overwriteOutputOption, keepDecompileArtifactsOption, keepRecompileArtifactsOption, renderingModeOption, unityEditorPath);

            return await new CommandLineBuilder(rootCommand)
                .UseHelp()
                .UseParseErrorReporting()
                .Build()
                .InvokeAsync(args);
        }

        static async void ProcessCommand(
            List<string> inputFiles, 
            DirectoryInfo? outputDirectory, 
            bool overwriteOutputDirectory, 
            bool keepDecompileArtifacts, 
            bool keepRecompileArtifacts,
            VRRenderingMode renderingMode,
            string? unityEditorPath)
        {
            try
            {
                // add some modifiers depending on flags
                if (renderingMode == VRRenderingMode.SinglePassInstanced) _modifiers.Add(new AddInstancingModifier(renderingMode));

                var directory = outputDirectory.FullName;

                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                if (inputFiles.Count == 0) throw new ArgumentException("No valid files passed");

                // Do file decompiling individually to prevent "mixing" of different assetbundle shaders
                List<ShaderBundleInfo> shaderBundleInfos = new();
                //Dictionary<string, List<string>> shaderFileInfos = new();
                foreach (var file in inputFiles)
                {
                    var shaderFiles = DecompileFile(file, directory, overwriteOutputDirectory, keepDecompileArtifacts);

                    if (shaderFiles != null)
                    {
                        shaderBundleInfos.Add(new ShaderBundleInfo(file, shaderFiles));
                    }
                }

                Console.WriteLine("Applying shader code modifiers...");
                Console.WriteLine(string.Join(", ", _modifiers.Select(x => x.GetType().Name)));
                foreach (var shaderBundle in shaderBundleInfos)
                {
                    foreach(var shaderFile in shaderBundle.DecompiledShaderPaths)
                    {
                        var shader = File.ReadAllText(shaderFile);
                        foreach(var modifier in _modifiers)
                        {
                            var modifierOutput = modifier.ModifyShader(shader);
                            if (!String.IsNullOrWhiteSpace(modifierOutput)) shader = modifierOutput;
                        }
                        File.WriteAllText(shaderFile, shader);
                    }
                }

                // recompile with shader file infos
                // section has not been converted to a new method because all recompilation happens at once, so individual logging of recompile efforts is unnecessary
                Console.WriteLine("Compiling all shader bundles...");

                if (_recompiler is UnityEditorRecompiler && !String.IsNullOrWhiteSpace(unityEditorPath)) (_recompiler as UnityEditorRecompiler).ForceSetUnityInstall(unityEditorPath);
                var newBundleInfos = _recompiler.RecompileShaderFilesIntoAssetbundle(shaderBundleInfos, directory, keepRecompileArtifacts, keepDecompileArtifacts, renderingMode);

                List<ShaderBundleInfo> successfulBundles = newBundleInfos.Where(x => x.State == ShaderBundleState.RecompiledSuccesfully).ToList();
                List<ShaderBundleInfo> unsuccessfulBundles = newBundleInfos.Where(x => x.State == ShaderBundleState.RecompiledWithErrors).ToList();

                // do some logging at the end
                Console.WriteLine($"\nAssetbundles successfully decompiled: {shaderBundleInfos.Count}/{inputFiles.Count} ({(shaderBundleInfos.Count / inputFiles.Count) * 100}%)");
                Console.WriteLine($"Assetbundles successfully recompiled: {successfulBundles.Count}/{inputFiles.Count} ({(successfulBundles.Count / inputFiles.Count) * 100}%)");
                if (unsuccessfulBundles.Count > 0)
                {
                    Console.WriteLine($"Assetbundles recompiled with errors (may be unusable): {unsuccessfulBundles.Count}/{inputFiles.Count} ({(unsuccessfulBundles.Count / inputFiles.Count) * 100}%)");
                    Console.WriteLine("The following shaders failed to compile properly:");
                    foreach(var bundle in unsuccessfulBundles)
                    {
                        foreach(var shader in bundle.BrokenShaders)
                        {
                            Console.WriteLine($"{bundle.AssetbundleName} - {shader}");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
            }
        }

        static List<string> DecompileFile(string file, string directory, bool overwriteOutputDirectory, bool keepDecompileArtifacts)
        {
            // Essentially just IDecompiler.DecompileAssetBundleIntoShaderFiles but with some I/O checking and logging
            if (!File.Exists(file)) throw new FileNotFoundException($"File {file} does not exist.");

            // Create directory to export into per-file
            var exportPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(file));
            if (Directory.Exists(exportPath))
            {
                if(overwriteOutputDirectory) Directory.Delete(exportPath, true);
                else throw new IOException($"Output directory already exists at {exportPath}. If you'd like to overwrite it, pass the --overwrite-output flag");
            }
            
            Directory.CreateDirectory(exportPath);

            var shaderFiles = _decompiler.DecompileAssetBundleIntoShaderFiles(file, exportPath, keepDecompileArtifacts);
            Console.WriteLine($"Successfully decompiled {Path.GetFileName(file)}...");

            foreach(var shaderFile in shaderFiles)
            {
                Console.WriteLine($"Found shader: {Path.GetFileName(shaderFile)}");
            }

            return shaderFiles;
            //ReadFile(new FileInfo(file));

        }

        static void ReadFile(FileInfo file)
        {
            File.ReadLines(file.FullName).ToList()
                .ForEach(line => Console.WriteLine(line));
        }
    }
}