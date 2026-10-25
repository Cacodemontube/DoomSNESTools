using System;using System.Collections.Generic;using System.Linq;
namespace DoomSNESed {
    public sealed class GraphicColumnPlan {
        public sealed class Column {public byte[] Data;public int Address=-1;public Column Parent;public int Offset;}
        public readonly Dictionary<string,Column> Columns=new Dictionary<string,Column>();
        public readonly List<Section> Retained=new List<Section>();
        static string Key(bool sprite,int height,byte[] pixels){return sprite+":"+height+":"+Convert.ToBase64String(pixels);}
        public Column Find(GraphicReplacement a,int x){return Columns[Key(a.Sprite,a.Height,NativeColumnCodec.Pixels(a,x))];}
        public GraphicColumnPlan(Rom rom,List<GraphicReplacement> replacements,bool reuse=true){
            var assets=replacements.Where(a=>a.Screen==0&&(a.Sprite||a.Id>=4)).ToArray();foreach(var a in assets)for(int x=0;x<a.Width;x++){var pixels=NativeColumnCodec.Pixels(a,x);string key=Key(a.Sprite,a.Height,pixels);if(!Columns.ContainsKey(key))Columns[key]=new Column{Data=NativeColumnCodec.Encode(pixels,a.Sprite)};}
            if(!reuse){ShareContained();return;}var heights=new HashSet<string>(assets.Select(a=>a.Sprite+":"+a.Height));var retained=new Dictionary<string,Section>();
            for(int sprite=0;sprite<2;sprite++)foreach(int id in sprite!=0?Enumerable.Range(0,232):Enumerable.Range(2,61).Select(i=>i*2)){var a=GraphicsAssets.Describe(rom.Bytes,sprite!=0,id);if(!heights.Contains(a.Sprite+":"+a.Height))continue;byte[] pixels=GraphicsAssets.Decode(rom.Bytes,a);var logical=new GraphicReplacement{Sprite=a.Sprite,Width=a.Width,Height=a.Height,Pixels=pixels};for(int x=0;x<a.Width;x++){string key=Key(a.Sprite,a.Height,NativeColumnCodec.Pixels(logical,x));Column column;if(!Columns.TryGetValue(key,out column))continue;int p=GraphicsAssets.Address(rom.Bytes,a.Header+2+x*3);if(p>=0x200000)continue;int end=GraphicStorage.End(rom.Bytes,a,p),length=end-p;if(length>column.Data.Length)continue;Section prior;if(retained.TryGetValue(key,out prior)&&prior.Size<=length)continue;column.Address=p;retained[key]=new Section{Start=p,Size=length};}}
            Retained.AddRange(retained.Values);
            ShareContained();
        }
        // MKWALL's FindWallImageStrip also searches inside earlier byte
        // streams. Identical encoded substrings are safe even when the parent
        // describes a different-height strip: the child starts its own packet.
        void ShareContained(){var encoding=System.Text.Encoding.GetEncoding(28591);var roots=new List<KeyValuePair<Column,string>>();foreach(var c in Columns.Values.Where(c=>c.Address<0).OrderByDescending(c=>c.Data.Length)){string data=encoding.GetString(c.Data);foreach(var root in roots){int offset=root.Value.IndexOf(data,StringComparison.Ordinal);if(offset<0)continue;c.Parent=root.Key;c.Offset=offset;break;}if(c.Parent==null)roots.Add(new KeyValuePair<Column,string>(c,data));}}
        public void ResolveContained(){foreach(var c in Columns.Values.Where(c=>c.Parent!=null)){Bin.Require(c.Parent.Address>=0,"Unallocated shared graphic strip.");c.Address=c.Parent.Address+c.Offset;}}
        public bool Fits(IEnumerable<Section> free){var space=free.Where(s=>s.Start<0x200000).Select(s=>new Section{Start=s.Start,Size=s.Size}).ToList();foreach(var column in Columns.Values.Where(c=>c.Address<0&&c.Parent==null).OrderByDescending(c=>c.Data.Length)){var section=space.Where(s=>s.Size>=column.Data.Length&&(s.Start&65535)+column.Data.Length<=65536).OrderBy(s=>s.Size).FirstOrDefault();if(section==null)return false;section.Start+=column.Data.Length;section.Size-=column.Data.Length;}return true;}
    }
}

