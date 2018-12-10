using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Unity.MSBuild;
using UnityEditor.Compilation;

namespace Unity.CodeAnalysis
{
    public class CodeAnalyzerEditorWindow : EditorWindow
    {
        private static CodeAnalyzerEditorWindow s_window;
        private static string CODE_ANALYZER_DLLPATH_ASSEMBLY_EDITOR = "unity.codeanalyzer.dllpath.assemblyeditor";
        private static string CODE_ANALYZER_CSHARP_DLLPATH_ASSEMBLY_EDITOR = "unity.codeanalyzer.csharp.dllpath.assemblyeditor";
        private static string CODE_ANALYZER_ENABLED = "unity.codeanalyzer.enabled";

        private static string s_dllpath_assemblyeditor;
        private static string s_csharp_dllpath_assemblyeditor;
        private static bool? s_enabled;

        [MenuItem("CodeAnalysis/Refresh")]
        public static void OnCSProjectModified()
        {
            var projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            var solution = UnitySolution.Parse(projectPath);

            var projEditor = solution.AssemblyCSharpEditor;
            if (projEditor != null)
            {
                if (projEditor.AnalyzerItems.Count() == 0)
                {
                    bool modified = false;
                    var path = PlayerPrefs.GetString(CODE_ANALYZER_DLLPATH_ASSEMBLY_EDITOR);
                    if (!string.IsNullOrEmpty(path) &&
                        (File.Exists(path) || File.Exists(Path.Combine(Application.dataPath, path))))
                    {
                        projEditor.AddAnalyzer(new AnalyzerItem(path));
                        modified = true;
                    }
                    path = PlayerPrefs.GetString(CODE_ANALYZER_CSHARP_DLLPATH_ASSEMBLY_EDITOR);
                    if (!string.IsNullOrEmpty(path) &&
                        (File.Exists(path) || File.Exists(Path.Combine(Application.dataPath, path))))
                    {
                        projEditor.AddAnalyzer(new AnalyzerItem(path));
                        modified = true;
                    }
                    if (modified)
                    {
                        projEditor.WriteToFile();
//                        Debug.Log("Roslyn has been added to Editor.");
                    }
                }
            }
        }

        private void CheckParameters()
        {
            if (string.IsNullOrEmpty(s_dllpath_assemblyeditor))
                s_dllpath_assemblyeditor = PlayerPrefs.GetString(CODE_ANALYZER_DLLPATH_ASSEMBLY_EDITOR);
            if (string.IsNullOrEmpty(s_csharp_dllpath_assemblyeditor))
                s_csharp_dllpath_assemblyeditor = PlayerPrefs.GetString(CODE_ANALYZER_CSHARP_DLLPATH_ASSEMBLY_EDITOR);
            if (s_enabled == null) s_enabled = PlayerPrefs.GetInt(CODE_ANALYZER_ENABLED, 0) == 1;
        }

        [MenuItem("CodeAnalysis/Config")]
        public static void OpenWindow()
        {
            s_window = EditorWindow.GetWindow<CodeAnalyzerEditorWindow>();
            s_window.Show();
        }

        private void OnGUI()
        {
            CheckParameters();

            EditorGUI.BeginChangeCheck();
            s_enabled = EditorGUILayout.Toggle("Code Analysis enable", s_enabled.Value);
            if (EditorGUI.EndChangeCheck())
            {
                PlayerPrefs.SetInt(CODE_ANALYZER_ENABLED, s_enabled.Value ? 1 : 0);
            }

            GUI.enabled = false;
            s_dllpath_assemblyeditor = EditorGUILayout.TextField("AnalyzerEditor", s_dllpath_assemblyeditor);
            GUI.enabled = true;

            if (GUILayout.Button("Select"))
            {
                var filePath = EditorUtility.OpenFilePanelWithFilters("Select Analyzer dll", Application.dataPath,
                    new string[] {"analyzer", "dll"});
                if (!string.IsNullOrEmpty(filePath))
                {
                    var analyzerPath = GetRelativePath(Application.dataPath, filePath);
                    s_dllpath_assemblyeditor = analyzerPath;
                    PlayerPrefs.SetString(CODE_ANALYZER_DLLPATH_ASSEMBLY_EDITOR, s_dllpath_assemblyeditor);
                }
            }

            GUI.enabled = false;
            s_csharp_dllpath_assemblyeditor = EditorGUILayout.TextField("AnalyzerCSharpEditor", s_csharp_dllpath_assemblyeditor);
            GUI.enabled = true;

            if (GUILayout.Button("Select"))
            {
                var filePath = EditorUtility.OpenFilePanelWithFilters("Select Analyzer CSharp dll", Application.dataPath,
                    new string[] { "analyzer", "dll" });

                if (!string.IsNullOrEmpty(filePath))
                {
                    var analyzerPath = GetRelativePath(Application.dataPath, filePath);
                    s_csharp_dllpath_assemblyeditor = analyzerPath;
                    PlayerPrefs.SetString(CODE_ANALYZER_CSHARP_DLLPATH_ASSEMBLY_EDITOR, s_csharp_dllpath_assemblyeditor);
                }
            }
        }

        public static string GetRelativePath(string folder, string file)
        {
            return Uri.UnescapeDataString(new Uri(folder).MakeRelativeUri(new Uri(file)).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }
    }

#if UNITY_2017_3_OR_NEWER
    [InitializeOnLoad]
    public static class UnityCSProjectCompilationCallback
    {
        static UnityCSProjectCompilationCallback()
        {
            CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            CodeAnalyzerEditorWindow.OnCSProjectModified();
        }
    }
}
#else
    class UnityCSProjectAnalyzerPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Any(asset => asset.ToLower().EndsWith(".cs")) ||
                deletedAssets.Any(asset => asset.ToLower().EndsWith(".cs")) ||
                movedAssets.Any(asset => asset.ToLower().EndsWith(".cs")) ||
                movedFromAssetPaths.Any(asset => asset.ToLower().EndsWith(".cs")))
            {
                ModifyCSProject();
            }
        }

        private static void ModifyCSProject()
        {
            CodeAnalyzerEditorWindow.OnCSProjectModified();
        }
    }
#endif