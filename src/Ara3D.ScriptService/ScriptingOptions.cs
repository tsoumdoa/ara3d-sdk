using System;
using Ara3D.Utils;

namespace Ara3D.ScriptService
{
    public class ScriptingOptions
    {
        public DirectoryPath ScriptsFolder { get; set; }
        public DirectoryPath LibrariesFolder { get; set; }
        
        public ScriptingOptions(DirectoryPath scripts, DirectoryPath libraries)
        {
            if (!scripts.Exists()) throw new Exception($"Scripts path does not exist {scripts}");
            ScriptsFolder = scripts;
            if (!libraries.Exists()) throw new Exception($"Libraries path does not exist {libraries}");
            LibrariesFolder = libraries;
        }
    }
}