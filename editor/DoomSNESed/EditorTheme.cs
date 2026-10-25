using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace DoomSNESed {
    public static class Theme {
        public static readonly Color Background=Color.FromArgb(9,19,16),Panel=Color.FromArgb(16,32,27),Raised=Color.FromArgb(24,46,37),Border=Color.FromArgb(46,78,61),Accent=Color.FromArgb(154,218,109),Mint=Color.FromArgb(132,224,174),Text=Color.FromArgb(223,236,225),Muted=Color.FromArgb(146,173,154);
        public static Button Button(string text){return new Button {Text=text,FlatStyle=FlatStyle.Flat,ForeColor=Text,BackColor=Raised,Height=32,AutoSize=true,Margin=new Padding(3),Padding=new Padding(7,0,7,0),Cursor=Cursors.Hand};}
    }
    public sealed class EditorBanner:Control {
        public string Title="DoomSNESed",Subtitle="REALITY ENGINE MAP EDITOR",Detail="Draw. Build. Explore.";public Icon BrandIcon;public Image BrandImage;
        protected override void Dispose(bool disposing){if(disposing&&BrandImage!=null){BrandImage.Dispose();BrandImage=null;}base.Dispose(disposing);}
        public EditorBanner(){DoubleBuffered=true;Dock=DockStyle.Top;Height=70;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var fill=new LinearGradientBrush(ClientRectangle,Color.FromArgb(34,70,42),Color.FromArgb(12,30,24),0f))e.Graphics.FillRectangle(fill,ClientRectangle);
            if(BrandImage!=null){double scale=System.Math.Min(44.0/BrandImage.Width,44.0/BrandImage.Height);int w=(int)(BrandImage.Width*scale),h=(int)(BrandImage.Height*scale);e.Graphics.DrawImage(BrandImage,new Rectangle(17+(44-w)/2,12+(44-h)/2,w,h));}else if(BrandIcon!=null)e.Graphics.DrawIcon(BrandIcon,new Rectangle(17,12,44,44));
            using(var title=new Font("Segoe UI Semibold",19))using(var brush=new SolidBrush(Theme.Text))e.Graphics.DrawString(Title,title,brush,70,7);
            using(var font=new Font("Segoe UI",9))using(var brush=new SolidBrush(Theme.Mint))e.Graphics.DrawString(Subtitle+"     /     "+Detail,font,brush,73,43);
            using(var pen=new Pen(Theme.Border))e.Graphics.DrawLine(pen,0,Height-1,Width,Height-1);
        }
    }
    public sealed class EditorMenuRenderer:ToolStripProfessionalRenderer {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(Theme.Panel);}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){using(var brush=new SolidBrush(e.Item.Selected?Theme.Raised:Theme.Panel))e.Graphics.FillRectangle(brush,new Rectangle(System.Drawing.Point.Empty,e.Item.Size));}
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=Theme.Text;base.OnRenderItemText(e);}
    }
}
