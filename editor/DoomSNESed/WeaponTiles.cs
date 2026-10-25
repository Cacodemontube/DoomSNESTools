using System;
namespace DoomSNESed {
    public static class WeaponTiles {
        static readonly int[] Images={0x1ffbe1,0x15feac,0x14fd54,0x18fe8e,0x1ffce8,0x1ff7d7,0x11fe29,0x17febf};
        static readonly int[] Misc={0x1ffbf1,0x15febc,0x14fd6c,0x18fe9e,0x1ffcf8,0x1ff7ef,0x11fe39,0x17fecb};
        static readonly int[] Chars={0x1ffcdc,0x15ffdd,0x14ffe2,0x18ffd9,0x1ffde0,0x1ff933,0x11ff8d,0x17ffdc};
        static readonly int[] Frames={4,4,6,4,4,6,4,3};
        // Decode DoomFX rlweapons.a _GetCharDataMChr, including nested duplicates.
        sealed class Reader {
            byte[] b;int root,p,last,mode,left,budget=20000;System.Collections.Generic.Stack<int[]> stack=new System.Collections.Generic.Stack<int[]>();int remaining=-1;
            public Reader(byte[] bytes,int start){b=bytes;root=start;p=start;}
            public void Seek(int offset){p=root+offset;last=mode=left=0;remaining=-1;stack.Clear();}
            public int Next(){while(--budget>0){if(left>0){left--;return mode==1?++last:mode==2?last:0;}if(remaining==0){var frame=stack.Pop();p=frame[0];remaining=frame[1];continue;}int code=b[p++];if(code<192){last=(code<<8)|b[p++];if(remaining>0)remaining--;return last;}if(code>=224&&code<252){int count;if(code<240){mode=1;count=(code&15)+1;}else if(code<248){mode=2;count=(code&7)+1;}else{mode=3;count=(code&3)+1;}if(remaining>=0){count=Math.Min(count,remaining);remaining-=count;}left=count;continue;}int n=code<224?(code&31)+2:((code&3)<<8|b[p++])+34;int offset=Bin.U16(b,p);p+=2;if(remaining>=0){n=Math.Min(n,remaining);remaining-=n;}stack.Push(new[]{p,remaining});remaining=n;p=root+offset;}throw new System.IO.InvalidDataException("Invalid weapon character stream.");}
        }
        public static int[] Palettes(byte[] rom,int weapon,int tiles){var palettes=new int[tiles];for(int i=0;i<tiles;i++)palettes[i]=-1;var reader=new Reader(rom,RomLayout.At(rom,Chars[weapon]));for(int frame=0;frame<Frames[weapon];frame++){int entry=RomLayout.At(rom,Images[weapon])+frame*4,m=RomLayout.At(rom,Misc[weapon])+Bin.U16(rom,entry+2),count=rom[m++];reader.Seek(Bin.U16(rom,entry));for(int oam=0;oam<count;oam++){int tile=reader.Next(),palette=(rom[m+oam*3]>>1)&7;Bin.Require(tile<tiles,"Weapon tile exceeds its native bank.");if(palettes[tile]<0)palettes[tile]=palette;}}for(int i=0;i<tiles;i++)if(palettes[i]<0)palettes[i]=1;return palettes;}
    }
}

