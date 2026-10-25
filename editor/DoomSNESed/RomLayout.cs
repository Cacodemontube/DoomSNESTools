using System;using System.Collections.Generic;using System.Linq;using System.Runtime.CompilerServices;
namespace DoomSNESed {
    // Link locations verified against ArchiveBKUP's rllevels, musicdrv, legal,
    // rlweapons and SystemText formats. Addresses describe metadata, never artwork.
    public static class RomLayout {
        sealed class Profile {public Dictionary<int,int> Points=new Dictionary<int,int>();}
        static readonly ConditionalWeakTable<byte[],Profile> profiles=new ConditionalWeakTable<byte[],Profile>();
        public static bool SourceBuilt(byte[] b){Profile p;return profiles.TryGetValue(b,out p);}
        public static int At(byte[] b,int address){Profile p;if(!profiles.TryGetValue(b,out p))return address;int value;if(p.Points.TryGetValue(address,out value))return value;
            if(address>=0xed5cd&&address<0xeffbb)return address+0x10019;
            if(address>=0x1f312e&&address<0x1f38ac)return address-0x1f312e+0x115552;
            if(address>=0x1f5ec6&&address<0x1f64a0)return address-0x1f5ec6+0x11a426;
            return address;
        }
        public static void Inherit(byte[] source,byte[] target){Profile p;if(profiles.TryGetValue(source,out p)){profiles.Remove(target);profiles.Add(target,p);}}
        static bool Match(byte[] b,int p,params byte[] bytes){return p>=0&&p+bytes.Length<=b.Length&&bytes.Select((v,i)=>v==b[p+i]).All(v=>v);}
        public static bool Detect(byte[] b){
            if(!Match(b,0x115552,0xad,0xd1,6,0x48,0x0a,0xaa,0x4b,0xab,0xbd,0xa6,0x5b,0x8f,0xbf,6,0)||!Match(b,0x120000,0x22,0xca,0xff,0x49,0x22,0xd2,0x12,0x52))return false;
            var p=new Profile();Action<int,int> add=(a,v)=>p.Points[a]=v;
            add(0x1a8000,0xb8000);add(0x1b0000,0xab770);add(0x1c1358,0xf8603);
            add(0x1fef6b,0x11f4f2);add(0x1f5811,0x119d71);
            add(0x1ff03d,0x11f5b9);add(0x1f5d94,0x11a2f4);add(0x1f5dd6,0x11a336);add(0x1f5e18,0x11a378);
            add(0x1f5d84,0x11a2e4);add(0x1ff08f,0x11f60b);add(0x1ff5f3,0x120083);
            add(0x1fa3cc,0x1197ba);add(0x1fa3ec,0x1197da);add(0x1fa1a0,0x11958e);add(0x1fa1b0,0x11959e);add(0x1fa1c0,0x1195ae);
            add(0x1ff0a8,0x11f624);add(0x1ff603,0x120093);add(0x1f150e,0x6e04);
            int[] oldImages={0x1ffbe1,0x15feac,0x14fd54,0x18fe8e,0x1ffce8,0x1ff7d7,0x11fe29,0x17febf};
            int[] oldMisc={0x1ffbf1,0x15febc,0x14fd6c,0x18fe9e,0x1ffcf8,0x1ff7ef,0x11fe39,0x17fecb};
            int[] oldChars={0x1ffcdc,0x15ffdd,0x14ffe2,0x18ffd9,0x1ffde0,0x1ff933,0x11ff8d,0x17ffdc};
            int[] oldTiles={0x1df63f,0x1f711d,0x1ecaed,0x1f238e,0x1fbe88,0x1f4b66,0x1f649d,0x1fb468};
            for(int i=0;i<8;i++){int bank=b[0xf7df8+i*2]&63,tileBank=b[0xf7df9+i*2]&63;add(oldImages[i],bank*65536+Bin.U16(b,0xf7e08+i*2));add(oldChars[i],bank*65536+Bin.U16(b,0xf7e28+i*2));add(oldMisc[i],bank*65536+Bin.U16(b,0xf7e48+i*2));add(oldTiles[i],tileBank*65536+Bin.U16(b,0xf7e68+i*2));}
            profiles.Remove(b);profiles.Add(b,p);return true;
        }
        public static int WeaponBytes(byte[] b,int weapon,int fallback){return SourceBuilt(b)?Bin.U16(b,0xf7e78+weapon*2):fallback;}
        public static List<Section> Free(byte[] b){int end=Math.Min(b.Length,0x200000),start=end;while(start>0x12130a&&(b[start-1]==0||b[start-1]==255))start--;
            Action<int> keep=n=>{Bin.Require(n<=end,"An existing source-ROM asset exceeds the Super FX window.");start=Math.Max(start,n);};
            int chunks=AudioRom.ChunkAddress(b);keep(AudioRom.ListEnd(b));keep(chunks+(chunks==At(b,AudioRom.Chunks)?371:512)*5);for(int i=0;i<AudioRom.ChunkCount(b);i++){int p=chunks+i*5,n=Bin.U16(b,p+3);keep(GraphicsAssets.Address(b,p)+((n&32768)!=0?(n&32767)+2:n));}
            int turbo=AudioRom.TurboAddress(b);keep(turbo+NativeCompression.EncodedSize(b,turbo));byte[] effects=NativeCompression.Decode(b,turbo);for(int i=0;i<107;i++)if(effects[i*6]!=0)keep(GraphicsAssets.Address(effects,i*6+1)+Bin.U16(effects,i*6+4));
            foreach(int table in ScreenAssets.Tables.Skip(1))for(int row=0;row<(table==ScreenAssets.Tables[1]?3:4);row++){int entry=At(b,table)+row*16,p=GraphicsAssets.Address(b,entry+2);keep(p+((Bin.U16(b,entry)&0x4000)!=0?Bin.U16(b,entry+10):NativeCompression.EncodedSize(b,p)));}
            foreach(int old in new[]{0x1f5d84,0x1ff08f,0x1ff5f3,0x1fa3cc,0x1fa3ec,0x1fa1a0,0x1fa1b0,0x1fa1c0}){int p=GraphicsAssets.Address(b,At(b,old));keep(p+NativeCompression.EncodedSize(b,p));}
            for(int i=0;i<TextAssets.CreditCount;i++){int p=At(b,TextAssets.Credits)+i*6;keep(GraphicsAssets.Address(b,p+2)+b[p+5]);}foreach(var line in CopyrightAssets.Read(b)){int p=At(b,CopyrightAssets.Table)+line.Record*6;keep(GraphicsAssets.Address(b,p+2)+b[p+5]);}
            for(int sprite=0;sprite<2;sprite++)foreach(int id in sprite!=0?Enumerable.Range(0,232):Enumerable.Range(0,63).Select(i=>i*2)){var asset=GraphicsAssets.Describe(b,sprite!=0,id);for(int x=0;x<asset.Width;x++){int p=GraphicsAssets.Address(b,asset.Header+2+x*3);keep(!asset.Sprite&&id<4?p+128:GraphicStorage.End(b,asset,p));}}
            start=(start+255)&~255;var list=new List<Section>();for(int p=start;p<end;){int n=Math.Min(end-p,65536-(p&65535));list.Add(new Section{Start=p,Size=n});p+=n;}return list;}
    }
}

