using System;using System.IO;using System.Linq;
namespace DoomSNESed {
    public static class PickupClavesTests {
        static void Check(bool value,string message){Bin.Require(value,"Pickup/claves: "+message);}
        public static void Run(string path,string output){
            Directory.CreateDirectory(output);
            string resources=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data");
            var rom=new Rom(path,resources);byte[] bytes=rom.Bytes;
            var beep=AudioRom.Sounds(bytes,resources).Single(s=>s.Wave==3);
            Check(beep.Effects.SequenceEqual(new[]{5,6,7}),"all three item pickup aliases exposed");
            Check(beep.ToString().Contains("Item pickup beep"),"clear sound-list name");
            foreach(int effect in beep.Effects){int rate;var pcm=NativeAudio.Render(bytes,NativeAudio.SoundRam(bytes,beep,effect,null),1,effect,.4,out rate);Check(rate>0&&pcm.Any(s=>s!=0),"native pickup alias audible: "+effect);}
            var bank=MusicBank.Read(bytes,5);
            Check(bank.Instruments.Any(i=>i.Id==56&&i.Wave==49),"E1M2 claves instrument discovered inside shared upload chunk");
            var apu=AudioRom.Module(bytes,5);var sample=AudioRom.WaveBlock(apu,AudioRom.Blocks(bytes,5),49);
            Check(sample.Address==33361&&sample.Data.Length==1443,"retail claves BRR boundaries");
            Check(!AudioRom.Blocks(bytes,5).Any(b=>b.Address==sample.Address),"test exercises embedded waveform");
            var input=new ImportedMusic{Name="GM claves",Report="",Seconds=1};
            input.Notes.Add(new MusicNote{Channel=9,Key=75,Velocity=100,Start=0,End=.2});
            var automatic=MusicBank.Automatic(bytes,input);
            Check(automatic.Assignments.Single().Patch==56,"GM percussion 75 maps to native claves");
            Check(NativeVoicing.DrumName(75)=="Claves"&&NativeVoicing.Pitch(input.Notes[0],56)==67,"claves name and native note");
            var song=MusicImport.Convert(input,"E1M1",6,0,false,automatic);int pitch;
            var pcmClave=NativeAudio.Render(bytes,MusicBank.PreviewRam(bytes,song),0,automatic.Song,1,out pitch);
            Check(pitch==14250&&pcmClave.Any(s=>s!=0),"matches E1M2's observed DSP pitch 1824/4096 * 32000");
            File.WriteAllBytes(Path.Combine(output,"midi-claves.wav"),NativeAudio.Wave(pcmClave));
            // Force relocation into a different home bank rather than merely
            // playing the original E1M2 bank with its existing directory.
            var mixed=new MusicSettings{Bank=4};mixed.Assignments.Add(new MusicAssignment{Channel=9,Key=75,Patch=56});
            MusicLibrary.Prepare(bytes,mixed);Check(mixed.Blocks!=null,"embedded sample relocated for mixed-bank playback");
            var relocated=MusicImport.Convert(input,"E1M1",6,0,false,mixed);
            var movedRam=MusicBank.PreviewRam(bytes,relocated);
            int patch=Bin.U16(movedRam,Bin.U16(movedRam,0x1ee2)+56*2),wave=movedRam[patch+2];
            int movedStart=Bin.U16(movedRam,0x1f00+wave*4)-3;
            Check(movedRam.Skip(movedStart).Take(sample.Data.Length).SequenceEqual(sample.Data),"relocated claves BRR data is byte-identical");
            var moved=NativeAudio.Render(bytes,movedRam,0,mixed.Song,1,out pitch);
            Check(pitch==14250&&moved.Any(s=>s!=0),"relocated claves retains native pitch and audibility");
            rom.Project.RetailAssets=true;rom.Project.CompactAssets=true;rom.Project.Audio.Music.Add(relocated);
            var image=rom.Compile();Check(AudioRom.Module(image,MusicTargets.Read(image,"E1M1")>>8).SequenceEqual(MusicBank.PreviewRam(bytes,relocated)),"claves ROM upload equals preview");
            var tone=new short[128];for(int i=0;i<tone.Length;i++)tone[i]=(short)(Math.Sin(i*2*Math.PI/16)*16000);
            string wav=Path.Combine(output,"pickup-replacement.wav");File.WriteAllBytes(wav,WavImport.WaveFile(tone,11025));
            var replacement=WavImport.Convert(wav,beep,false,false,false,false);replacement.Effect=5;
            var changed=new Rom(path,resources);changed.Project.RetailAssets=true;changed.Project.CompactAssets=true;changed.Project.Audio.Sounds.Add(replacement);
            var changedBytes=changed.Compile();int rate2;
            var preview=NativeAudio.Render(bytes,NativeAudio.SoundRam(bytes,beep,5,replacement.Data),1,5,.4,out pitch);
            var exported=NativeAudio.Render(changedBytes,AudioRom.Module(changedBytes,0),1,5,.4,out rate2);
            Check(pitch==rate2&&preview.SequenceEqual(exported),"pickup replacement preview equals exported ROM playback");
            File.WriteAllText(Path.Combine(output,"results.txt"),"PASS pickup aliases 5/6/7, native playback, embedded E1M2 claves, GM75 mapping, exact pitch, relocation, ROM/preview parity.\r\n");
        }
    }
}
