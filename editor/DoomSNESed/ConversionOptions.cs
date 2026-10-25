using System;
using System.Collections.Generic;
using System.Linq;
namespace DoomSNESed {
    public sealed class ConversionOptions {
        public bool MergeSectors=true,WeldVertices=true,CollapseWalls=true,CompactTags=true,BestFit=true,ClosestActions=true,RejectDisconnected=true,RemoveDecorations;
        public int LightingStep,MonsterLimit;
    }
    public static class ConversionOptimizer {
        public static void Apply(Rom rom,Map map,ConversionOptions options,List<string> report,Action merge){
            map.OptimizeReject=options.RejectDisconnected;int sectors=map.Sectors.Count,lines=map.Walls.Count,vertices=map.Vertices.Count,things=map.Things.Count;
            if(options.WeldVertices){var coordinates=new Dictionary<string,int>();var points=new List<Point>();var indices=new int[map.Vertices.Count];for(int i=0;i<indices.Length;i++){Point p=map.Vertices[i];string key=p.X+","+p.Y;int index;if(!coordinates.TryGetValue(key,out index)){index=points.Count;coordinates.Add(key,index);points.Add(p);}indices[i]=index;}foreach(Wall w in map.Walls){w.Start=indices[w.Start];w.End=indices[w.End];}map.Vertices=points;Bin.Require(map.Walls.All(w=>w.Start!=w.End),"Welding exposed a zero-length linedef; repair it in the source WAD.");}
            Bin.Require(new[]{0,8,16,32,64}.Contains(options.LightingStep),"Lighting step must be 0, 8, 16, 32 or 64.");
            if(options.LightingStep>0){int changed=0;foreach(Sector s in map.Sectors.Where(s=>s.Type==0&&s.Tag==0&&s.Motion==SectorMotion.None)){int light=Math.Min(247,(int)Math.Round(s.Darkness/(double)options.LightingStep)*options.LightingStep);if(light!=s.Darkness)changed++;s.Darkness=light;s.NeighborDarkness=light;}report.Add("Lighting simplification: "+changed+" static sectors quantized to "+options.LightingStep+"-unit darkness steps (appearance changes).");}
            if(options.MergeSectors)merge();
            if(options.CollapseWalls)Collapse(rom,map);
            if(options.RemoveDecorations){map.Things.RemoveAll(t=>t.Type>=50&&t.Type<=63);report.Add("Scenery removal enabled: decorative sprites and their collision are removed; gameplay pickups, barrels and starts remain.");}
            Bin.Require(options.MonsterLimit>=0&&options.MonsterLimit<=80,"Monster limit must be 0 (off) or 1..80 per skill.");
            if(options.MonsterLimit>0){Thing player=map.Things.Single(t=>t.Type==0);for(int skill=0;skill<3;skill++){int bit=1<<skill;var monsters=map.Things.Where(t=>t.Type>=6&&t.Type<=14&&(t.Flags&bit)!=0).ToList();int excess=Math.Max(0,monsters.Count-options.MonsterLimit);var candidates=monsters.Where(t=>t.Type<12).OrderByDescending(t=>(long)(t.X-player.X)*(t.X-player.X)+(long)(t.Y-player.Y)*(t.Y-player.Y)).Take(excess).ToList();Bin.Require(candidates.Count==excess,"Monster limit is below the boss count; increase the limit. Bosses are preserved.");foreach(Thing t in candidates)t.Flags&=~bit;report.Add("Skill "+(skill+1)+": "+candidates.Count+" distant non-boss monsters removed from this skill (gameplay changes).");}map.Things.RemoveAll(t=>(t.Flags&7)==0);}
            // Keep cleanup independent of sector merging, including when merging is disabled.
            var faces=new HashSet<int>(map.Walls.SelectMany(w=>new[]{w.Face,w.BackFace}));map.Faces.RemoveAll(f=>!faces.Contains(f.Offset));var used=map.Walls.SelectMany(w=>new[]{w.Start,w.End}).Distinct().OrderBy(i=>i).ToArray();var remap=new Dictionary<int,int>();for(int i=0;i<used.Length;i++)remap[used[i]]=i;foreach(Wall w in map.Walls){w.Start=remap[w.Start];w.End=remap[w.End];}map.Vertices=used.Select(i=>map.Vertices[i]).ToList();
            report.Add("Before -> after: "+sectors+" -> "+map.Sectors.Count+" sectors; "+lines+" -> "+map.Walls.Count+" lines; "+vertices+" -> "+map.Vertices.Count+" vertices; "+things+" -> "+map.Things.Count+" things.");
        }
        static bool SameFace(Face a,Face b){return a!=null&&b!=null&&a.Solid&&b.Solid&&a.Near==b.Near&&a.Upper==b.Upper;}
        static int TextureWidth(Rom rom,int id){int header=0x1b007e+Bin.U16(rom.Bytes,0x1b0000+id);return rom.Bytes[header+1]+1;}
        public static void Collapse(Rom rom,Map map){
            bool changed;do{changed=false;var incident=new Dictionary<int,List<Wall>>();foreach(Wall w in map.Walls)foreach(int v in new[]{w.Start,w.End}){List<Wall> list;if(!incident.TryGetValue(v,out list)){list=new List<Wall>();incident[v]=list;}list.Add(w);}
                foreach(var pair in incident){if(pair.Value.Count!=2)continue;Wall a=pair.Value[0],b=pair.Value[1];if(a.End!=pair.Key||b.Start!=pair.Key){Wall swap=a;a=b;b=swap;}if(a.End!=pair.Key||b.Start!=pair.Key||a.Start==b.End)continue;
                    if(a.Type!=0||b.Type!=0||a.Tag!=0||b.Tag!=0||a.Flags!=b.Flags||a.OffsetY!=b.OffsetY||a.BackFace>=0||b.BackFace>=0)continue;Face front=GeometryTools.Front(map,a);if(!SameFace(front,GeometryTools.Front(map,b)))continue;
                    Point p=map.Vertices[a.Start],mid=map.Vertices[a.End],end=map.Vertices[b.End];if(GeometryTools.Cross(p,mid,end)!=0||(long)(mid.X-p.X)*(end.X-mid.X)+(long)(mid.Y-p.Y)*(end.Y-mid.Y)<=0)continue;
                    double length=(mid.Vec-p.Vec).GetLength();int width=TextureWidth(rom,front.Upper);if(Math.Abs(length-Math.Round(length))>0.00001||((int)Math.Round(length)+a.OffsetX-b.OffsetX)%width!=0)continue;
                    // Degree two, identical solid surfaces, no action/tag, and continuous texture phase.
                    a.End=b.End;map.Walls.Remove(b);changed=true;break;
                }
            }while(changed);
        }
        public static Dictionary<int,int> Tags(byte[] sectors,byte[] lines,bool compact,List<string> report){var values=new SortedSet<int>();for(int p=0;p<sectors.Length;p+=26)if(Bin.U16(sectors,p+24)!=0)values.Add(Bin.U16(sectors,p+24));for(int p=0;p<lines.Length;p+=14)if(Bin.U16(lines,p+8)!=0)values.Add(Bin.U16(lines,p+8));Bin.Require(!compact||values.Count<=255,"More than 255 distinct nonzero tags; combine tag groups in the source map.");var result=new Dictionary<int,int>{{0,0}};int next=1;foreach(int value in values)result[value]=compact?next++:value;if(compact&&values.Any(v=>result[v]!=v))report.Add("Tags compacted: "+string.Join(", ",values.Where(v=>result[v]!=v).Select(v=>v+"->"+result[v]).ToArray())+". Line/sector links preserved.");return result;}
        public static void Budget(Map map,NativeBuildInfo info,List<string> report){if(map.OptimizeReject){byte[] reject=NativeCompiler.Reject(map);int rejected=Enumerable.Range(0,map.Sectors.Count*map.Sectors.Count).Count(i=>(reject[i/8]&(1<<(i&7)))!=0);report.Add("Visibility: "+rejected+" disconnected sector pairs rejected before enemy sight checks; connected door/portal paths stay visible.");}report.Add("BUDGET "+map.Name+": geometry "+info.GeometryBytes+" / "+info.GeometryCapacity+" bytes; map data "+info.DataBytes+" / "+info.DataCapacity+" bytes; "+info.Segments+" / 2438 segments, "+info.Vertices+" / 1056 vertices, "+info.Areas+" / 512 subsectors.");for(int skill=0;skill<3;skill++){int bit=1<<skill;int moving=map.Things.Count(t=>(t.Flags&bit)!=0&&(t.Flags&0x50)==0x40),fixedCount=map.Things.Count(t=>(t.Flags&bit)!=0&&(t.Flags&0x50)==0);report.Add("  Skill "+(skill+1)+" object pools: "+moving+" / 84 movable, "+fixedCount+" / 180 fixed.");}if(info.GeometryBytes>info.GeometryCapacity*.9||info.DataBytes>info.DataCapacity*.9)report.Add("  Near ROM capacity: try collinear cleanup / matching-sector merge; optional lighting simplification may permit more sector merges.");}
    }
}
