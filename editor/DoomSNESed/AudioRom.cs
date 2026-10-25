using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace DoomSNESed {
    // USA retail offsets verified byte-for-byte against ArchiveBKUP MUSIC_I/L/T/O.C.
    // Module upload and six-byte turbo entries follow DoomFX musicdrv.a / spmus2.asm.
    public sealed class ApuBlock {public int Address,Chunk;public byte[] Data;}
    public sealed class NativeSound {public int Wave,Address,Bytes,Rate;public bool Turbo;public int[] Effects;public string Name;public override string ToString(){string[] labels={"","Blocked use / lost soul","Switch","Item pickup beep","Silent placeholder","Player pain","Door opening","Door closing","Punch","Chainsaw","Pistol","Monster activity","Monster death","Baron death","Baron alert","Imp alert","Cacodemon death","Cacodemon alert","Cyberdemon alert","Claw attack","Demon activity","Demon pain","Fireball launch","Fireball explosion","Player death","Former human death","Monster pain","Former human alert","Platform start","Platform stop","Demon death","Demon alert","Shotgun","Lost soul attack","Spiderdemon alert","Moving stone","Teleport"};return Wave.ToString("D2")+" - "+(Wave<labels.Length?labels[Wave]:Name)+" | "+(Turbo?"streamed":"resident");}}
    public static class AudioRom {
        public const int Index=0xeeb96,Lists=0xeebf6,Chunks=0xef572,Turbo=0xefcb1,SongTable=0x1f3782,BlastStart=0xb807;
        public static int ListAddress(byte[] b){return GraphicsAssets.Address(b,RomLayout.At(b,0xefdd7));} public static int ChunkAddress(byte[] b){return GraphicsAssets.Address(b,RomLayout.At(b,0xefe14));} public static int TurboAddress(byte[] b){return Bin.U16(b,RomLayout.At(b,0xefd71))+(b[RomLayout.At(b,0xefd74)]&63)*65536;}
        public static int ListEnd(byte[] b){int root=ListAddress(b),end=root;for(int m=0;m<48;m++){int p=root+Bin.U16(b,RomLayout.At(b,Index)+m*2);for(int g=0;g<256;g++){Bin.Require(p+2<=b.Length,"Invalid audio module list.");int dest=Bin.U16(b,p);p+=2;if(dest==0)break;int n=0;for(;n<512;n++){Bin.Require(p+2<=b.Length,"Truncated audio chunk list.");int id=Bin.S16(b,p);p+=2;if(id<0)break;}Bin.Require(n<512,"Unterminated audio chunk list.");}end=Math.Max(end,p);}Bin.Require(end-root<=65536,"Audio list table exceeds its bank.");return end;}
        public static int ChunkCount(byte[] b){int root=ListAddress(b),max=370;for(int m=0;m<48;m++){int p=root+Bin.U16(b,RomLayout.At(b,Index)+m*2);for(int g=0;g<256;g++){Bin.Require(p+2<=b.Length,"Invalid audio module list.");int dest=Bin.U16(b,p);p+=2;if(dest==0)break;for(int n=0;n<512;n++){Bin.Require(p+2<=b.Length,"Truncated audio chunk list.");int id=Bin.S16(b,p);p+=2;if(id<0)break;Bin.Require(id<512,"Invalid audio chunk index.");max=Math.Max(max,id);}}}return max+1;}
        public static List<ApuBlock> Blocks(byte[] rom,int module){Bin.Require(module>=0&&module<48,"Unknown native audio module.");int lists=ListAddress(rom),chunks=ChunkAddress(rom),p=lists+Bin.U16(rom,RomLayout.At(rom,Index)+module*2);var result=new List<ApuBlock>();for(int group=0;group<128;group++){Bin.Require(p>=lists&&p+2<=rom.Length,"Invalid audio chunk list.");int destination=Bin.U16(rom,p);p+=2;if(destination==0)return result;for(int count=0;count<512;count++){Bin.Require(p+2<=rom.Length,"Invalid audio chunk group.");int id=Bin.S16(rom,p);p+=2;if(id<0)break;Bin.Require(id<512,"Invalid native audio chunk.");int entry=chunks+id*5,address=GraphicsAssets.Address(rom,entry),size=Bin.U16(rom,entry+3);byte[] data=NativeCompression.DecodeMusicChunk(rom,address,size);Bin.Require(destination+data.Length<=65536,"Audio chunk exceeds SPC RAM.");result.Add(new ApuBlock{Address=destination,Chunk=id,Data=data});destination+=data.Length;}}throw new InvalidDataException("Audio chunk list has no terminator.");}
        public static byte[] Module(byte[] rom,int module){var apu=new byte[65536];foreach(ApuBlock block in Blocks(rom,0).Concat(module==0?Enumerable.Empty<ApuBlock>():Blocks(rom,module)))Buffer.BlockCopy(block.Data,0,apu,block.Address,block.Data.Length);return apu;}
        // Itemup 5/6/7 use BSN 2's envelope-selected wave 3; its nominal
        // waveform byte is zero. Native DSP execution confirms the resident
        // wave-3 directory address, rather than an editable wave-0 sample.
        public static List<NativeSound> Sounds(byte[] rom,string resources){byte[] turbo=NativeCompression.Decode(rom,TurboAddress(rom)),apu=Module(rom,0);string[] names=File.ReadAllLines(Path.Combine(resources,"AudioEffects.txt"));var sounds=new List<NativeSound>();int effects=Bin.U16(apu,0x1eee),instruments=Bin.U16(apu,0x1ee2);
            for(int wave=1;wave<=9;wave++){int selected=wave,start=Bin.U16(apu,0x1f00+wave*4)-3;var block=Blocks(rom,0).First(b=>start>=b.Address&&start<b.Address+b.Data.Length);int end=block.Address+block.Data.Length;for(int other=1;other<=9;other++){int next=Bin.U16(apu,0x1f00+other*4)-3;if(next>start)end=Math.Min(end,next);}for(int effect=0;effect<107;effect++){int next=Bin.U16(apu,effects+effect*2);if(next>start)end=Math.Min(end,next);}int[] aliases=Enumerable.Range(0,107).Where(i=>turbo[i*6]==0).Where(i=>{int fx=Bin.U16(apu,effects+i*2),sound=apu[fx+1],patch=Bin.U16(apu,instruments+sound*2);int waveId=apu[patch+2];if(i>=5&&i<=7&&sound==2&&waveId==0)waveId=3;return waveId==selected;}).ToArray();sounds.Add(new NativeSound{Wave=wave,Address=start,Bytes=end-start,Rate=11025,Turbo=false,Effects=aliases,Name=aliases.Length==0?"Resident sample "+wave:string.Join(" / ",aliases.Select(i=>names[i]).ToArray())});}
            foreach(var group in Enumerable.Range(0,107).Where(i=>turbo[i*6]!=0).GroupBy(i=>turbo[i*6])){int effect=group.First(),entry=effect*6;int address=GraphicsAssets.Address(turbo,entry+1),bytes=Bin.U16(turbo,entry+4);sounds.Add(new NativeSound{Wave=group.Key,Address=address,Bytes=bytes,Rate=11025,Turbo=true,Effects=group.ToArray(),Name=string.Join(" / ",group.Select(i=>names[i]).ToArray())});}return sounds;}
        // Some music uploads contain more than one waveform in a single chunk
        // (E1M2's wave 49 / claves follows wave 48). A directory entry need not
        // point at the start of an upload chunk.
        public static ApuBlock WaveBlock(byte[] apu,IEnumerable<ApuBlock> blocks,int wave){
            int start=Bin.U16(apu,0x1f00+wave*4)-3;
            var exact=blocks.FirstOrDefault(b=>b.Address==start);if(exact!=null)return exact;
            var block=blocks.FirstOrDefault(b=>start>=b.Address&&start+12<=b.Address+b.Data.Length);
            Bin.Require(block!=null,"Native waveform is outside its uploaded sample bank.");
            int end=start+3,limit=block.Address+block.Data.Length;
            do{Bin.Require(end+9<=limit,"Truncated native BRR waveform.");end+=9;}while((apu[end-9]&1)==0);
            return new ApuBlock{Address=start,Data=apu.Skip(start).Take(end-start).ToArray()};
        }
        public static byte[] Sample(byte[] rom,NativeSound sound){var data=new byte[sound.Bytes];Buffer.BlockCopy(sound.Turbo?rom:Module(rom,0),sound.Address,data,0,data.Length);return data;}
    }
}


