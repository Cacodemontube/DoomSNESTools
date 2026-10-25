using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using CodeImp.DoomBuilder.Geometry;

namespace DoomSNESed {
    public static class SelfTest {
        static int checks;
        static void Check(bool ok,string name) {if(!ok)throw new Exception("FAILED: "+name);checks++;}
        static void Reject(Action action,string name) {bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected,name);}
        public static int Run(string[] args) {
            string output=args.Length>1?args[1]:Path.Combine(Path.GetTempPath(),"DoomSNESed-tests");Directory.CreateDirectory(output);
            try {
                Check(args.Length>0,"ROM argument supplied");string root=AppDomain.CurrentDomain.BaseDirectory;
                Rom rom=new Rom(args[0],Path.Combine(root,"editor-data"));Context.Rom=rom;
                Check(rom.Project.Maps.Count==22,"22 SNES maps");byte[] original=(byte[])rom.Bytes.Clone();
                Check(rom.Compile().SequenceEqual(original),"all 22 maps round-trip byte-for-byte");
                using(var preview=new AssetPreview())foreach(int id in rom.WallTextures) {preview.ShowWall(rom,id);Check(preview.HasImage,"ROM wall asset decode "+id);}
                Check(Line2D.GetDistanceToLineSq(new Vector2D(0,0),new Vector2D(10,0),new Vector2D(5,3),true)==9,"upstream geometry hit testing");
                Check(System.ComponentModel.TypeDescriptor.GetProperties(new SectorProperties(rom.Project.Maps[0].Sectors[0]))["Floor"].IsReadOnly,"existing floor is read-only in editor");
                Check(!System.ComponentModel.TypeDescriptor.GetProperties(new RoomProperties(rom.MakeRoom(0,1024,1024)))["Floor"].IsReadOnly,"new-room floor is editable");
                Map hangar=rom.Project.Maps[0];Thing start=hangar.Things.Single(t=>t.Type==0);int oldx=start.X;start.X+=16;
                byte[] moved=rom.Compile();int op=rom.B(0,0x3608);Check(Bin.S16(moved,op+2)==oldx+16,"object coordinate reaches native ROM data");
                Check(moved.Where((b,i)=>b!=original[i]).Count()<=2,"object move changes only its X word");start.X=oldx;
                Sector sector=hangar.Sectors[0];int floor=sector.Floor;sector.Floor+=8;Reject(delegate{rom.Compile();},"existing platform heights locked");sector.Floor=floor;
                string project=Path.Combine(output,"roundtrip.sned");rom.SaveProject(project);rom.LoadProject(project);Check(rom.Compile().SequenceEqual(original),"project save/load preserves all maps");
                foreach(Map map in rom.Project.Maps.ToArray()) {
                    int index=rom.Project.Maps.IndexOf(map);Map room=rom.MakeRoom(map.Slot,1024,1024);rom.Project.Maps[index]=room;
                    byte[] built=rom.Compile();Section a=rom.GetSection(map.Slot,"A"),b=rom.GetSection(map.Slot,"B");
                    // Every changed byte stays in this slot's allocations or
                    // its individual pointer/count table entries.
                    for(int p=0;p<built.Length;p++)if(built[p]!=original[p]) {
                        bool allowed=(p>=a.Start && p<a.Start+a.Size)||(p>=b.Start && p<b.Start+b.Size)||Rom.Tables.Where(t=>t!=0x32a8 && t!=0x3566).Any(t=>p>=Rom.TableBank+t+map.Slot*2 && p<Rom.TableBank+t+map.Slot*2+2);
                        Check(allowed,"room writes remain in declared map data");
                    }
                    Buffer.BlockCopy(built,0,rom.Bytes,0,built.Length);Map reread=rom.Read(map.Slot);
                    Check(rom.Word(0x348e,map.Slot)-rom.Word(0x3458,map.Slot)==10,"native segment table biases");
                    int segp=rom.A(map.Slot,0x3458)+11;
                    Check(Bin.U16(built,segp)==Rom.VertexRam+20 && Bin.U16(built,segp+5*11)==Rom.VertexRam+16,"first and last native segment references");
                    Check(reread.Vertices.Count==6 && reread.Walls.Count==6 && reread.Sectors.Count==1,"new room native data reimports");
                    Check(reread.Things.Count==1 && reread.Things[0].Type==0,"native player start reimports");
                    Check(reread.Walls.Count(w=>w.Type==11)==1,"native usable exit wall");
                    Buffer.BlockCopy(original,0,rom.Bytes,0,original.Length);rom.Project.Maps[index]=map;
                }
                Map fresh=rom.MakeRoom(0,1024,1024);rom.Project.Maps[0]=fresh;
                rom.SavePatch(Path.Combine(output,"room-empty.dse"));
                fresh.Things.Add(new Thing {Type=6,Flags=0xef,X=128,Y=0});fresh.Things.Add(new Thing {Type=15,Flags=0x87,X=0,Y=128});
                byte[] custom=rom.Compile();rom.SavePatch(Path.Combine(output,"room.dse"),custom);Bin.Atomic(Path.Combine(output,"room.sfc"),custom);
                rom.SaveProject(Path.Combine(output,"room.sned"));
                Thing thing=fresh.Things[1];thing.X=1024;Reject(delegate{rom.Compile();},"out-of-room object rejected");thing.X=128;
                fresh.RoomWidth=257;Reject(delegate{rom.Compile();},"invalid room width rejected");fresh.RoomWidth=1024;
                fresh.Sectors[0].FloorTexture=255;Reject(delegate{rom.Compile();},"sky floor rejected");
                rom.Project.Maps[0]=rom.Read(0);AdvancedTests.Run(rom,output);InteractionTests.Run(args[0],output);ConverterTests.Run(args[0],output);AudioTests.Run(args[0],output);MusicHudTests.Run(args[0],output);MusicRefinementTests.Run(args[0],output);ModifiedRomTests.Run(args[0],output);MusicVoiceTests.Run(args[0],output);MusicRestartTests.Run(args[0],output);MusicNaturalTests.Run(args[0],output);ModifiedLayoutTests.Run(args[0],output);MusicSteadyTests.Run(args[0],output);PickupClavesTests.Run(args[0],output);MusicPreviewTests.Run(args[0],output);GraphicPackingTests.Run(args[0],output);GraphicSizeTests.Run(args[0],output);CatalogSectorTests.Run(args[0],output);PortalCompatibilityTests.Run(args[0],output);
                File.WriteAllText(Path.Combine(output,"results.txt"),"PASS: "+checks+" assertions. All original maps round-trip, all 22 slots compile and reimport a room, geometry and invalid edits checked.\n");return 0;
            } catch(Exception e) {File.WriteAllText(Path.Combine(output,"results.txt"),e.ToString());return 1;}
        }
    }
}







