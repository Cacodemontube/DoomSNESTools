using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace DoomSNESed {
    public sealed partial class ConverterForm {
        async Task AutoConfigure(){
            try {
                Bin.Require(File.Exists(romPath.Text),"Choose your original base SNES ROM.");
                if(wad==null||!string.Equals(wad.PathName,Path.GetFullPath(wadPath.Text),StringComparison.OrdinalIgnoreCase))LoadWad();
                Bin.Require(wad!=null,"Choose a valid PWAD first.");
                string source=Path.GetFullPath(romPath.Text);WadFile input=wad;
                Enabled=false;ShowReport();report.Text="Checking every PWAD level for native suitability...";
                var progress=new Progress<string>(text=>{if(!IsDisposed)report.Text=text;});
                try {
                    ConversionRecommendation result=await Task.Run(delegate{return ConversionRecommendations.Analyze(new Rom(source,resources),input,resources,text=>((IProgress<string>)progress).Report(text));});
                    if(IsDisposed)return;
                    merge.Checked=result.Options.MergeSectors;weld.Checked=result.Options.WeldVertices;collapse.Checked=result.Options.CollapseWalls;
                    tags.Checked=result.Options.CompactTags;bestFit.Checked=result.Options.BestFit;closest.Checked=result.Options.ClosestActions;reject.Checked=result.Options.RejectDisconnected;
                    lighting.SelectedIndex=0;monsters.SelectedIndex=0;decorations.Checked=false;lossy.Checked=result.AllowSubstitutions;
                    for(int i=0;i<levels.Items.Count;i++)levels.SetItemChecked(i,result.Levels.Contains((string)levels.Items[i]));
                    campaign=null;play.Enabled=false;report.Text=result.Report;
                    if(result.Levels.Count>0&&(graphicChanges.Count>0||audioChanges.Any)){
                        report.Text="Checking staged graphics and audio against the recommended maps...";
                        try{StorageSummary budget=null;string checkedAssets=await Task.Run(()=>ConverterProgram.Analyze(source,input,result.Levels.ToArray(),result.Options,result.AllowSubstitutions,resources,graphicChanges,b=>budget=b,audioChanges));storageMeter.Summary=budget;storageMeter.Invalidate();report.Text=result.Report+"\r\n\r\nSTAGED ASSETS FIT\r\n"+checkedAssets;}
                        catch(Exception ex){storageMeter.Summary=new StorageSummary{Error="Staged assets do not fit; see report."};storageMeter.Invalidate();report.Text=result.Report+"\r\n\r\nSTAGED ASSET CHECK STOPPED\r\n"+ex.Message+"\r\nAdjust imports or map selection before building.";}
                    }
                }finally{if(!IsDisposed)Enabled=true;}
            }catch(Exception ex){if(!IsDisposed){report.Text="Auto configure stopped: "+ex.Message;ShowReport();}}
        }
    }
}
