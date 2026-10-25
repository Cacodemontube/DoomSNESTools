using System;
using System.Drawing;
using System.Windows.Forms;
namespace DoomSNESed {
    // Use the same green renderer for the selected value and its popup instead
    // of relying on the native ComboBox's OS-dependent selected-value colours.
    public sealed class ChoiceButton:Button {
        string[] values=new string[0];int index;readonly ContextMenuStrip menu=new ContextMenuStrip{Renderer=new EditorMenuRenderer(),BackColor=Theme.Panel,ForeColor=Theme.Text};
        public event EventHandler SelectedIndexChanged;
        public int SelectedIndex {get{return index;}set{if(value<0||value>=values.Length)throw new ArgumentOutOfRangeException("value");index=value;Text=values[index]+"   \u25be";for(int i=0;i<menu.Items.Count;i++)((ToolStripMenuItem)menu.Items[i]).Checked=i==index;if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
        public ChoiceButton(){FlatStyle=FlatStyle.Flat;BackColor=Theme.Raised;ForeColor=Theme.Text;Height=32;TextAlign=ContentAlignment.MiddleLeft;}
        public void SetValues(string[] choices){values=choices;menu.Items.Clear();for(int i=0;i<values.Length;i++){int selected=i;var item=new ToolStripMenuItem(values[i]){ForeColor=Theme.Text,BackColor=Theme.Panel};item.Click+=delegate{SelectedIndex=selected;};menu.Items.Add(item);}SelectedIndex=0;}
        protected override void OnClick(EventArgs e){base.OnClick(e);menu.Show(this,new System.Drawing.Point(0,Height));}
        protected override void Dispose(bool disposing){if(disposing)menu.Dispose();base.Dispose(disposing);}
    }
}
