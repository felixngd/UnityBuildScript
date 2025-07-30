using Google;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using Voidex.BuildPipeline;
using Environment = Voidex.BuildPipeline.Environment;

public static class CustomBuilder
{
    #region Build

    public static void Build()
    {
        PerformBuild();
        BeforeBuild();
        // This was set during CI pipeline
        Console.WriteLine($"LOG::: Build Target {EditorUserBuildSettings.activeBuildTarget}");
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.iOS:
                BuildiOS();
                break;
            case BuildTarget.Android:
                BuildAndroid();
                break;
            default: throw new Exception("Not support this build target " + EditorUserBuildSettings.activeBuildTarget);
        }

        void BuildAndroid()
        {
            BuildAndroidProduction();
        }

        void BuildiOS()
        {
            BuildXcodeProject();
        }

        AfterBuild();
    }

    private static void BuildAndroidInternalProduction()
    {
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        AddScriptingDefineSymbol("PRODUCTION");
        RemoveScriptingDefineSymbol("DEVELOPMENT"); // Someone might accidentally add this
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.symlinkSources = false;
        EditorUserBuildSettings.androidCreateSymbolsZip = false;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        SwitchScriptingImplement(ScriptingImplementation.IL2CPP);

        var report = BuildPipeline.BuildPlayer(
            GetEnabledScenes(), GetAndroidBuildPath(".apk"), BuildTarget.Android, BuildOptions.None);
        var code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
        EditorApplication.Exit(code);
    }

    private static void AfterBuild()
    {
    }

    public static void BuildAndroidProduction()
    {
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        AddScriptingDefineSymbol("PRODUCTION");
        RemoveScriptingDefineSymbol("DEVELOPMENT"); // Someone might accidentally add this
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.symlinkSources = false;
        EditorUserBuildSettings.androidCreateSymbolsZip = true;
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        PlayerSettings.Android.useAPKExpansionFiles = true;
        SwitchScriptingImplement(ScriptingImplementation.IL2CPP);

        var report = BuildPipeline.BuildPlayer(
            GetEnabledScenes(), GetAndroidBuildPath(".aab"), BuildTarget.Android, BuildOptions.None);
        var code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
        EditorApplication.Exit(code);
    }

    public static void BuildAndroidDevelopment()
    {
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;

        RemoveScriptingDefineSymbol("PRODUCTION");
        AddScriptingDefineSymbol("DEVELOPMENT");
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.symlinkSources = false;
        EditorUserBuildSettings.androidCreateSymbolsZip = false;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        PlayerSettings.Android.useAPKExpansionFiles = false;

        SwitchScriptingImplement(ScriptingImplementation.IL2CPP);
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = GetEnabledScenes();
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.locationPathName = GetAndroidBuildPath();

        var report = BuildPipeline.BuildPlayer(GetEnabledScenes(), GetAndroidBuildPath(), BuildTarget.Android, BuildOptions.None);
        //var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
        EditorApplication.Exit(code);
    }

    public static void BuildXcodeProject()
    {
        AddScriptingDefineSymbol("PRODUCTION");
        RemoveScriptingDefineSymbol("DEVELOPMENT"); // Someone might accidentally add this
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.symlinkSources = true;
        SwitchScriptingImplement(ScriptingImplementation.IL2CPP);
        var report = BuildPipeline.BuildPlayer(GetEnabledScenes(), GetXcodeFolder(), BuildTarget.iOS, BuildOptions.None);
        var code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
        EditorApplication.Exit(code);
    }

    public static void BuildXcodeProjectDevelopment()
    {
        AddScriptingDefineSymbol("DEVELOPMENT");
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.symlinkSources = false;
        SwitchScriptingImplement(ScriptingImplementation.IL2CPP);
        var report =
            BuildPipeline.BuildPlayer(GetEnabledScenes(), GetXcodeFolder(), BuildTarget.iOS, BuildOptions.None);
        var code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
        EditorApplication.Exit(code);
    }

    private static void SwitchScriptingImplement(ScriptingImplementation target)
    {
        if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != target)
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, target);
    }

    public static void AddScriptingDefineSymbol(params string[] defines)
    {
        var definesString =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        var allDefines = definesString.Split(';').ToList();
        foreach (var define in defines)
            if (!allDefines.Contains(define))
            {
                Debug.Log($"LOG::: Add {define} from Define Symbols");
                allDefines.Add(define);
            }
        //allDefines.AddRange(defines);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
            string.Join(";", allDefines.ToArray()));
    }

    public static void RemoveScriptingDefineSymbol(params string[] defines)
    {
        var definesString =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        var allDefines = definesString.Split(';').ToList();
        foreach (var define in defines)
            if (allDefines.Contains(define))
            {
                Debug.Log($"LOG::: remove {define} from Define Symbols");
                allDefines.Remove(define);
            }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
            string.Join(";", allDefines.ToArray()));
    }

    #endregion
    public static void PrebuildProcessor(string buildNumber)
    {
        var types = TypeCache.GetTypesDerivedFrom<ICustomPrebuild>();

        foreach (var type in types)
        {
            ICustomPrebuild instance = (ICustomPrebuild)Activator.CreateInstance(type);
            instance.SetUp(buildNumber);
            Console.WriteLine("Detect prebuild: " + type.ToString());
        }
    }
    #region Environment

    public static void CleanUpDeletedScenes()
    {
        var currentScenes = EditorBuildSettings.scenes;
        EditorBuildSettings.scenes = currentScenes.Where(ebss => AssetDatabase.LoadAssetAtPath(ebss.path, typeof(SceneAsset)) != null).ToArray();
    }

    private static void BeforeBuild()
    {
        CleanUpDeletedScenes();

        KeyStore();

        UnityEditor.U2D.SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget, false);

        AssetDatabase.SaveAssets();

        SetupBuildVersion();
        SetupBuildBundleVersion();

        var config = GetBuildConfig();
        var environment = Configs[ConfigKey.Env];
        switch (environment)
        {
            case "UAT":
                config.SetEnvironment(Environment.UAT, Configs[ConfigKey.ScriptDefineSymbols]);
                break;
            case "Production":
                config.SetEnvironment(Environment.Production, Configs[ConfigKey.ScriptDefineSymbols]);
                break;
            case "Staging":
                config.SetEnvironment(Environment.Staging, Configs[ConfigKey.ScriptDefineSymbols]);
                break;
        }

        AssetDatabase.SaveAssets();
        BuildAddressable();
    }
    public static ProjectBuildConfiguration GetBuildConfig()
    {
        //find build config
        var buildConfig = AssetDatabase.FindAssets("t:ProjectBuildConfiguration")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ProjectBuildConfiguration>)
            .FirstOrDefault();

        if (buildConfig == null)
            throw new Exception("Cannot find build config");

        return buildConfig;
    }

    private static void KeyStore()
    {
    }

    private static void BuildAddressable()
    {
        AddressableAssetSettings.CleanPlayerContent(); // Clean everything
        Console.WriteLine("LOG::: Start Build addressable");

        // There is issue where Addressable in batch mode. Ask for confirm scene modified and stop build.
        // You can check log and see that addressable build time = 0
        AssetDatabase.SaveAssets();
        AddressableAssetSettings.BuildPlayerContent();
        Console.WriteLine("LOG::: Build addressable Done");
    }

    private static void SetupBuildBundleVersion()
    {
        try
        {
            var baseBuildCount = int.Parse(Configs[ConfigKey.BuildNumber]);
            PlayerSettings.Android.bundleVersionCode = baseBuildCount;
            PlayerSettings.iOS.buildNumber = (baseBuildCount).ToString();
            PrebuildProcessor(Configs[ConfigKey.BuildNumber]);
        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR::: MISSING Environment for Build versioning");
            Console.WriteLine(e);
        }
    }

    private static void SetupBuildVersion()
    {
        Console.WriteLine("LOG::: Found release version in config");
        PlayerSettings.bundleVersion = Configs[ConfigKey.AppVersion];
    }

    #endregion

    #region Naming build folder

    private static string[] GetEnabledScenes()
    {
        return (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
    }

    // File name is simple. Only when archive/moving file. It change name using shell
    public static string GetAndroidBuildPath(string extension = ".apk")
    {
        return Path.Combine(Configs[ConfigKey.OutputPath], Configs[ConfigKey.OutputFileName] + "." + Configs[ConfigKey.OutputExtension]);
        //var projectDir = new DirectoryInfo(Application.dataPath).Parent;
        //var time = DateTime.Now; // Use build time as name not git Commit date time. Due to sometime parse failed
        //var branch = Configs["GIT_BRANCH"];
        //branch = ReplaceInvalidChars(branch);
        //var buildName = $"{PlayerSettings.productName.Replace(" ", "").Replace(":", "")}--{time:yyyy-MM-dd--HH-mm}--{branch}";

        //AppendConfig("BUILD_FILE_NAME", buildName);
        //AppendConfig("BUILD_FILE_SYMBOLS", $"{buildName}-{PlayerSettings.bundleVersion}-v{PlayerSettings.Android.bundleVersionCode}-IL2CPP.symbols");
        //AppendConfig("BUILD_FILE_SHRINK_SYMBOLS", $"{buildName}-{PlayerSettings.bundleVersion}-v{PlayerSettings.Android.bundleVersionCode}-IL2CPP.symbols.shrink");

        //AppendConfig("BUILD_FILE_MAPPING", $"{buildName}_mapping");
        //AppendConfig("BUNDLE_VERSION_CODE", $"{PlayerSettings.Android.bundleVersionCode}");

        //buildName = $"{buildName}" + extension;
        //var path = Path.Combine(projectDir.FullName, "build", "Android", buildName);
        //Console.WriteLine("BUILD PATH: " + path);
        //return path;
    }

    public static string ReplaceInvalidChars(string filename)
    {
        return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
    }

    public static string GetXcodeFolder()
    {
        return Configs[ConfigKey.OutputPath];
        //var projectDir = new DirectoryInfo(Application.dataPath).Parent;
        //var path = Path.Combine(projectDir.FullName, "build", "iOS");
        //Debug.Log(path);
        //Console.WriteLine("BUILD PATH: " + path);
        //return path;
    }

    #endregion

    #region Configs

    private static Dictionary<string, string> _configs;

    public static Dictionary<string, string> Configs
    {
        get
        {
            if (_configs != null)
                return _configs;
            else
                _configs = new();
            return _configs;
        }
    }

    private static void AppendConfig(string key, string value)
    {
        var projectDir = new DirectoryInfo(Application.dataPath).Parent;
        var configFile = Path.Combine(projectDir.FullName, "config.cfg");
        try
        {
            File.AppendAllLines(configFile, new[] { $"{key}={value}" });
        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR::: Broken config files");
            Console.WriteLine(e);
        }
    }

    #endregion

    private static void PerformBuild()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        //log all arguments joined by space
        Debug.Log(string.Join(" ", args));

        for (int i = 0; i < args.Length - 1; i++)
        {
            Configs[args[i]] = args[i + 1];
        }


        try
        {
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }
        catch (Exception)
        {
        }
    }

    public class ConfigKey
    {
        public const string BuildTarget = "-buildTarget";
        public const string OutputPath = "-outputPath";
        public const string Il2CppCodeGen = "-il2cppCodegen";
        public const string XCodeBuildConfig = "-xcodeBuildConfig";
        public const string CustomSceneToBuild = "-customScenesToBuild";
        public const string XCodeBuildAppend = "-buildXcodeAppend";
        public const string ScriptDefineSymbols = "-scriptDefinedSymbols";
        public const string AndroidCreateSymbols = "-androidCreateSymbols";
        public const string SplitApplicationBinary = "-splitApplicationBinary";
        public const string BuildAppBundle = "-buildAppBundle";
        public const string ExportProject = "-exportProject";
        public const string BuildServer = "-buildServer";
        public const string OutputExtension = "-outputExtension";
        public const string ScriptingBackend = "-scriptingBackend";
        public const string Env = "-env";
        public const string AppVersion = "-appversion";
        public const string BuildNumber = "-buildNumber";
        public const string Configuration = "-configuration";
        public const string OutputFileName = "-outputFileName";
    }
}
