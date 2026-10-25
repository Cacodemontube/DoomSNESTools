using System;using System.Drawing;
namespace DoomSNESed {
    public static class PickupFont {
        public const int Screen=20,Address=0x1f150e,Bytes=58*64;
        public static NativeGraphic Asset(){return new NativeGraphic{Screen=Screen,Name="Red pickup / message font (ASCII ! through Z)",Width=128,Height=32};}
        public static byte[] Pixels(byte[] rom){var data=new byte[128*32];for(int c=0;c<58;c++)for(int y=0;y<8;y++)for(int x=0;x<8;x++)data[(c/16*8+y)*128+c%16*8+x]=rom[RomLayout.At(rom,Address)+c*64+y*8+x];return data;}
        public static Bitmap Preview(Rom rom,GraphicReplacement replacement){byte[] pixels=replacement==null?Pixels(rom.Bytes):replacement.Pixels;var image=new Bitmap(128,32);for(int y=0;y<32;y++)for(int x=0;x<128;x++)image.SetPixel(x,y,pixels[y*128+x]==0?Color.Transparent:AssetPreview.Palette(rom,pixels[y*128+x]));return image;}
        public static GraphicReplacement Import(Rom rom,string path){using(var image=new Bitmap(path)){Bin.Require(image.Width==128&&image.Height==32,"The pickup font requires a 128 x 32 PNG/BMP. Export its template first.");var pixels=new byte[128*32];byte[] original=Pixels(rom.Bytes);for(int y=0;y<32;y++)for(int x=0;x<128;x++){Color colour=image.GetPixel(x,y);if(colour.A<128)continue;int selected=original[y*128+x];if(selected==0)selected=1;Color preferred=AssetPreview.Palette(rom,selected);int best=(preferred.R-colour.R)*(preferred.R-colour.R)+(preferred.G-colour.G)*(preferred.G-colour.G)+(preferred.B-colour.B)*(preferred.B-colour.B);for(int i=1;i<256;i++){Color p=AssetPreview.Palette(rom,i);int d=(p.R-colour.R)*(p.R-colour.R)+(p.G-colour.G)*(p.G-colour.G)+(p.B-colour.B)*(p.B-colour.B);if(d<best){best=d;selected=i;}}pixels[y*128+x]=(byte)selected;}return new GraphicReplacement{Screen=Screen,Width=128,Height=32,Pixels=pixels};}}
        public static void Apply(byte[] target,GraphicReplacement asset){Bin.Require(asset.Width==128&&asset.Height==32&&asset.Pixels!=null&&asset.Pixels.Length==4096,"Invalid pickup font dimensions.");for(int c=0;c<58;c++)for(int y=0;y<8;y++)for(int x=0;x<8;x++)target[RomLayout.At(target,Address)+c*64+y*8+x]=asset.Pixels[(c/16*8+y)*128+c%16*8+x];}
    }
}

