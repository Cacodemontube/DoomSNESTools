using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace DoomSNESed {
    public sealed class NewMapDialog:Form {
        readonly ListBox choices=new ListBox();public int Choice {get{return choices.SelectedIndex;}}
        public NewMapDialog(string slot){Text="Create a map in "+slot;Size=new Size(520,350);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;BackColor=Theme.Panel;ForeColor=Theme.Text;Font=new Font("Segoe UI",10);
            var label=new Label {Text="Choose a starting point\nThis replaces "+slot+" in your project. The source ROM stays unchanged.",Dock=DockStyle.Top,Height=70,Padding=new Padding(16)};
            choices.Items.AddRange(new object[]{"Empty map - draw your own sectors","Starter room - reshape, split or extend it","Working door example - two rooms with a usable door","Working lift example - learn tagged line actions"});choices.SelectedIndex=0;choices.Dock=DockStyle.Fill;choices.BackColor=Theme.Background;choices.ForeColor=Theme.Text;choices.BorderStyle=BorderStyle.None;choices.ItemHeight=34;
            var buttons=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=50,FlowDirection=FlowDirection.RightToLeft};var create=Theme.Button("Create map");create.DialogResult=DialogResult.OK;var cancel=Theme.Button("Cancel");cancel.DialogResult=DialogResult.Cancel;buttons.Controls.Add(create);buttons.Controls.Add(cancel);AcceptButton=create;CancelButton=cancel;Controls.Add(choices);Controls.Add(label);Controls.Add(buttons);
        }
    }
    public sealed class AssetDialog:Form {
        public int Value;readonly ListView list=new ListView();readonly ImageList images=new ImageList {ImageSize=new Size(72,72),ColorDepth=ColorDepth.Depth32Bit};
        public AssetDialog(Rom rom,bool wall,bool ceiling,int current){Text=wall?"Choose a ROM wall texture":"Choose a SNES floor / ceiling colour";Size=new Size(680,520);StartPosition=FormStartPosition.CenterParent;BackColor=Theme.Panel;ForeColor=Theme.Text;Font=new Font("Segoe UI",10);Value=current;
            var title=new Label {Dock=DockStyle.Top,Height=55,Padding=new Padding(14),Text=wall?"ROM wall textures - select a thumbnail and choose Use asset.":"SNES Doom uses solid floor and ceiling colours. Ceiling 255 is sky."};
            list.Dock=DockStyle.Fill;list.BackColor=Theme.Background;list.ForeColor=Theme.Text;list.LargeImageList=images;list.MultiSelect=false;list.HideSelection=false;list.View=View.LargeIcon;
            var values=wall?rom.WallTextures:rom.FlatTextures.Where(v=>ceiling||v!=255).ToList();
            foreach(int id in values){using(var bitmap=new Bitmap(72,72))using(var graphics=Graphics.FromImage(bitmap)){graphics.Clear(Theme.Raised);if(wall){using(var p=new AssetPreview()){p.ShowWall(rom,id);using(Bitmap source=p.CopyImage()){if(source!=null){graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;graphics.DrawImage(source,new Rectangle(0,0,72,72));}}}}else graphics.Clear(id==255?Color.FromArgb(46,83,90):AssetPreview.Palette(rom,id));images.Images.Add((Image)bitmap.Clone());}
                var item=new ListViewItem(wall?NativeWallCatalog.Label(id):(id==255?"Sky ":"Colour ")+id,images.Images.Count-1){Tag=id};list.Items.Add(item);if(id==current)item.Selected=true;
            }
            var buttons=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=48,FlowDirection=FlowDirection.RightToLeft};var use=Theme.Button("Use asset");use.Click+=delegate{Choose();};var cancel=Theme.Button("Cancel");cancel.DialogResult=DialogResult.Cancel;buttons.Controls.Add(use);buttons.Controls.Add(cancel);list.DoubleClick+=delegate{Choose();};CancelButton=cancel;Controls.Add(list);Controls.Add(title);Controls.Add(buttons);
        }
        void Choose(){if(list.SelectedItems.Count==0)return;Value=(int)list.SelectedItems[0].Tag;DialogResult=DialogResult.OK;Close();}
        protected override void Dispose(bool disposing){if(disposing)images.Dispose();base.Dispose(disposing);}
    }
}
