// DoomSNESed test editor. GPL-3.0-or-later.
// Reality Engine layouts: Randal Linden's DoomFX source, rle.i, exmy.i,
// rllevels.a, rlobjects.a and ripdoom3.asm. No ROM data is embedded.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using CodeImp.DoomBuilder.Geometry;

namespace DoomSNESed {
    public static class Bin {
        public static int U16(byte[] b, int p) { return b[p] | (b[p+1] << 8); }
        public static int S16(byte[] b, int p) { return (short)U16(b,p); }
        public static void W16(byte[] b, int p, int v) { b[p]=(byte)v; b[p+1]=(byte)(v>>8); }
        public static byte[] Hash(byte[] b) { using(var sha=SHA256.Create()) return sha.ComputeHash(b); }
        public static string Hex(byte[] b) { return BitConverter.ToString(b).Replace("-", "").ToLowerInvariant(); }
        public static void Require(bool ok, string message) { if(!ok) throw new InvalidDataException(message); }
        public static void Atomic(string path, byte[] data) {
            path=Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp=path+".tmp-"+Guid.NewGuid().ToString("N");
            try { File.WriteAllBytes(tmp,data); if(File.Exists(path)) File.Replace(tmp,path,null); else File.Move(tmp,path); }
            finally { if(File.Exists(tmp)) File.Delete(tmp); }
        }
    }
    public class Point { public int X, Y; public Point() {} public Point(int x,int y) { X=x;Y=y; } [ScriptIgnore]public Vector2D Vec { get { return new Vector2D(X,Y); } } }
    public class Wall { public int Start, End, Flags, Angle, Face, Type, Tag; public int BackFace=-1,OffsetX,OffsetY; }
    public enum SectorMotion { None, Door, Lift, MovingFloor, Stairs, Crusher }
    public class Sector { public int Type, Darkness, NeighborDarkness, Floor, Ceiling, FloorTexture, CeilingTexture, Tag; public SectorMotion Motion; }
    public class Thing { public int Type, X, Y, Angle, Flags; }
    public class Face { public int Offset, Near, Far=-1, Upper, Lower; public bool Solid; }
    public class Section { public string Name; public int Start, Size; }
    public class Map {
        public int Slot; public string Name,SourceMarker; public bool NewRoom,EditGeometry,OptimizeReject;
        public List<Point> Vertices=new List<Point>(); public List<Wall> Walls=new List<Wall>();
        public List<Sector> Sectors=new List<Sector>(); public List<Thing> Things=new List<Thing>();
        public List<Face> Faces=new List<Face>();
        public int RoomWidth=1024, RoomHeight=1024, RoomTexture=0;
        public override string ToString() { return Name; }
    }
    public class Project { public List<TextReplacement> Texts=new List<TextReplacement>();public bool RetailAssets,CompactAssets,AllowExpandedAssets;public AudioProject Audio=new AudioProject(); public List<GraphicReplacement> Graphics=new List<GraphicReplacement>();public int[] CampaignSlots;public int Version=1; public string SourceSha256=Rom.Identity; public List<Map> Maps=new List<Map>(); }

    public sealed partial class Rom {
        public const string Identity="d45e26eb10c323ecd480e5f2326b223e29264c3adde67f48f0d2791128e519e8";
        public const int VertexRam=0x3bb6, TableBank=0x1f0000;
        public readonly string SourceHash;public bool IsModified {get{return SourceHash!=Identity;}}
        public readonly string Resources;public readonly byte[] Bytes; public readonly string PathName;
        public readonly List<Section> Sections=new List<Section>();
        public readonly List<int> WallTextures=new List<int>(), FlatTextures=new List<int>();
        internal readonly Dictionary<int,System.Drawing.Bitmap> WallImages=new Dictionary<int,System.Drawing.Bitmap>();public readonly string[] ObjectNames;
        public readonly Project Project=new Project();public StorageSummary Budget;
        public static readonly int[] Tables={0x32a8,0x3314,0x334a,0x3380,0x33b6,0x33ec,0x3422,0x3458,0x348e,0x34c4,0x34fa,0x3530,0x3566,0x35d2,0x3608,0x363e,0x3674,0x36aa,0x36e0,0x3716,0x374c};
        public int Word(int table,int slot) { return Bin.U16(Bytes,RomLayout.At(Bytes,TableBank+table)+slot*2); }
        public int Far(int table,int slot) { int p=RomLayout.At(Bytes,TableBank+table)+slot*4; return (Bytes[p]|Bytes[p+1]<<8|Bytes[p+2]<<16)&0x1fffff; }
        public int A(int slot,int table) { return (Far(0x32a8,slot)&0x1f0000)|Word(table,slot); }
        public int B(int slot,int table) { return (Far(0x3566,slot)&0x1f0000)|Word(table,slot); }
        public Section GetSection(int slot,string kind) { string name="E"+(slot/9+1)+"M"+(slot%9+1)+"_"+kind; return Sections.Single(s=>s.Name==name); }
        public Rom(string path, string resources) {
            Resources=resources;PathName=System.IO.Path.GetFullPath(path); byte[] raw=File.ReadAllBytes(path);
            if(raw.Length==0x200200||raw.Length==0x400200) { Bytes=new byte[raw.Length-512]; Buffer.BlockCopy(raw,512,Bytes,0,Bytes.Length); } else Bytes=raw;
            ModifiedRom.Validate(Bytes);SourceHash=Bin.Hex(Bin.Hash(Bytes));Project.SourceSha256=SourceHash;
            ObjectNames=File.ReadAllLines(System.IO.Path.Combine(resources,"objects.txt"));
            foreach(string line in File.ReadAllLines(System.IO.Path.Combine(resources,"sections.txt"))) {
                string[] s=line.Split(' '); Sections.Add(new Section {Name=s[0],Start=Convert.ToInt32(s[1],16),Size=Convert.ToInt32(s[2],16)});
            }
            if(RomLayout.SourceBuilt(Bytes)){Sections.Clear();for(int slot=0;slot<27;slot++){if(Word(0x33b6,slot)==0)continue;Bin.Require(slot==0,"This source layout currently supports its linked E1M1 map allocation.");Sections.Add(new Section{Name="E1M1_A",Start=Far(0x32a8,slot),Size=0xb770});Sections.Add(new Section{Name="E1M1_B",Start=Far(0x3566,slot),Size=0x10fe59-Far(0x3566,slot)});}}
            foreach(Section s in Sections.Where(s=>s.Name.EndsWith("_A") && s.Size>0)) {
                int slot=(s.Name[1]-'1')*9+(s.Name[3]-'1');
                Map map=Read(slot); Project.Maps.Add(map);
                foreach(Face f in map.Faces) { Add(WallTextures,f.Upper); if(!f.Solid) Add(WallTextures,f.Lower); }
                foreach(Sector sector in map.Sectors) { Add(FlatTextures,sector.FloorTexture); Add(FlatTextures,sector.CeilingTexture); }
            }
            // Switch-on frames may never occur in a map's initial faces.
            // Expose every native table entry, including animation-only art.
            WallTextures.Clear();WallTextures.AddRange(NativeWallCatalog.Ids);
            if(IsModified&&!RomLayout.SourceBuilt(Bytes))ModifiedRom.ProtectSections(Bytes,Sections);WallTextures.Sort(); FlatTextures.Clear(); FlatTextures.AddRange(Enumerable.Range(1,255));
        }
        static void Add(List<int> values,int n) { if(!values.Contains(n))values.Add(n); }
        public Map Read(int slot) {
            Map m=new Map {Slot=slot,Name="E"+(slot/9+1)+"M"+(slot%9+1)};
            int vp=(Far(0x32a8,slot)&0x1f0000)|((Word(0x3380,slot)+VertexRam)&65535);
            int vc=Word(0x33b6,slot), lc=Word(0x3422,slot),sc=Word(0x35d2,slot);
            Bin.Require(vc>0 && vc<=1056 && lc>0 && lc<=1118 && sc>0 && sc<=205,"Invalid Reality Engine map counts: "+m.Name);
            for(int i=0;i<vc;i++)m.Vertices.Add(new Point(Bin.S16(Bytes,vp+i*4),Bin.S16(Bytes,vp+i*4+2)));
            int lp=A(slot,0x33ec),fp=A(slot,0x3530); Dictionary<int,Face> faces=new Dictionary<int,Face>();
            for(int i=0;i<lc;i++) {
                int p=lp+i*12; Wall w=new Wall {Start=(Bin.U16(Bytes,p)-VertexRam)/4,End=(Bin.U16(Bytes,p+2)-VertexRam)/4,Flags=Bin.U16(Bytes,p+4),Angle=Bin.U16(Bytes,p+6),Face=Bin.U16(Bytes,p+8),Type=Bytes[p+10],Tag=Bytes[p+11]};
                Bin.Require(w.Start>=0 && w.End>=0 && w.Start<vc && w.End<vc,"Invalid vertex reference: "+m.Name);
                m.Walls.Add(w);
            }
            // Segments can refer to back faces that have no direct line reference.
            // EMSEGS is biased by -rlgSize; EMSEGS2 is biased by -1.
            int segp=A(slot,0x3458)+11, segc=Word(0x34c4,slot);
            for(int i=0;i<segc;i++) {
                int p=segp+i*11, off=Bin.U16(Bytes,p+7); bool solid=(Bytes[p+4]&1)!=0;
                if(faces.ContainsKey(off))continue;
                int q=fp+off; Face f=new Face {Offset=off,Near=Bytes[q],Solid=solid};
                if(solid) f.Upper=Bytes[q+1]; else { f.Far=Bytes[q+1]; f.Upper=Bytes[q+2]; f.Lower=Bytes[q+3]; }
                faces.Add(off,f); m.Faces.Add(f);
            }
            for(int i=0;i<segc;i++) {
                int p=segp+i*11,line=Bin.U16(Bytes,p+9),off=Bin.U16(Bytes,p+7);
                if(line>=lc)continue;
                Wall w=m.Walls[line];Face front=m.Faces.FirstOrDefault(f=>f.Offset==w.Face);
                if(front!=null && !front.Solid && faces[off].Near==front.Far)w.BackFace=off;
                if(w.Face==off) {w.OffsetX=Bytes[p+5];w.OffsetY=Bytes[p+6];}
            }
            int sp=Far(0x3566,slot);
            for(int i=0;i<sc;i++) { int p=sp+i*10; m.Sectors.Add(new Sector {Type=Bytes[p]/4,Darkness=Bytes[p+1],NeighborDarkness=Bytes[p+2],Floor=Bin.S16(Bytes,p+3),Ceiling=Bin.S16(Bytes,p+5),FloorTexture=Bytes[p+7],CeilingTexture=Bytes[p+8],Tag=Bytes[p+9]}); }
            Bin.Require(Bin.U16(Bytes,sp+sc*10)==0x524c,"Missing sector terminator: "+m.Name);
            int dp=B(slot,0x3674),doorEnd=GetSection(slot,"B").Start+GetSection(slot,"B").Size;
            while(dp+8<doorEnd && Bytes[dp]!=255) {int sector=Bytes[dp];if(sector<sc)m.Sectors[sector].Motion=SectorMotion.Door;dp+=8+Bytes[dp+7]*2;}
            int op=B(slot,0x3608),end=B(slot,0x363e);
            while(op+2<=end && Bin.U16(Bytes,op)!=0) {
                int type=Bytes[op+1],len=type<6?8:6;
                Bin.Require(type<ObjectNames.Length && op+len+2<=end,"Invalid object list: "+m.Name);
                m.Things.Add(new Thing {Flags=Bytes[op],Type=type,X=Bin.S16(Bytes,op+2),Y=Bin.S16(Bytes,op+4),Angle=type<6?Bin.U16(Bytes,op+6):0});op+=len;
            }
            return m;
        }
        public string ObjectName(int type) { return type>=0 && type<ObjectNames.Length?ObjectNames[type]:"Unknown"; }
        public void Validate(Map m) {
            if(RomLayout.SourceBuilt(Bytes)){ValidateSourceMap(m);return;}
            Bin.Require(m.Slot>=0 && m.Slot<27 && Project.Maps.Any(x=>x.Slot==m.Slot),"Unknown map slot.");
            Bin.Require(m.Sectors.Count>0 && m.Sectors.Count<=205 && m.Vertices.Count<=1056 && m.Walls.Count<=1118,"Reality Engine map capacity exceeded: "+m.Sectors.Count+" / 205 sectors, "+m.Vertices.Count+" / 1056 vertices, "+m.Walls.Count+" / 1118 linedefs. Simplify the source map; these are engine limits, not ROM file space.");
            foreach(Sector s in m.Sectors) {
                Bin.Require(s.Floor>=-4096 && s.Ceiling<=4096 && s.Floor<=s.Ceiling && (!m.NewRoom || s.Ceiling-s.Floor>=64),"Floor must be at or below ceiling, within -4096..4096. New rooms need at least 64 units of clearance.");
                Bin.Require(s.Darkness>=0 && s.Darkness<=247 && s.NeighborDarkness>=0 && s.NeighborDarkness<=247,"Darkness must be 0..247.");
                Bin.Require(s.FloorTexture>=0 && s.FloorTexture<255 && s.CeilingTexture>=0 && s.CeilingTexture<=255,"Select floor/ceiling assets present in the ROM.");
                Bin.Require(s.Type>=0 && s.Type<=16,"Unsupported SNES sector effect.");
            }
            foreach(Face f in m.Faces)Bin.Require(WallTextures.Contains(f.Upper) && (f.Solid || WallTextures.Contains(f.Lower)),"Unknown ROM wall asset.");
            Bin.Require(m.Things.Count(t=>t.Type==0)==1,"Every level needs exactly one Player1Start.");
            Map baseline=m.NewRoom || m.EditGeometry?null:Read(m.Slot);
            int tasks=m.Sectors.Count(s=>new[]{1,2,3,4,8,12,13}.Contains(s.Type));
            int originalTasks=baseline==null?0:baseline.Sectors.Count(s=>new[]{1,2,3,4,8,12,13}.Contains(s.Type));
            Bin.Require(tasks<=Math.Max(48,originalTasks),"The Reality Engine has at most 48 task slots for lighting and platforms.");
            for(int skill=0;skill<3;skill++) {
                int activeMovable=m.Things.Count(t=>(t.Flags&(1<<skill))!=0 && (t.Flags&0x50)==0x40);
                int activeFixed=m.Things.Count(t=>(t.Flags&(1<<skill))!=0 && (t.Flags&0x50)==0);
                int maxMovable=baseline==null?84:Math.Max(84,baseline.Things.Count(t=>(t.Flags&(1<<skill))!=0 && (t.Flags&0x50)==0x40));
                int maxFixed=baseline==null?180:Math.Max(180,baseline.Things.Count(t=>(t.Flags&(1<<skill))!=0 && (t.Flags&0x50)==0));
                Bin.Require(activeMovable<=maxMovable && activeFixed<=maxFixed,"Object capacity exceeded. Existing retail populations may be preserved; new rooms allow 84 movable and 180 fixed objects per skill.");
            }
            foreach(Thing t in m.Things) {
                Bin.Require(t.Type>=0 && t.Type<64,"Only SNES player, teleport, monster, pickup and scenery types are available.");
                Bin.Require(t.X>=-32768 && t.X<=32767 && t.Y>=-32768 && t.Y<=32767,"Object coordinates overflow.");
                Bin.Require((t.Flags&7)!=0,"Select at least one skill.");
                Bin.Require(t.Type!=0 || (t.Flags&7)==7,"The player start must exist at every skill.");
                if(m.NewRoom)Bin.Require(Math.Abs(t.X)<m.RoomWidth/2-32 && Math.Abs(t.Y)<m.RoomHeight/2-32,"Place objects inside the room, at least 32 units from a wall.");
            }
            if(m.EditGeometry) {GeometryTools.Validate(m);}
            else if(m.NewRoom) {
                Bin.Require(m.Sectors.Count==1,"This test compiler supports one sector per new room map.");
                Bin.Require(m.RoomWidth>=256 && m.RoomWidth<=4096 && m.RoomHeight>=256 && m.RoomHeight<=4096 && m.RoomWidth%128==0 && m.RoomHeight%128==0,"Room dimensions must be multiples of 128, from 256 to 4096.");
                Bin.Require(WallTextures.Contains(m.RoomTexture),"Select a ROM wall asset.");
            } else {
                Map original=Read(m.Slot);
                Bin.Require(m.Vertices.Count==original.Vertices.Count && m.Walls.Count==original.Walls.Count && m.Sectors.Count==original.Sectors.Count && m.Faces.Count==original.Faces.Count,"Existing-map topology cannot be changed by this test compiler.");
                for(int i=0;i<m.Vertices.Count;i++)Bin.Require(m.Vertices[i].X==original.Vertices[i].X && m.Vertices[i].Y==original.Vertices[i].Y,"Existing vertices are locked to preserve BSP and collision data.");
                for(int i=0;i<m.Sectors.Count;i++)Bin.Require(m.Sectors[i].Floor==original.Sectors[i].Floor && m.Sectors[i].Ceiling==original.Sectors[i].Ceiling && m.Sectors[i].Tag==original.Sectors[i].Tag,"Existing heights and tags are locked to preserve compiled platform behavior.");
                for(int i=0;i<m.Walls.Count;i++) {
                    Wall w=m.Walls[i],o=original.Walls[i];
                    Bin.Require(w.Start==o.Start && w.End==o.End && w.Flags==o.Flags && w.Angle==o.Angle && w.Face==o.Face && w.Type==o.Type && w.Tag==o.Tag,"Existing line geometry/actions are locked ("+m.Name+", wall "+i+").");
                }
                for(int i=0;i<m.Faces.Count;i++)Bin.Require(m.Faces[i].Offset==original.Faces[i].Offset && m.Faces[i].Solid==original.Faces[i].Solid && m.Faces[i].Near==original.Faces[i].Near && m.Faces[i].Far==original.Faces[i].Far,"Existing face links are locked.");
                Bin.Require(ObjectBytes(m.Things).Length<=B(m.Slot,0x363e)-B(m.Slot,0x3608),"Object list exceeds this map's ROM allocation. Remove objects before adding more.");
            }
        }
        static string Serialize(object o) { return ProjectJson.Create().Serialize(o); }
        static object Canonical(object value){
            var dictionary=value as Dictionary<string,object>;
            if(dictionary!=null){var sorted=new SortedDictionary<string,object>(StringComparer.Ordinal);foreach(var pair in dictionary)sorted[pair.Key]=Canonical(pair.Value);return sorted;}
            var array=value as object[];return array==null?value:array.Select(Canonical).ToArray();
        }
        static string MapState(Map map){var json=ProjectJson.Create();return json.Serialize(Canonical(json.DeserializeObject(json.Serialize(map))));}
        public static byte[] ObjectBytes(List<Thing> things) {
            using(var ms=new MemoryStream())using(var w=new BinaryWriter(ms)) {
                foreach(Thing t in things) {
                    w.Write((byte)t.Flags);w.Write((byte)t.Type);w.Write((short)t.X);w.Write((short)t.Y);if(t.Type<6)w.Write((ushort)t.Angle);
                }
                w.Write((ushort)0);return ms.ToArray();
            }
        }
        public byte[] Compile() {
            if(RomLayout.SourceBuilt(Bytes))return CompileSourceLayout();
            if(Project.RetailAssets){Bin.Require(Project.Maps.All(m=>MapState(m)==MapState(Read(m.Slot))),"ROM-only asset projects preserve original maps. Start a separate level project to edit their geometry or properties.");var expanded=new byte[0x400000];for(int i=0;i<expanded.Length;i++)expanded[i]=255;Buffer.BlockCopy(Bytes,0,expanded,0,Bytes.Length);var space=ModifiedRom.Expansion(Bytes);Budget=new StorageSummary{Capacity=space.Sum(s=>s.Size)};GraphicsAssets.Apply(this,expanded,space);AudioBuild.Apply(this,expanded,space,Resources);TextAssets.Apply(this,expanded,space);CpuRomPointers.Normalize(this,expanded,space);expanded[0x7fd7]=12;WadConverter.Checksum(expanded);if(Project.CompactAssets){byte[] compact;if(RomCompactor.TryCompact(this,expanded,out compact))return compact;Bin.Require(Project.AllowExpandedAssets,"These edits do not fit a compatible 2 MiB ROM. Use smaller artwork, auto-fit samples or fewer music changes. Alternatively enable Allow 4 MiB if needed; Snes9x 1.60 cannot load expanded Super FX assets correctly.");}return expanded;}
            byte[] target=IsModified?Enumerable.Repeat((byte)255,0x400000).ToArray():(byte[])Bytes.Clone();if(IsModified)Buffer.BlockCopy(Bytes,0,target,0,Bytes.Length);var graphicsSpace=IsModified?ModifiedRom.Expansion(Bytes):new List<Section>();Budget=new StorageSummary{Capacity=graphicsSpace.Sum(s=>s.Size)};
            if(Project.CampaignSlots!=null)ValidateCampaign(Project);
            foreach(Map m in Project.Maps) { if(Project.CampaignSlots!=null&&!Project.CampaignSlots.Contains(m.Slot)){CampaignStorage.Strip(this,target,m,graphicsSpace);continue;}Validate(m); if(m.EditGeometry) {NativeBuildInfo info=NativeCompiler.Build(this,target,m);Section a=GetSection(m.Slot,"A"),b=GetSection(m.Slot,"B");Budget.Capacity+=a.Size+b.Size;Budget.Maps+=info.GeometryBytes+info.DataBytes;if(!IsModified){graphicsSpace.Add(new Section{Start=a.Start+info.GeometryBytes,Size=a.Size-info.GeometryBytes});graphicsSpace.Add(new Section{Start=b.Start+info.DataBytes,Size=b.Size-info.DataBytes});}continue;} if(m.NewRoom) { RoomCompiler.Build(this,target,m);continue; }
                int sp=Far(0x3566,m.Slot);
                for(int i=0;i<m.Sectors.Count;i++)WriteSector(target,sp+i*10,m.Sectors[i]);
                int fp=A(m.Slot,0x3530);
                foreach(Face f in m.Faces) { int p=fp+f.Offset; if(f.Solid)target[p+1]=(byte)f.Upper;else {target[p+2]=(byte)f.Upper;target[p+3]=(byte)f.Lower;} }
                byte[] objects=ObjectBytes(m.Things); int op=B(m.Slot,0x3608); Buffer.BlockCopy(objects,0,target,op,objects.Length);
            }
            GraphicsAssets.Apply(this,target,graphicsSpace);AudioBuild.Apply(this,target,graphicsSpace,Resources);TextAssets.Apply(this,target,graphicsSpace);CpuRomPointers.Normalize(this,target,graphicsSpace);if(Project.CampaignSlots!=null){ValidateCampaign(Project);WadConverter.ApplyCampaign(target,Project.CampaignSlots);}
            if(IsModified){target[0x7fd7]=12;WadConverter.Checksum(target);}Budget.OutputBytes=target.Length;Budget.ReusableFree=Math.Max(0,Budget.Capacity-Budget.Maps-Budget.Graphics-Budget.Music-Budget.Sounds);return target;
        }
        public static void WriteSector(byte[] b,int p,Sector s) { b[p]=(byte)(s.Type*4);b[p+1]=(byte)s.Darkness;b[p+2]=(byte)s.NeighborDarkness;Bin.W16(b,p+3,s.Floor);Bin.W16(b,p+5,s.Ceiling);b[p+7]=(byte)s.FloorTexture;b[p+8]=(byte)s.CeilingTexture;b[p+9]=(byte)s.Tag; }
        static void ValidateCampaign(Project p){if(p.CampaignSlots==null)return;Bin.Require(p.CampaignSlots.Length>0&&p.CampaignSlots.Length<=22&&p.CampaignSlots.Distinct().Count()==p.CampaignSlots.Length&&p.CampaignSlots.All(slot=>p.Maps.Any(m=>m.Slot==slot)),"Invalid campaign slot sequence.");Bin.Require(p.CampaignSlots[0]==0||!p.CampaignSlots.Skip(1).Contains(0),"E1M1 is reserved for the first campaign map.");}
        public void SaveProject(string path) { Bin.Atomic(path,System.Text.Encoding.UTF8.GetBytes(Serialize(Project))); }
        public void LoadProject(string path) {
            var json=ProjectJson.Create();
            Project p=json.Deserialize<Project>(File.ReadAllText(path));
            Bin.Require(p!=null && p.Version==1 && p.SourceSha256==SourceHash && p.Maps!=null && p.Maps.Count==Project.Maps.Count,"Invalid or incompatible DoomSNESed project.");
            Bin.Require(p.Maps.Select(m=>m.Slot).Distinct().Count()==p.Maps.Count && p.Maps.All(m=>Project.Maps.Any(x=>x.Slot==m.Slot)),"Invalid project map slots.");
            ValidateCampaign(p);foreach(Map m in p.Maps){if(m.EditGeometry)GeometryTools.ValidateDraft(m);else Validate(m);} Project.Maps.Clear();Project.Maps.AddRange(p.Maps);Project.Texts=p.Texts??new List<TextReplacement>();Project.RetailAssets=p.RetailAssets;Project.CompactAssets=p.CompactAssets;Project.AllowExpandedAssets=p.AllowExpandedAssets;Project.CampaignSlots=p.CampaignSlots;Project.Graphics=p.Graphics??new List<GraphicReplacement>();Project.Audio=p.Audio??new AudioProject();
        }
        public void SavePatch(string path) { SavePatch(path,Compile()); }
        public void SavePatch(string path,byte[] target) {SavePatch(path,target,Project.CampaignSlots!=null);}
        public void SavePatch(string path,byte[] target,bool campaign) {
            Bin.Require(!RomLayout.SourceBuilt(Bytes),"This source-linked engine can be exported as an .sfc ROM for emulators/hardware. Native recompiler patches require the retail engine layout.");
            using(var ms=new MemoryStream())using(var w=new BinaryWriter(ms)) {
                w.Write(System.Text.Encoding.ASCII.GetBytes(Project.CompactAssets&&target.Length==0x200000?"DSEAST7\0":IsModified?"DSEMOD6\0":Project.RetailAssets?"DSEMOD5\0":AudioBuild.HasAudio(Project)?"DSEAUD4\0":(Project.Graphics!=null&&Project.Graphics.Count>0||Project.Texts!=null&&Project.Texts.Count>0)?"DSEGFX3\0":campaign?"DSECNV2\0":"DSEMAP1\0"));w.Write(Bin.Hash(Bytes));w.Write(Bin.Hash(target));
                var boundaries=new HashSet<int>(Sections.Where(s=>s.Size>0).SelectMany(s=>new[]{s.Start,s.Start+s.Size}).Concat(new[]{0x1f32a8,0x1f32ac,0x1f3314,0x1f3566,0x1f356a,0x1f35d2,0x1f3782}));int count=0;for(int p=0;p<target.Length;) { if((p<Bytes.Length?Bytes[p]:(byte)255)==target[p]) {p++;continue;}int start=p++;while(p<target.Length&&!boundaries.Contains(p)&&(p<Bytes.Length?Bytes[p]:(byte)255)!=target[p])p++;count++; }
                w.Write(count);
                for(int p=0;p<target.Length;) { if((p<Bytes.Length?Bytes[p]:(byte)255)==target[p]) {p++;continue;}int start=p++;while(p<target.Length&&!boundaries.Contains(p)&&(p<Bytes.Length?Bytes[p]:(byte)255)!=target[p])p++;w.Write(start);w.Write(p-start);w.Write(target,start,p-start); }
                w.Flush();Bin.Atomic(path,ms.ToArray());
            }
        }
        public Map MakeRoom(int slot,int width,int height) {
            Map template=Read(0);
            int wall=template.Faces.Where(f=>f.Solid).GroupBy(f=>f.Upper).OrderByDescending(g=>g.Count()).First().Key;
            int floor=template.Sectors.GroupBy(s=>s.FloorTexture).OrderByDescending(g=>g.Count()).First().Key;
            int ceiling=template.Sectors.Where(s=>s.CeilingTexture!=255).GroupBy(s=>s.CeilingTexture).OrderByDescending(g=>g.Count()).First().Key;
            Map m=new Map {Slot=slot,Name="E"+(slot/9+1)+"M"+(slot%9+1),NewRoom=true,RoomWidth=width,RoomHeight=height,RoomTexture=wall};
            m.Sectors.Add(new Sector {Floor=0,Ceiling=128,Darkness=32,NeighborDarkness=32,FloorTexture=floor,CeilingTexture=ceiling});
            m.Things.Add(new Thing {Type=0,Flags=0xef,Angle=0,X=0,Y=0});RoomCompiler.Layout(m);return m;
        }
    }
}




