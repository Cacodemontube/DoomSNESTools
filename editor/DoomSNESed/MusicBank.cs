using System;
using System.Collections.Generic;
using System.Linq;
namespace DoomSNESed {
    public sealed class MusicAssignment {public int Key=-1;public int Channel,Program,Patch,Transpose,Priority;public bool Mute;}
    public sealed class MusicSettings {public bool NaturalRelease=true;public int ReleaseMs=0;public int Bank=4,Origin=0x8fe9,Song=2,Grid=50,VelocitySteps=8;public List<ApuBlock> Blocks;public List<MusicAssignment> Assignments=new List<MusicAssignment>();}
    public sealed class NativeInstrument {public int Id{get;set;}public int Wave{get;set;}public string Name{get;set;}public override string ToString(){return Id+" - "+Name+" (wave "+Wave+")";}}
    public sealed class NativeMusicBank {
        public int Module,Origin,Song;public string Name;public List<NativeInstrument> Instruments=new List<NativeInstrument>();
        public int Capacity {get{return 0xb490-Origin;}}
        public override string ToString(){return Name+" | "+Capacity+" sequence bytes";}
    }
    public static class MusicBank {
        public static void PreserveMutes(MusicSettings before,MusicSettings after){if(before==null||after==null)return;foreach(var a in after.Assignments){var old=before.Assignments.FirstOrDefault(n=>n.Channel==a.Channel&&n.Program==a.Program&&n.Key==a.Key);if(old!=null){a.Mute=old.Mute;a.Priority=old.Priority;}}}
        public static bool Muted(MusicSettings settings,MusicNote note){return settings!=null&&settings.Assignments.Any(a=>a.Mute&&a.Channel==note.Channel&&a.Program==note.Program&&(a.Key<0||a.Key==note.Key));}
        public static string ProgramName(int p){if(p==47)return "Timpani (GM 48)";if(p==117)return "Melodic tom (GM 118)";string[] families={"Piano","Chromatic percussion","Organ","Guitar","Bass","Strings","Ensemble","Brass","Reed","Pipe","Synth lead","Synth pad","Synth effects","Ethnic","Percussion","Sound effects"};string[] guitars={"Nylon guitar","Steel guitar","Jazz guitar","Clean guitar","Muted guitar","Overdriven guitar","Distortion guitar","Guitar harmonics"};if(p>=24&&p<32)return guitars[p-24];return p>=0&&p<128?families[p/8]+" / GM "+(p+1):"Percussion";}
        static string Name(int id,int wave){return NativeVoicing.Name(id);}
        public static NativeMusicBank Read(byte[] rom,int module){Bin.Require(module>=3&&module<=20,"Choose an original level/intermission instrument bank.");byte[] apu=AudioRom.Module(rom,module);var blocks=AudioRom.Blocks(rom,module);int origin=blocks.Last().Address,dir=Bin.U16(apu,0x1ee2),songs=Bin.U16(apu,0x1efa);int song=module-2;origin=Bin.U16(apu,songs+song*2);Bin.Require(origin>=blocks.Last().Address&&origin<0xb490,"Invalid original song entry.");var result=new NativeMusicBank{Module=module,Origin=origin,Song=song,Name=module==3?"Intermission bank":"Bank "+module+" / "+string.Join(", ",Enumerable.Range(0,27).Where(i=>(Bin.U16(rom,RomLayout.At(rom,AudioRom.SongTable)+i*2)>>8)==module&&Bin.U16(rom,RomLayout.At(rom,0x1f33b6)+i*2)>0).Select(i=>"E"+(i/9+1)+"M"+(i%9+1)).ToArray())};int definitions=blocks.Where(b=>Enumerable.Range(37,91).Any(w=>Bin.U16(apu,0x1f00+w*4)==b.Address+3)).Max(b=>b.Address+b.Data.Length);for(int id=39;id<100;id++){int pointer=Bin.U16(apu,dir+id*2);if(pointer<definitions||!blocks.Any(b=>pointer>=b.Address&&pointer+6<=b.Address+b.Data.Length)||pointer+6>origin)continue;int wave=apu[pointer+2];if(wave<37||wave>127||apu[pointer+5]!=0||!blocks.Any(b=>b.Address+3<=Bin.U16(apu,0x1f00+wave*4)&&Bin.U16(apu,0x1f00+wave*4)+9<=b.Address+b.Data.Length))continue;result.Instruments.Add(new NativeInstrument{Id=id,Wave=wave,Name=Name(id,wave)});}Bin.Require(result.Instruments.Count>0,"Native bank has no instruments.");return result;}
        public static List<NativeMusicBank> All(byte[] rom){return Enumerable.Range(3,18).Select(m=>Read(rom,m)).ToList();}
        public static int Closest(NativeMusicBank bank,MusicNote note){return NativeVoicing.Closest(bank,note);}
        public static MusicSettings Automatic(byte[] rom,ImportedMusic input){var groups=input.Notes.GroupBy(n=>new{n.Channel,n.Program,Key=n.Channel==9?n.Key:-1}).ToArray();var banks=All(rom).Where(b=>groups.All(g=>b.Instruments.Any(i=>NativeVoicing.Percussion(i.Id)==(g.Key.Channel==9)))).ToList();var best=banks.OrderBy(b=>groups.Sum(g=>{return g.Sum(n=>Math.Max(.01,n.End-n.Start))*NativeVoicing.Penalty(b,g.First());})).ThenByDescending(b=>b.Capacity).First();var settings=new MusicSettings{Bank=best.Module,Origin=best.Origin,Song=best.Song};foreach(var group in groups){var note=group.First();int exact=NativeVoicing.RequiredPatch(note);settings.Assignments.Add(new MusicAssignment{Channel=group.Key.Channel,Program=group.Key.Program,Key=group.Key.Key,Patch=exact!=0?exact:Closest(best,note)});}MusicLibrary.Prepare(rom,settings);return settings;}
        public static byte[] PreviewRam(byte[] rom,MusicReplacement music){var settings=music.Settings??new MusicSettings();var bank=Read(rom,settings.Bank);byte[] apu=AudioRom.Module(rom,settings.Blocks==null?bank.Module:0);if(settings.Blocks!=null)foreach(var block in settings.Blocks)Buffer.BlockCopy(block.Data,0,apu,block.Address,block.Data.Length);Array.Clear(apu,settings.Origin,0xb490-settings.Origin);Buffer.BlockCopy(music.Sequence,0,apu,settings.Origin,music.Sequence.Length);return apu;}
    }
}



