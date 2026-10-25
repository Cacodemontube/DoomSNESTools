using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
namespace DoomSNESed {
    // Three 100 ms PCM buffers, produced off the UI thread. The native driver
    // remains alive between buffers; no song restart or resampling is involved.
    public sealed class SpcPreviewPlayer:IDisposable {
        [StructLayout(LayoutKind.Sequential,Pack=2)]struct Format {public ushort Tag,Channels;public uint Rate,Bytes;public ushort Align,Bits,Extra;}
        [StructLayout(LayoutKind.Sequential)]struct Header {public IntPtr Data;public uint Bytes,Recorded;public UIntPtr User;public uint Flags,Loops;public IntPtr Next;public UIntPtr Reserved;}
        [StructLayout(LayoutKind.Explicit,Size=12)]struct Position {[FieldOffset(0)]public uint Type;[FieldOffset(4)]public uint Value;}
        [DllImport("winmm.dll")]static extern int waveOutOpen(out IntPtr h,uint device,ref Format f,IntPtr callback,IntPtr instance,uint flags);
        [DllImport("winmm.dll")]static extern int waveOutPrepareHeader(IntPtr h,IntPtr header,int size);
        [DllImport("winmm.dll")]static extern int waveOutUnprepareHeader(IntPtr h,IntPtr header,int size);
        [DllImport("winmm.dll")]static extern int waveOutWrite(IntPtr h,IntPtr header,int size);
        [DllImport("winmm.dll")]static extern int waveOutPause(IntPtr h);
        [DllImport("winmm.dll")]static extern int waveOutRestart(IntPtr h);
        [DllImport("winmm.dll")]static extern int waveOutReset(IntPtr h);
        [DllImport("winmm.dll")]static extern int waveOutClose(IntPtr h);
        [DllImport("winmm.dll")]static extern int waveOutGetPosition(IntPtr h,ref Position p,int size);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr DoomAudioOpen(byte[] driver,int length,byte[] ram,int song);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern int DoomAudioRead(IntPtr h,int frames,[Out]short[] pcm,[Out]int[] levels,int capacity);
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern void DoomAudioClose(IntPtr h);
        readonly object sync=new object();readonly CancellationTokenSource cancel=new CancellationTokenSource();readonly bool mute;IntPtr output;Task playback;bool disposed;
        public SpcPreviewPlayer(bool mute=false){this.mute=mute;}
        public int[] Levels {get;private set;}public double Seconds {get;private set;}
        public double PlayedSeconds {get{lock(sync){if(output==IntPtr.Zero)return 0;var p=new Position{Type=2};if(waveOutGetPosition(output,ref p,12)!=0)return 0;return p.Type==2?p.Value/32000.0:p.Type==4?p.Value/128000.0:p.Value/1000.0;}}}
        static void Check(int code,string operation){if(code!=0)throw new InvalidOperationException(operation+" failed (Windows audio "+code+").");}
        public Task Play(byte[] driver,byte[] ram,int song,double seconds,Action started){
            lock(sync){
            if(disposed)throw new ObjectDisposedException("SpcPreviewPlayer");
            if(playback!=null)throw new InvalidOperationException("Create a new player for each preview.");
            Seconds=Math.Min(600,Math.Max(.02,seconds));Levels=new int[(int)Math.Ceiling(Seconds*50)*8];
            playback=Task.Run(delegate{IntPtr engine=IntPtr.Zero;var buffers=new IntPtr[3];var headers=new IntPtr[3];var prepared=new bool[3];int size=Marshal.SizeOf(typeof(Header));IntPtr device=IntPtr.Zero;
                try{
                    cancel.Token.ThrowIfCancellationRequested();engine=DoomAudioOpen(driver,driver.Length,ram,song);Bin.Require(engine!=IntPtr.Zero,"Native SPC preview could not start.");
                    var format=new Format{Tag=1,Channels=2,Rate=32000,Bytes=128000,Align=4,Bits=16};Check(waveOutOpen(out device,uint.MaxValue,ref format,IntPtr.Zero,IntPtr.Zero,0),"Open playback");
                    lock(sync){output=device;}Check(waveOutPause(device),"Buffer playback");
                    for(int i=0;i<3;i++){buffers[i]=Marshal.AllocHGlobal(3200*4);headers[i]=Marshal.AllocHGlobal(size);Marshal.StructureToPtr(new Header{Data=buffers[i],Bytes=3200*4},headers[i],false);Check(waveOutPrepareHeader(device,headers[i],size),"Prepare playback");prepared[i]=true;}
                    int frames=(int)(Seconds*32000),done=0,index=0,queued=0;bool playing=false;
                    while(done<frames){cancel.Token.ThrowIfCancellationRequested();var header=(Header)Marshal.PtrToStructure(headers[index],typeof(Header));
                        while((header.Flags&16)!=0&&(header.Flags&1)==0){cancel.Token.ThrowIfCancellationRequested();Thread.Sleep(3);header=(Header)Marshal.PtrToStructure(headers[index],typeof(Header));}
                        int count=Math.Min(3200,frames-done);var pcm=new short[count*2];var levels=new int[((count+639)/640)*8];
                        Bin.Require(DoomAudioRead(engine,count,pcm,levels,levels.Length)==count,"Native SPC streaming stopped.");
                        cancel.Token.ThrowIfCancellationRequested();
                        Array.Copy(levels,0,Levels,(done/640)*8,Math.Min(levels.Length,Levels.Length-(done/640)*8));if(mute)Array.Clear(pcm,0,pcm.Length);Marshal.Copy(pcm,0,buffers[index],pcm.Length);
                        // Windows owns the prepared header's flags and linkage.
                        // Reusing it must not copy a stale snapshot over them.
                        Marshal.WriteInt32(headers[index],IntPtr.Size,count*4);Check(waveOutWrite(device,headers[index],size),"Queue playback");
                        done+=count;queued++;index=(index+1)%3;
                        if(!playing&&(queued==3||done==frames)){Check(waveOutRestart(device),"Start playback");playing=true;if(started!=null)started();}
                    }
                    for(int i=0;i<3;i++)if(prepared[i])while((((Header)Marshal.PtrToStructure(headers[i],typeof(Header))).Flags&16)!=0&&(((Header)Marshal.PtrToStructure(headers[i],typeof(Header))).Flags&1)==0){cancel.Token.ThrowIfCancellationRequested();Thread.Sleep(3);}
                }finally{
                    lock(sync){if(device!=IntPtr.Zero){waveOutReset(device);output=IntPtr.Zero;for(int i=0;i<3;i++)if(prepared[i])waveOutUnprepareHeader(device,headers[i],size);waveOutClose(device);}}
                    for(int i=0;i<3;i++){if(headers[i]!=IntPtr.Zero)Marshal.FreeHGlobal(headers[i]);if(buffers[i]!=IntPtr.Zero)Marshal.FreeHGlobal(buffers[i]);}if(engine!=IntPtr.Zero)DoomAudioClose(engine);
                }
            });return playback;
            }
        }
        public void Dispose(){
            Task pending;lock(sync){if(!disposed){disposed=true;cancel.Cancel();}pending=playback;}
            // Only the rendering worker resets/closes the device. Wait outside
            // sync so it can return all prepared buffers before another preview
            // starts, and never reset a header concurrently with waveOutWrite.
            if(pending!=null&&Task.CurrentId!=pending.Id)try{pending.GetAwaiter().GetResult();}catch(OperationCanceledException){}catch(Exception){/* Play reports playback errors to its caller. */}
        }
    }
}
