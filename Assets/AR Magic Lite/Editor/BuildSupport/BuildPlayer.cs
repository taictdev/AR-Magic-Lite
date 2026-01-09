using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public static class BuildPlayer
{
    private const string OutputPathStreamingAssets = "StreamingAssets/bb";
    private const string SplashScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PackageName = "com.tcgames.armagiclite";

    private static void IncreaseVersionNumbers()
    {
        PlayerSettings.bundleVersion = IncreaseVersion(PlayerSettings.bundleVersion);
        PlayerSettings.Android.bundleVersionCode++;
        PlayerSettings.iOS.buildNumber = IncreaseVersion(PlayerSettings.iOS.buildNumber);
    }

    private static string IncreaseVersion(string version)
    {
        string[] parts = version.Split('.');
        int lastPart = int.Parse(parts[parts.Length - 1]);
        lastPart++;
        parts[parts.Length - 1] = lastPart.ToString();
        return string.Join(".", parts);
    }

    private static void ClearBuildFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
    }

    private static void ShowOutputFolder(string outputPath)
    {
#if UNITY_EDITOR_WIN
        EditorUtility.RevealInFinder(outputPath);
#else // MACOS
        EditorUtility.RevealInFinder(outputPath.Replace("/", "\\"));
#endif
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        string[] files = Directory.GetFiles(sourceDir);
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        string[] dirs = Directory.GetDirectories(sourceDir);
        foreach (string dir in dirs)
        {
            string dirName = Path.GetFileName(dir);
            string destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }

    private static string GetBuildPathAddressable()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        string buildPath = "";
        if (settings.BuildRemoteCatalog)
        {
            buildPath = settings.RemoteCatalogBuildPath.GetValue(settings);
            ClearBuildFolder(buildPath);
        }

        return buildPath;
    }

    private static void PerformBuildPlayer(string outputPath, BuildTarget target, BuildOptions options = BuildOptions.None)
    {
        string[] scenes = {
            SplashScenePath,
        };

        PlayerSettings.SplashScreen.show = false;

        BuildPipeline.BuildPlayer(scenes, outputPath, target, options);
    }

    private static void BuildMobile(string outputPath, BuildTarget target, BuildTargetGroup targetGroup, BuildOptions options = BuildOptions.None | BuildOptions.CompressWithLz4HC)
    {
        string[] scenes = {
            SplashScenePath,
        };

        PlayerSettings.SplashScreen.show = false;

        BuildPlayerOptions buildOptions = new()
        {
            scenes = scenes,
            locationPathName = outputPath,
            targetGroup = targetGroup,
            target = target,
            options = options
        };

        BuildPipeline.BuildPlayer(buildOptions);
    }

    private static void PerformBuild(BuildTarget target, BuildTargetGroup targetGroup = BuildTargetGroup.WebGL, BuildOptions options = BuildOptions.None)
    {
        //string outputPath = networkMode == NetworkManager.Mode.LOCAL ? EditorUtility.SaveFolderPanel("Select Output Folder", "", "") : GetOutputPath(networkMode, target);
        string outputPath = GetOutputPath(target);

        if (string.IsNullOrEmpty(outputPath))
            return;

        // 🔹 Read version from environment variable
        string version = System.Environment.GetEnvironmentVariable("BUILD_VERSION");

        if (string.IsNullOrEmpty(version))
        {
            version = "0.0.1";
        }

        PlayerSettings.bundleVersion = version;

        if (targetGroup == BuildTargetGroup.WebGL)
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PerformBuildPlayer(outputPath, target);
            // CopyDirectory("ServerData/WebGL", Path.Combine(outputPath, OutputPathStreamingAssets, target.ToString()));
        }
        else if (targetGroup == BuildTargetGroup.Android)
        {
            BuildMobile(outputPath, target, targetGroup, BuildOptions.Development);
        }

        ShowOutputFolder(outputPath);
    }

    private static string GetOutputPath(BuildTarget target)
    {
        string outputPath = $"Build/{target}/AR-Magic-Lite";

        return outputPath;
    }

    [MenuItem("AR Magic Lite Tools/Build/Android (AAB) #B")]
    public static void AutoBuildAAB()
    {
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.applicationIdentifier = PackageName;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageName);
        // PlayerSettings.Android.useCustomKeystore = true;

        // Key store
        // PlayerSettings.Android.keystoreName = "./tcgamestudio.keystore";
        // PlayerSettings.Android.keystorePass = "abcd1234";

        // PlayerSettings.Android.keyaliasName = "tcgame";
        // PlayerSettings.Android.keyaliasPass = "abcd1234";

        BuildTarget target = BuildTarget.Android;
        string outputPath = GetOutputPath(target);
        PerformBuildPlayer(outputPath, target);
        ShowOutputFolder(outputPath);
    }

    [MenuItem("AR Magic Lite Tools/Build/Android (APK) #A")]
    public static void AutoBuildAPK()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageName);
        PlayerSettings.Android.useCustomKeystore = false;

        // Key store
        // PlayerSettings.Android.keystoreName = "./tcgamestudio.keystore";
        // PlayerSettings.Android.keystorePass = "abcd1234";

        // PlayerSettings.Android.keyaliasName = "tcgame";
        // PlayerSettings.Android.keyaliasPass = "abcd1234";

        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;

        BuildTarget target = BuildTarget.Android;
        string outputPath = GetOutputPath(target) + ".apk";
        PerformBuildPlayer(outputPath, target);
#if UNITY_EDITOR_WIN
        ShowOutputFolder(outputPath);
#endif
    }

    [MenuItem("AR Magic Lite Tools/Build/WebGL #W")]
    public static void AutoBuildWebGL()
    {
        BuildTarget target = BuildTarget.WebGL;
        PerformBuild(target, BuildTargetGroup.WebGL, BuildOptions.None);
    }
}