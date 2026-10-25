using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace DoomSNESed {
    public static class MusicRestartTests {
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr DoomAudioOpen(byte[] d,int n,byte[] ram,int song);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern int DoomAudioRead(IntPtr h,int n,[Out]short[] pcm,[Out]int[] levels,int capacity);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern int DoomAudioCommand(IntPtr h,int cmd,int arg);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern int DoomAudioState(IntPtr h,[Out]byte[] ram,[Out]byte[] dsp,[Out]int[] registers);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern void DoomAudioClose(IntPtr h);
        public static void Run(string path,string output){
            var rom=new Rom(path,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data"));
            var score=new ImportedMusic{Name="Mixed bank restart regression",Report="",Seconds=3};var settings=new MusicSettings();int[] patches={45,78,44,60,68,46,42,50};
            for(int channel=0;channel<8;channel++){
                settings.Assignments.Add(new MusicAssignment{Channel=channel,Patch=patches[channel]});
                for(int note=0;note<12;note++)score.Notes.Add(new MusicNote{Start=note*.25,End=note*.25+.2,Key=48+(note+channel)%24,Channel=channel,Velocity=90});
            }
            MusicLibrary.Prepare(rom.Bytes,settings);var song=MusicImport.Convert(score,"E1M1",8,0,true,settings);
            byte[] driver=NativeMusicGate.Driver(rom.Bytes),ram=MusicBank.PreviewRam(rom.Bytes,song);var engine=DoomAudioOpen(driver,driver.Length,ram,settings.Song);
            Bin.Require(engine!=IntPtr.Zero,"Restart regression could not start the driver.");
            try{
                var pcm=new short[64000];var actual=new byte[65536];var dsp=new byte[128];var registers=new int[4];
                for(int restart=0;restart<8;restart++){
                    for(int second=0;second<4;second++){
                        Bin.Require(DoomAudioRead(engine,32000,pcm,null,0)==32000,"Streaming render failed.");
                        Bin.Require(DoomAudioState(engine,actual,dsp,registers)==1,"Driver snapshot failed.");
                        Bin.Require(driver.Skip(NativeMusicGate.StopAddress-0x700).Take(30).SequenceEqual(actual.Skip(NativeMusicGate.StopAddress).Take(30)),"Music note-stop instructions were overwritten by runtime workspace.");
                        Bin.Require(driver.Skip(0x1e91-0x700).Take(31).SequenceEqual(actual.Skip(0x1e91).Take(31)),"Music timer instructions were overwritten.");
                        Bin.Require(pcm.Any(value=>value!=0),"Restart left music silent.");
                    }
                    Bin.Require(DoomAudioCommand(engine,36,0x7f01)==1,"Pause fade failed.");DoomAudioRead(engine,32000,pcm,null,0);
                    Bin.Require(DoomAudioCommand(engine,6,settings.Song)==1&&DoomAudioCommand(engine,18,0x7f01)==1,"Resume failed.");
                }
            }finally{DoomAudioClose(engine);}
            // A previously exported ROM must be repairable without a MIDI or
            // SNED: changing graphics alone must not retain the old bad driver.
            rom.Project.RetailAssets=true;rom.Project.Audio.Music.Add(song);byte[] legacy=rom.Compile();
            byte[] original=NativeCompression.Decode(rom.Bytes,0xed652),old=(byte[])driver.Clone();
            Buffer.BlockCopy(driver,NativeMusicGate.StopAddress-0x700,old,0x1024-0x700,30);
            Buffer.BlockCopy(original,NativeMusicGate.StopAddress-0x700,old,NativeMusicGate.StopAddress-0x700,30);Bin.W16(old,0x1c47-0x700,0x1024);
            byte[] packed=NativeCompression.Encode(old);Array.Clear(legacy,0xed652,0x1544);Buffer.BlockCopy(packed,0,legacy,0xed652,packed.Length);WadConverter.Checksum(legacy);
            string legacyPath=Path.Combine(output,"legacy-music-driver.sfc");File.WriteAllBytes(legacyPath,legacy);
            var repair=new Rom(legacyPath,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data"));repair.Project.RetailAssets=true;
            Bin.Require(NativeMusicGate.NeedsUpgrade(repair.Bytes),"Legacy driver was not recognized.");byte[] fixedRom=repair.Compile();
            Bin.Require(NativeCompression.Decode(fixedRom,0xed652).SequenceEqual(driver),"ROM-only export did not upgrade the legacy note-stop driver.");
            for(int module=0;module<48;module++)Bin.Require(AudioRom.Module(legacy,module).SequenceEqual(AudioRom.Module(fixedRom,module)),"Legacy repair changed music data.");
            File.WriteAllText(Path.Combine(output,"music-restarts-013-results.txt"),"PASS: eight mixed-bank voices, score loops and eight fade/stop/restart cycles; note-stop and timer machine code remain intact; rendering stays audible.\r\n");
        }
    }
}
