using System;
using System.IO;
using System.Linq;
namespace DoomSNESed {
    public static class CampaignStorageTests {
        static void Check(bool ok,string message){Bin.Require(ok,"Campaign storage: "+message);}
        public static void Run(string path,string output){
            string resources=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data"),folder=Path.Combine(output,"campaign-storage");Directory.CreateDirectory(folder);
            var baseRom=new Rom(path,resources);string fixture=Path.Combine(folder,"maps.wad");InteractionTests.Fixture(baseRom,fixture);
            foreach(int count in new[]{1,2,3,22}){
                var rom=new Rom(path,resources);var wad=new WadFile(fixture);
                var template=wad.Lumps.TakeWhile((l,i)=>i==0||l.Name!="E1M2").ToArray();
                for(int n=2;n<count;n++){string marker="E"+(n/9+1)+"M"+(n%9+1);wad.Levels.Add(marker);foreach(var lump in template)wad.Lumps.Add(new WadFile.Lump{Name=lump.Name=="E1M1"?marker:lump.Name,Data=(byte[])lump.Data.Clone()});}
                byte[] built=new WadConverter(rom,resources).Convert(wad,wad.Levels.Take(count).ToArray(),false);
                Check(built.Length==0x200000,"keep original 2 MiB size");Check(rom.Budget.DisabledMaps==22-count,"strip only unchecked maps");
                Check(rom.Budget.MapBytesReclaimed==(count==22?0:rom.Project.Maps.Where(m=>!rom.Project.CampaignSlots.Contains(m.Slot)).Sum(m=>rom.GetSection(m.Slot,"A").Size+rom.GetSection(m.Slot,"B").Size)-rom.Budget.MapSlotOverhead),"accurate reclaimed byte count");
                if(count==1)Check(rom.Budget.MapBytesReclaimed>512000,"single-map campaign reclaims substantial original map storage");
                foreach(var map in rom.Project.Maps){if(rom.Project.CampaignSlots.Contains(map.Slot)){Check(Bin.U16(built,Rom.TableBank+0x35d2+map.Slot*2)==map.Sectors.Count,"selected sectors retained");continue;}
                    Check(Bin.U16(built,Rom.TableBank+0x35d2+map.Slot*2)==1,"retired slot contains a tiny valid fallback");
                    Section a=rom.GetSection(map.Slot,"A");Check(built.Skip(a.Start+512).Take(a.Size-512).All(v=>v==255),"original disabled geometry is physically removed");
                }
                string target=Path.Combine(folder,"selected-"+count+".sfc"),project=Path.Combine(folder,"selected-"+count+".sned");File.WriteAllBytes(target,built);rom.SavePatch(Path.ChangeExtension(target,".dse"),built);rom.SaveProject(project);
                var opened=new Rom(path,resources);opened.LoadProject(project);Check(opened.Compile().SequenceEqual(built),"SNED deterministic round-trip");
                // The exported ROM remains readable, including its fixed slots.
                Check(new Rom(target,resources).Project.Maps.Count==22,"stripped ROM remains editable");
                Console.WriteLine("Campaign "+count+" selected: "+CampaignStorage.Report(rom.Budget).Trim());
                if(count==1){
                    for(int module=0;module<48;module++)Check(AudioRom.Module(built,module).SequenceEqual(AudioRom.Module(rom.Bytes,module)),"stripping changed original audio module "+module);
                    foreach(int id in rom.WallTextures)Check(GraphicsAssets.Decode(built,GraphicsAssets.Describe(built,false,id)).SequenceEqual(GraphicsAssets.Decode(rom.Bytes,GraphicsAssets.Describe(rom.Bytes,false,id))),"stripping changed shared wall/sky "+id);
                    var random=new Random(713);
                    foreach(int id in Enumerable.Range(2,8).Select(i=>i*2)){var asset=GraphicsAssets.Describe(rom.Bytes,false,id);byte[] pixels=new byte[asset.Width*asset.Height];random.NextBytes(pixels);rom.Project.Graphics.Add(new GraphicReplacement{Id=id,Width=asset.Width,Height=asset.Height,Pixels=pixels});}
                    byte[] edited=rom.Compile();Check(rom.Budget.Graphics>30000,"fixture must exceed the old one-map asset allowance");
                    foreach(var change in rom.Project.Graphics)Check(GraphicsAssets.Decode(edited,GraphicsAssets.Describe(edited,false,change.Id)).SequenceEqual(change.Pixels),"large artwork import changed pixels");
                    bool reused=rom.Project.Graphics.Any(change=>{var asset=GraphicsAssets.Describe(edited,false,change.Id);return Enumerable.Range(0,asset.Width).Any(x=>{int pointer=GraphicsAssets.Address(edited,asset.Header+2+x*3);return rom.Project.Maps.Where(m=>!rom.Project.CampaignSlots.Contains(m.Slot)).Any(m=>new[]{rom.GetSection(m.Slot,"A"),rom.GetSection(m.Slot,"B")}.Any(s=>pointer>=s.Start&&pointer<s.Start+s.Size));});});
                    Check(reused,"artwork did not use reclaimed disabled-map space");
                    File.WriteAllBytes(Path.Combine(folder,"large-artwork.sfc"),edited);rom.SavePatch(Path.Combine(folder,"large-artwork.dse"),edited);
                }
            }
            // An inactive draft is preserved in the project, then compiled only
            // when enabled again. Its old allocation must not retain level data.
            var draft=new Rom(path,resources);draft.Project.CampaignSlots=new[]{0};draft.Project.Maps[0]=MapTemplates.Starter(draft,0);byte[] single=draft.Compile();
            draft.Project.CampaignSlots=new[]{0,1};byte[] two=draft.Compile();
            Check(Bin.U16(two,Rom.TableBank+0x35d2+2)==draft.Project.Maps.Single(m=>m.Slot==1).Sectors.Count,"re-enabled original map restored from source");
            Check(draft.Budget.DisabledMaps==20,"re-enabled map is no longer reclaimed");
            var alias=new Rom(path,resources);alias.Project.CampaignSlots=new[]{9,11};byte[] aliased=alias.Compile();
            Check(Bin.U16(aliased,Rom.TableBank+0x35d2)==alias.Project.Maps.Single(m=>m.Slot==9).Sectors.Count,"non-E1M1 start alias preserved");
            File.WriteAllBytes(Path.Combine(folder,"aliased-start.sfc"),aliased);alias.SavePatch(Path.Combine(folder,"aliased-start.dse"),aliased);
            // Original-ROM asset mode has no campaign selection to strip.
            var untouched=new Rom(path,resources);Check(untouched.Compile().SequenceEqual(untouched.Bytes),"ordinary editor keeps all maps");
            File.WriteAllText(Path.Combine(output,"campaign-storage-results.txt"),"PASS: 1/2/3/22 selected maps, physical removal, exact accounting, valid fallback slots, deterministic SNED/ROM reopen, re-enabled maps restored, ordinary editor preserves originals.\r\n");
        }
    }
}
