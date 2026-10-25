using System;
namespace DoomSNESed {
    public static class MusicTargets {
        public const string Title="Title music",Intermission="Intermission music";
        public const int TitleOperand=0x1fef6b,IntermissionOperand=0x1f5811;
        public static string Canonical(string name){string value=(name??"").Trim();if(value.Equals("TITLE",StringComparison.OrdinalIgnoreCase)||value.Equals(Title,StringComparison.OrdinalIgnoreCase))return Title;if(value.Equals("INTERMISSION",StringComparison.OrdinalIgnoreCase)||value.Equals(Intermission,StringComparison.OrdinalIgnoreCase))return Intermission;return value;}
        public static int Operand(string name){name=Canonical(name);return name==Title?TitleOperand:name==Intermission?IntermissionOperand:0;}
        public static bool IsSpecial(string name){return Operand(name)!=0;}
        public static int Read(byte[] rom,string name){int p=RomLayout.At(rom,Operand(name));if(p!=0){CheckOperand(rom,p);return Bin.U16(rom,p);}string value=name.Split(' ')[0];Bin.Require(value.Length==4&&value[0]=='E'&&value[2]=='M'&&value[1]>='1'&&value[1]<='3'&&value[3]>='1'&&value[3]<='9',"Choose an original ROM level, title or intermission music.");int slot=(value[1]-'1')*9+value[3]-'1';return Bin.U16(rom,RomLayout.At(rom,AudioRom.SongTable)+slot*2);}
        public static void Write(byte[] rom,string name,int entry){int p=RomLayout.At(rom,Operand(name));Bin.Require(p!=0,"Unknown scene music target.");CheckOperand(rom,p);Bin.Require((entry>>8)<48&&(entry&255)<19,"Invalid scene song assignment.");Bin.W16(rom,p,entry);}
        static void CheckOperand(byte[] rom,int p){Bin.Require(rom[p-1]==0xa9&&rom[p+2]==0x22&&rom[p+3]==(RomLayout.SourceBuilt(rom)?0xcd:0xb4)&&rom[p+4]==0xfe&&rom[p+5]==(RomLayout.SourceBuilt(rom)?0x4f:0x4e),"Unsupported title/intermission music loader.");}
    }
}

