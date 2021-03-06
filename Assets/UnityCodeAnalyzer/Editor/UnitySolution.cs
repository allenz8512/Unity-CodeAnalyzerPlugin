using System;
using System.IO;

using UnityEngine;

namespace Unity.MSBuild
{
    public class UnitySolution
    {

        public UnityCSProject AssemblyCSharpEditor;

        public string SlnFile;
        private UnitySolution()
        {
        }

        public static UnitySolution Parse(string unitypath)
        {

            //check unity project;
            if (string.IsNullOrEmpty(unitypath))
            {
                return null;
            }
            var dirInfo = new DirectoryInfo(unitypath);

            var dirname = dirInfo.Name;
            var slnpath = Path.Combine(unitypath, dirname + ".sln");

            if (!File.Exists(slnpath))
            {
                return null;
            }

            string slnContent = File.ReadAllText(slnpath);

            bool isvs = true;
            if (slnContent.ToLower().Contains("assembly-csharp.csproj"))
            {
                isvs = false;
            }


            DirectoryInfo dirinfo = new DirectoryInfo(unitypath);
            if (!dirinfo.Exists) return null;

            var assetpath = Path.Combine(dirinfo.FullName, "Assets");
            if (!Directory.Exists(assetpath))
            {
                UnityEngine.Debug.Log("Invalid Unity project path");
                return null;
            }

            var csprojPathEditor = Path.Combine(dirinfo.FullName, dirinfo.Name + ".Editor.csproj");

            if (!isvs)
            {
                csprojPathEditor = Path.Combine(dirinfo.FullName, "Assembly-CSharp-Editor.csproj");

            }

            if (!File.Exists(csprojPathEditor))
            {
                UnityEngine.Debug.LogError("Missing " + csprojPathEditor);
            }

            var solution = new UnitySolution();

            solution.AssemblyCSharpEditor = UnityCSProject.Parse(csprojPathEditor);

            return solution;
        }
    }
}