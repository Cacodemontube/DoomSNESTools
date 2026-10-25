using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace DoomSNESed {
    // Edits are committed by the owner only after OK. Cancel restores its snapshot.
    public sealed class SelectionDialog:Form {
        readonly PropertyGrid grid=new PropertyGrid();readonly AssetPreview preview=new AssetPreview();
        public SelectionDialog(Rom rom,object[] values,bool walls){
            Text=(walls?"Linedef properties":"Sector properties")+" — "+values.Length+" selected";Size=new Size(760,700);MinimumSize=new Size(620,540);StartPosition=FormStartPosition.CenterParent;BackColor=Theme.Background;ForeColor=Theme.Text;Font=new Font("Segoe UI",10);
            grid.Dock=DockStyle.Fill;grid.ToolbarVisible=false;grid.PropertySort=PropertySort.Categorized;grid.ViewBackColor=Theme.Panel;grid.ViewForeColor=Theme.Text;grid.CategoryForeColor=Theme.Accent;grid.HelpBackColor=Theme.Background;grid.HelpForeColor=Theme.Muted;grid.LineColor=Theme.Border;grid.SelectedObjects=values;
            var title=new Label{Dock=DockStyle.Top,Height=48,Padding=new Padding(12),Text="Select a texture / colour field, then browse thumbnails. Mixed values stay unchanged until edited.",ForeColor=Theme.Mint};
            Action show=delegate{if(grid.SelectedGridItem==null||grid.SelectedGridItem.PropertyDescriptor==null)return;PropertyDescriptor pd=grid.SelectedGridItem.PropertyDescriptor;object v=pd.GetValue(values[0]);if(!(v is int))return;if(pd.Category=="Surfaces") {if(walls)preview.ShowWall(rom,(int)v);else if(pd.Name=="FloorTexture"||pd.Name=="CeilingTexture")preview.ShowFlat(rom,(int)v);}};
            grid.SelectedGridItemChanged+=delegate{show();};grid.PropertyValueChanged+=delegate{show();};
            var browse=Theme.Button("Browse selected texture / colour...");browse.Dock=DockStyle.Bottom;browse.Height=38;browse.Click+=delegate{try{var item=grid.SelectedGridItem;Bin.Require(item!=null&&item.PropertyDescriptor!=null,"Select a texture or colour field first.");PropertyDescriptor pd=item.PropertyDescriptor;bool flat=pd.Name=="FloorTexture"||pd.Name=="CeilingTexture";Bin.Require(flat||new[]{"FrontTexture","FrontLower","BackTexture","BackLower"}.Contains(pd.Name),"Select a texture or colour field first.");using(var picker=new AssetDialog(rom,!flat,pd.Name=="CeilingTexture",(int)pd.GetValue(values[0])))if(picker.ShowDialog(this)==DialogResult.OK){foreach(object value in values)pd.SetValue(value,picker.Value);grid.Refresh();show();}}catch(Exception ex){MessageBox.Show(this,ex.Message,"Surface properties");}};
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,FlowDirection=FlowDirection.RightToLeft};var ok=Theme.Button("Apply changes");ok.DialogResult=DialogResult.OK;var cancel=Theme.Button("Cancel");cancel.DialogResult=DialogResult.Cancel;buttons.Controls.Add(ok);buttons.Controls.Add(cancel);AcceptButton=ok;CancelButton=cancel;
            Controls.Add(grid);Controls.Add(title);Controls.Add(preview);Controls.Add(browse);Controls.Add(buttons);
        }
    }
}
