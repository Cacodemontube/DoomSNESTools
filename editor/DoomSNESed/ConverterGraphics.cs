using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace DoomSNESed {
    public sealed partial class ConverterForm {
        Action graphicsRefresh;
        readonly List<GraphicReplacement> graphicChanges=new List<GraphicReplacement>();
        void AddGraphicsTab(){
            AddGraphicTab("Textures",0);
            AddGraphicTab("Skies",1);
            AddGraphicTab("Sprites",2);
            AddGraphicTab("Graphics",3);
        }
        void AddGraphicTab(string title,int category){
            var tab=new TabPage(title){BackColor=Theme.Panel};tabs.TabPages.Add(tab);
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,Padding=new Padding(12)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,48));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,category==0||category==2?110:64));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,140));
            var top=new FlowLayoutPanel{Dock=DockStyle.Fill};var load=Theme.Button("Load from base ROM");
            top.Controls.Add(load);top.Controls.Add(new Label{Text=category==3?"Screens, text font, weapons, status bar and faces":category==2?"Enemies, pickups and scenery: one native frame per entry":category==1?"Both native 128 x 128 sky panoramas":"Wall images; floors and ceilings use palette colours",AutoSize=true,ForeColor=Theme.Muted,Margin=new Padding(12,8,0,0)});
            var sizeMode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=320};sizeMode.Items.AddRange(GraphicSizeOptimizer.Modes);sizeMode.SelectedIndex=1;
            var optimize=Theme.Button("Apply to selected artwork");bool updatingSize=false;
            if(category==0||category==2){top.SetFlowBreak(top.Controls[top.Controls.Count-1],true);top.Controls.Add(new Label{Text="Import size / quality",AutoSize=true,ForeColor=Theme.Text,Margin=new Padding(0,7,8,0)});top.Controls.Add(sizeMode);top.Controls.Add(optimize);}
            layout.Controls.Add(top,0,0);layout.SetColumnSpan(top,2);
            var list=new ListBox{Dock=DockStyle.Fill,BackColor=Theme.Background,ForeColor=Theme.Text,HorizontalScrollbar=true};
            var preview=new NativeTextPreview{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(36,44,40)};
            layout.Controls.Add(list,0,1);layout.Controls.Add(preview,1,1);
            var bottom=new FlowLayoutPanel{Dock=DockStyle.Fill};var import=Theme.Button("Import PNG / BMP...");var export=Theme.Button("Export PNG...");var reset=Theme.Button("Restore original");var status=new Label{AutoSize=true,MaximumSize=new Size(1000,0),ForeColor=Theme.Mint};
            bottom.Controls.Add(import);bottom.Controls.Add(export);bottom.Controls.Add(reset);bottom.SetFlowBreak(reset,true);bottom.Controls.Add(status);layout.Controls.Add(bottom,0,2);layout.SetColumnSpan(bottom,2);tab.Controls.Add(layout);
            Rom assets=null;
            Action show=delegate{
                var item=list.SelectedItem as NativeGraphic;if(item==null||assets==null)return;
                var change=graphicChanges.FirstOrDefault(g=>g.Id==item.Id&&g.Sprite==item.Sprite&&g.Screen==item.Screen);
                if(change!=null&&(category==0||category==2)){updatingSize=true;sizeMode.SelectedIndex=change.OriginalPixels==null?0:Math.Max(0,Math.Min(GraphicSizeOptimizer.Modes.Length-1,change.SizeMode));updatingSize=false;}
                Image old=preview.Image;preview.Image=item.Screen>=6?HudAssets.Preview(assets,item.Screen,change):item.Screen>0?(change==null?ScreenAssets.Original(assets,item.Screen):ScreenAssets.Preview(change)):GraphicsAssets.Preview(assets,item,change==null?GraphicsAssets.Decode(assets.Bytes,item):change.Pixels);if(old!=null)old.Dispose();
                status.Text=item.Width+" x "+item.Height+" pixels required. "+(change==null?"Base ROM artwork.":"Replacement staged.")+"\n"+(item.Screen>=6?"Native 16-colour artwork. Export a template first; weapon tiles are shared across animation frames.":item.Screen>0?"Screens use 16 colours; existing text overlays remain.":"Colours match the ROM palette. Sprite PNG transparency is preserved.")+"  "+graphicChanges.Count+" total graphics replacements.";if(item.Screen==0&&(item.Sprite||item.Id>=4)&&change!=null)status.Text+="\nLossless native strip estimate: "+NativeColumnCodec.Estimate(change).ToString("N0")+" B before sharing/reuse. Extra allocation is shown after Analyze.";
                if(change!=null&&change.OriginalPixels!=null)status.Text+="\n"+GraphicSizeOptimizer.Report(change);
                if(category==0||category==2)status.Text+="\nSize presets reduce colours/detail. Preserve quality restores the imported pixels. Preview reflects the saved artwork.";
                else if(category==1)status.Text+="\nSky pixels use fixed native storage; reducing their colours does not free ROM space.";
                else status.Text+="\nNative compression is automatic where supported; PNG file size does not determine ROM usage.";
            };
            Action refresh=delegate{try{
                assets=new Rom(romPath.Text,resources);list.Items.Clear();
                if(category==3){list.Items.Add(new NativeGraphic{Screen=1,Width=256,Height=224});list.Items.Add(new NativeGraphic{Screen=2,Width=256,Height=224});for(int screen=3;screen<=5;screen++)list.Items.Add(new NativeGraphic{Screen=screen,Width=256,Height=224});foreach(var hud in HudAssets.All(assets))list.Items.Add(hud);}
                else if(category==2){string[] names=File.ReadAllLines(Path.Combine(resources,"SpriteNames.txt"));for(int id=0;id<232;id++){var frame=GraphicsAssets.Describe(assets.Bytes,true,id);frame.Name=id+" / "+names[id];list.Items.Add(frame);}}
                else foreach(int id in category==1?new[]{0,2}:assets.WallTextures.Where(i=>i>=4).ToArray()){var graphic=GraphicsAssets.Describe(assets.Bytes,false,id);graphic.Name=NativeWallCatalog.Label(id);list.Items.Add(graphic);}
                if(list.Items.Count>0)list.SelectedIndex=0;
            }catch(Exception ex){MessageBox.Show(this,ex.Message,title);}};
            romPath.TextChanged+=delegate{assets=null;list.Items.Clear();var old=preview.Image;preview.Image=null;if(old!=null)old.Dispose();if(tabs.SelectedTab==tab&&File.Exists(romPath.Text))refresh();};
            if(category==0)graphicsRefresh=refresh;load.Click+=delegate{refresh();};tabs.SelectedIndexChanged+=delegate{if(tabs.SelectedTab==tab&&assets==null&&File.Exists(romPath.Text))refresh();};
            list.SelectedIndexChanged+=delegate{try{show();}catch(Exception ex){status.Text=ex.Message;}};
            import.Click+=delegate{try{var item=list.SelectedItem as NativeGraphic;if(item==null)return;using(var dialog=new OpenFileDialog{Filter="Image|*.png;*.bmp"})if(dialog.ShowDialog(this)==DialogResult.OK){var change=GraphicsAssets.Import(assets,item,dialog.FileName);if(GraphicSizeOptimizer.Supported(change))GraphicSizeOptimizer.Apply(assets,change,sizeMode.SelectedIndex);graphicChanges.RemoveAll(g=>g.Id==item.Id&&g.Sprite==item.Sprite&&g.Screen==item.Screen);graphicChanges.Add(change);AssetsChanged();show();}}catch(Exception ex){MessageBox.Show(this,ex.Message,"Import graphics");}};
            Action applySize=delegate{try{var item=list.SelectedItem as NativeGraphic;if(item==null||assets==null)return;var change=graphicChanges.FirstOrDefault(g=>g.Id==item.Id&&g.Sprite==item.Sprite&&g.Screen==item.Screen);if(change==null){change=GraphicSizeOptimizer.FromRom(assets,item);graphicChanges.Add(change);}GraphicSizeOptimizer.Apply(assets,change,sizeMode.SelectedIndex);AssetsChanged();show();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Optimize artwork");}};
            optimize.Click+=delegate{applySize();};sizeMode.SelectedIndexChanged+=delegate{if(updatingSize)return;var item=list.SelectedItem as NativeGraphic;if(item!=null&&graphicChanges.Any(g=>g.Id==item.Id&&g.Sprite==item.Sprite&&g.Screen==item.Screen))applySize();};
            export.Click+=delegate{if(preview.Image==null)return;using(var dialog=new SaveFileDialog{Filter="PNG|*.png",FileName="native-asset.png"})if(dialog.ShowDialog(this)==DialogResult.OK)try{ProtectInputFiles(dialog.FileName);Bin.Require(string.Equals(Path.GetExtension(dialog.FileName),".png",StringComparison.OrdinalIgnoreCase),"Choose a .png filename.");preview.Image.Save(dialog.FileName,System.Drawing.Imaging.ImageFormat.Png);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Export PNG");}};
            reset.Click+=delegate{var item=list.SelectedItem as NativeGraphic;if(item==null)return;graphicChanges.RemoveAll(g=>g.Id==item.Id&&g.Sprite==item.Sprite&&g.Screen==item.Screen);AssetsChanged();show();};
            FormClosed+=delegate{if(preview.Image!=null)preview.Image.Dispose();};
        }
        void AssetsChanged(){play.Enabled=false;storageMeter.Summary=null;storageMeter.Invalidate();}
    }
}


