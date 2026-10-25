using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DoomSNESed {
    public sealed class SceneCamera {public double X,Y,Z=42,Angle,Pitch;}
    public sealed class ScenePick {public int Wall=-1,Face=-1,Sector=-1;public string Surface="";}
    public sealed class SceneTextures {
        public readonly List<GraphicReplacement> Graphics;public readonly Rom Rom;public readonly int[] Palette=new int[256];
        public sealed class Texture {public int Width,Height;public int[] Pixels;}
        readonly Dictionary<int,Texture> textures=new Dictionary<int,Texture>();
        public SceneTextures(Rom rom){Rom=rom;Graphics=rom.Project.Graphics;for(int i=0;i<256;i++)Palette[i]=AssetPreview.Palette(rom,i).ToArgb();}
        public Texture Get(int id){Texture texture;if(textures.TryGetValue(id,out texture))return texture;
            using(var preview=new AssetPreview()){preview.ShowWall(Rom,id);using(Bitmap image=preview.CopyImage()){if(image==null){texture=new Texture {Width=1,Height=1,Pixels=new[]{Color.Magenta.ToArgb()}};}else{var data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);int[] pixels=new int[image.Width*image.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);image.UnlockBits(data);texture=new Texture {Width=image.Width,Height=image.Height,Pixels=pixels};}}}
            textures[id]=texture;return texture;
        }
    }
    public static class SceneRenderer {
        sealed class Hit {public double Distance,U;public int Line;}
        static int Shade(int color,double light){light=Math.Max(.08,Math.Min(1,light));return unchecked((int)0xff000000)|((int)(((color>>16)&255)*light)<<16)|((int)(((color>>8)&255)*light)<<8)|(int)((color&255)*light);}
        static int Mod(int n,int size){return(n%size+size)%size;}
        static double Ceiling(Map map,int sector,bool doors){Sector s=map.Sectors[sector];if(doors && s.Motion==SectorMotion.Door){var neighbors=GeometryTools.Neighbors(map,sector);return neighbors.Select(n=>map.Sectors[n].Ceiling).DefaultIfEmpty(s.Ceiling).Min()-4;}return s.Ceiling;}
        public static Bitmap Render(Map map,SceneCamera camera,SceneTextures textures,int width,int height,bool doors,out ScenePick pick) {
            int[] pixels=new int[width*height];pick=new ScenePick();ScenePick selected=pick;
            var faceLookup=map.Faces.ToDictionary(f=>f.Offset);Face[] frontFaces=new Face[map.Walls.Count],backFaces=new Face[map.Walls.Count];
            for(int i=0;i<map.Walls.Count;i++){faceLookup.TryGetValue(map.Walls[i].Face,out frontFaces[i]);faceLookup.TryGetValue(map.Walls[i].BackFace,out backFaces[i]);}
            int origin=GeometryTools.SectorAt(map,new Point((int)camera.X,(int)camera.Y));
            double focal=width*.72,horizon=height*.5+camera.Pitch,fx=Math.Cos(camera.Angle),fy=Math.Sin(camera.Angle);
            for(int x=0;x<width;x++) {
                for(int y=0;y<height;y++)pixels[y*width+x]=Shade(Color.FromArgb(35,58,51).ToArgb(),.45+.35*y/height);
                double k=(x-width*.5)/focal,dx=fx+fy*k,dy=fy-fx*k;var hits=new List<Hit>();
                for(int i=0;i<map.Walls.Count;i++) {Wall w=map.Walls[i];Point a=map.Vertices[w.Start],b=map.Vertices[w.End];double sx=b.X-a.X,sy=b.Y-a.Y,den=dx*sy-dy*sx;if(Math.Abs(den)<1e-8)continue;double ax=a.X-camera.X,ay=a.Y-camera.Y,t=(ax*sy-ay*sx)/den,u=(ax*dy-ay*dx)/den;
                    if(t>.05 && u>=-.00001&&u<=1.00001)hits.Add(new Hit {Distance=t,U=u,Line=i});
                }
                hits.Sort((a,b)=>a.Distance.CompareTo(b.Distance));int sector=origin,top=0,bottom=height-1;
                if(sector<0 && hits.Count>0){Face initial=GeometryTools.Front(map,map.Walls[hits[0].Line]);sector=initial==null?0:initial.Near;}
                foreach(Hit hit in hits) {
                    if(sector<0||sector>=map.Sectors.Count||top>bottom)break;Wall wall=map.Walls[hit.Line];Face front=frontFaces[hit.Line],face=front;
                    if(front==null)continue;if(front.Near!=sector){if(front.Far!=sector)continue;face=backFaces[hit.Line];if(face==null)continue;}
                    Sector near=map.Sectors[sector];double ceil=Ceiling(map,sector,doors),depth=hit.Distance;int ceilY=(int)Math.Round(horizon-(ceil-camera.Z)*focal/depth),floorY=(int)Math.Round(horizon-(near.Floor-camera.Z)*focal/depth);
                    double light=(1-near.Darkness/280.0)/(1+depth/4000.0);
                    Action<int,int,int,string> solid=(y1,y2,colour,surfaceName)=>{y1=Math.Max(top,Math.Max(0,y1));y2=Math.Min(bottom,Math.Min(height-1,y2));for(int y=y1;y<=y2;y++)pixels[y*width+x]=colour;
                        if(x==width/2 && y1<=height/2 && y2>=height/2){selected.Wall=hit.Line;selected.Face=face.Offset;selected.Sector=sector;selected.Surface=surfaceName;}
                    };
                    solid(top,ceilY-1,near.CeilingTexture==255?Color.FromArgb(40,64,65).ToArgb():Shade(textures.Palette[near.CeilingTexture],light),"Ceiling");
                    solid(floorY+1,bottom,Shade(textures.Palette[near.FloorTexture],light),"Floor");
                    Action<int,int,int,string> surface=(y1,y2,id,name)=>{
                        if(y1>y2)return;var texture=textures.Get(id);double length=(map.Vertices[wall.End].Vec-map.Vertices[wall.Start].Vec).GetLength();int tx=Mod((int)(hit.U*length)+wall.OffsetX,texture.Width);
                        if(face!=front)tx=texture.Width-1-tx;y1=Math.Max(top,Math.Max(0,y1));y2=Math.Min(bottom,Math.Min(height-1,y2));
                        for(int y=y1;y<=y2;y++){double z=camera.Z+(horizon-y)*depth/focal;int ty=Mod((int)(ceil-z)+wall.OffsetY,texture.Height);pixels[y*width+x]=Shade(texture.Pixels[ty*texture.Width+tx],light);}
                        if(x==width/2 && y1<=height/2 && y2>=height/2){selected.Wall=hit.Line;selected.Face=face.Offset;selected.Sector=sector;selected.Surface=name;}
                    };
                    if(face.Solid) {surface(ceilY,floorY,face.Upper,"Wall");break;}
                    int far=face.Far;if(far<0||far>=map.Sectors.Count)break;double farCeil=Ceiling(map,far,doors);int farTop=(int)Math.Round(horizon-(farCeil-camera.Z)*focal/depth),farBottom=(int)Math.Round(horizon-(map.Sectors[far].Floor-camera.Z)*focal/depth);
                    if(farCeil<ceil)surface(ceilY,farTop-1,face.Upper,"Upper wall");if(map.Sectors[far].Floor>near.Floor)surface(farBottom+1,floorY,face.Lower,"Lower wall");
                    top=Math.Max(top,Math.Max(ceilY,farTop));bottom=Math.Min(bottom,Math.Min(floorY,farBottom));sector=far;
                }
            }
            var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);var data=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);Marshal.Copy(pixels,0,data.Scan0,pixels.Length);bitmap.UnlockBits(data);return bitmap;
        }
    }
    public sealed class Scene3D:Control {
        public Map Map;public Rom Rom;public SceneCamera Camera=new SceneCamera();public bool OpenDoors;
        public Action<ScenePick> OnPick;public Action<int,bool,int> OnHeight;public Action OnExit;
        SceneTextures textures;ScenePick pick=new ScenePick();readonly HashSet<Keys> keys=new HashSet<Keys>();readonly Timer timer=new Timer {Interval=33};bool looking;System.Drawing.Point mouse;
        public Scene3D(){Dock=DockStyle.Fill;DoubleBuffered=true;BackColor=Color.FromArgb(10,23,19);TabStop=true;
            timer.Tick+=delegate {if(!Visible||!Focused||keys.Count==0)return;double speed=keys.Contains(Keys.ShiftKey)?24:8,dx=0,dy=0;
                if(keys.Contains(Keys.W)){dx+=Math.Cos(Camera.Angle)*speed;dy+=Math.Sin(Camera.Angle)*speed;}if(keys.Contains(Keys.S)){dx-=Math.Cos(Camera.Angle)*speed;dy-=Math.Sin(Camera.Angle)*speed;}
                if(keys.Contains(Keys.A)){dx-=Math.Sin(Camera.Angle)*speed;dy+=Math.Cos(Camera.Angle)*speed;}if(keys.Contains(Keys.D)){dx+=Math.Sin(Camera.Angle)*speed;dy-=Math.Cos(Camera.Angle)*speed;}
                Camera.X+=dx;Camera.Y+=dy;if(keys.Contains(Keys.Left))Camera.Angle+=.06;if(keys.Contains(Keys.Right))Camera.Angle-=.06;if(keys.Contains(Keys.E))Camera.Z+=4;if(keys.Contains(Keys.Q))Camera.Z-=4;Invalidate();};timer.Start();
        }
        public void ResetCamera(){if(Map==null)return;Thing player=Map.Things.FirstOrDefault(t=>t.Type==0);Point p=player==null?GeometryTools.Center(Map,0):new Point(player.X,player.Y);int sector=GeometryTools.SectorAt(Map,p);Camera=new SceneCamera {X=p.X,Y=p.Y,Z=(sector>=0?Map.Sectors[sector].Floor:0)+42,Angle=player==null?0:player.Angle*Math.PI*2/65536};Invalidate();}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Map==null||Rom==null)return;if(textures==null||textures.Rom!=Rom||textures.Graphics!=Rom.Project.Graphics)textures=new SceneTextures(Rom);
            int width=Math.Max(160,Math.Min(480,ClientSize.Width/2)),height=Math.Max(100,(int)(width*(double)Math.Max(1,ClientSize.Height)/Math.Max(1,ClientSize.Width)));
            using(Bitmap image=SceneRenderer.Render(Map,Camera,textures,width,height,OpenDoors,out pick)){e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(image,ClientRectangle);}
            using(var pen=new Pen(Color.FromArgb(145,250,186),1)){e.Graphics.DrawLine(pen,Width/2-7,Height/2,Width/2+7,Height/2);e.Graphics.DrawLine(pen,Width/2,Height/2-7,Width/2,Height/2+7);}
            using(var brush=new SolidBrush(Color.FromArgb(210,8,24,18)))e.Graphics.FillRectangle(brush,0,0,Width,44);
            e.Graphics.DrawString("3D VIEW   /   "+Map.Name+"   /   "+(pick.Sector<0?"Fly to a sector":pick.Surface+" - sector "+pick.Sector),Font,Brushes.PaleGreen,14,13);
            using(var brush=new SolidBrush(Color.FromArgb(210,8,24,18)))e.Graphics.FillRectangle(brush,0,Height-54,Width,54);
            e.Graphics.DrawString("WASD move  |  Right drag / arrows look  |  Q/E fly  |  Shift faster  |  F3 return to 2D\nLeft click selects the aimed surface  |  Ctrl + wheel: floor height  |  Shift + wheel: ceiling",Font,Brushes.PaleGreen,12,Height-48);
        }
        protected override bool IsInputKey(Keys keyData){return true;}
        protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Escape||e.KeyCode==Keys.F3){keys.Clear();if(OnExit!=null)OnExit();return;}keys.Add(e.KeyCode);e.Handled=true;}
        protected override void OnKeyUp(KeyEventArgs e){base.OnKeyUp(e);keys.Remove(e.KeyCode);}
        protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);keys.Clear();}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Right){looking=true;mouse=e.Location;Capture=true;}else if(e.Button==MouseButtons.Left&&OnPick!=null)OnPick(pick);}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(looking){Camera.Angle-=(e.X-mouse.X)*.007;Camera.Pitch=Math.Max(-120,Math.Min(120,Camera.Pitch+(e.Y-mouse.Y)*.5));mouse=e.Location;Invalidate();}}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);looking=false;Capture=false;}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(pick.Sector>=0&&OnHeight!=null&&(ModifierKeys&(Keys.Control|Keys.Shift))!=0)OnHeight(pick.Sector,(ModifierKeys&Keys.Shift)!=0,e.Delta>0?8:-8);}
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    }
}
