using System;using System.Collections.Generic;using System.Linq;
namespace DoomSNESed {
    public sealed partial class Rom {
        void ValidateSourceMap(Map map){
            Bin.Require(Project.Maps.Any(m=>m.Slot==map.Slot),"Unknown source map slot.");Map original=Read(map.Slot);
            Bin.Require(map.Vertices.Count>0&&map.Vertices.Count<=1056&&map.Walls.Count>0&&map.Walls.Count<=1118&&map.Sectors.Count>0&&map.Sectors.Count<=205,"Source map exceeds the Reality Engine capacities.");
            for(int i=0;i<map.Sectors.Count;i++){var s=map.Sectors[i];bool unchanged=i<original.Sectors.Count&&s.Floor==original.Sectors[i].Floor&&s.Ceiling==original.Sectors[i].Ceiling;Bin.Require(unchanged||(s.Floor>=-4096&&s.Ceiling<=4096&&s.Floor<=s.Ceiling),"Edited sector heights must fit the engine and floor must be below ceiling.");Bin.Require(s.FloorTexture>=0&&s.FloorTexture<255&&s.CeilingTexture>=0&&s.CeilingTexture<=255&&s.Darkness>=0&&s.Darkness<=255&&s.NeighborDarkness>=0&&s.NeighborDarkness<=255&&s.Tag>=0&&s.Tag<=255&&s.Type>=0&&s.Type<=63,"Invalid source sector value.");}
            Bin.Require(map.Things.Count(t=>t.Type==0)==1&&map.Things.All(t=>t.Type>=0&&t.Type<64&&t.X>=-32768&&t.X<=32767&&t.Y>=-32768&&t.Y<=32767),"Invalid source map object.");
            if(!map.EditGeometry&&!map.NewRoom){Bin.Require(map.Vertices.Count==original.Vertices.Count&&map.Walls.Count==original.Walls.Count&&map.Sectors.Count==original.Sectors.Count&&map.Faces.Count==original.Faces.Count,"Use geometry editing for structural map changes.");Bin.Require(ObjectBytes(map.Things).Length<=B(map.Slot,0x363e)-B(map.Slot,0x3608),"Object list exceeds its source allocation.");}
            foreach(var f in map.Faces)Bin.Require(f.Upper>=0&&f.Upper<=124&&f.Upper%2==0&&(f.Solid||f.Lower>=0&&f.Lower<=124&&f.Lower%2==0),"Invalid source wall texture.");
        }
        byte[] CompileSourceLayout(){
            Bin.Require(Project.CampaignSlots==null,"Source-linked ROMs retain their own campaign code. Edit the existing map or assets without a PWAD campaign.");
            var target=(byte[])Bytes.Clone();RomLayout.Inherit(Bytes,target);var free=RomLayout.Free(Bytes);Budget=new StorageSummary{Capacity=free.Sum(s=>s.Size)};
            foreach(var map in Project.Maps){Validate(map);if(Project.RetailAssets)Bin.Require(MapState(map)==MapState(Read(map.Slot)),"ROM-only projects preserve their source maps.");else if(map.EditGeometry){NativeCompiler.Build(this,target,map);}else if(map.NewRoom){RoomCompiler.Build(this,target,map);}else{
                int sectors=Far(0x3566,map.Slot);for(int i=0;i<map.Sectors.Count;i++)WriteSector(target,sectors+i*10,map.Sectors[i]);
                int faces=A(map.Slot,0x3530);foreach(var f in map.Faces){int p=faces+f.Offset;if(f.Solid)target[p+1]=(byte)f.Upper;else{target[p+2]=(byte)f.Upper;target[p+3]=(byte)f.Lower;}}
                byte[] objects=ObjectBytes(map.Things);Buffer.BlockCopy(objects,0,target,B(map.Slot,0x3608),objects.Length);
            }}
            GraphicsAssets.Apply(this,target,free);AudioBuild.Apply(this,target,free,Resources);TextAssets.Apply(this,target,free);
            Budget.OutputBytes=target.Length;Budget.ReusableFree=free.Sum(s=>s.Size);WadConverter.Checksum(target);return target;
        }
    }
}
