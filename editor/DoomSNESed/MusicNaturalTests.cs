using System;
using System.IO;
using System.Linq;
namespace DoomSNESed {
    public static class MusicNaturalTests {
        static void Check(bool valid,string message){Bin.Require(valid,"Natural music: "+message);}
        static double TailEnergy(short[] pcm){return Enumerable.Range(6400,6400).Sum(i=>(double)pcm[i*2]*pcm[i*2]+(double)pcm[i*2+1]*pcm[i*2+1]);}
        public static void Run(string path,string output){
            string folder=Path.Combine(output,"natural-music-014"),resources=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data");Directory.CreateDirectory(folder);var rom=new Rom(path,resources);
            var score=new ImportedMusic{Name="Native release",Report="",Seconds=2};score.Notes.Add(new MusicNote{Start=0,End=.1,Key=60,Velocity=100});
            var tight=new MusicSettings{NaturalRelease=false};tight.Assignments.Add(new MusicAssignment{Patch=44});
            var natural=new MusicSettings{NaturalRelease=true};natural.Assignments.Add(new MusicAssignment{Patch=44});int rate;
            var hard=MusicImport.Convert(score,"E1M1",1,0,false,tight);var soft=MusicImport.Convert(score,"E1M1",1,0,false,natural);
            var a=NativeAudio.Render(rom.Bytes,MusicBank.PreviewRam(rom.Bytes,hard),2,tight.Song,2,out rate);
            var b=NativeAudio.Render(rom.Bytes,MusicBank.PreviewRam(rom.Bytes,soft),2,natural.Song,2,out rate);
            Check(TailEnergy(b)>TailEnergy(a)*4,"native guitar release was still abruptly gated");
            NativeAudio.Render(rom.Bytes,MusicBank.PreviewRam(rom.Bytes,soft),4,natural.Song,4,out rate);Check(rate==0,"natural release leaked indefinitely");
            score.Notes.Add(new MusicNote{Start=.15,End=.25,Key=64,Velocity=100});natural.ReleaseMs=120;
            Check(MusicImport.Convert(score,"E1M1",2,0,false,natural).Voices==2,"extra tail protection did not use a spare voice");natural.ReleaseMs=0;
            Check(MusicImport.Convert(score,"E1M1",2,0,false,natural).Voices==1,"normal voice reuse duplicated a monophonic part");
            Check(MusicTargets.Read(rom.Bytes,MusicTargets.Title)==0x0200&&MusicTargets.Read(rom.Bytes,MusicTargets.Intermission)==0x0301,"original title/intermission references are incorrect");
            var title=MusicImport.Convert(score,MusicTargets.Title,6,0,true,natural);var inter=MusicImport.Convert(score,MusicTargets.Intermission,6,0,false,natural);inter.Settings=new MusicSettings{NaturalRelease=true};inter.Settings.Assignments.Add(new MusicAssignment{Patch=45});inter=MusicImport.Convert(score,MusicTargets.Intermission,6,0,false,inter.Settings);
            rom.Project.RetailAssets=true;rom.Project.CompactAssets=true;rom.Project.Audio.Music.Add(title);rom.Project.Audio.Music.Add(inter);var image=rom.Compile();
            Check(image.Skip(AudioRom.SongTable).Take(54).SequenceEqual(rom.Bytes.Skip(AudioRom.SongTable).Take(54)),"scene replacements changed level song assignments");
            int titleModule=MusicTargets.Read(image,MusicTargets.Title)>>8,interModule=MusicTargets.Read(image,MusicTargets.Intermission)>>8;
            Check(titleModule>=21&&interModule>titleModule,"scene replacements did not get separate modules");
            Check(AudioRom.Module(image,titleModule).SequenceEqual(MusicBank.PreviewRam(rom.Bytes,title))&&AudioRom.Module(image,interModule).SequenceEqual(MusicBank.PreviewRam(rom.Bytes,inter)),"scene uploads differ from preview");
            string scene=Path.Combine(folder,"title-intermission");File.WriteAllBytes(scene+".sfc",image);rom.SavePatch(scene+".dse",image);rom.SaveProject(scene+".sned");
            var reopen=new Rom(scene+".sfc",resources);reopen.Project.RetailAssets=true;reopen.Project.CompactAssets=true;reopen.Project.AllowExpandedAssets=true;reopen.Project.Audio.Music.Add(MusicImport.Convert(score,"E1M1",6,0,true,natural));var edited=reopen.Compile();
            Check((MusicTargets.Read(edited,MusicTargets.Title)>>8)==titleModule&&(MusicTargets.Read(edited,MusicTargets.Intermission)>>8)==interModule,"later level edit replaced a scene assignment");
            Check(AudioRom.Module(edited,titleModule).SequenceEqual(AudioRom.Module(image,titleModule))&&AudioRom.Module(edited,interModule).SequenceEqual(AudioRom.Module(image,interModule)),"later level edit overwrote a scene module");
            File.WriteAllText(Path.Combine(output,"natural-music-014-results.txt"),"PASS: native release tails and eventual silence; spare-voice protection; title/intermission modules, preview/ROM parity, level isolation and safe re-editing.\r\n");
        }
    }
}
