using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using CodeImp.DoomBuilder.Geometry;

namespace DoomSNESed {
    public static class GeometryTools {
        public static Face Front(Map m,Wall w) {return m.Faces.FirstOrDefault(f=>f.Offset==w.Face);}
        public static Face Back(Map m,Wall w) {return w.BackFace<0?null:m.Faces.FirstOrDefault(f=>f.Offset==w.BackFace);}
        public static int Near(Map m,Wall w) {Face f=Front(m,w);return f==null?-1:f.Near;}
        public static int Far(Map m,Wall w) {Face f=Front(m,w);return f==null?-1:f.Far;}
        public static long Cross(Point a,Point b,Point p) {return(long)(b.X-a.X)*(p.Y-a.Y)-(long)(b.Y-a.Y)*(p.X-a.X);}
        public static bool Same(Point a,Point b) {return a.X==b.X && a.Y==b.Y;}
        public static bool OnSegment(Point a,Point b,Point p) {return Cross(a,b,p)==0 && p.X>=Math.Min(a.X,b.X) && p.X<=Math.Max(a.X,b.X) && p.Y>=Math.Min(a.Y,b.Y) && p.Y<=Math.Max(a.Y,b.Y);}
        public static int Vertex(Map m,Point p) {int index=m.Vertices.FindIndex(v=>Same(v,p));if(index>=0)return index;m.Vertices.Add(new Point(p.X,p.Y));return m.Vertices.Count-1;}
        static int FaceId(Map m) {return m.Faces.Count==0?0:m.Faces.Max(f=>f.Offset)+4;}
        public static Face AddFace(Map m,int near,int far,int texture) {var f=new Face {Offset=FaceId(m),Near=near,Far=far,Solid=far<0,Upper=texture,Lower=texture};m.Faces.Add(f);return f;}
        public static void Enable(Map m,int texture) {
            if(m.EditGeometry)return;
            m.NewRoom=false;m.EditGeometry=true;
            foreach(Wall w in m.Walls) {Face f=Front(m,w),back=Back(m,w);Bin.Require(f!=null,"This original map has a line without a surface reference. Restore it or start a new map.");
                Face clone=AddFace(m,f.Near,f.Far,f.Upper);clone.Lower=f.Lower;w.Face=clone.Offset;
                if(!f.Solid) {Face b=AddFace(m,f.Far,f.Near,back==null?texture:back.Upper);b.Lower=back==null?texture:back.Lower;w.BackFace=b.Offset;}
            }
            var used=new HashSet<int>(m.Walls.SelectMany(w=>new[]{w.Face,w.BackFace}));m.Faces=m.Faces.Where(f=>used.Contains(f.Offset)).ToList();
        }
        public static int SectorAt(Map m,Point point) {
            // Odd/even ray test works for concave contours and sector islands.
            bool[] inside=new bool[m.Sectors.Count];var faces=m.Faces.ToDictionary(f=>f.Offset);
            foreach(Wall w in m.Walls){Face f;if(!faces.TryGetValue(w.Face,out f)||f.Near==f.Far)continue;Point a=m.Vertices[w.Start],b=m.Vertices[w.End];
                if(OnSegment(a,b,point))return f.Near;
                if((a.Y>point.Y)!=(b.Y>point.Y)&&point.X<(double)(b.X-a.X)*(point.Y-a.Y)/(b.Y-a.Y)+a.X){if(f.Near>=0&&f.Near<inside.Length)inside[f.Near]=!inside[f.Near];if(f.Far>=0&&f.Far<inside.Length)inside[f.Far]=!inside[f.Far];}
            }return Array.FindIndex(inside,value=>value);
        }
        public static List<int> Neighbors(Map m,int sector) {return m.Walls.Where(w=>Far(m,w)>=0 && (Near(m,w)==sector || Far(m,w)==sector)).Select(w=>Near(m,w)==sector?Far(m,w):Near(m,w)).Where(s=>s!=sector).Distinct().ToList();}
        public static List<int> Boundary(Map m,int sector) {return Enumerable.Range(0,m.Walls.Count).Where(i=>Near(m,m.Walls[i])==sector || Far(m,m.Walls[i])==sector).ToList();}
        public static Point Center(Map m,int sector) {var points=Boundary(m,sector).SelectMany(i=>new[]{m.Vertices[m.Walls[i].Start],m.Vertices[m.Walls[i].End]}).ToArray();return points.Length==0?new Point():new Point((points.Min(p=>p.X)+points.Max(p=>p.X))/2,(points.Min(p=>p.Y)+points.Max(p=>p.Y))/2);}
        public static void SplitWall(Map m,int index,Point p) {
            Wall w=m.Walls[index];if(Same(m.Vertices[w.Start],p)||Same(m.Vertices[w.End],p))return;
            int v=Vertex(m,p);Wall second=ProjectJson.Create().Deserialize<Wall>(ProjectJson.Create().Serialize(w));
            second.Start=v;w.End=v;m.Walls.Add(second);
        }
        public static int AddPolygon(Map m,List<Point> input,int texture) {
            Bin.Require(input.Count>=3,"A sector needs at least three points.");
            List<Point> points=input.Select(p=>new Point(p.X,p.Y)).ToList();if(Same(points[0],points[points.Count-1]))points.RemoveAt(points.Count-1);
            Bin.Require(points.Count>=3 && points.Select(p=>p.X+","+p.Y).Distinct().Count()==points.Count,"A sector cannot repeat a vertex.");
            double area=0;for(int i=0;i<points.Count;i++) {Point a=points[i],b=points[(i+1)%points.Count];area+=(double)a.X*b.Y-(double)b.X*a.Y;
                for(int j=i+2;j<points.Count;j++) {if(i==0 && j==points.Count-1)continue;Point c=points[j],d=points[(j+1)%points.Count];Bin.Require(!StrictCross(a,b,c,d),"The outline crosses itself.");}
            }
            Bin.Require(Math.Abs(area)>=128,"The sector is too small or has no area.");if(area>0)points.Reverse();
            // Split existing walls at the outline's contact vertices. Then split
            // outline edges at existing vertices so partially shared walls weld.
            foreach(Point p in points)for(int i=0;i<m.Walls.Count;i++) {Wall w=m.Walls[i];if(OnSegment(m.Vertices[w.Start],m.Vertices[w.End],p))SplitWall(m,i,p);}
            var expanded=new List<Point>();
            for(int i=0;i<points.Count;i++) {Point a=points[i],b=points[(i+1)%points.Count];expanded.Add(a);
                var cuts=m.Vertices.Where(p=>!Same(p,a)&&!Same(p,b)&&OnSegment(a,b,p)).Distinct().OrderBy(p=>(p.Vec-a.Vec).GetLengthSq()).ToArray();expanded.AddRange(cuts);
                foreach(Wall w in m.Walls)Bin.Require(!StrictCross(a,b,m.Vertices[w.Start],m.Vertices[w.End]),"Draw beside existing sectors, or use Split sector. Overlapping sector outlines are not allowed.");
                Point mid=new Point((a.X+b.X)/2,(a.Y+b.Y)/2);double len=(b.Vec-a.Vec).GetLength();Point probe=new Point((int)Math.Round(mid.X+(b.Y-a.Y)*2/len),(int)Math.Round(mid.Y-(b.X-a.X)*2/len));
                Bin.Require(SectorAt(m,probe)<0,"This outline overlaps an existing sector. Use Split sector to divide a room.");
            }
            int sector=m.Sectors.Count;Sector source=m.Sectors.Count==0?new Sector {Floor=0,Ceiling=128,FloorTexture=6,CeilingTexture=6}:m.Sectors[0];
            m.Sectors.Add(new Sector {Floor=source.Floor,Ceiling=Math.Max(source.Ceiling,source.Floor+128),FloorTexture=source.FloorTexture,CeilingTexture=source.CeilingTexture,Darkness=source.Darkness,NeighborDarkness=source.Darkness});
            for(int i=0;i<expanded.Count;i++) {
                int a=Vertex(m,expanded[i]),b=Vertex(m,expanded[(i+1)%expanded.Count]);if(a==b)continue;
                Wall reverse=m.Walls.FirstOrDefault(w=>w.Start==b && w.End==a);
                Bin.Require(!m.Walls.Any(w=>w.Start==a && w.End==b),"The new sector overlaps an existing sector.");
                if(reverse!=null) {Face front=Front(m,reverse);Bin.Require(front.Solid,"A line cannot border more than two sectors.");front.Solid=false;front.Far=sector;front.Lower=front.Upper;reverse.BackFace=AddFace(m,sector,front.Near,texture).Offset;reverse.Flags&=~1;}
                else {Face face=AddFace(m,sector,-1,texture);m.Walls.Add(new Wall {Start=a,End=b,Face=face.Offset,Flags=1});}
            }
            m.EditGeometry=true;m.NewRoom=false;return sector;
        }
        public static bool StrictCross(Point a,Point b,Point c,Point d) {long c1=Cross(a,b,c),c2=Cross(a,b,d),c3=Cross(c,d,a),c4=Cross(c,d,b);return ((c1>0&&c2<0)||(c1<0&&c2>0))&&((c3>0&&c4<0)||(c3<0&&c4>0));}
        public static void Divide(Map m,Point a,Point b,int texture) {
            Bin.Require(!Same(a,b),"Choose two different points on the sector boundary.");
            int sector=SectorAt(m,new Point((a.X+b.X)/2,(a.Y+b.Y)/2));Bin.Require(sector>=0,"The dividing line must cross an existing sector.");
            foreach(Point p in new[]{a,b}) {bool found=false;for(int i=0;i<m.Walls.Count;i++) {Wall w=m.Walls[i];if((Near(m,w)==sector||Far(m,w)==sector)&&OnSegment(m.Vertices[w.Start],m.Vertices[w.End],p)) {SplitWall(m,i,p);found=true;}}Bin.Require(found,"Both ends must touch an existing boundary.");}
            // Follow this sector's directed contour and split it into two loops.
            var directed=new List<Tuple<int,bool,int,int>>();for(int i=0;i<m.Walls.Count;i++) {Wall w=m.Walls[i];if(Near(m,w)==sector)directed.Add(Tuple.Create(i,false,w.Start,w.End));if(Far(m,w)==sector)directed.Add(Tuple.Create(i,true,w.End,w.Start));}
            int av=Vertex(m,a),bv=Vertex(m,b),current=av;var path=new List<Tuple<int,bool,int,int>>();
            while(current!=bv) {var edge=directed.FirstOrDefault(t=>t.Item3==current&&!path.Contains(t));Bin.Require(edge!=null && path.Count<directed.Count,"Split requires two points on the same closed contour.");path.Add(edge);current=edge.Item4;}
            foreach(Wall w in m.Walls)Bin.Require(!StrictCross(a,b,m.Vertices[w.Start],m.Vertices[w.End]),"The dividing line crosses another boundary.");
            Sector old=m.Sectors[sector];int next=m.Sectors.Count;m.Sectors.Add(new Sector {Floor=old.Floor,Ceiling=old.Ceiling,FloorTexture=old.FloorTexture,CeilingTexture=old.CeilingTexture,Darkness=old.Darkness,NeighborDarkness=old.NeighborDarkness,Type=old.Type});
            foreach(var edge in path) {Wall w=m.Walls[edge.Item1];Face face=edge.Item2?Back(m,w):Front(m,w);Face opposite=edge.Item2?Front(m,w):Back(m,w);
                // Faces may be shared in imported maps: clone before relinking.
                Face clone=AddFace(m,next,face.Far,face.Upper);clone.Lower=face.Lower;if(edge.Item2)w.BackFace=clone.Offset;else w.Face=clone.Offset;
                if(opposite!=null) {Face other=AddFace(m,opposite.Near,next,opposite.Upper);other.Lower=opposite.Lower;if(edge.Item2)w.Face=other.Offset;else w.BackFace=other.Offset;}
            }
            Face frontNew=AddFace(m,next,sector,texture),backNew=AddFace(m,sector,next,texture);
            m.Walls.Add(new Wall {Start=bv,End=av,Face=frontNew.Offset,BackFace=backNew.Offset});m.EditGeometry=true;m.NewRoom=false;
        }
        public static void Reverse(Map m,Wall w) {int v=w.Start;w.Start=w.End;w.End=v;int face=w.Face;w.Face=w.BackFace;w.BackFace=face;}
        public static void Preset(Map m,int sector,SectorMotion motion) {
            Sector s=m.Sectors[sector];SectorMotion previous=s.Motion;
            if(previous!=SectorMotion.None&&previous!=motion){foreach(int index in Boundary(m,sector)){Wall line=m.Walls[index];int oldAction=previous==SectorMotion.Lift?62:previous==SectorMotion.MovingFloor?18:previous==SectorMotion.Stairs?7:73;if((previous==SectorMotion.Door&&EngineActions.Doors[line.Type]==1&&Far(m,line)==sector)||(previous!=SectorMotion.Door&&line.Tag==s.Tag&&line.Type==oldAction)){line.Type=0;line.Tag=0;}}
                if(previous==SectorMotion.Door)s.Ceiling=Math.Max(s.Floor+128,Neighbors(m,sector).Select(n=>m.Sectors[n].Ceiling).DefaultIfEmpty(s.Floor+128).Min());}
            s.Motion=motion;
            if(motion==SectorMotion.Door) {s.Ceiling=s.Floor;foreach(int index in Boundary(m,sector)) {Wall w=m.Walls[index];if(Far(m,w)<0)continue;if(Near(m,w)==sector)Reverse(m,w);w.Type=1;w.Tag=0;} }
            else if(motion!=SectorMotion.None) {
                if(s.Tag==0) {for(int tag=1;tag<256;tag++)if(!m.Sectors.Any(x=>x.Tag==tag)) {s.Tag=tag;break;}}
                var boundary=Boundary(m,sector);Wall trigger=boundary.Select(i=>m.Walls[i]).FirstOrDefault(w=>Far(m,w)>=0)??boundary.Select(i=>m.Walls[i]).FirstOrDefault();
                Bin.Require(trigger!=null,"Draw a sector first.");if(Near(m,trigger)==sector && Far(m,trigger)>=0)Reverse(m,trigger);
                trigger.Tag=s.Tag;trigger.Type=motion==SectorMotion.Lift?62:motion==SectorMotion.MovingFloor?18:motion==SectorMotion.Stairs?7:73;
                if(motion==SectorMotion.Lift) {s.Floor=Math.Max(s.Floor,Neighbors(m,sector).Select(n=>m.Sectors[n].Floor).DefaultIfEmpty(0).Min()+64);s.Ceiling=Math.Max(s.Ceiling,s.Floor+128);}
            }
        }
        public static void Validate(Map m) {
            ValidateDraft(m);
            Bin.Require(m.Walls.Count>=3,"Draw a closed sector before building.");
            foreach(Wall w in m.Walls) {
                Bin.Require(w.Start>=0&&w.End>=0&&w.Start<m.Vertices.Count&&w.End<m.Vertices.Count&&w.Start!=w.End,"A linedef has an invalid vertex.");Face f=Front(m,w),back=Back(m,w);
                Bin.Require(f!=null && f.Near>=0 && f.Near<m.Sectors.Count,"Every linedef needs a front sector.");
                Bin.Require(f.Far<0 || (back!=null && back.Near==f.Far && back.Far==f.Near && f.Far<m.Sectors.Count),"Two-sided line has inconsistent sector links.");
                Bin.Require(w.Type>=0&&w.Type<110&&w.Tag>=0&&w.Tag<=255,"Line actions and tags exceed native SNES limits.");
                if(EngineActions.Doors[w.Type]==1)Bin.Require(f.Far>=0,"Manual door action requires a two-sided line into a door sector.");
                if(EngineActions.Doors[w.Type]==2||EngineActions.Floors[w.Type]!="0"||EngineActions.Lifts[w.Type]!="0"||EngineActions.Ceilings[w.Type]!="0"||w.Type==7||w.Type==8||w.Type==39||w.Type==97)
                    Bin.Require(w.Tag>0&&m.Sectors.Any(s=>s.Tag==w.Tag),"Line action "+w.Type+" needs a nonzero tag matching a target sector.");
            }
            foreach(Point p in m.Vertices)Bin.Require(Math.Abs(p.X)<=8192&&Math.Abs(p.Y)<=8192,"Keep geometry within -8192..8192 units.");
            for(int s=0;s<m.Sectors.Count;s++) {
                var counts=new Dictionary<int,int>();foreach(Wall w in m.Walls) {int sign=(Near(m,w)==s?1:0)-(Far(m,w)==s?1:0);if(sign==0)continue;if(!counts.ContainsKey(w.Start))counts[w.Start]=0;if(!counts.ContainsKey(w.End))counts[w.End]=0;counts[w.Start]+=sign;counts[w.End]-=sign;}
                Bin.Require(counts.Count>=3 && counts.Values.All(v=>v==0),"Sector "+s+" has an open boundary. Finish or repair its outline.");
            }
            for(int i=0;i<m.Walls.Count;i++)for(int j=i+1;j<m.Walls.Count;j++) {Wall a=m.Walls[i],b=m.Walls[j];Bin.Require(!StrictCross(m.Vertices[a.Start],m.Vertices[a.End],m.Vertices[b.Start],m.Vertices[b.End]),"Linedefs cross without a shared vertex.");}
            foreach(Thing t in m.Things.Where(t=>(t.Flags&16)==0)) {int sector=SectorAt(m,new Point(t.X,t.Y));Bin.Require(sector>=0,"Object "+t.Type+" is outside the drawn sectors.");if(t.Type==0)Bin.Require(m.Sectors[sector].Ceiling-m.Sectors[sector].Floor>=56,"The player start needs 56 units of clearance.");}
        }
        public static void ValidateDraft(Map m) {
            Bin.Require(m.Vertices!=null&&m.Walls!=null&&m.Faces!=null&&m.Sectors!=null&&m.Things!=null,"Project has missing geometry arrays.");
            Bin.Require(m.Vertices.Count<=1056&&m.Walls.Count<=1118&&m.Sectors.Count<=205,"Draft exceeds the native geometry limits.");
            foreach(Wall w in m.Walls)Bin.Require(w.Start>=0&&w.End>=0&&w.Start<m.Vertices.Count&&w.End<m.Vertices.Count&&Front(m,w)!=null,"Draft has an invalid line reference.");
        }
    }
}
