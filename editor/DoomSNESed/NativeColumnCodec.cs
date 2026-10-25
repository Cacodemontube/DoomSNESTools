using System;using System.Collections.Generic;
namespace DoomSNESed {
    public static class NativeColumnCodec {
        // Minimum-size packets in the original signed-run format. No new
        // decoder, colour changes or resolution reduction is required.
        public static byte[] Encode(byte[] pixels,bool sprite){int end=pixels.Length;if(sprite)while(end>0&&pixels[end-1]==0)end--;var costs=new int[end+1];var lengths=new int[end];var repeat=new bool[end];costs[end]=sprite?2:0;
            for(int p=end-1;p>=0;p--){int best=int.MaxValue,count=1;bool run=false;if(sprite&&pixels[p]!=0){best=1+costs[p+1];}
                if(!sprite)for(int n=1;n<=Math.Min(127,end-p);n++){int cost=1+n+costs[p+n];if(cost<=best){best=cost;count=n;run=false;}}
                int same=1;while(same<127&&p+same<end&&pixels[p+same]==pixels[p])same++;for(int n=1;n<=same;n++){int cost=(sprite?(pixels[p]==0?2:3):2)+costs[p+n];if(cost<best){best=cost;count=n;run=true;}}
                costs[p]=best;lengths[p]=count;repeat[p]=run;
            }
            var data=new List<byte>(costs[0]);for(int p=0;p<end;){int count=lengths[p];if(repeat[p]){if(sprite)data.Add(0);data.Add(pixels[p]==0&&sprite?(byte)count:unchecked((byte)-count));if(!sprite||pixels[p]!=0)data.Add(pixels[p]);}else{if(!sprite)data.Add((byte)count);for(int i=0;i<count;i++)data.Add(pixels[p+i]);}p+=count;}if(sprite){data.Add(0);data.Add(0);}return data.ToArray();
        }
        public static int Estimate(GraphicReplacement asset){var unique=new HashSet<string>();int bytes=0;for(int x=0;x<asset.Width;x++){var data=Encode(Pixels(asset,x),asset.Sprite);if(unique.Add(Convert.ToBase64String(data)))bytes+=data.Length;}return bytes;}
        public static byte[] Pixels(GraphicReplacement asset,int column){var pixels=new byte[asset.Height];for(int y=0;y<asset.Height;y++)pixels[y]=asset.Pixels[(asset.Height-1-y)*asset.Width+column];return pixels;}
    }
}

