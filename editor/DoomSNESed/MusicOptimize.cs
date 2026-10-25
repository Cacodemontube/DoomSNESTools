using System;
using System.IO;
namespace DoomSNESed {
    public static class MusicOptimize {
        // Preserve the requested polyphony unless reducing it is explicitly allowed.
        public static MusicReplacement Fit(ImportedMusic input,string map,int voices,double seconds,bool loop,MusicSettings settings,bool allowVoiceReduction=false){
            var json=ProjectJson.Create();settings=json.Deserialize<MusicSettings>(json.Serialize(settings));
            double duration=seconds>0?Math.Min(seconds,input.Seconds):input.Seconds;string last="";
            int minimum=allowVoiceReduction?1:voices;
            for(int v=voices;v>=minimum;v--)foreach(int gain in new[]{8,4,1}){
                settings.Grid=50;settings.VelocitySteps=gain;
                try{var result=MusicImport.Convert(input,map,v,duration,loop,settings);result.VoiceBudget=voices;result.Report+="\r\nAuto fit: native beat/absolute timing; "+v+" / "+voices+" voice budget, "+gain+" velocity levels; "+(result.Seconds<input.Seconds?"duration capped.":"full length retained.");return result;}
                catch(InvalidDataException ex){last=ex.Message;if(!last.Contains("SPC bytes"))throw;}
            }
            double lo=0,hi=duration;MusicReplacement best=null;
            for(int i=0;i<18;i++){double mid=(lo+hi)/2;try{var song=MusicImport.Convert(input,map,minimum,mid,loop,settings);lo=mid;best=song;}catch(InvalidDataException ex){if(!ex.Message.Contains("SPC bytes"))throw;hi=mid;}}
            Bin.Require(best!=null&&best.Seconds>=1,"The score cannot fit the selected bank. "+last);
            best.VoiceBudget=voices;best.Report+="\r\nAuto fit: "+minimum+" / "+voices+" voice budget; shortened from "+duration.ToString("0.0")+" to "+best.Seconds.ToString("0.0")+" seconds to fit SPC memory. "+(allowVoiceReduction?"Voice reduction was allowed.":"Requested voice budget retained; enable fewer voices to favor full length.")+" Ending loops/stops as selected.";return best;
        }
    }
}


