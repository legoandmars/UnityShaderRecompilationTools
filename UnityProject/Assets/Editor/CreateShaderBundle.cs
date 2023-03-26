using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityShaderRecompilationTools.Models;

public class CreateShaderBundle
{
    // Should be done automatically, MenuItem is for testing/manual activation
    [MenuItem("Compile/Shader Bundles")]
    static void CreateShaderBundles()
    {
        // TODO: add broken shader behaviour (replace with default?)
        // Right now the onus is on the model mod to do it
        List<string> allBrokenShaders = new List<string>();
        
        var args = System.Environment.GetCommandLineArgs();
        VRRenderingMode renderingMode = VRRenderingMode.None;
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Trim() == "-vrrenderingmode")
            {
                if (args[i + 1] != null)
                {
                    if (Enum.TryParse<VRRenderingMode>(args[i + 1].Trim(), out renderingMode)) continue;
                }
            }
        }
        Debug.Log(renderingMode);

        if(renderingMode == VRRenderingMode.None) PlayerSettings.virtualRealitySupported = false;
        else
        {
            PlayerSettings.virtualRealitySupported = true;

            if (renderingMode == VRRenderingMode.SinglePass) PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
            if (renderingMode == VRRenderingMode.SinglePassInstanced) PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
            if (renderingMode == VRRenderingMode.MultiPass) PlayerSettings.stereoRenderingPath = StereoRenderingPath.MultiPass;
        }

        // Attempting to minimize AssetDatabase calls because it Breaks Stuff:tm:
        var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
        var resources = Path.Combine(projectPath, "Assets", "Resources");

        var directories = Directory.GetDirectories(resources);
        for (int i = 0; i < directories.Length; i++)
        {
            var directory = directories[i];
            var directoryName = Path.GetFileName(directory);
            
            Debug.Log("DIRNAME:");
            Debug.Log(directoryName);

            var files = Directory.GetFiles(directory);
            
            var shaderParent = new GameObject("ShaderParent");

            // Load & Add shaders
            for (var j = 0; j < files.Length; j++)
            {
                if (files[j].EndsWith(".meta") || files[j].EndsWith(".mat")) continue;
                var shader = Resources.Load<Shader>($"{directoryName}\\{Path.GetFileNameWithoutExtension(files[j])}");

                Debug.Log(shader.name);
                Debug.Log(shader);
                    
                //existingShaders.Add(shader.name);

                var mat = new Material(shader);
                Debug.Log(mat.shader);
                
                if (mat.shader == null || !mat.shader.isSupported)
                {
                    Debug.Log("Shader null pass 1!");
                    continue; // probably unusable. TODO: better error handling
                }
                
                AssetDatabase.CreateAsset(mat,
                    $"Assets/Resources/{directoryName}/{Path.GetFileNameWithoutExtension(files[j])}.mat");

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

                cube.name = i.ToString();
                cube.transform.SetParent(shaderParent.transform);
                cube.GetComponent<Renderer>().sharedMaterial = mat;
            }

            
            // Build assetbundle
            // TODO: More Explicit platform handling
            var path = Path.Combine(projectPath, "../", $"{directoryName}.shaderbundle");

            string fileName = Path.GetFileName(path);
            string folderPath = Path.GetDirectoryName(path);

        
            BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            
            PrefabUtility.SaveAsPrefabAsset(shaderParent, "Assets/_Shaders.prefab");
            AssetBundleBuild assetBundleBuild = default;
            assetBundleBuild.assetBundleName = fileName;
            assetBundleBuild.assetNames = new string[]
            {
                "Assets/_Shaders.prefab"
            };

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath,
                new AssetBundleBuild[] {assetBundleBuild}, BuildAssetBundleOptions.ForceRebuildAssetBundle,
                EditorUserBuildSettings.activeBuildTarget);
            EditorPrefs.SetString("currentBuildingAssetBundlePath", folderPath);
            EditorUserBuildSettings.SwitchActiveBuildTarget(selectedBuildTargetGroup, activeBuildTarget);

            if (File.Exists(path)) File.Delete(path);

            // Unity seems to save the file in lower case, which is a problem on Linux, as file systems are case sensitive there
            File.Move(Path.Combine(Application.temporaryCachePath, fileName.ToLowerInvariant()), path);

            // Deleting is probably not needed anymore, since the entire folder will get deleted at the end.
            // AssetDatabase.DeleteAsset($"Assets/Resources/{directoryNum}");
            
            // Unfortunately, a lot of broken assetbundles compile completely fine.
            // We need to actually try to load the assetbundle to see if it works
            // TODO: test this more explicitly
            // TODO: Make this more robust - no shader "programs" will compile when a compilation fails, and this is probably more detectable. 
            var bundle = AssetBundle.LoadFromFile(path);
            var newShaderParent = bundle.LoadAsset<GameObject>("assets/_shaders.prefab");
            foreach (var renderer in newShaderParent.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial.shader == null ||
                    renderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader" ||
                    !renderer.sharedMaterial.shader.isSupported)
                {
                    Debug.Log(renderer.sharedMaterial.name);
                    Debug.Log(renderer.sharedMaterial.shader.name);
                    Debug.Log("Shader null pass 2!");
                    allBrokenShaders.Add($"{directoryName}/{renderer.sharedMaterial.name}");
                    //File.Delete(path);
                    //return;
                }
            }

            if (allBrokenShaders.Count > 0)
            {
                File.WriteAllLines(Path.Combine(projectPath, "../", "broken-shaders.txt"), allBrokenShaders);
            }
        }
        Debug.Log(projectPath);
    }
}
