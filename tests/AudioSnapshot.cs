using System;using System.IO;using System.Linq;using System.Text;using DoomSNESed;
class AudioSnapshot {
    static string Digest(short[] pcm){byte[] bytes=new byte[pcm.Length*2];Buffer.BlockCopy(pcm,0,bytes,0,bytes.Length);return Bin.Hex(Bin.Hash(bytes));}
    static int Main(string[] args){try{
        if(args.Length!=2)throw new Exception("Usage: AudioSnapshot ROM output.txt");
        string data=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data");var rom=new Rom(args[0],data);var report=new StringBuilder();int rate;
        for(int bank=4;bank<=6;bank++){var pcm=NativeAudio.Render(rom.Bytes,AudioRom.Module(rom.Bytes,bank),0,bank-2,1,out rate);if(!pcm.Any(v=>v!=0))throw new Exception("Silent retail music");report.AppendLine("bank "+bank+" "+rate+" "+Digest(pcm));}
        var input=new ImportedMusic{Name="Standalone comparison",Seconds=2,Report=""};input.Notes.Add(new MusicNote{Start=0,End=.7,Channel=0,Program=0,Key=60,Velocity=100});input.Notes.Add(new MusicNote{Start=.2,End=.5,Channel=9,Key=38,Velocity=100});var settings=MusicBank.Automatic(rom.Bytes,input);var song=MusicImport.Convert(input,"E1M1",6,0,false,settings);var converted=NativeAudio.Render(rom.Bytes,MusicBank.PreviewRam(rom.Bytes,song),0,settings.Song,2,out rate);report.AppendLine("converted music "+rate+" "+Digest(converted));
        var slot=AudioRom.Sounds(rom.Bytes,data).Single(s=>s.Wave==10);var replacement=WavImport.Encode(Enumerable.Range(0,1024).Select(i=>(short)(Math.Sin(i*.2)*5000)).ToArray(),false);var ram=NativeAudio.SoundRam(rom.Bytes,slot,slot.Effects[0],replacement);var sound=NativeAudio.Render(rom.Bytes,ram,1,slot.Effects[0],1,out rate);if(!sound.Any(v=>v!=0))throw new Exception("Silent replacement sound");report.AppendLine("replacement sound "+rate+" "+Digest(sound));File.WriteAllText(args[1],report.ToString());Console.WriteLine(report);return 0;
    }catch(Exception e){Console.WriteLine(e);return 1;}}
}
