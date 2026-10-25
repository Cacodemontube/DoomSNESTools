using System;
using System.IO;
using System.Linq;
namespace DoomSNESed {
    public static class MusicVoiceTests {
        static void Check(bool valid,string message){Bin.Require(valid,message);}
        public static void Run(string path,string output){
            var rom=new Rom(path,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data"));
            var score=new ImportedMusic{Name="Voice capacity regression",Report="",Seconds=3};
            var settings=new MusicSettings();
            for(int channel=0;channel<8;channel++){
                score.Notes.Add(new MusicNote{Start=0,End=2,Channel=channel,Program=30,Key=48+channel*3,Velocity=100});
                settings.Assignments.Add(new MusicAssignment{Channel=channel,Program=30,Patch=44});
            }
            foreach(int count in new[]{6,8}){
                var song=MusicImport.Convert(score,"E1M1",count,0,false,settings);AudioBuild.ValidateMusic(song);
                Check(song.Voices==count,"Requested polyphony was lost.");int active;
                NativeAudio.Render(rom.Bytes,MusicBank.PreviewRam(rom.Bytes,song),4,settings.Song,1,out active);
                Check(active==count,"Native driver did not play every requested voice.");
            }
            var dense=new ImportedMusic{Name="SPC capacity pressure",Report="",Seconds=180};
            for(int note=0;note<900;note++)for(int channel=0;channel<6;channel++)
                dense.Notes.Add(new MusicNote{Start=note*.2,End=note*.2+.18,Channel=channel,Program=30,Key=48+(note+channel)%24,Velocity=90});
            var fitted=MusicOptimize.Fit(dense,"E1M1",6,0,false,settings,false);
            Check(fitted.Voices==6&&fitted.VoiceBudget==6,"Strict fitting silently reduced the voice budget.");
            Check(fitted.Seconds<dense.Seconds&&fitted.Report.Contains("shortened"),"Strict fitting did not disclose duration loss.");
            var solo=new ImportedMusic{Name="Solo",Report="",Seconds=3};solo.Notes.Add(score.Notes[0]);
            Check(MusicImport.Convert(solo,"E1M1",8,0,false,settings).Voices==1,"Sparse music must not duplicate notes to fill voices.");
            bool rejected=false;try{MusicImport.Convert(score,"E1M1",9,0,false,settings);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"More than eight hardware voices must be rejected.");
            File.WriteAllText(Path.Combine(output,"music-voices-012-results.txt"),"PASS: six/eight simultaneous native DSP voices; strict fitting preserves polyphony and reports shortening; sparse scores and hardware limit.\r\n");
        }
    }
}
