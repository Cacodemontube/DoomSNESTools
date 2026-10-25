// Native Reality Engine map compiler. Platform formats and classifications
// follow Randal Linden's GPL-3.0 DoomFX ripdoom3.asm and rle.i.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DoomSNESed {
    public sealed class NativeBuildInfo {public int Vertices,Lines,Segments,Areas,Nodes,GeometryBytes,GeometryCapacity,DataBytes,DataCapacity,Doors,Lifts,Floors,Stairs,Ceilings;}
    public static class NativeCompiler {
        sealed class Seg {public Point A,B;public int Line,Face,Sector,Offset;}
        sealed class Child {public int Ref;public List<Seg> Segs;}
        sealed class Node {public Point A,B;public Child Left,Right;}
        sealed class Tree {
            public readonly List<Node> Nodes=new List<Node>();public readonly List<List<Seg>> Leaves=new List<List<Seg>>();
            static double Side(Seg divider,Point p) {long cross=GeometryTools.Cross(divider.A,divider.B,p);double eps=(divider.B.Vec-divider.A.Vec).GetLength()*0.75;return Math.Abs(cross)<=eps?0:cross;}
            static bool RightOn(Seg divider,Seg s) {return (long)(divider.B.X-divider.A.X)*(s.B.X-s.A.X)+(long)(divider.B.Y-divider.A.Y)*(s.B.Y-s.A.Y)>=0;}
            public Child Build(List<Seg> segs,int depth) {
                Bin.Require(segs.Count>0 && depth<64,"The BSP is too complex. Simplify intersecting or very small geometry.");
                bool convex=segs.All(s=>s.Sector==segs[0].Sector);
                if(convex)foreach(Seg s in segs)if(segs.Any(t=>Side(s,t.A)>0 || Side(s,t.B)>0)){convex=false;break;}
                if(convex&&depth>0) {Bin.Require(segs.Count<=255 && Leaves.Count<512,"A subsector exceeds the native capacity.");int index=Leaves.Count;Leaves.Add(segs);return new Child {Ref=0x8000+index*4,Segs=segs};}
                Seg best=null;double bestScore=double.MaxValue;
                // The game enters the root as a NODE without checking its leaf
                // bit. Even a convex room therefore needs a real root partition.
                if(convex){int min=segs.Min(s=>Math.Min(s.A.X,s.B.X)),max=segs.Max(s=>Math.Max(s.A.X,s.B.X));int mid=min+(max-min)/2;Bin.Require(mid>min&&mid<max,"Sector is too narrow for a native root partition.");best=new Seg{A=new Point(mid,0),B=new Point(mid,1)};bestScore=-1;}
                foreach(Seg candidate in segs.Take(512)) {
                    int left=0,right=0,splits=0;
                    foreach(Seg s in segs) {double a=Side(candidate,s.A),b=Side(candidate,s.B);
                        if(a==0&&b==0) {if(RightOn(candidate,s))right++;else left++;}
                        else if(a>=0&&b>=0)left++;else if(a<=0&&b<=0)right++;else {left++;right++;splits++;}
                    }
                    if(left==0||right==0)continue;double score=splits*256+Math.Abs(left-right);if(score<bestScore){best=candidate;bestScore=score;}
                }
                Bin.Require(best!=null,"Cannot build a valid BSP. Check overlapping sectors and line directions.");
                var l=new List<Seg>();var r=new List<Seg>();
                foreach(Seg s in segs) {double a=Side(best,s.A),b=Side(best,s.B);
                    if(a==0&&b==0){(RightOn(best,s)?r:l).Add(s);}
                    else if(a>=0&&b>=0)l.Add(s);else if(a<=0&&b<=0)r.Add(s);
                    else {double f=a/(a-b);Point p=new Point((int)Math.Round(s.A.X+(s.B.X-s.A.X)*f),(int)Math.Round(s.A.Y+(s.B.Y-s.A.Y)*f));
                        Bin.Require(!GeometryTools.Same(p,s.A)&&!GeometryTools.Same(p,s.B),"A BSP split is smaller than one unit. Move this vertex to the grid.");
                        var first=new Seg {A=s.A,B=p,Line=s.Line,Face=s.Face,Sector=s.Sector,Offset=s.Offset};var second=new Seg {A=p,B=s.B,Line=s.Line,Face=s.Face,Sector=s.Sector,Offset=s.Offset+(int)Math.Round((p.Vec-s.A.Vec).GetLength())};
                        if(a>0){l.Add(first);r.Add(second);}else{r.Add(first);l.Add(second);}
                    }
                }
                Child lc=Build(l,depth+1),rc=Build(r,depth+1);int n=Nodes.Count;Bin.Require(n*28<32768,"BSP node offsets exceed the native range.");Nodes.Add(new Node {A=best.A,B=best.B,Left=lc,Right=rc});return new Child {Ref=n*28,Segs=segs};
            }
        }
        static byte[] Bytes(Action<BinaryWriter> write) {using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)){write(w);w.Flush();return stream.ToArray();}}
        static void W(BinaryWriter w,int value){w.Write((short)value);}
        static void Bounds(BinaryWriter w,Child c) {W(w,c.Segs.Max(s=>Math.Max(s.A.Y,s.B.Y)));W(w,c.Segs.Min(s=>Math.Min(s.A.Y,s.B.Y)));W(w,c.Segs.Min(s=>Math.Min(s.A.X,s.B.X)));W(w,c.Segs.Max(s=>Math.Max(s.A.X,s.B.X)));W(w,c.Ref);}
        static void Table(byte[] b,int table,int slot,int value) {Bin.W16(b,RomLayout.At(b,Rom.TableBank+table)+slot*2,value);}
        static List<int> Neighbors(Map m,int sector){return GeometryTools.Neighbors(m,sector);}
        static int[] DoorSectors(Map m) {
            var ids=new HashSet<int>(Enumerable.Range(0,m.Sectors.Count).Where(s=>m.Sectors[s].Motion==SectorMotion.Door));
            foreach(Wall w in m.Walls) {int type=EngineActions.Doors[w.Type];if(type==1 && GeometryTools.Far(m,w)>=0)ids.Add(GeometryTools.Far(m,w));if(type==2&&w.Tag>0)foreach(int s in Enumerable.Range(0,m.Sectors.Count).Where(s=>m.Sectors[s].Tag==w.Tag))ids.Add(s);}
            return ids.OrderBy(s=>s).ToArray();
        }
        static int[] LineFlags(Map m,int[] doors) {
            return m.Walls.Select(w=> {Face f=GeometryTools.Front(m,w);int flags=(f.Solid?1:0)|(w.Flags&0x2c)|(EngineActions.Flags[w.Type]<<8);
                if(doors.Contains(f.Near)||doors.Contains(f.Far))flags|=16;
                if(!f.Solid){if(m.Sectors[f.Near].Floor!=m.Sectors[f.Far].Floor)flags|=0x100;if(m.Sectors[f.Near].Ceiling!=m.Sectors[f.Far].Ceiling)flags|=0x200;}
                return flags;
            }).ToArray();
        }
        public static NativeBuildInfo Build(Rom rom,byte[] target,Map map) {
            GeometryTools.Validate(map);int[] doors=DoorSectors(map),flags=LineFlags(map,doors);var tree=new Tree();var source=new List<Seg>();
            for(int i=0;i<map.Walls.Count;i++){Wall w=map.Walls[i];Face front=GeometryTools.Front(map,w),back=GeometryTools.Back(map,w);source.Add(new Seg {A=map.Vertices[w.Start],B=map.Vertices[w.End],Line=i,Face=front.Offset,Sector=front.Near});if(back!=null)source.Add(new Seg {A=map.Vertices[w.End],B=map.Vertices[w.Start],Line=i,Face=back.Offset,Sector=back.Near});}
            Child root=tree.Build(source,0);var vertices=map.Vertices.Select(p=>new Point(p.X,p.Y)).ToList();
            Func<Point,int> vertex=p=>{int index=vertices.FindIndex(v=>GeometryTools.Same(v,p));if(index<0){index=vertices.Count;vertices.Add(p);}return Rom.VertexRam+index*4;};
            var faceOffsets=new Dictionary<int,int>();var faceCache=new Dictionary<string,int>();byte[] faces=Bytes(w=>{foreach(Face f in map.Faces){byte[] data=f.Solid?new[]{(byte)f.Near,(byte)f.Upper}:new[]{(byte)f.Near,(byte)f.Far,(byte)f.Upper,(byte)f.Lower};string key=Convert.ToBase64String(data);int offset;if(!faceCache.TryGetValue(key,out offset)){offset=(int)w.BaseStream.Position;w.Write(data);faceCache.Add(key,offset);}faceOffsets[f.Offset]=offset;}});
            byte[] segments=Bytes(w=>{foreach(var leaf in tree.Leaves)foreach(Seg s in leaf){Wall line=map.Walls[s.Line];Face f=map.Faces.First(x=>x.Offset==s.Face);int sf=f.Solid?0x41:0;
                if(mSky(map,f))sf|=2;if((flags[s.Line]&16)!=0)sf|=4;
                W(w,vertex(s.A));W(w,vertex(s.B));w.Write((byte)sf);w.Write((byte)(line.OffsetX+s.Offset));w.Write((byte)line.OffsetY);W(w,faceOffsets[s.Face]);W(w,s.Line);
            }});
            Bin.Require(vertices.Count<=1056 && segments.Length/11<=2438,"The compiled map exceeds the Reality Engine vertex/segment limit.");
            byte[] nodes=Bytes(w=>{foreach(Node n in tree.Nodes){W(w,n.A.Y);W(w,n.B.X-n.A.X);W(w,n.A.X);W(w,n.B.Y-n.A.Y);Bounds(w,n.Left);Bounds(w,n.Right);}});
            byte[] areas=Bytes(w=>{int offset=0;foreach(var leaf in tree.Leaves){w.Write((byte)leaf.Count);W(w,offset);w.Write((byte)leaf[0].Sector);offset+=leaf.Count*11;}});
            byte[] vertexData=Bytes(w=>{foreach(Point p in vertices){W(w,p.X);W(w,p.Y);}});
            byte[] lines=Bytes(w=>{for(int i=0;i<map.Walls.Count;i++){Wall line=map.Walls[i];Point a=map.Vertices[line.Start],b=map.Vertices[line.End];int angle=((int)(Math.Atan2(b.Y-a.Y,b.X-a.X)*65536/(Math.PI*2))+32)&0xffc0;
                W(w,Rom.VertexRam+line.Start*4);W(w,Rom.VertexRam+line.End*4);W(w,flags[i]);W(w,angle);W(w,faceOffsets[line.Face]);w.Write((byte)line.Type);w.Write((byte)line.Tag);
            }});
            Section aSection=rom.GetSection(map.Slot,"A"),bSection=rom.GetSection(map.Slot,"B");int pos=aSection.Start;
            Action<byte[]> put=data=>{Bin.Require(pos+data.Length<=aSection.Start+aSection.Size,"Geometry needs "+(pos+data.Length-aSection.Start)+" bytes; this slot has "+aSection.Size+". Simplify the map or choose a larger slot.");Buffer.BlockCopy(data,0,target,pos,data.Length);pos+=data.Length;};
            Table(target,0x3314,map.Slot,root.Ref);put(nodes);Table(target,0x334a,map.Slot,pos-0x8000);put(areas);Table(target,0x3380,map.Slot,pos-Rom.VertexRam);Table(target,0x33b6,map.Slot,vertices.Count);put(vertexData);
            Table(target,0x33ec,map.Slot,pos);Table(target,0x3422,map.Slot,map.Walls.Count);put(lines);Table(target,0x3458,map.Slot,pos-11);Table(target,0x348e,map.Slot,pos-1);Table(target,0x34c4,map.Slot,segments.Length/11);put(segments);
            Table(target,0x34fa,map.Slot,pos);put(map.OptimizeReject?Reject(map):new byte[(map.Sectors.Count*map.Sectors.Count+7)/8]);Table(target,0x3530,map.Slot,pos);put(faces);
            var info=new NativeBuildInfo {Vertices=vertices.Count,Lines=map.Walls.Count,Segments=segments.Length/11,Nodes=tree.Nodes.Count,Areas=tree.Leaves.Count,GeometryBytes=pos-aSection.Start,GeometryCapacity=aSection.Size,DataCapacity=bSection.Size,Doors=doors.Length};
            pos=bSection.Start;
            Action<byte[]> dataPut=data=>{Bin.Require(pos+data.Length<=bSection.Start+bSection.Size,"Objects/collision/platforms need "+(pos+data.Length-bSection.Start)+" bytes; this slot has "+bSection.Size+". Reduce the map or choose a larger slot.");Buffer.BlockCopy(data,0,target,pos,data.Length);pos+=data.Length;};
            dataPut(Bytes(w=>{for(int i=0;i<map.Sectors.Count;i++){byte[] sector=new byte[10];Rom.WriteSector(sector,0,map.Sectors[i]);w.Write(sector);}W(w,0x524c);}));Table(target,0x35d2,map.Slot,map.Sectors.Count);
            Table(target,0x3608,map.Slot,pos);dataPut(Rom.ObjectBytes(map.Things));Table(target,0x363e,map.Slot,pos);dataPut(Blockmap(map));
            Table(target,0x3674,map.Slot,pos);dataPut(DoorData(map,doors));Table(target,0x36aa,map.Slot,pos);dataPut(FloorData(map,info));Table(target,0x36e0,map.Slot,pos);dataPut(CeilingData(map,info));Table(target,0x3716,map.Slot,pos);dataPut(StairData(map,info));Table(target,0x374c,map.Slot,pos);dataPut(LiftData(map,info));info.DataBytes=pos-bSection.Start;return info;
        }
        // DoomFX rlenemies.a: bit (enemySector * sectorCount + targetSector).
        // Only disconnected portal components are rejected. Doors remain edges
        // even when closed, so opening a door never exposes a false rejection.
        public static byte[] Reject(Map map){int count=map.Sectors.Count;var parent=Enumerable.Range(0,count).ToArray();Func<int,int> root=null;root=i=>parent[i]==i?i:(parent[i]=root(parent[i]));foreach(Wall w in map.Walls){Face f=GeometryTools.Front(map,w);if(f!=null&&f.Near>=0&&f.Far>=0)parent[root(f.Far)]=root(f.Near);}var table=new byte[(count*count+7)/8];for(int a=0;a<count;a++)for(int b=0;b<count;b++)if(root(a)!=root(b)){int bit=a*count+b;table[bit/8]|=(byte)(1<<(bit&7));}return table;}
        static bool mSky(Map m,Face f){return m.Sectors[f.Near].CeilingTexture==255 && (f.Solid || m.Sectors[f.Far].CeilingTexture==255);}
        static bool IntersectsCell(Point a,Point b,int x,int y){double enter=0,leave=1;int[] p={a.X,a.Y},delta={b.X-a.X,b.Y-a.Y},low={x,y};for(int axis=0;axis<2;axis++){if(delta[axis]==0){if(p[axis]<low[axis]||p[axis]>low[axis]+128)return false;continue;}double t1=(low[axis]-p[axis])/(double)delta[axis],t2=(low[axis]+128-p[axis])/(double)delta[axis];enter=Math.Max(enter,Math.Min(t1,t2));leave=Math.Min(leave,Math.Max(t1,t2));if(enter>leave)return false;}return true;}
        static byte[] Blockmap(Map m) {
            int x=(int)Math.Floor(m.Vertices.Min(p=>p.X)/128.0)*128,y=(int)Math.Floor(m.Vertices.Min(p=>p.Y)/128.0)*128;
            int nx=(m.Vertices.Max(p=>p.X)-x)/128+1,ny=(m.Vertices.Max(p=>p.Y)-y)/128+1;Bin.Require(nx<=255&&ny<=255,"Blockmap dimensions overflow.");
            var cells=new List<int>[nx*ny];for(int cy=0;cy<ny;cy++)for(int cx=0;cx<nx;cx++){var ids=new List<int>();for(int i=0;i<m.Walls.Count;i++){Wall line=m.Walls[i];if(IntersectsCell(m.Vertices[line.Start],m.Vertices[line.End],x+cx*128,y+cy*128))ids.Add(i);}Bin.Require(ids.Count<=63,"More than 63 collision lines share a 128-unit block. Simplify dense geometry.");cells[cy*nx+cx]=ids;}
            // Nearby cells may safely share a conservative candidate list:
            // native collision still tests each actual line. This reduces ROM
            // space while respecting the engine's 63-line movement buffer.
            for(int ty=0;ty<ny;ty+=2)for(int tx=0;tx<nx;tx+=2){var tile=new List<int>();for(int yy=ty;yy<Math.Min(ny,ty+2);yy++)for(int xx=tx;xx<Math.Min(nx,tx+2);xx++)tile.Add(yy*nx+xx);var union=tile.SelectMany(i=>cells[i]).Distinct().OrderBy(i=>i).ToList();int old=tile.Select(i=>string.Join(",",cells[i])).Distinct().Sum(key=>key.Length==0?1:key.Split(',').Length+1);if(union.Count<=63&&union.Count+1<old)foreach(int i in tile)cells[i]=union;}
            return Bytes(w=> {W(w,x);W(w,y);w.Write((byte)nx);w.Write((byte)ny);long entries=w.BaseStream.Position;w.Write(new byte[nx*ny*2]);var lists=new Dictionary<string,int>();
                for(int cy=0;cy<ny;cy++)for(int cx=0;cx<nx;cx++){var ids=cells[cy*nx+cx];
                    string key=string.Join(",",ids);int list;if(!lists.TryGetValue(key,out list)){list=(int)w.BaseStream.Position;lists.Add(key,list);foreach(int i in ids)W(w,i);W(w,-1);}
                    long end=w.BaseStream.Position,entry=entries+(cy*nx+cx)*2;w.BaseStream.Position=entry;W(w,list-(int)entry-1);w.BaseStream.Position=end;
                }
            });
        }
        static byte[] DoorData(Map m,int[] doors) {return Bytes(w=>{foreach(int s in doors){var adjacent=Neighbors(m,s);Bin.Require(adjacent.Count>0,"Door sector "+s+" must touch another sector.");var lines=GeometryTools.Boundary(m,s);Bin.Require(lines.Count<=255,"Too many door boundary lines.");Point origin=GeometryTools.Center(m,s);w.Write((byte)s);W(w,adjacent.Min(n=>m.Sectors[n].Ceiling)-4);W(w,origin.X);W(w,origin.Y);w.Write((byte)lines.Count);foreach(int line in lines)W(w,line);}w.Write((byte)255);});}
        static IEnumerable<Wall> Groups(Map m,string[] table){return m.Walls.Where(w=>table[w.Type]!="0" && w.Tag!=0).GroupBy(w=>w.Tag+":"+w.Type).Select(g=>g.First());}
        static List<int> Tagged(Map m,int tag) {var ids=Enumerable.Range(0,m.Sectors.Count).Where(s=>m.Sectors[s].Tag==tag).ToList();Bin.Require(ids.Count>0,"Line tag "+tag+" does not target a sector.");return ids;}
        static int Height(Map m,int s,string code,out int model) {
            Sector sector=m.Sectors[s];var ns=Neighbors(m,s);model=s;
            if(code=="GetSectorF24")return sector.Floor+24;if(code=="GetSectorF32")return sector.Floor+32;if(code=="GetSectorF64")return sector.Floor+64;
            if(code=="GetSectorDonut"){Bin.Require(s>0,"Donut sector numbering is invalid.");return Height(m,s-1,"GetSectorNHEF",out model);}
            Bin.Require(ns.Count>0,"Moving floor sector "+s+" needs an adjacent sector.");
            IEnumerable<int> candidates=ns;
            if(code=="GetSectorNHEF") {candidates=ns.Where(n=>m.Sectors[n].Floor>sector.Floor);Bin.Require(candidates.Any(),"Floor sector "+s+" has no higher neighbor.");}
            if(code=="GetSectorLIC" || code=="GetSectorLIC8") {model=ns.OrderBy(n=>m.Sectors[n].Ceiling).First();return Math.Min(sector.Ceiling,m.Sectors[model].Ceiling)-(code.EndsWith("8")?8:0);}
            if(code=="GetSectorHEF" || code=="GetSectorHEF8") {model=ns.OrderByDescending(n=>m.Sectors[n].Floor).First();return m.Sectors[model].Floor+(code.EndsWith("8")?8:0);}
            model=candidates.OrderBy(n=>m.Sectors[n].Floor).First();return m.Sectors[model].Floor;
        }
        static byte[] FloorData(Map m,NativeBuildInfo info) {return Bytes(w=>{foreach(Wall line in Groups(m,EngineActions.Floors)){var targets=Tagged(m,line.Tag);if(line.Type==9){int extra=targets[0]-1;Bin.Require(extra>=0,"Donut sector numbering is invalid.");if(!targets.Contains(extra))targets.Add(extra);}w.Write((byte)line.Tag);w.Write((byte)line.Type);w.Write((byte)targets.Count);foreach(int s in targets){int model,height=Height(m,line.Type==9&&s==targets[targets.Count-1]?targets[0]:s,EngineActions.Floors[line.Type],out model);Point p=GeometryTools.Center(m,line.Type==9&&s==targets[targets.Count-1]?targets[0]:s);w.Write((byte)s);W(w,height);W(w,p.X);W(w,p.Y);w.Write((byte)model);}info.Floors++;}W(w,-1);});}
        static byte[] LiftData(Map m,NativeBuildInfo info) {return Bytes(w=>{foreach(Wall line in Groups(m,EngineActions.Lifts)){var targets=Tagged(m,line.Tag);w.Write((byte)line.Tag);w.Write((byte)line.Type);w.Write((byte)targets.Count);foreach(int s in targets){var ns=Neighbors(m,s);Bin.Require(ns.Count>0,"Lift sector "+s+" needs an adjacent lower sector.");int low=Math.Min(m.Sectors[s].Floor,ns.Min(n=>m.Sectors[n].Floor)),high=EngineActions.Lifts[line.Type]=="GetMovingFloorHeights"?Math.Max(m.Sectors[s].Floor,ns.Max(n=>m.Sectors[n].Floor)):m.Sectors[s].Floor;Point p=GeometryTools.Center(m,s);w.Write((byte)s);W(w,low);W(w,high);W(w,p.X);W(w,p.Y);}info.Lifts++;}W(w,-1);});}
        static byte[] CeilingData(Map m,NativeBuildInfo info) {return Bytes(w=>{foreach(Wall line in Groups(m,EngineActions.Ceilings)){var targets=Tagged(m,line.Tag);w.Write((byte)line.Tag);w.Write((byte)line.Type);w.Write((byte)targets.Count);foreach(int s in targets){Point p=GeometryTools.Center(m,s);var lines=GeometryTools.Boundary(m,s);Bin.Require(lines.Count<=255,"Too many crusher boundary lines.");w.Write((byte)s);W(w,m.Sectors[s].Ceiling);W(w,p.X);W(w,p.Y);w.Write((byte)lines.Count);foreach(int i in lines)W(w,i);}info.Ceilings++;}W(w,-1);});}
        static byte[] StairData(Map m,NativeBuildInfo info) {return Bytes(w=>{foreach(Wall trigger in m.Walls.Where(l=>(l.Type==7||l.Type==8)&&l.Tag>0).GroupBy(l=>l.Tag).Select(g=>g.First())){int start=Tagged(m,trigger.Tag)[0],current=start;var chain=new List<int>();while(!chain.Contains(current)){chain.Add(current);Wall next=m.Walls.FirstOrDefault(l=>GeometryTools.Near(m,l)==current&&GeometryTools.Far(m,l)>=0&&!chain.Contains(GeometryTools.Far(m,l))&&m.Sectors[GeometryTools.Far(m,l)].FloorTexture==m.Sectors[start].FloorTexture);if(next==null)break;current=GeometryTools.Far(m,next);}Point p=GeometryTools.Center(m,start);w.Write((byte)trigger.Tag);W(w,p.X);W(w,p.Y);W(w,m.Sectors[start].Floor);w.Write((byte)chain.Count);foreach(int s in chain)w.Write((byte)s);info.Stairs++;}W(w,-1);});}
    }
}

