// PC action descriptions: Ultimate Doom Builder Doom_linedefs.cfg / Boom_linedefs.cfg.
// Native target behavior: DoomFX ripdoom3.asm and rlmove7.a / rluse2.a.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace DoomSNESed {
    public sealed class CompatibilityActions {
        sealed class Description {public int Id;public string Trigger,Title;}
        readonly Dictionary<int,Description> descriptions=new Dictionary<int,Description>();
        readonly Dictionary<int,int> closest=new Dictionary<int,int>{
            {4,90},{6,77},{12,13},{15,14},{17,13},{24,5},{25,73},{45,102},{46,61},{47,20},{48,0},{50,42},{53,87},{54,89},{55,56},{57,74},{60,23},
            {64,91},{65,56},{66,14},{67,14},{68,20},{69,18},{71,70},{76,16},{79,35},{80,13},{81,13},{83,19},{84,37},{92,58},{93,59},{94,56},{95,22},{96,30},
            {100,8},{101,5},{103,61},{104,35},{105,90},{106,86},{107,75},{108,90},{109,2},{110,3},{111,29},{112,61},{113,42},{114,63},{115,61},{116,42},{117,1},{118,31},{119,22},
            {120,88},{121,10},{122,21},{123,62},{124,52},{125,39},{126,97},{127,7},{128,22},{129,22},{130,22},{131,18},{132,18},{138,13},{139,35},{141,73},
        };
        public CompatibilityActions(string resources){foreach(string line in File.ReadAllLines(Path.Combine(resources,"PcActions.txt"))){string[] fields=line.Split('|');int id;if(fields.Length==3&&int.TryParse(fields[0],out id)&&!descriptions.ContainsKey(id))descriptions[id]=new Description{Id=id,Trigger=fields[1],Title=fields[2]};}}
        string Describe(int id){Description d;return descriptions.TryGetValue(id,out d)?id+" ["+d.Trigger+"] "+d.Title:id.ToString();}
        static string Normalize(string value){return value.Replace(" (fast)","").Replace(" (silent)","").Replace(" (also monsters)","").Trim();}
        public int Line(int original,bool enabled,bool allowOmissions,Action<string> report,bool canWalk=true){
            if(Enum.IsDefined(typeof(NativeAction),original)){if(original==30)report("PC shortest-texture floor raise uses the native fixed 64-unit raise.");return original;}
            int target=-1;if(enabled){if(!closest.TryGetValue(original,out target)){target=-1;Description source;if(descriptions.TryGetValue(original,out source)){
                        // Also handles Boom's fixed-number variants when their effect is
                        // exactly the same as a known native operation. Do not guess a
                        // different key requirement, motion direction or target height.
                        var candidates=descriptions.Values.Where(d=>d.Id!=0&&Enum.IsDefined(typeof(NativeAction),d.Id)&&Normalize(d.Title)==Normalize(source.Title)&&!(d.Trigger.StartsWith("D")&&!source.Trigger.StartsWith("D"))).OrderBy(d=>d.Trigger==source.Trigger?0:d.Trigger.Length>0&&source.Trigger.Length>0&&d.Trigger[0]==source.Trigger[0]?1:2).ThenBy(d=>d.Id).ToArray();
                        if(candidates.Length>0)target=candidates[0].Id;
                    }}
                if(target>=0&&!canWalk&&(EngineActions.Flags[target]&4)!=0){report("Closest variant for "+Describe(original)+" requires crossing the line, but this line is one-sided; that replacement would be unusable.");target=-1;}
                if(target>=0){report("PC linedef "+Describe(original)+" -> SNES "+Describe(target)+" ("+((NativeAction)target)+"). Speed, activation, repeat count or effect may differ; review this replacement.");return target;}}
            Bin.Require(allowOmissions,"No enabled SNES replacement for linedef "+Describe(original)+". Enable closest-action replacements, edit this action in the PWAD, or explicitly allow omissions.");report("No useful SNES analogue: linedef "+Describe(original)+" omitted by the selected omissions option.");return 0;
        }
        public int Sector(int original,bool enabled,bool allowOmissions,out bool manualDoor,Action<string> report){manualDoor=false;if(Enum.IsDefined(typeof(SectorEffect),original))return original;if(enabled&&original==17){report("PC sector 17 random flicker -> SNES random blink (1); lighting timing differs.");return 1;}if(enabled&&(original==10||original==14)){manualDoor=true;report("PC timed-door sector "+original+" -> manual open/wait/close door, initially closed. Automatic 30-second / five-minute timer is replaced with Use activation.");return 0;}Bin.Require(allowOmissions,"No enabled SNES replacement for sector effect "+original+". Edit this effect or explicitly allow omissions.");report("No useful SNES analogue: sector effect "+original+" -> Normal by the selected omissions option.");return 0;}
    }
}
