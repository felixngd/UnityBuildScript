using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Voidex.BuildPipeline
{
    [CreateAssetMenu(fileName = "ProjectBuildConfiguration", menuName = "Build Pipeline/Build Configuration")]
    public class ProjectBuildConfiguration : ScriptableObject
    {
        public Environment environment;

        public string[] stagingScriptingDefineSymbols;
        public string[] productionScriptingDefineSymbols;
        public string[] uatScriptingDefineSymbols;

        private string _additionalScriptingDefineSymbols;

        public void Reload()
        {
            //separate scripting define symbols by ; then add them to the list
            stagingScriptingDefineSymbols =
                stagingScriptingDefineSymbols.Select(x => x.Split(";")).SelectMany(x => x).ToArray();
            productionScriptingDefineSymbols = productionScriptingDefineSymbols.Select(x => x.Split(";"))
                .SelectMany(x => x).ToArray();
            uatScriptingDefineSymbols =
                uatScriptingDefineSymbols.Select(x => x.Split(";")).SelectMany(x => x).ToArray();

            switch (environment)
            {
                case Environment.Staging:
                    string[] stagingSymbols = stagingScriptingDefineSymbols;

                    if (!string.IsNullOrEmpty(_additionalScriptingDefineSymbols) &&
                        _additionalScriptingDefineSymbols != "NONE")
                    {
                        stagingSymbols = stagingSymbols.Concat(_additionalScriptingDefineSymbols.Split(";")).ToArray();
                    }

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, stagingSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, stagingSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, stagingSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, stagingSymbols);

                    Debug.Log("Staging symbols: " + string.Join(";", stagingSymbols));
                    break;
                case Environment.Production:
                    string[] productionSymbols = productionScriptingDefineSymbols;

                    if (!string.IsNullOrEmpty(_additionalScriptingDefineSymbols) &&
                        _additionalScriptingDefineSymbols != "NONE")
                    {
                        productionSymbols = productionSymbols.Concat(_additionalScriptingDefineSymbols.Split(";"))
                            .ToArray();
                    }

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, productionSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, productionSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, productionSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, productionSymbols);

                    Debug.Log("Production symbols: " + string.Join(";", productionSymbols));
                    break;
                case Environment.UAT:
                    var uatSymbols = uatScriptingDefineSymbols;
                    if (!string.IsNullOrEmpty(_additionalScriptingDefineSymbols) &&
                        _additionalScriptingDefineSymbols != "NONE")
                    {
                        uatSymbols = uatSymbols.Concat(_additionalScriptingDefineSymbols.Split(";")).ToArray();
                    }

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, uatSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, uatSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, uatSymbols);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, uatSymbols);

                    Debug.Log("UAT symbols: " + string.Join(";", uatSymbols));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //save the asset
            AssetDatabase.SaveAssets();
        }
        
        public void SetEnvironment(Environment env, string additionalSymbols = "")
        {
            this.environment = env;
            _additionalScriptingDefineSymbols = additionalSymbols;
            Reload();
        }
    }
    
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ProjectBuildConfiguration))]
    public class ExampleScriptableObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProjectBuildConfiguration scriptableObject = (ProjectBuildConfiguration)target;

            if (GUILayout.Button("Apply Scripting Define Symbols"))
            {
                scriptableObject.Reload();
            }
        }
    }
#endif
}