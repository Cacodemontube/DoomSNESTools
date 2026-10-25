using System;using System.IO;using System.Linq;using System.Drawing;using System.Collections.Generic;
namespace DoomSNESed {
    public static class ModifiedLayoutTests {
        static void Check(bool value,string message){Bin.Require(value,"Modified layouts: "+message);}
        public static void Run(string retailPath,string output){string resources=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data"),folder=Path.Combine(output,"layouts-015");Directory.CreateDirectory(folder);
            foreach(string path in new[]{retailPath,Path.Combine(Path.GetDirectoryName(retailPath),"RL5934.sfc")}){
                if(!File.Exists(path))continue;var rom=new Rom(path,resources);bool linked=RomLayout.SourceBuilt(rom.Bytes);string name=linked?"source":"retail";
                Check(rom.Compile().SequenceEqual(rom.Bytes),name+" unchanged round trip");Check(rom.Project.Maps.Count==(linked?1:22),name+" linked map slots");
                foreach(var asset in HudAssets.All(rom))using(var image=HudAssets.Preview(rom,asset.Screen,null))Check(image.Width==asset.Width&&image.Height==asset.Height,"native HUD dimensions");
                foreach(int id in Enumerable.Range(0,232))GraphicsAssets.Decode(rom.Bytes,GraphicsAssets.Describe(rom.Bytes,true,id));foreach(int id in Enumerable.Range(0,63).Select(i=>i*2))GraphicsAssets.Decode(rom.Bytes,GraphicsAssets.Describe(rom.Bytes,false,id));
                var lines=CopyrightAssets.Read(rom.Bytes);Check(lines.Count==18,"copyright entry count");Check(CopyrightAssets.Encode("©®™").SequenceEqual(new byte[]{127,128,129}),"special glyph encoding");lines[0].Text="TEST © 2026";lines[0].Column=2;lines[0].Row=1;lines[1].Text="";
                rom.Project.RetailAssets=true;rom.Project.CompactAssets=true;rom.Project.Texts.Add(new TextReplacement{Id=6,Lines=lines});
                // Real music and streamed sample replacement exercise relocated operands.
                if(linked){var score=new ImportedMusic{Name="Source layout test",Seconds=2,Report=""};score.Notes.Add(new MusicNote{Start=0,End=.5,Key=60,Velocity=100});var song=MusicImport.Convert(score,MusicTargets.Title,6,0,false,new MusicSettings());rom.Project.Audio.Music.Add(song);var sample=AudioRom.Sounds(rom.Bytes,resources).First(s=>s.Wave==10);rom.Project.Audio.Sounds.Add(new SoundReplacement{Wave=10,Data=WavImport.Encode(Enumerable.Range(0,512).Select(i=>(short)(Math.Sin(i*.2)*4000)).ToArray(),false),Rate=11025,Name="test"});}
                byte[] built=rom.Compile();Check(built.Length==0x200000,"edits stay 2 MiB");Check(RomLayout.SourceBuilt(built)==linked,"working image retains layout");string saved=Path.Combine(folder,name);File.WriteAllBytes(saved+".sfc",built);rom.SaveProject(saved+".sned");if(!linked)rom.SavePatch(saved+".dse",built);
                var reopen=new Rom(saved+".sfc",resources);var reread=CopyrightAssets.Read(reopen.Bytes);Check(reread[0].Text=="TEST © 2026"&&reread[0].Column==2&&reread[0].Row==1&&string.IsNullOrWhiteSpace(reread[1].Text),"copyright saved ROM reopen");
                using(var preview=CopyrightAssets.Preview(reopen,null,new List<GraphicReplacement>())){Check(preview.GetPixel(0,0).ToArgb()==Color.Black.ToArgb(),"copyright background black");preview.Save(saved+"-copyright.png");}
                reopen.Project.RetailAssets=true;reopen.Project.CompactAssets=true;byte[] second=reopen.Compile();Check(CopyrightAssets.Read(second)[0].Text==reread[0].Text,"copyright survives second build");
                if(linked){Check(MusicTargets.Read(built,MusicTargets.Title)>>8>=21,"source title music redirected");Check(AudioRom.Module(built,MusicTargets.Read(built,MusicTargets.Title)>>8).SequenceEqual(MusicBank.PreviewRam(rom.Bytes,rom.Project.Audio.Music[0])),"source music preview matches ROM");Check(reopen.Project.Maps[0].Vertices.Count==770,"custom source map preserved");var projectRom=new Rom(path,resources);projectRom.LoadProject(saved+".sned");Check(projectRom.Compile().SequenceEqual(built),"source project round trip");}
            }
            File.WriteAllText(Path.Combine(output,"layouts-015-results.txt"),"PASS retail/source layout round trips; complete sprite/wall and HUD decoding; copyright glyphs, export, reimport, preview and second build; source audio relocation and project round trip.\r\n");
        }
    }
}
