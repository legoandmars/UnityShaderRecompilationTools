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

namespace UnityShaderRecompilationTools
{
    internal class Program
    {
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

            // TODO: need to add stuff that detects if a shader is compiled for VR automatically
            // and more "automatic" handling of this
            // for now this is set to "compile with single pass instanced" by default

            var renderingModeOption = new Option<VRRenderingMode>(
                aliases: new[] { "--vr-rendering-mode" },
                description: "Recompile for a specific vr rendering mode",
                getDefaultValue: () => VRRenderingMode.SinglePassInstanced);
            rootCommand.AddOption(renderingModeOption);


            rootCommand.SetHandler(ProcessCommand, inputFilesOption, outputOption, overwriteOutputOption, keepDecompileArtifactsOption, keepRecompileArtifactsOption, renderingModeOption);

           return await new CommandLineBuilder(rootCommand)
                .UseHelp()
                .UseParseErrorReporting()
                .Build()
                .InvokeAsync(args);

            /*
            var fileOption = new Option<List<FileInfo>?>(
                name: "--files",
                parseArgument: new ParseArgument<List<FileInfo>>().ExistingOnly(),
                description: "The files to read and display on the console.");

            var rootCommand = new RootCommand("Sample app for System.CommandLine");
            rootCommand.AddOption(fileOption);

            rootCommand.SetHandler((files) =>
            {
                foreach (var file in files)
                {
                    ReadFile(file!);
                }
            },
                fileOption);

            return await rootCommand.InvokeAsync(args);*/

        }

        private static IDecompiler decompiler = new AssetRipperDecompiler();

        static async void ProcessCommand(
            List<string> inputFiles, 
            DirectoryInfo? outputDirectory, 
            bool overwriteOutputDirectory, 
            bool keepDecompileArtifacts, 
            bool keepRecompileArtifacts,
            VRRenderingMode renderingMode)
        {
            try
            {
                var directory = outputDirectory.FullName;

                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                if (inputFiles.Count == 0) throw new ArgumentException("No valid files passed");

                // Do file decompiling individually to prevent "mixing" of different assetbundle shaders
                foreach (var file in inputFiles)
                {
                    var shaderFiles = DecompileFile(file, directory, overwriteOutputDirectory, keepDecompileArtifacts);
                }
            }
            catch(Exception exception)
            {
                Console.Error.WriteLine(exception);
            }
        }

        static List<string> DecompileFile(string file, string directory, bool overwriteOutputDirectory, bool keepDecompileArtifacts)
        {
            // Essentially just IDecompiler.DecompileAssetBundleIntoShaderFiles but with some I/O checking and logging
            if (!File.Exists(file)) throw new ArgumentException($"File {file} does not exist.");

            // Create directory to export into per-file
            var exportPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(file));
            if (Directory.Exists(exportPath))
            {
                if(overwriteOutputDirectory) Directory.Delete(exportPath, true);
                else throw new IOException($"Output directory already exists at {exportPath}. If you'd like to overwrite it, pass the --overwrite-output flag");
            }
            
            Directory.CreateDirectory(exportPath);

            var shaderFiles = decompiler.DecompileAssetBundleIntoShaderFiles(file, exportPath, keepDecompileArtifacts);
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