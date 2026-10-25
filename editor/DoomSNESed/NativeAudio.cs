using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace DoomSNESed {
    public static class NativeAudio {
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern int DoomAudioRenderLevels(byte[] driver,int length,byte[] ram,int mode,int id,int frames,[Out]short[] pcm,out int rate,[Out]int[] levels,int capacity);
        public static short[] RenderLevels(byte[] rom,byte[] ram,int id,double seconds,out int[] levels){int frames=Math.Max(1,(int)(Math.Min(600,seconds)*32000));var pcm=new short[frames*2];levels=new int[((frames+639)/640)*8];byte[] driver=NativeMusicGate.Driver(rom);int rate;int result=DoomAudioRenderLevels(driver,driver.Length,ram,0,id,frames,pcm,out rate,levels,levels.Length);Bin.Require(result==frames,"Native SPC level preview could not start ("+result+").");return pcm;}
        [DllImport("DoomSNESAudio.dll",CallingConvention=CallingConvention.Cdecl)]static extern int DoomAudioRender(byte[] driver,int length,byte[] ram,int mode,int id,int frames,[Out]short[] pcm,out int rate);
        public static short[] Render(byte[] rom,byte[] ram,int mode,int id,double seconds,out int rate){int frames=Math.Max(1,(int)(Math.Min(600,seconds)*32000));var pcm=new short[frames*2];byte[] driver=NativeMusicGate.Driver(rom);int result=DoomAudioRender(driver,driver.Length,ram,mode,id,frames,pcm,out rate);Bin.Require(result==frames,"Native SPC preview could not start ("+result+"). Keep DoomSNESAudio.dll beside the converter.");return pcm;}
        public static byte[] Wave(short[] pcm){using(var ms=new MemoryStream())using(var w=new BinaryWriter(ms)){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+pcm.Length*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)2);w.Write(32000);w.Write(128000);w.Write((short)4);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(pcm.Length*2);foreach(short s in pcm)w.Write(s);return ms.ToArray();}}
        public static byte[] SoundRam(byte[] rom,NativeSound slot,int effect,byte[] replacement){byte[] ram=AudioRom.Module(rom,0),data=replacement??AudioRom.Sample(rom,slot);int start=slot.Turbo?AudioRom.BlastStart:slot.Address;Buffer.BlockCopy(data,0,ram,start,data.Length);if(slot.Turbo||replacement!=null){Bin.W16(ram,0x1f00+slot.Wave*4,start+3);Bin.W16(ram,0x1f00+slot.Wave*4+2,start+3+Bin.U16(data,0));}return ram;}
    }
}
