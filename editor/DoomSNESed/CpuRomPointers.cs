using System;using System.Collections.Generic;using System.Linq;
namespace DoomSNESed {
    public static class CpuRomPointers {
        static bool Fix(byte[] bytes,int p){if(bytes[p+2]>=0x60&&bytes[p+2]<=0x7d){bytes[p+2]+=0x80;return true;}return false;}
        public static void Normalize(Rom rom,byte[] target,List<Section> free){
            if(target.Length<=0x200000)return;
            foreach(int table in ScreenAssets.Tables.Skip(1))for(int row=0;row<(table==ScreenAssets.Tables[1]?3:4);row++)Fix(target,table+row*16+2);
            foreach(int p in new[]{0x1f5d84,0x1ff08f,0x1ff5f3,0x1fa3cc,0x1fa3ec,0x1fa1a0,0x1fa1c0,0x1fa1b0})Fix(target,p);
            for(int i=0;i<TextAssets.CreditCount;i++)Fix(target,TextAssets.Credits+i*6+2);
            foreach(var line in CopyrightAssets.Read(target))Fix(target,CopyrightAssets.Table+line.Record*6+2);
            foreach(int p in new[]{0xefdd7,0xefdec,0xefe01,0xefe0f,0xefe14,0xefe1c})Fix(target,p);
            int chunks=AudioRom.ChunkAddress(target),count=AudioRom.ChunkCount(target);for(int i=0;i<count;i++)Fix(target,chunks+i*5);
            int turbo=AudioRom.TurboAddress(target);byte[] data=NativeCompression.Decode(target,turbo);bool changed=false;for(int i=0;i<107;i++)if(data[i*6]!=0)changed|=Fix(data,i*6+1);
            if(changed){int used=0;byte[] packed=NativeCompression.EncodeLiterals(data);int address=GraphicsAssets.Store(target,packed,free,ref used);Bin.W16(target,0xefd71,address);target[0xefd74]=GraphicsAssets.CpuBank(address);rom.Budget.Music+=used;}
            else if(target[0xefd74]>=0x60&&target[0xefd74]<=0x7d)target[0xefd74]+=0x80;
        }
    }
}

