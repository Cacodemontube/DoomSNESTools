using System;using System.Collections.Generic;using System.Linq;
namespace DoomSNESed {
    public sealed class MusicClock {
        public double Rate=MusicImport.TickRate;public int Tempo=255;
        public double NativeRate {get{return 50.0*Tempo/256;}}
        public int Tick(double seconds){return (int)Math.Round(seconds*Rate,MidpointRounding.AwayFromZero);}
        static double Error(double[] times,double rate){return Math.Sqrt(times.Sum(t=>{double e=Math.Round(t*rate)/rate-t;return e*e;})/times.Length);}
        // Use the retail FA tempo accumulator. A regular beat should span an
        // integer number of score ticks instead of alternating short/long gaps
        // on an unrelated 50 Hz grid. Irregular scores keep the finest clock.
        public static MusicClock Choose(ImportedMusic input,double duration){var result=new MusicClock();var starts=input.Notes.Where(n=>n.Velocity>0&&n.Start<duration).Select(n=>n.Start).Distinct().OrderBy(t=>t).ToArray();if(starts.Length<8)return result;
            int stride=Math.Max(1,starts.Length/2048);var times=starts.Where((t,i)=>i%stride==0).Take(2048).ToArray();double baseline=Error(times,result.Rate),best=baseline;var candidates=new HashSet<double>();
            if(input.QuarterSeconds>0)for(int ticks=4;ticks<=96;ticks++)candidates.Add(ticks/input.QuarterSeconds);
            // Also recognizes old saved projects and MUS, which have event
            // times but no explicit BPM metadata. No notes are moved off their
            // original absolute timeline before choosing this single clock.
            foreach(double gap in starts.Skip(1).Zip(starts,(a,b)=>a-b).Where(t=>t>=.03&&t<=2).Select(t=>Math.Round(t,7)).Distinct().Take(128))for(int ticks=1;ticks<=64;ticks++)candidates.Add(ticks/gap);
            foreach(double rate in candidates.Where(r=>r>=32&&r<=MusicImport.TickRate).OrderByDescending(r=>r)){int tempo=(int)Math.Round(rate*256/50);double actual=50.0*tempo/256;if(Math.Abs(actual/rate-1)>.0015)continue;double error=Error(times,rate);if(error<best-.0001){best=error;result.Rate=rate;result.Tempo=tempo;}}
            // Reject weak guesses. Tempo maps/rubato retain high resolution;
            // musical-grid adaptation must substantially improve onset accuracy.
            if(best+.0005>=baseline*.75)return new MusicClock();return result;
        }
    }
}
