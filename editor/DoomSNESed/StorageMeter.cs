using System;
using System.Drawing;
using System.Windows.Forms;
namespace DoomSNESed {
    public sealed class StorageSummary {public int OutputBytes,ReusableFree,Capacity,Maps,Graphics,GraphicsReclaimed,GraphicsReused,Music,Sounds,ApuSequenceMax,SampleMax;public string Error;}
    public sealed class StorageMeter:Control {
        public StorageSummary Summary;
        readonly ToolTip details=new ToolTip();
        public StorageMeter(){DoubleBuffered=true;Dock=DockStyle.Fill;BackColor=Theme.Background;ForeColor=Theme.Text;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var data=Summary;int graphics=data==null?0:Math.Max(0,data.Graphics-data.GraphicsReclaimed);int capacity=data==null?0:data.Capacity-Math.Min(data.Graphics,data.GraphicsReclaimed);
            string text=data==null?"Storage: Analyze or Build to measure maps, graphics, music and samples.":data.Error!=null?"Does not fit: "+data.Error:(data.OutputBytes>0?"Output "+(data.OutputBytes/1048576)+" MiB | ":"")+"Last analysis: maps "+data.Maps.ToString("N0")+" B | graphics extra "+graphics.ToString("N0")+" B | music/tables "+data.Music.ToString("N0")+" B | sounds "+data.Sounds.ToString("N0")+" B | free "+Math.Max(0,data.Capacity-data.Maps-data.Graphics-data.Music-data.Sounds).ToString("N0")+" B";
            details.SetToolTip(this,data==null?"":data.Graphics.ToString("N0")+" B of new graphics data; "+data.GraphicsReclaimed.ToString("N0")+" B of old columns reclaimed; "+data.GraphicsReused.ToString("N0")+" B of existing columns reused.\nThe blue section counts extra allocation after reclamation, not the full replacement size. Free space is available allocation space, not the ROM file size; bank fragmentation can still constrain imports.");
            e.Graphics.DrawString(text,Font,data!=null&&data.Error!=null?Brushes.Salmon:Brushes.LightGray,8,2);int width=Math.Max(1,Width-16);e.Graphics.FillRectangle(Brushes.DimGray,8,25,width,10);if(data==null||capacity<=0)return;int maps=(int)Math.Min(width,(long)width*data.Maps/capacity),graphicWidth=(int)Math.Min(width-maps,(long)width*graphics/capacity);e.Graphics.FillRectangle(Brushes.SeaGreen,8,25,maps,10);e.Graphics.FillRectangle(Brushes.CornflowerBlue,8+maps,25,graphicWidth,10);int music=(int)Math.Min(width-maps-graphicWidth,(long)width*data.Music/capacity),sounds=(int)Math.Min(width-maps-graphicWidth-music,(long)width*data.Sounds/capacity);e.Graphics.FillRectangle(Brushes.MediumPurple,8+maps+graphicWidth,25,music,10);e.Graphics.FillRectangle(Brushes.DarkOrange,8+maps+graphicWidth+music,25,sounds,10);if(data.Error!=null)e.Graphics.FillRectangle(Brushes.IndianRed,8,25,width,10);
        }
        protected override void Dispose(bool disposing){if(disposing)details.Dispose();base.Dispose(disposing);}
    }
}
