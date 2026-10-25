using System;using System.Collections.Generic;using System.Linq;
namespace DoomSNESed {
    public static class GraphicStorage {
        public static void ReserveWindow(Rom rom,byte[] target,List<Section> free,ref int used,bool force=false){
            if(target.Length<0x400000||!force&&!rom.Project.Graphics.Any(a=>a.Screen==0&&(a.Sprite||a.Id>=4)))return;
            int oldTable=AudioRom.ChunkAddress(rom.Bytes);byte[] table=new byte[512*5];Buffer.BlockCopy(rom.Bytes,oldTable,table,0,(oldTable==AudioRom.Chunks?371:512)*5);
            // Move a few large CPU-uploaded sample chunks, byte for byte, out
            // of the Super FX window. Their SPC destinations stay unchanged.
            var chunks=Enumerable.Range(0,371).Select(i=>new{Id=i,Address=GraphicsAssets.Address(rom.Bytes,oldTable+i*5),Encoded=Bin.U16(rom.Bytes,oldTable+i*5+3)}).Select(c=>new{c.Id,c.Address,c.Encoded,Size=(c.Encoded&32768)!=0?(c.Encoded&32767)+2:c.Encoded}).Where(c=>(c.Encoded&32768)!=0&&c.Size>=4096&&c.Address>=0x10000&&c.Address+c.Size<0xed652).OrderByDescending(c=>c.Size).Take(3).ToArray();
            var reclaimed=new List<Section>();foreach(var c in chunks){int address=GraphicsAssets.Store(target,rom.Bytes.Skip(c.Address).Take(c.Size).ToArray(),free,ref used);Bin.W16(table,c.Id*5,address);table[c.Id*5+2]=GraphicsAssets.CpuBank(address);reclaimed.Add(new Section{Start=c.Address,Size=c.Size});}
            int tableAddress=GraphicsAssets.Store(target,table,free,ref used);foreach(int operand in new[]{0xefe0f,0xefe14,0xefe1c}){int offset=GraphicsAssets.Address(rom.Bytes,operand)-oldTable;Bin.W16(target,operand,tableAddress+offset);target[operand+2]=GraphicsAssets.CpuBank(tableAddress+offset);}int oldLists=AudioRom.ListAddress(rom.Bytes);int listAddress=GraphicsAssets.Store(target,rom.Bytes.Skip(oldLists).Take(AudioRom.ListEnd(rom.Bytes)-oldLists).ToArray(),free,ref used);foreach(int operand in new[]{0xefdd7,0xefdec,0xefe01}){Bin.W16(target,operand,listAddress);target[operand+2]=GraphicsAssets.CpuBank(listAddress);}free.AddRange(reclaimed);rom.Budget.Capacity+=reclaimed.Sum(s=>s.Size);
        }
        // Super FX wall/sprite reads address the original 2 MiB ROM window.
        // Expanded banks remain available for CPU-loaded screens and audio.
        internal static int End(byte[] bytes,NativeGraphic asset,int p){int y=0;while(y<asset.Height){Bin.Require(p+2<bytes.Length,"Invalid native graphic column.");int value=bytes[p++];if(asset.Sprite){if(value!=0){y++;continue;}int run=(sbyte)bytes[p++];if(run==0)break;y+=Math.Abs(run);if(run<0)p++;}else{int run=(sbyte)value;Bin.Require(run!=0,"Invalid wall column.");y+=Math.Abs(run);p+=run<0?1:run;}}if(asset.Sprite&&y>=asset.Height&&p+1<bytes.Length&&bytes[p]==0&&bytes[p+1]==0)p+=2;return p;}
        public static List<Section> Reclaim(Rom rom,List<GraphicReplacement> replacements=null,List<Section> retained=null){replacements=replacements??rom.Project.Graphics;var available=new bool[0x200000];var protectedBytes=new bool[0x200000];for(int sprite=0;sprite<2;sprite++)foreach(int id in sprite!=0?Enumerable.Range(0,232):Enumerable.Range(2,61).Select(i=>i*2)){
            var asset=GraphicsAssets.Describe(rom.Bytes,sprite!=0,id);bool replaced=replacements.Any(a=>a.Screen==0&&a.Sprite==asset.Sprite&&a.Id==id);
            for(int x=0;x<asset.Width;x++){int start=GraphicsAssets.Address(rom.Bytes,asset.Header+2+x*3),end=End(rom.Bytes,asset,start);for(int p=start;p<Math.Min(end,available.Length);p++)if(replaced)available[p]=true;else protectedBytes[p]=true;}
        }
        if(retained!=null)foreach(var keep in retained)for(int p=keep.Start;p<Math.Min(protectedBytes.Length,keep.Start+keep.Size);p++)protectedBytes[p]=true;
        var result=new List<Section>();for(int p=0;p<available.Length;){if(!available[p]||protectedBytes[p]){p++;continue;}int start=p;while(p<available.Length&&available[p]&&!protectedBytes[p]&&(p==start||(p&65535)!=0))p++;result.Add(new Section{Start=start,Size=p-start});}return result;}
    }
}


