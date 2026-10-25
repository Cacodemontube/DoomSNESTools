using System;using System.Collections.Generic;using System.Linq;
namespace DoomSNESed {
    public static class MusicLibrary {
        public static List<NativeInstrument> All(byte[] rom){return MusicBank.All(rom).SelectMany(b=>b.Instruments).GroupBy(i=>i.Id).Select(g=>g.First()).OrderBy(i=>i.Id).ToList();}
        // Preserve the common SFX area and relocate only the samples and curves
        // actually selected. No additional DSP voices or SPC RAM are assumed.
        public static void Prepare(byte[] rom,MusicSettings settings){
            var banks=MusicBank.All(rom);var home=banks.First(b=>b.Module==settings.Bank);var ids=settings.Assignments.Where(a=>!a.Mute).Select(a=>a.Patch).Distinct().ToArray();
            settings.Blocks=null;settings.Origin=home.Origin;settings.Song=home.Song;
            if(ids.All(id=>home.Instruments.Any(i=>i.Id==id)))return;
            byte[] common=AudioRom.Module(rom,0),directory=(byte[])common.Clone();int start=AudioRom.Blocks(rom,0).Where(b=>b.Address<0xb490).Max(b=>b.Address+b.Data.Length);var payload=new List<byte>();var stored=new Dictionary<string,int>();
            Func<byte[],int> store=data=>{string key=Convert.ToBase64String(data);int address;if(!stored.TryGetValue(key,out address)){address=start+payload.Count;Bin.Require(address+data.Length<0xb490,"Selected music samples do not fit SPC RAM. Select fewer/smaller samples or mute unused instruments.");payload.AddRange(data);stored[key]=address;}return address;};
            var curves=new[]{new Dictionary<string,int>(),new Dictionary<string,int>(),new Dictionary<string,int>()};int[] next={2,1,1};
            foreach(int id in ids){
                var source=home.Instruments.Any(i=>i.Id==id)?home:banks.FirstOrDefault(b=>b.Instruments.Any(i=>i.Id==id));Bin.Require(source!=null,"Unknown music sample "+id+".");byte[] apu=AudioRom.Module(rom,source.Module);int bsnDir=Bin.U16(apu,0x1ee2),p=Bin.U16(apu,bsnDir+id*2);byte[] bsn=apu.Skip(p).Take(6).ToArray();int wave=bsn[2];var sample=AudioRom.WaveBlock(apu,AudioRom.Blocks(rom,source.Module),wave);int address=store(sample.Data);
                // Remap wave IDs too: the same wave slot means different data in different banks.
                string sampleKey="wave:"+Convert.ToBase64String(sample.Data);int waveId;if(!stored.TryGetValue(sampleKey,out waveId)){waveId=37+stored.Keys.Count(k=>k.StartsWith("wave:"));Bin.Require(waveId<128,"Too many music waveforms.");stored[sampleKey]=waveId;Bin.W16(directory,0x1f00+waveId*4,address+3);Bin.W16(directory,0x1f02+waveId*4,address+3+Bin.U16(apu,0x1f02+wave*4)-Bin.U16(apu,0x1f00+wave*4));}bsn[2]=(byte)waveId;
                for(int kind=0;kind<3;kind++)if((bsn[0]&(kind==0?1:kind==1?4:8))!=0){int slot=kind==0?1:kind==1?3:4,table=Bin.U16(apu,0x1ee4+kind*2),curve=Bin.U16(apu,table+bsn[slot]*2);Bin.Require(curve+8<apu.Length,"Invalid native envelope.");int length=8+apu[curve+5]*apu[curve+6];Bin.Require(length>=8&&curve+length<=0xb490,"Invalid native envelope size.");byte[] data=apu.Skip(curve).Take(length).ToArray();string key=Convert.ToBase64String(data);int index;if(!curves[kind].TryGetValue(key,out index)){index=next[kind]++;Bin.Require(index<(kind==0?58:kind==1?20:7),"Selected instruments exceed native envelope slots.");curves[kind][key]=index;Bin.W16(directory,table+index*2,store(data));}bsn[slot]=(byte)index;}
                Bin.W16(directory,bsnDir+id*2,store(bsn));
            }
            settings.Origin=start+payload.Count;Bin.W16(directory,Bin.U16(common,0x1efa)+settings.Song*2,settings.Origin);
            settings.Blocks=new List<ApuBlock>{new ApuBlock{Address=0x1f94,Data=directory.Skip(0x1f94).Take(0x25ec-0x1f94).ToArray()},new ApuBlock{Address=start,Data=payload.ToArray()}};
        }
    }
}


