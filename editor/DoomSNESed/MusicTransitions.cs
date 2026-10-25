using System;
using System.Linq;
using System.Collections.Generic;
namespace DoomSNESed {
    public static class MusicTransitions {
        // Mixed instrument banks replace these shared directories, including
        // envelope and song pointers. Retail modules upload only their deltas.
        public const int Start=0x1f94,Length=0x25ec-Start;
        public static byte[] Baseline(byte[] rom){return AudioRom.Module(rom,0).Skip(Start).Take(Length).ToArray();}
        public static IEnumerable<int> Modules(byte[] rom){
            return Enumerable.Range(1,20).Concat(Enumerable.Range(0,27).Select(i=>Bin.U16(rom,RomLayout.At(rom,AudioRom.SongTable)+i*2)>>8))
                .Concat(new[]{MusicTargets.Read(rom,MusicTargets.Title)>>8,MusicTargets.Read(rom,MusicTargets.Intermission)>>8}).Where(m=>m>0&&m<48).Distinct();
        }
        public static bool HasMixedModules(byte[] rom){
            return Modules(rom).Any(m=>AudioRom.Blocks(rom,m).Any(b=>b.Address==Start&&b.Data.Length==Length));
        }
        public static bool NeedsRepair(byte[] rom){
            if(!HasMixedModules(rom))return false;
            byte[] baseline=Baseline(rom);
            return Modules(rom).Any(m=>{var b=AudioRom.Blocks(rom,m).FirstOrDefault();return b==null||b.Address!=Start||!b.Data.SequenceEqual(baseline);});
        }
    }
}
