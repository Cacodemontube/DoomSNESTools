using System;using System.IO;using System.Linq;using System.Drawing;using System.Collections.Generic;
namespace DoomSNESed {
    public static class GraphicSizeTests {
        static void Check(bool ok,string message){Bin.Require(ok,"Graphic size: "+message);}
        public static void Run(string path,string output){
            string resources=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"editor-data"),folder=Path.Combine(output,"size-018");Directory.CreateDirectory(folder);var retail=new Rom(path,resources);
            var tall=new GraphicReplacement{Sprite=true,Id=39,Width=1,Height=8,Pixels=new byte[]{1,2,3,4,5,6,7,8}};var shortOne=new GraphicReplacement{Sprite=true,Id=40,Width=1,Height=4,Pixels=new byte[]{1,2,3,4}};
            var plan=new GraphicColumnPlan(retail,new List<GraphicReplacement>{shortOne,tall},false);var parent=plan.Find(tall,0);var child=plan.Find(shortOne,0);Check(child.Parent==parent&&child.Offset==4,"shorter stream shares an encoded substring");Check(plan.Fits(new[]{new Section{Start=0x10000,Size=parent.Data.Length}}),"fit check charges containing strip once");parent.Address=0x10000;plan.ResolveContained();Check(child.Address==0x10004,"contained pointer resolves to parent bytes");
            foreach(string name in new[]{"KICK","iconofshit"}){
                string source=Path.Combine(Path.GetDirectoryName(path),"testfiles",name+".sfc");if(!File.Exists(source))continue;var rom=new Rom(source,resources);var changes=new List<GraphicReplacement>();
                foreach(bool sprite in new[]{false,true})foreach(int id in sprite?Enumerable.Range(0,232):rom.WallTextures.Where(i=>i>=4)){
                    var native=GraphicsAssets.Describe(rom.Bytes,sprite,id);var a=GraphicSizeOptimizer.FromRom(rom,native);if(a.Pixels.SequenceEqual(GraphicsAssets.Decode(retail.Bytes,GraphicsAssets.Describe(retail.Bytes,sprite,id))))continue;
                    byte[] original=(byte[])a.Pixels.Clone();int before=NativeColumnCodec.Estimate(a);byte[] balanced=null;
                    for(int mode=1;mode<GraphicSizeOptimizer.Modes.Length;mode++){
                        GraphicSizeOptimizer.Apply(rom,a,mode);Check(NativeColumnCodec.Estimate(a)<=before,"preset never increases independent strip cost");Check(a.Pixels.Length==original.Length,"dimensions preserved");Check(!sprite||Enumerable.Range(0,original.Length).All(i=>(original[i]==0)==(a.Pixels[i]==0)),"sprite transparency unchanged");if(mode==1)balanced=(byte[])a.Pixels.Clone();
                    }
                    GraphicSizeOptimizer.Apply(rom,a,0);Check(a.Pixels.SequenceEqual(original),"preserve quality restores source pixels");GraphicSizeOptimizer.Apply(rom,a,1);Check(a.Pixels.SequenceEqual(balanced),"changing presets is deterministic and noncumulative");
                    var json=ProjectJson.Create();var reopened=json.Deserialize<GraphicReplacement>(json.Serialize(a));GraphicSizeOptimizer.Apply(rom,reopened,0);Check(reopened.Pixels.SequenceEqual(original),"saved original artwork survives JSON round trip");changes.Add(a);
                }
                Check(changes.Count>0,"modified ROM has changed graphics");
                string png=Path.Combine(folder,name+"-source.png"),bmp=Path.Combine(folder,name+"-source.bmp");
                // BMP has no alpha; compare an opaque wall export.
                var wall=GraphicsAssets.Describe(rom.Bytes,false,52);using(var image=GraphicsAssets.Preview(rom,wall,GraphicsAssets.Decode(rom.Bytes,wall))){image.Save(png,System.Drawing.Imaging.ImageFormat.Png);image.Save(bmp,System.Drawing.Imaging.ImageFormat.Bmp);}Check(GraphicsAssets.Import(rom,wall,png).Pixels.SequenceEqual(GraphicsAssets.Import(rom,wall,bmp).Pixels),"PNG and BMP produce identical native wall indices");
                rom.Project.Graphics=changes;rom.Project.RetailAssets=true;rom.Project.CompactAssets=true;byte[] packed=rom.Compile();Check(packed.Length==0x200000,"optimized ROM remains 2 MiB");
                foreach(bool sprite in new[]{false,true})foreach(int id in sprite?Enumerable.Range(0,232):Enumerable.Range(2,61).Select(i=>i*2)){var change=changes.FirstOrDefault(a=>a.Sprite==sprite&&a.Id==id);byte[] expected=change==null?GraphicsAssets.Decode(rom.Bytes,GraphicsAssets.Describe(rom.Bytes,sprite,id)):change.Pixels;Check(GraphicsAssets.Decode(packed,GraphicsAssets.Describe(packed,sprite,id)).SequenceEqual(expected),"every changed and unchanged wall/sprite survives packing");}
                string saved=Path.Combine(folder,name+"-balanced");File.WriteAllBytes(saved+".sfc",packed);rom.SaveProject(saved+".sned");if(!RomLayout.SourceBuilt(rom.Bytes))rom.SavePatch(saved+".dse",packed);int beforeTotal=0,afterTotal=0;foreach(var a in changes){afterTotal+=NativeColumnCodec.Estimate(a);byte[] p=a.Pixels;a.Pixels=a.OriginalPixels;beforeTotal+=NativeColumnCodec.Estimate(a);a.Pixels=p;}Console.WriteLine(name+" balanced strips "+beforeTotal+" -> "+afterTotal+"; new="+rom.Budget.Graphics+" reclaimed="+rom.Budget.GraphicsReclaimed+" reused="+rom.Budget.GraphicsReused);
                var reload=new Rom(source,resources);reload.LoadProject(saved+".sned");Check(reload.Compile().SequenceEqual(packed),"optimized project rebuilds exact ROM");
            }
            File.WriteAllText(Path.Combine(output,"graphic-size-018-results.txt"),"PASS native substring sharing, presets reduce strip cost, transparency, reversible/noncumulative edits, JSON source retention, PNG/BMP equivalence, user ROM pixel parity, 2 MiB output and project rebuilds.\r\n");
        }
    }
}

