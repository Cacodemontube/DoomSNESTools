using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DoomSNESed {
    public sealed class AssetPreview:Control {
        Bitmap bitmap;string caption="Select a wall or sector to preview its ROM asset.";
        public bool HasImage {get{return bitmap!=null;}}
        public AssetPreview() {DoubleBuffered=true;Dock=DockStyle.Bottom;Height=160;BackColor=Color.FromArgb(25,29,35);}
        public static Color Palette(Rom rom,int index) {
            // RLRGBs, normal palette, USA retail. Verified against the guest
            // CGRAM dump; colours and pixels are read from the user's ROM.
            int rgb=Bin.U16(rom.Bytes,RomLayout.At(rom.Bytes,0x1c1358)+index*2);
            return Color.FromArgb((rgb&31)*255/31,((rgb>>5)&31)*255/31,((rgb>>10)&31)*255/31);
        }
        public Bitmap CopyImage() {return bitmap==null?null:new Bitmap(bitmap);}
        public void ShowFlat(Rom rom,int id) {
            Clear();if(id==255)ShowWall(rom,0);caption=id==255?"Ceiling sky (255)":"Solid surface colour "+id;
            if(id!=255) {bitmap=new Bitmap(32,32);using(Graphics g=Graphics.FromImage(bitmap))g.Clear(Palette(rom,id));}Invalidate();
        }
        public void Clear() {if(bitmap!=null)bitmap.Dispose();bitmap=null;caption="Select a wall or sector to preview its ROM asset.";Invalidate();}
        public void ShowWall(Rom rom,int id) {
            Clear();caption="ROM wall asset "+id;
            try {
                GraphicReplacement replacement=rom.Project.Graphics==null?null:rom.Project.Graphics.FirstOrDefault(g=>!g.Sprite&&g.Screen==0&&g.Id==id);
                if(replacement!=null||id==0||id==2){NativeGraphic asset=GraphicsAssets.Describe(rom.Bytes,false,id);bitmap=GraphicsAssets.Preview(rom,asset,replacement==null?GraphicsAssets.Decode(rom.Bytes,asset):replacement.Pixels);caption+=" ("+asset.Width+" x "+asset.Height+")";Invalidate();return;}
                Bin.Require(rom.WallTextures.Contains(id),"Unknown wall ID");Bitmap cached;if(rom.WallImages.TryGetValue(id,out cached)){bitmap=new Bitmap(cached);caption+="  ("+bitmap.Width+" x "+bitmap.Height+")";Invalidate();return;}
                byte[] b=rom.Bytes;int header=RomLayout.At(b,0x1b0000)+0x7e+Bin.U16(b,RomLayout.At(b,0x1b0000)+id);
                int height=b[header]&254;if(height==0)height=256;int width=b[header+1]+1;
                Bin.Require(width<=256 && height<=256,"Invalid wall size");bitmap=new Bitmap(width,height);
                for(int x=0;x<width;x++) {
                    int table=header+2+x*3,bank=b[table+2],address=Bin.U16(b,table);
                    int p=bank<0x40?(bank&63)*0x8000+(address&0x7fff):(bank&31)*0x10000+address;
                    for(int y=0;y<height;) {
                        int run=(sbyte)b[p++];Bin.Require(run!=0,"Invalid wall RLE");int count=Math.Min(Math.Abs(run),height-y);
                        if(run<0) {Color colour=Palette(rom,b[p++]);while(count-->0)bitmap.SetPixel(x,y++,colour);}
                        else while(count-->0)bitmap.SetPixel(x,y++,Palette(rom,b[p++]));
                    }
                }
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);rom.WallImages[id]=new Bitmap(bitmap);caption+="  ("+width+" x "+height+")";
            } catch(Exception) {if(bitmap!=null)bitmap.Dispose();bitmap=null;caption="Preview unavailable for wall "+id;}
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);e.Graphics.DrawString(caption,Font,Brushes.Silver,new RectangleF(8,8,Width-16,35));
            if(bitmap!=null) {float scale=Math.Min((Width-20f)/bitmap.Width,(Height-48f)/bitmap.Height);int w=(int)(bitmap.Width*scale),h=(int)(bitmap.Height*scale);e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(bitmap,new Rectangle((Width-w)/2,42,w,h),0,0,bitmap.Width,bitmap.Height,GraphicsUnit.Pixel);}
        }
        protected override void Dispose(bool disposing) {if(disposing && bitmap!=null)bitmap.Dispose();base.Dispose(disposing);}
    }
}

