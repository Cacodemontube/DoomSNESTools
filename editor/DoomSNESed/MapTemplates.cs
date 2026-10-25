using System.Linq;
namespace DoomSNESed {
    public static class MapTemplates {
        static int Rect(Map m,int x1,int y1,int x2,int y2,int texture){return GeometryTools.AddPolygon(m,new System.Collections.Generic.List<Point>{new Point(x1,y1),new Point(x1,y2),new Point(x2,y2),new Point(x2,y1)},texture);}
        public static Map Blank(Rom rom,int slot){Map m=rom.MakeRoom(slot,1024,1024);m.NewRoom=false;m.EditGeometry=true;m.Vertices.Clear();m.Walls.Clear();m.Faces.Clear();m.Sectors.Clear();m.Things.Clear();return m;}
        public static Map Starter(Rom rom,int slot){Map m=rom.MakeRoom(slot,1024,1024);GeometryTools.Enable(m,m.RoomTexture);return m;}
        public static Map Door(Rom rom,int slot){Map m=Blank(rom,slot);int texture=m.RoomTexture;
            Rect(m,-512,-128,0,128,texture);Rect(m,0,-128,64,128,texture);Rect(m,64,-128,576,128,texture);GeometryTools.Preset(m,1,SectorMotion.Door);m.Things.Add(new Thing{Type=0,Flags=0xef,X=-256,Y=0});m.Walls.First(w=>m.Vertices[w.Start].X==576&&m.Vertices[w.End].X==576).Type=11;return m;
        }
        public static Map Lift(Rom rom,int slot){Map m=Blank(rom,slot);int texture=m.RoomTexture;Rect(m,-512,-128,0,128,texture);Rect(m,0,-128,512,128,texture);GeometryTools.Preset(m,1,SectorMotion.Lift);m.Things.Add(new Thing{Type=0,Flags=0xef,X=-64,Y=0});return m;}
    }
}
