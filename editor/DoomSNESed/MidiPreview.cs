using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
namespace DoomSNESed {
    public sealed class MidiPreview:IDisposable {
        [DllImport("winmm.dll")]static extern int midiOutOpen(out IntPtr device,int id,IntPtr callback,IntPtr instance,int flags);
        [DllImport("winmm.dll")]static extern int midiOutShortMsg(IntPtr device,int message);
        [DllImport("winmm.dll")]static extern int midiOutReset(IntPtr device);
        [DllImport("winmm.dll")]static extern int midiOutClose(IntPtr device);
        IntPtr device;readonly CancellationTokenSource cancel=new CancellationTokenSource();bool closed;
        public MidiPreview(){Bin.Require(midiOutOpen(out device,-1,IntPtr.Zero,IntPtr.Zero,0)==0,"Windows MIDI synthesizer could not be opened.");}
        public async Task Play(ImportedMusic music,double seconds,MusicSettings settings=null){var events=music.Notes.Where(n=>n.Start<seconds&&!MusicBank.Muted(settings,n)).SelectMany(n=>new[]{new{Time=n.Start,Note=n,On=true},new{Time=Math.Min(seconds,n.End),Note=n,On=false}}).OrderBy(e=>e.Time).ThenBy(e=>e.On?1:0);var watch=Stopwatch.StartNew();try{foreach(var e in events){int delay=(int)((e.Time-watch.Elapsed.TotalSeconds)*1000);if(delay>0)await Task.Delay(delay,cancel.Token);if(cancel.IsCancellationRequested||closed)return;int channel=e.Note.Channel;if(e.On&&channel!=9)midiOutShortMsg(device,0xc0|channel|(e.Note.Program<<8));midiOutShortMsg(device,(e.On?0x90:0x80)|channel|(e.Note.Key<<8)|((e.On?e.Note.Velocity:0)<<16));}}catch(OperationCanceledException){}finally{if(!closed)midiOutReset(device);}}
        public void Dispose(){if(closed)return;closed=true;cancel.Cancel();midiOutReset(device);midiOutClose(device);device=IntPtr.Zero;}
    }
}
