using System;
using System.Collections.Generic;
using System.Linq;
namespace DoomSNESed {
    public static class ModifiedRom {
        // DoomFX USA loader/engine signatures, not ROM artwork or copyrighted assets.
        public static void Validate(byte[] bytes){Bin.Require(bytes.Length==0x200000||bytes.Length==0x400000,"Use a 2 MiB or 4 MiB Doom USA ROM (.sfc or headered .smc).");if(RomLayout.Detect(bytes))return;int[] starts={0xefd80,0x1fa000},lengths={48,64};string[] hashes={"43a8e09cbf2726e2e3e44ff952a0cb8b61f598f6e59fa882fc8c5f0672c5dca7","8bbd982d199a09e9d405cbcb92b9c016317d483875bbd85992598166f4b06b74"};for(int i=0;i<starts.Length;i++)Bin.Require(Bin.Hex(Bin.Hash(bytes.Skip(starts[i]).Take(lengths[i]).ToArray()))==hashes[i],"This ROM does not retain the supported Doom USA engine layout. Other regional revisions and incompatible engine hacks cannot be edited.");}
        static int EncodedEnd(byte[] b,int p){Bin.Require(p>=0&&p+6<=b.Length,"Asset pointer is outside the ROM.");int kind=Bin.U16(b,p);return p+(kind==0?4+Bin.U16(b,p+2):kind==4?4+Bin.U16(b,p+4):65536);}
        public static void ProtectSections(byte[] b,List<Section> sections){Action<int> protect=p=>{foreach(var s in sections.Where(s=>s.Size>0))if(p>=s.Start&&p<s.Start+s.Size)s.Size=p-s.Start;};
            foreach(int table in ScreenAssets.Tables.Skip(1))for(int row=0;row<(table==ScreenAssets.Tables[1]?3:4);row++)protect(GraphicsAssets.Address(b,table+row*16+2));
            foreach(int p in new[]{0x1f5d84,0x1ff08f,0x1ff5f3,0x1fa3cc,0x1fa3ec,0x1fa1a0,0x1fa1b0,0x1fa1c0})protect(GraphicsAssets.Address(b,p));
            for(int i=0;i<TextAssets.CreditCount;i++)protect(GraphicsAssets.Address(b,TextAssets.Credits+i*6+2));
            foreach(var line in CopyrightAssets.Read(b))protect(GraphicsAssets.Address(b,CopyrightAssets.Table+line.Record*6+2));int chunks=AudioRom.ChunkAddress(b);protect(AudioRom.ListAddress(b));protect(chunks);protect(AudioRom.TurboAddress(b));for(int i=0;i<AudioRom.ChunkCount(b);i++)protect(GraphicsAssets.Address(b,chunks+i*5));
            byte[] turbo=NativeCompression.Decode(b,AudioRom.TurboAddress(b));for(int i=0;i<107;i++)if(turbo[i*6]!=0)protect(GraphicsAssets.Address(turbo,i*6+1));
            for(int sprite=0;sprite<2;sprite++)foreach(int id in sprite!=0?Enumerable.Range(0,232):Enumerable.Range(2,61).Select(i=>i*2)){var asset=GraphicsAssets.Describe(b,sprite!=0,id);for(int x=0;x<asset.Width;x++)protect(GraphicsAssets.Address(b,asset.Header+2+x*3));}
        }
        public static List<Section> Expansion(byte[] b){int high=0x200000;if(b.Length>high){for(int i=Math.Min(b.Length,0x3e0000)-1;i>=high;i--)if(b[i]!=255){high=i+1;break;}}
            Action<int> keep=end=>{Bin.Require(end<=b.Length||b.Length==0x200000,"An existing asset exceeds the ROM.");if(end>0x200000)high=Math.Max(high,end);};
            foreach(int table in ScreenAssets.Tables.Skip(1))for(int row=0;row<(table==ScreenAssets.Tables[1]?3:4);row++){int entry=table+row*16,p=GraphicsAssets.Address(b,entry+2);if(p>=0x200000)keep((Bin.U16(b,entry)&0x4000)!=0?p+Bin.U16(b,entry+10):EncodedEnd(b,p));}
            foreach(int pointer in new[]{0x1f5d84,0x1ff08f,0x1ff5f3,0x1fa3cc,0x1fa3ec,0x1fa1a0,0x1fa1b0,0x1fa1c0}){int p=GraphicsAssets.Address(b,pointer);if(p>=0x200000)keep(EncodedEnd(b,p));}
            for(int i=0;i<TextAssets.CreditCount;i++){int p=TextAssets.Credits+i*6,a=GraphicsAssets.Address(b,p+2);if(a>=0x200000)keep(a+b[p+5]);}
            foreach(var line in CopyrightAssets.Read(b)){int p=CopyrightAssets.Table+line.Record*6,a=GraphicsAssets.Address(b,p+2);if(a>=0x200000)keep(a+b[p+5]);}int lists=AudioRom.ListAddress(b),chunks=AudioRom.ChunkAddress(b),count=AudioRom.ChunkCount(b);if(lists>=0x200000)keep(AudioRom.ListEnd(b));if(chunks>=0x200000)keep(chunks+(chunks==AudioRom.Chunks?371:512)*5);for(int i=0;i<count;i++){int p=chunks+i*5,a=GraphicsAssets.Address(b,p),size=Bin.U16(b,p+3);if(a>=0x200000)keep(a+((size&32768)!=0?(size&32767)+2:size));}int turbo=AudioRom.TurboAddress(b);if(turbo>=0x200000)keep(EncodedEnd(b,turbo));
            byte[] effects=NativeCompression.Decode(b,turbo);for(int i=0;i<107;i++)if(effects[i*6]!=0){int a=GraphicsAssets.Address(effects,i*6+1);if(a>=0x200000)keep(a+Bin.U16(effects,i*6+4));}
            for(int sprite=0;sprite<2;sprite++)foreach(int id in sprite!=0?Enumerable.Range(0,232):Enumerable.Range(2,61).Select(i=>i*2)){var asset=GraphicsAssets.Describe(b,sprite!=0,id);for(int x=0;x<asset.Width;x++){int p=GraphicsAssets.Address(b,asset.Header+2+x*3);if(p<0x200000)continue;int y=0;while(y<asset.Height){Bin.Require(p+2<b.Length,"Invalid modified texture column.");int v=b[p++];if(asset.Sprite){if(v!=0){y++;continue;}int run=(sbyte)b[p++];if(run==0)break;y+=Math.Abs(run);if(run<0)p++;}else{int run=(sbyte)v;Bin.Require(run!=0,"Invalid modified wall column.");y+=Math.Abs(run);p+=run<0?1:run;}}keep(p);}}
            int start=(high+65535)&~65535;var result=new List<Section>();for(int bank=start;bank<0x3e0000;bank+=65536)result.Add(new Section{Start=bank,Size=65536});return result;
        }
    }
}


