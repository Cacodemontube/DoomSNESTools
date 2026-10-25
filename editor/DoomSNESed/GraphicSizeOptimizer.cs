using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
namespace DoomSNESed {
    // Lossy preparation is explicit and reversible; storage still uses the
    // retail renderer's palette indices and signed column packets.
    public static class GraphicSizeOptimizer {
        public static readonly string[] Modes={"Preserve quality (lossless)","Balanced size: 32 colours, 2 x 1 detail","Smaller: 16 colours, 2 x 2 detail","Smallest: 8 colours, 4 x 2 detail","Colours only: 32 colours, full detail"};
        public static bool Supported(GraphicReplacement a){return a.Screen==0&&(a.Sprite||a.Id>=4);}
        static long Distance(Color a,Color b){long r=a.R-b.R,g=a.G-b.G,z=a.B-b.B;return r*r+2*g*g+z*z;}
        public static GraphicReplacement FromRom(Rom rom,NativeGraphic a){return new GraphicReplacement{Sprite=a.Sprite,Screen=a.Screen,Id=a.Id,Width=a.Width,Height=a.Height,Pixels=GraphicsAssets.Decode(rom.Bytes,a)};}
        public static void Apply(Rom rom,GraphicReplacement a,int mode){
            Bin.Require(Supported(a)&&mode>=0&&mode<Modes.Length,"Size reduction applies to wall textures and sprite frames.");
            if(a.OriginalPixels==null)a.OriginalPixels=(byte[])a.Pixels.Clone();
            Bin.Require(a.OriginalPixels.Length==a.Width*a.Height,"Invalid original artwork dimensions.");
            a.SizeMode=mode;a.Pixels=(byte[])a.OriginalPixels.Clone();if(mode==0)return;
            var palette=Enumerable.Range(0,256).Select(i=>AssetPreview.Palette(rom,i)).ToArray();
            var histogram=new int[256];foreach(byte p in a.OriginalPixels)if(!a.Sprite||p!=0)histogram[p]++;
            int limit=mode==1||mode==4?32:mode==2?16:8;var colours=new List<int>();
            // Deterministic weighted colour clustering, without dithering:
            // dithering creates extra vertical transitions and defeats RLE.
            var occupied=Enumerable.Range(0,256).Where(i=>histogram[i]>0).ToArray();
            if(occupied.Length==0)return;
            colours.Add(occupied.OrderByDescending(i=>histogram[i]).ThenBy(i=>i).First());
            while(colours.Count<Math.Min(limit,occupied.Length)){
                int next=occupied.Where(i=>!colours.Contains(i)).OrderByDescending(i=>colours.Min(c=>Distance(palette[i],palette[c]))*histogram[i]).ThenBy(i=>i).First();colours.Add(next);
            }
            for(int iteration=0;iteration<6;iteration++){
                var weights=new long[colours.Count];var red=new long[colours.Count];var green=new long[colours.Count];var blue=new long[colours.Count];
                foreach(int i in occupied){int best=Enumerable.Range(0,colours.Count).OrderBy(c=>Distance(palette[i],palette[colours[c]])).First();long w=histogram[i];weights[best]+=w;red[best]+=palette[i].R*w;green[best]+=palette[i].G*w;blue[best]+=palette[i].B*w;}
                for(int c=0;c<colours.Count;c++)if(weights[c]>0){var mean=Color.FromArgb((int)(red[c]/weights[c]),(int)(green[c]/weights[c]),(int)(blue[c]/weights[c]));colours[c]=Enumerable.Range(a.Sprite?1:0,a.Sprite?255:256).OrderBy(i=>Distance(mean,palette[i])).ThenBy(i=>i).First();}
            }
            int blockX=mode==4?1:mode==3?4:2,blockY=mode==1||mode==4?1:2;
            for(int y=0;y<a.Height;y+=blockY)for(int x=0;x<a.Width;x+=blockX){
                int r=0,g=0,b=0,count=0;for(int dy=0;dy<blockY&&y+dy<a.Height;dy++)for(int dx=0;dx<blockX&&x+dx<a.Width;dx++){int p=a.OriginalPixels[(y+dy)*a.Width+x+dx];if(a.Sprite&&p==0)continue;r+=palette[p].R;g+=palette[p].G;b+=palette[p].B;count++;}if(count==0)continue;
                var mean=Color.FromArgb(r/count,g/count,b/count);byte match=(byte)colours.OrderBy(i=>Distance(mean,palette[i])).ThenBy(i=>i).First();
                for(int dy=0;dy<blockY&&y+dy<a.Height;dy++)for(int dx=0;dx<blockX&&x+dx<a.Width;dx++){int pos=(y+dy)*a.Width+x+dx;if(!a.Sprite||a.OriginalPixels[pos]!=0)a.Pixels[pos]=match;}
            }
            // Very simple artwork can already be optimal. Never replace it
            // with a larger independently packed image just to meet a preset.
            int reduced=NativeColumnCodec.Estimate(a);byte[] result=a.Pixels;a.Pixels=a.OriginalPixels;int original=NativeColumnCodec.Estimate(a);a.Pixels=reduced<original?result:(byte[])a.OriginalPixels.Clone();
        }
        public static string Report(GraphicReplacement a){if(!Supported(a)||a.OriginalPixels==null)return "";byte[] current=a.Pixels;int packed=NativeColumnCodec.Estimate(a);a.Pixels=a.OriginalPixels;int before=NativeColumnCodec.Estimate(a);a.Pixels=current;return before.ToString("N0")+" -> "+packed.ToString("N0")+" B ("+(before==0?0:100.0*(before-packed)/before).ToString("0.0")+"% smaller), before cross-asset sharing. Transparency and dimensions preserved.";}
    }
}
