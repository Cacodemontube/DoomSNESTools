using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
namespace DoomSNESed {
    public static class MusicPreviewTests {
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr DoomAudioOpen(byte[] driver,int length,byte[] ram,int song);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern int DoomAudioRead(IntPtr engine,int frames,[Out]short[] pcm,[Out]int[] levels,int capacity);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern void DoomAudioClose(IntPtr engine);
        static void Check(bool value,string message){Bin.Require(value,"Music preview: "+message);}
        static byte[] RomBytes(string path){return new Rom(path,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data")).Bytes;}
        public static void Run(string path,string output){
            Directory.CreateDirectory(output);byte[] rom=RomBytes(path),driver=NativeMusicGate.Driver(rom);
            // A dominant melodic part selects a home bank without claves. The
            // percussion must still use E1M2's sample rather than a bank fallback.
            foreach(int program in new[]{0,24,32,56,80,88}){
                var input=new ImportedMusic{Name="Mixed GM claves",Seconds=20};
                input.Notes.Add(new MusicNote{Channel=0,Program=program,Key=60,Velocity=100,End=20});
                input.Notes.Add(new MusicNote{Channel=9,Key=75,Velocity=100,End=.1});
                var settings=MusicBank.Automatic(rom,input);
                Check(settings.Assignments.Single(a=>a.Channel==9).Patch==56,"mixed GM "+program+" retains native claves");
                Check(settings.Blocks!=null||MusicBank.Read(rom,settings.Bank).Instruments.Any(i=>i.Id==56),"claves sample included in upload");
                var music=MusicOptimize.Fit(input,"E1M1",6,0,true,settings);
                Check(music.Settings.Assignments.Single(a=>a.Channel==9).Patch==56,"automatic fitting preserves claves");
            }
            // A fresh session must not inherit PCM/APU state from previous songs.
            // Chunked streaming and a single read must produce identical samples.
            for(int pass=0;pass<24;pass++){
                int bank=4+pass%3,song=bank-2;byte[] ram=AudioRom.Module(rom,bank);
                IntPtr whole=DoomAudioOpen(driver,driver.Length,ram,song),stream=DoomAudioOpen(driver,driver.Length,ram,song);
                try{Check(whole!=IntPtr.Zero&&stream!=IntPtr.Zero,"independent sessions open");
                    var expected=new short[19200];Check(DoomAudioRead(whole,9600,expected,null,0)==9600,"reference render");
                    var actual=new short[19200];for(int block=0;block<3;block++){var pcm=new short[6400];Check(DoomAudioRead(stream,3200,pcm,null,0)==3200,"stream render");Array.Copy(pcm,0,actual,block*6400,pcm.Length);}
                    Check(expected.SequenceEqual(actual),"fresh and interleaved streaming PCM match, pass "+pass);
                }finally{if(whole!=IntPtr.Zero)DoomAudioClose(whole);if(stream!=IntPtr.Zero)DoomAudioClose(stream);}
            }
            File.WriteAllText(Path.Combine(output,"music-preview-results.txt"),"PASS: six mixed GM banks retain E1M2 claves through automatic fitting; 24 interleaved fresh SPC sessions produce exact chunked/reference PCM.\r\n");
        }
        // Separate from the offline suite because this needs a Windows audio device.
        public static void RunPlayback(string path,string output){
            byte[] rom=RomBytes(path),driver=NativeMusicGate.Driver(rom);
            for(int pass=0;pass<40;pass++){
                var player=new SpcPreviewPlayer(true);Task task;
                using(var started=new ManualResetEventSlim()){
                    int bank=4+pass%3;task=player.Play(driver,AudioRom.Module(rom,bank),bank-2,60,()=>started.Set());
                    if(pass%3!=0){if(!started.Wait(5000)){task.GetAwaiter().GetResult();throw new Exception("Preview did not start");}if(pass%3==2)Thread.Sleep(120);}
                    player.Dispose();Check(task.IsCompleted,"Stop waits for buffer and device cleanup, pass "+pass);
                    try{task.GetAwaiter().GetResult();}catch(OperationCanceledException){}
                    player.Dispose();bool rejected=false;try{player.Play(driver,AudioRom.Module(rom,bank),bank-2,1,null);}catch(ObjectDisposedException){rejected=true;}
                    Check(rejected,"disposed player cannot reopen a device");
                }
                if(pass%5==0){GC.Collect();GC.WaitForPendingFinalizers();}
            }
            using(var player=new SpcPreviewPlayer(true))player.Play(driver,AudioRom.Module(rom,4),2,.35,null).GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(output,"music-preview-playback-results.txt"),"PASS: 40 Windows audio start/stop cycles, immediate cancellation, queued-buffer cancellation, repeated disposal, GC pressure and natural completion. Playback is muted for these checks.\r\n");
        }
    }
}
