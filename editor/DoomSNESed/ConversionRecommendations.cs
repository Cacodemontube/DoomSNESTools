using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace DoomSNESed {
    public sealed class ConversionRecommendation {
        public readonly List<string> Levels=new List<string>();
        public readonly ConversionOptions Options=new ConversionOptions();
        public string Report;
        // Standard Doom graphics/decorations need SNES substitutions. Keep the
        // option visible, and exclude untranslatable action logic from auto selection.
        public bool AllowSubstitutions=true;
    }
    public static class ConversionRecommendations {
        public static ConversionRecommendation Analyze(Rom rom,WadFile wad,string resources,Action<string> progress){
            var result=new ConversionRecommendation();var notes=new List<string>();var available=rom.Project.Maps.Select(m=>m.Slot).OrderBy(i=>i).ToList();int checkedMaps=0;
            foreach(string level in wad.Levels){checkedMaps++;if(progress!=null)progress("Checking "+level+" ("+checkedMaps+" / "+wad.Levels.Count+")...");
                if(result.Levels.Count>=22||available.All(slot=>result.Levels.Count>0&&slot==0)){notes.Add("DISABLED "+level+": no remaining native storage slot.");continue;}
                var converter=new WadConverter(rom,resources){Options=result.Options,AllowLossy=true};
                try{
                    Map map=converter.Import(wad,level,available[0],true);
                    if(converter.Report.Any(r=>r.Contains("No useful SNES analogue: linedef")||r.Contains("No useful SNES analogue: sector")||r.Contains("replacement would be unusable"))){notes.Add("DISABLED "+level+": action logic has no usable native replacement. "+string.Join(" ",converter.Report.Where(r=>r.Contains("No useful SNES analogue")||r.Contains("unusable")).ToArray()));continue;}
                    rom.Validate(map);NativeBuildInfo info=null;string failure="";
                    // Measure geometry once in a large allocation, then use those
                    // actual byte counts to reserve the same best-fit slots as Build.
                    foreach(int slot in available.OrderByDescending(s=>rom.GetSection(s,"A").Size).ThenByDescending(s=>rom.GetSection(s,"B").Size)){
                        map.Slot=slot;try{info=NativeCompiler.Build(rom,(byte[])rom.Bytes.Clone(),map);break;}catch(InvalidDataException ex){failure=ex.Message;if(!failure.Contains("bytes; this slot has"))throw;}
                    }
                    if(info==null){notes.Add("DISABLED "+level+": "+failure);continue;}
                    int index=result.Levels.Count;var order=available.Where(c=>index==0||c!=0).OrderBy(c=>index==0&&c==0?-1:index>0&&(c==9||c==18)?int.MaxValue:rom.GetSection(c,"A").Size).ThenBy(c=>rom.GetSection(c,"B").Size);
                    int chosen=order.Where(c=>info.GeometryBytes<=rom.GetSection(c,"A").Size&&info.DataBytes<=rom.GetSection(c,"B").Size).DefaultIfEmpty(-1).First();
                    if(chosen<0){notes.Add("DISABLED "+level+": remaining slots cannot fit "+info.GeometryBytes+" geometry / "+info.DataBytes+" data bytes.");continue;}
                    // Preserve original combat/population rather than recommending
                    // automatic monster or room removal to make an over-budget map fit.
                    int peakMoving=Enumerable.Range(0,3).Max(skill=>map.Things.Count(t=>(t.Flags&(1<<skill))!=0&&(t.Flags&0x50)==0x40));
                    int peakFixed=Enumerable.Range(0,3).Max(skill=>map.Things.Count(t=>(t.Flags&(1<<skill))!=0&&(t.Flags&0x50)==0));
                    if(info.Segments>1800||info.Areas>400||peakMoving>72||peakFixed>160){notes.Add("DISABLED "+level+": fits hard limits but is relatively heavy ("+info.Segments+" segments, "+info.Areas+" subsectors, peak pools "+peakMoving+" movable / "+peakFixed+" fixed). Enable manually if desired, or tune concessions.");continue;}
                    result.Levels.Add(level);available.Remove(chosen);int substitutions=converter.Report.Count(r=>r.StartsWith("SUBSTITUTION:"));
                    notes.Add("RECOMMENDED "+level+": "+map.Sectors.Count+" sectors, "+info.Segments+" segments; "+info.GeometryBytes+" / "+rom.GetSection(chosen,"A").Size+" geometry bytes, "+info.DataBytes+" / "+rom.GetSection(chosen,"B").Size+" data bytes; "+substitutions+" substitution categories.");
                    notes.AddRange(converter.Report.Where(r=>r.StartsWith("SUBSTITUTION:")).Select(r=>"  "+r));
                }catch(InvalidDataException ex){notes.Add("DISABLED "+level+": "+ex.Message);}
            }
            result.Report="AUTO CONFIGURE COMPLETE - no ROM/project/patch exported\r\n"+result.Levels.Count+" / "+wad.Levels.Count+" levels selected in PWAD order.\r\nPreferred settings: exact geometry cleanup, matching-sector merge, conservative visibility, compact tags, closest usable actions, best-fit storage.\r\nLighting changes, scenery removal and monster reduction remain OFF.\r\nReported texture/object substitutions are ON; levels needing unsupported action omissions are excluded.\r\nHeavy-map cutoffs are recommendations, not engine limits. Review substitutions and adjust checkboxes before Build campaign. Gameplay still needs playtesting.\r\n\r\n"+string.Join("\r\n",notes.ToArray());
            return result;
        }
    }
}
