using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpiritTide.EditorTools
{
    public static class TideBuilder
    {
        public const string ScenePath = "Assets/SpiritTide/Scenes/Karakura.unity";
        [Serializable] private class BuildRecord { public bool passed; public string unity, version, guid, target, output, managed_sha256; public long bytes; public int errors, warnings; }
        [MenuItem("Tools/Spirit Tide/Validate and Build Windows")]
        public static void PrepareAndBuild()
        {
            Prepare(); TideValidation.Run();
            string target = Path.GetFullPath("Builds/Windows/SpiritTide.exe"); Directory.CreateDirectory(Path.GetDirectoryName(target));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[]{ScenePath}, locationPathName=target, target=BuildTarget.StandaloneWindows64, options=BuildOptions.None });
            string assembly=Path.Combine(Path.GetDirectoryName(target),"SpiritTide_Data/Managed/Assembly-CSharp.dll");
            string hash="";if(File.Exists(assembly))using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(assembly))).Replace("-","").ToLowerInvariant();
            Write("BuildLogs/windows-build.json",new BuildRecord{passed=report.summary.result==BuildResult.Succeeded,unity=Application.unityVersion,version=PlayerSettings.bundleVersion,guid=report.summary.guid.ToString(),managed_sha256=hash,target="Windows64",output=target,bytes=(long)report.summary.totalSize,errors=(int)report.summary.totalErrors,warnings=(int)report.summary.totalWarnings});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Windows build failed.");
        }
        [MenuItem("Tools/Spirit Tide/Validate")]
        public static void ValidateOnly(){Prepare();TideValidation.Run();}
        [MenuItem("Tools/Spirit Tide/Export iPhone Xcode Project")]
        public static void ExportIOS()
        {
            Prepare();TideValidation.Run();
            PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.appleEnableAutomaticSigning=false;
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS,1);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS,ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode=true;EditorUserBuildSettings.development=false;
            string target=Path.GetFullPath("Builds/iOS-Xcode");
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=target,target=BuildTarget.iOS,options=BuildOptions.CompressWithLz4HC});
            Write("BuildLogs/ios-export.json",new BuildRecord{passed=report.summary.result==BuildResult.Succeeded,unity=Application.unityVersion,version=PlayerSettings.bundleVersion,guid=report.summary.guid.ToString(),target="iOS-arm64-device",output=target,bytes=(long)report.summary.totalSize,errors=(int)report.summary.totalErrors,warnings=(int)report.summary.totalWarnings});
            if(report.summary.result!=BuildResult.Succeeded||report.summary.totalErrors>0)throw new BuildFailedException("iPhone export failed.");
        }
        private static void PhoneTexture(TextureImporter texture)
        {
            var ios=texture.GetPlatformTextureSettings("iPhone");ios.name="iPhone";ios.overridden=true;
            ios.maxTextureSize=2048;ios.format=TextureImporterFormat.ASTC_6x6;texture.SetPlatformTextureSettings(ios);
        }
        private static void Prepare()
        {
            Directory.CreateDirectory("BuildLogs");
            Directory.CreateDirectory("Assets/SpiritTide/Scenes");
            foreach(string path in Directory.GetFiles("Assets/SpiritTide/Resources/SpiritTide/Art","*.png"))
            {
                TextureImporter t=AssetImporter.GetAtPath(path) as TextureImporter;
                if(t==null)throw new BuildFailedException("Missing texture importer: "+path);
                t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
                t.alphaIsTransparency=true;t.mipmapEnabled=false;t.isReadable=false;t.maxTextureSize=2048;
                t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Compressed;
                t.spritePixelsPerUnit=128;
                TextureImporterPlatformSettings ios=t.GetPlatformTextureSettings("iPhone");ios.name="iPhone";ios.overridden=true;
                ios.maxTextureSize=2048;ios.format=TextureImporterFormat.ASTC_6x6;t.SetPlatformTextureSettings(ios);t.SaveAndReimport();
            }
            PlayerSettings.companyName="Jemin";
            PlayerSettings.productName="Spirit Tide";
            foreach(string path in Directory.GetFiles("Assets/SpiritTide/Resources/SpiritTide/Art/Campaign","*.png"))
            {
                var t=AssetImporter.GetAtPath(path) as TextureImporter;t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;t.alphaIsTransparency=Path.GetFileName(path).StartsWith("enemies_");t.mipmapEnabled=false;t.isReadable=false;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;PhoneTexture(t);t.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles("Assets/SpiritTide/Resources/SpiritTide/Art/Experience","*.png"))
            {
                var t=AssetImporter.GetAtPath(path) as TextureImporter;t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;t.alphaIsTransparency=path.Contains("motion")||path.Contains("npcs");t.mipmapEnabled=false;t.isReadable=false;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;PhoneTexture(t);t.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles("Assets/SpiritTide/Resources/SpiritTide/Art/Skills","*.png"))
            {
                var texture=AssetImporter.GetAtPath(path) as TextureImporter;texture.textureType=TextureImporterType.Default;
                texture.npotScale=TextureImporterNPOTScale.None;texture.mipmapEnabled=false;texture.isReadable=false;texture.wrapMode=TextureWrapMode.Clamp;
                texture.maxTextureSize=2048;texture.textureCompression=TextureImporterCompression.Uncompressed;PhoneTexture(texture);texture.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles("Assets/SpiritTide/Resources/SpiritTide/Art/Rig","*.png"))
            {
                var texture=AssetImporter.GetAtPath(path) as TextureImporter;texture.textureType=TextureImporterType.Default;texture.npotScale=TextureImporterNPOTScale.None;
                texture.alphaIsTransparency=true;texture.mipmapEnabled=true;texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=2;texture.isReadable=false;texture.wrapMode=TextureWrapMode.Clamp;texture.maxTextureSize=2048;texture.textureCompression=TextureImporterCompression.Uncompressed;PhoneTexture(texture);texture.SaveAndReimport();
            }
            PlayerSettings.bundleVersion="0.13.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.yjmjemin.bleachspirittide");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.yjmjemin.bleachspirittide");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString="17.0";PlayerSettings.iOS.buildNumber="10";
            PlayerSettings.iOS.targetDevice=iOSTargetDevice.iPhoneOnly;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.defaultScreenWidth=1560;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,new[]{GraphicsDeviceType.Metal});
            QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=0;
            UnityEngine.SceneManagement.Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            GameObject camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";
            Camera cam=camera.GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.055f,.115f,.145f);cam.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            new GameObject("SpiritTide").AddComponent<TideGame>();
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        public static void Write(string path,object data){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(data,true));}
    }
}
