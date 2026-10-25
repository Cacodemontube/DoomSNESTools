using System;
using System.IO;
using System.Linq;
namespace DoomSNESed {
    public static class MusicTransitionTests {
        static void Check(bool value,string message){Bin.Require(value,"Music transitions: "+message);}
        static void Upload(byte[] ram,byte[] rom,int module){foreach(var block in AudioRom.Blocks(rom,module))Buffer.BlockCopy(block.Data,0,ram,block.Address,block.Data.Length);}
        public static void Verify(byte[] rom,int importedModule){
            Check(!MusicTransitions.NeedsRepair(rom),"all module loads restore shared tables");
            for(int module=1;module<=20;module++){
                var clean=AudioRom.Module(rom,module);var after=AudioRom.Module(rom,importedModule);Upload(after,rom,module);
                Check(after.Skip(MusicTransitions.Start).Take(MusicTransitions.Length).SequenceEqual(clean.Skip(MusicTransitions.Start).Take(MusicTransitions.Length)),"import -> retail module "+module+" leaves stale pointers");
                // Execute the original scene tunes from the resulting uploaded
                // memory, including all sample, envelope and sequence lookups.
                if(module==2||module==3){int rate;var reference=NativeAudio.Render(rom,clean,0,module-2,2,out rate);var actual=NativeAudio.Render(rom,after,0,module-2,2,out rate);Check(reference.Any(v=>v!=0),"original scene music audible");Check(reference.SequenceEqual(actual),"scene PCM differs after imported song, module "+module);}
            }
        }
        public static void Run(string path,string output){
            var rom=new Rom(path,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data"));
            var input=new ImportedMusic{Name="Mixed-table transition",Seconds=2};var settings=new MusicSettings();
            foreach(int channel in new[]{0,1}){settings.Assignments.Add(new MusicAssignment{Channel=channel,Patch=channel==0?45:56});input.Notes.Add(new MusicNote{Channel=channel,Key=60,Velocity=100,End=1});}
            MusicLibrary.Prepare(rom.Bytes,settings);Check(settings.Blocks!=null,"fixture must replace shared tables");
            rom.Project.RetailAssets=true;rom.Project.Audio.Music.Add(MusicImport.Convert(input,"E1M1",6,0,true,settings));
            byte[] built=rom.Compile();Verify(built,MusicTargets.Read(built,"E1M1")>>8);
            foreach(int module in Enumerable.Range(0,21))Check(AudioRom.Module(rom.Bytes,module).SequenceEqual(AudioRom.Module(built,module)),"original standalone module changed: "+module);
            Check(AssetDialog.FlatChoices(rom,true).First()==255&&AssetDialog.FlatChoices(rom,true).Count(id=>id==255)==1,"sky first in ceiling picker");
            Check(!AssetDialog.FlatChoices(rom,false).Contains(255),"sky excluded from unsupported floors");
            using(var preview=new AssetPreview()){preview.ShowFlat(rom,255);Check(preview.HasImage,"sky thumbnail has no image");}
            File.WriteAllText(Path.Combine(output,"music-transitions-results.txt"),"PASS: mixed-bank -> all 20 retail modules restores shared instrument/envelope/song tables; title and intermission PCM exactly matches clean loads; original modules unchanged; visible ceiling sky thumbnail.\r\n");
        }
    }
}
