using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace DoomSNESed {
    public static class AdvancedTests {
        static void Check(bool value,string message){Bin.Require(value,"Advanced test: "+message);}
        public static Map Empty(Rom rom,int slot) {Map seed=rom.MakeRoom(slot,1024,1024);seed.NewRoom=false;seed.EditGeometry=true;seed.Vertices.Clear();seed.Walls.Clear();seed.Faces.Clear();seed.Sectors.Clear();seed.Things.Clear();return seed;}
        public static int Rect(Map m,int x1,int y1,int x2,int y2,int texture){return GeometryTools.AddPolygon(m,new List<Point>{new Point(x1,y1),new Point(x1,y2),new Point(x2,y2),new Point(x2,y1)},texture);}
        public static Map DoorMap(Rom rom){Map m=Empty(rom,0);int texture=rom.MakeRoom(0,1024,1024).RoomTexture;
            Rect(m,-512,-128,0,128,texture);Rect(m,0,-128,64,128,texture);Rect(m,64,-128,576,128,texture);GeometryTools.Preset(m,1,SectorMotion.Door);m.Things.Add(new Thing {Type=0,Flags=0xef,X=-256,Y=0});m.Walls.First(w=>m.Vertices[w.Start].X==576 && m.Vertices[w.End].X==576).Type=11;return m;
        }
        public static Map LiftMap(Rom rom){Map m=Empty(rom,0);int texture=rom.MakeRoom(0,1024,1024).RoomTexture;
            Rect(m,-512,-128,0,128,texture);Rect(m,0,-128,512,128,texture);GeometryTools.Preset(m,1,SectorMotion.Lift);m.Things.Add(new Thing {Type=0,Flags=0xef,X=-64,Y=0});return m;
        }
        public static void Run(Rom rom,string output){
            byte[] source=(byte[])rom.Bytes.Clone();Map original=rom.Project.Maps[0];int tex=rom.MakeRoom(0,1024,1024).RoomTexture;
            Map m=Empty(rom,0);GeometryTools.AddPolygon(m,new List<Point>{new Point(-512,-512),new Point(-512,512),new Point(0,512),new Point(0,0),new Point(512,0),new Point(512,-512)},tex);
            Rect(m,512,-512,768,0,tex);m.Things.Add(new Thing {Type=0,Flags=0xef,X=-256,Y=-256});rom.Project.Maps[0]=m;
            byte[] bytes=rom.Compile();var info=NativeCompiler.Build(rom,bytes,m);Check(info.Nodes>0&&info.Areas>=3,"concave floorplan subdivided into convex BSP leaves");
            Check(m.Walls.Count(w=>GeometryTools.Far(m,w)>=0)==1,"shared wall welded into portal");
            Buffer.BlockCopy(bytes,0,rom.Bytes,0,bytes.Length);Map decoded=rom.Read(0);Check(decoded.Sectors.Count==2,"concave native reimport");Buffer.BlockCopy(source,0,rom.Bytes,0,source.Length);
            rom.SavePatch(Path.Combine(output,"drawn.dse"),bytes);Bin.Atomic(Path.Combine(output,"drawn.sfc"),bytes);rom.SaveProject(Path.Combine(output,"drawn.sned"));
            m=Empty(rom,0);Rect(m,-512,-256,512,256,tex);m.Things.Add(new Thing {Type=0,Flags=0xef,X=-256,Y=0});GeometryTools.Divide(m,new Point(0,-256),new Point(0,256),tex);Check(m.Sectors.Count==2,"linedef split creates two sectors");GeometryTools.Validate(m);rom.Project.Maps[0]=m;rom.Compile();
            m=DoorMap(rom);rom.Project.Maps[0]=m;bytes=rom.Compile();info=NativeCompiler.Build(rom,bytes,m);Check(info.Doors==1,"door table compiled");rom.SavePatch(Path.Combine(output,"door.dse"),bytes);Bin.Atomic(Path.Combine(output,"door.sfc"),bytes);rom.SaveProject(Path.Combine(output,"door.sned"));
            var camera=new SceneCamera {X=-256,Y=0,Z=42};var textures=new SceneTextures(rom);ScenePick closedPick,openPick;
            using(var closedImage=SceneRenderer.Render(m,camera,textures,320,200,false,out closedPick))using(var openImage=SceneRenderer.Render(m,camera,textures,320,200,true,out openPick)){
                Check(closedPick.Sector==0&&openPick.Sector==2,"3D picking changes from closed door to far room");closedImage.Save(Path.Combine(output,"preview-door-closed.png"));openImage.Save(Path.Combine(output,"preview-door-open.png"));
            }
            Check(!System.IO.File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"DoomSNESed.ico")),"editor has no loose icon dependency");
            using(var icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location))Check(icon!=null&&icon.Width>=16,"executable contains native icon resource");
            GeometryTools.Preset(m,1,SectorMotion.Lift);info=NativeCompiler.Build(rom,(byte[])rom.Bytes.Clone(),m);Check(info.Doors==0&&info.Lifts==1,"switching presets removes the previous preset's actions");
            m=LiftMap(rom);rom.Project.Maps[0]=m;bytes=rom.Compile();info=NativeCompiler.Build(rom,bytes,m);Check(info.Lifts==1,"lift table compiled");rom.SavePatch(Path.Combine(output,"lift.dse"),bytes);Bin.Atomic(Path.Combine(output,"lift.sfc"),bytes);rom.SaveProject(Path.Combine(output,"lift.sned"));
            m=Empty(rom,0);Rect(m,-512,-256,512,256,tex);m.Things.Add(new Thing {Type=0,Flags=0xef,X=0,Y=0});rom.Project.Maps[0]=m;bytes=rom.Compile();info=NativeCompiler.Build(rom,bytes,m);Check(info.Nodes==1&&info.Areas==2,"single convex sector has real root node required by native object lookup");rom.SavePatch(Path.Combine(output,"convex.dse"),bytes);
            foreach(int type in new[]{5,14,18,19,23,36,38,56,58,62,73,87,88,7}){
                Map effect=LiftMap(rom);effect.Sectors[1].Floor=type==18||type==73?0:64;effect.Sectors[1].Ceiling=192;effect.Sectors[0].Floor=type==18?64:0;effect.Things[0].X=type==18?-32:-64;Wall trigger=effect.Walls.First(w=>GeometryTools.Far(effect,w)>=0);trigger.Type=type;trigger.Tag=1;effect.Sectors[1].Tag=1;rom.Project.Maps[0]=effect;bytes=rom.Compile();if(type==7||type==18||type==73)rom.SavePatch(Path.Combine(output,"effect-"+type+".dse"),bytes);
            }
            // Rebuild a retail map too: its tighter ROM budgets exercise BSP
            // split selection and conservative shared blockmap lists.
            var json=new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=8*1024*1024};Map rebuilt=json.Deserialize<Map>(json.Serialize(original));GeometryTools.Enable(rebuilt,tex);rom.Project.Maps[0]=rebuilt;bytes=rom.Compile();rom.SavePatch(Path.Combine(output,"hangar-rebuilt.dse"),bytes);Check(rebuilt.Sectors.Count==85,"original E1M1 geometry and platform rebuild fits its slot");
            // Every original remains read-only until explicitly converted.
            rom.Project.Maps[0]=original;Check(rom.Compile().SequenceEqual(source),"advanced compilation did not modify source ROM or original project");
            File.WriteAllText(Path.Combine(output,"advanced-results.txt"),"PASS: concave polygons, automatic portals, line splits, required convex root node, 3D picks, embedded icon, native doors/lifts/floors/stairs/crusher data; source remains unchanged.\n");
        }
    }
}
