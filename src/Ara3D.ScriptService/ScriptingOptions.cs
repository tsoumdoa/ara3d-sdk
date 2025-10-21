using System;
using Ara3D.Utils;

namespace Ara3D.ScriptService
{
    public class ScriptingOptions
    {
        public DirectoryPath ScriptsFolder { get; set; }
        public DirectoryPath LibrariesFolder { get; set; }
        public string AppName { get; set; }
        
        public ScriptingOptions(string appName, DirectoryPath scripts, DirectoryPath libraries)
        {
            AppName = appName;
            if (!scripts.Exists()) throw new Exception($"Scripts path does not exist {scripts}");
            ScriptsFolder = scripts;
            if (!libraries.Exists()) throw new Exception($"Libraries path does not exist {libraries}");
            LibrariesFolder = libraries;
        }
    }
}