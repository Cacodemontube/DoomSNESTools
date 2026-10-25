using System;
using System.IO;
namespace DoomSNESed {
    public static class GameIntegration {
        public static string FindRecomp(string toolsDirectory){foreach(string relative in new[]{"DoomSNESRecomp.exe",@"..\DoomSNESRecomp\build\DoomSNESRecomp.exe",@"..\..\DoomSNESRecomp\build\DoomSNESRecomp.exe"}){string path=Path.GetFullPath(Path.Combine(toolsDirectory,relative));if(File.Exists(path))return path;}return null;}
        public static string Recomp {get{return FindRecomp(AppDomain.CurrentDomain.BaseDirectory);}}
        public static string PatchDirectory {get{string exe=Recomp;return Path.Combine(exe==null?AppDomain.CurrentDomain.BaseDirectory:Path.GetDirectoryName(exe),"editor");}}
    }
}
