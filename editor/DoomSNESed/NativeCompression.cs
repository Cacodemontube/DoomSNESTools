using System;
using System.Collections.Generic;
namespace DoomSNESed {
    // DoomFX comp.a: backwards RLC stream, including its final-word sentinel.
    public static class NativeCompression {
        public static int EncodedSize(byte[] b,int address){int kind=Bin.U16(b,address);if(kind==0)return 4+Bin.U16(b,address+2);if(kind==4)return 4+Bin.U16(b,address+4);Bin.Require(kind==8||kind==12,"Unsupported compressed allocation.");int left=Bin.U16(b,address+2),stagger=b[address+4],p=address+5;while(left>0){int code=b[p++],count;if((code&128)!=0){count=((code>>3)&15)+3;p++;}else if((code&64)!=0){count=(code&63)+1;p+=count;}else if((code&32)!=0){count=stagger==1?1:(code&31)+3;if(stagger!=1)p++;}else{count=code&31;if(count==0)break;count+=2;p++;}Bin.Require(count<=left&&p<=b.Length,"Invalid compressed allocation.");left-=count;}Bin.Require(left==0,"Incomplete compressed allocation.");return p-address;}
        public static byte[] Encode(byte[] input){
            int size=input.Length;Bin.Require(size>0&&size<=65535,"Invalid native RLC input.");var cost=new int[size+1];var lengths=new int[size+1];var offsets=new int[size+1];
            for(int p=1;p<=size;p++){
                int best=int.MaxValue,length=1,offset=0;
                for(int n=1;n<=Math.Min(264,p);n++){int c=cost[p-n]+(n<=8?5:11)+n*8;if(c<best){best=c;length=n;}}
                int[] match=new int[Math.Min(p,260)+1];int missing=match.Length-2;
                for(int d=1;d<=Math.Min(4095,size-p);d++){
                    if(input[p-1]!=input[p-1+d])continue;int n=1;while(n<match.Length-1&&input[p-1-n]==input[p-1-n+d])n++;
                    for(int count=2;count<=n;count++)if(match[count]==0&&(count!=2||d<256)&&(count!=3||d<512)&&(count!=4||d<1024)){match[count]=d;missing--;}
                    if(missing==0)break;
                }
                for(int n=2;n<match.Length;n++)if(match[n]!=0){int c=cost[p-n]+(n==2?10:n==3?12:n==4?13:23);if(c<best){best=c;length=n;offset=match[n];}}
                cost[p]=best;lengths[p]=length;offsets[p]=offset;
            }
            var bits=new List<int>();Action<int,int> put=(v,n)=>{for(int i=n-1;i>=0;i--)bits.Add((v>>i)&1);};
            for(int p=size;p>0;){int n=lengths[p],d=offsets[p];if(d==0){if(n<=8){put(0,2);put(n-1,3);}else{put(7,3);put(n-9,8);}for(int i=0;i<n;i++)put(input[--p],8);}else{if(n==2){put(1,2);put(d,8);}else if(n==3){put(4,3);put(d,9);}else if(n==4){put(5,3);put(d,10);}else{put(6,3);put(n-5,8);put(d,12);}p-=n;}}
            var words=new List<int>();int first=bits.Count%16,index=0,word=1<<first;for(int i=0;i<first;i++)word|=bits[index++]<<i;words.Add(word);while(index<bits.Count){word=0;for(int i=0;i<16;i++)word|=bits[index++]<<i;words.Add(word);}var output=new byte[6+words.Count*2];Bin.W16(output,0,4);Bin.W16(output,2,size);Bin.W16(output,4,output.Length-4);for(int i=0;i<words.Count;i++)Bin.W16(output,6+i*2,words[words.Count-1-i]);return output;
        }
        // Literal packets deliberately trade a small size overhead for predictable
        // decoding into RAM (type zero returns a ROM pointer and cannot initialize
        // musicdrv.a's RAM turbo table).
        public static byte[] EncodeLiterals(byte[] input){Bin.Require(input.Length>0&&input.Length<=65535,"Invalid native RLC input.");var bits=new List<int>();Action<int,int> put=(value,count)=>{for(int i=count-1;i>=0;i--)bits.Add((value>>i)&1);};for(int p=input.Length;p>0;){int count=Math.Min(8,p);put(0,2);put(count-1,3);for(int i=0;i<count;i++)put(input[--p],8);}var words=new List<int>();int first=bits.Count%16,index=0,word=1<<first;for(int i=0;i<first;i++)word|=bits[index++]<<i;words.Add(word);while(index<bits.Count){word=0;for(int i=0;i<16;i++)word|=bits[index++]<<i;words.Add(word);}var output=new byte[6+words.Count*2];Bin.W16(output,0,4);Bin.W16(output,2,input.Length);Bin.W16(output,4,output.Length-4);for(int i=0;i<words.Count;i++)Bin.W16(output,6+i*2,words[words.Count-1-i]);return output;}
        public static byte[] DecodeMusicChunk(byte[] bytes,int address,int encodedSize){if((encodedSize&32768)==0){var raw=new byte[encodedSize];Buffer.BlockCopy(bytes,address,raw,0,raw.Length);return raw;}int size=encodedSize&32767;var stream=new byte[size+6];Bin.W16(stream,0,4);Bin.W16(stream,2,Bin.U16(bytes,address));Bin.W16(stream,4,size+2);Buffer.BlockCopy(bytes,address+2,stream,6,size);return Decode(stream,0);}
        static byte[] Eclipse(byte[] bytes,int address,int size){int stagger=bytes[address+4],source=address+5,p=size;var linear=new byte[size];while(p>0){int code=bytes[source++],count,offset=0;if((code&128)!=0){count=((code>>3)&15)+3;offset=(((code&7)<<8)|bytes[source++])+1;}else if((code&64)!=0){count=(code&63)+1;Bin.Require(count<=p,"Invalid Eclipse literal.");for(int i=count;i>0;i--)linear[--p]=bytes[source+i-1];source+=count;continue;}else if((code&32)!=0){if(stagger==1){count=1;offset=(code&31)+1;}else{count=(code&31)+3;int value=bytes[source++];Bin.Require(count<=p,"Invalid Eclipse delta.");while(count-->0)linear[--p]=(byte)value--;continue;}}else{count=code&31;if(count==0)break;count+=2;byte value=bytes[source++];Bin.Require(count<=p,"Invalid Eclipse repeat.");while(count-->0)linear[--p]=value;continue;}Bin.Require(count<=p&&p+offset<=size,"Invalid Eclipse reference.");while(count-->0){p--;linear[p]=linear[p+offset];}}Bin.Require(p==0,"Incomplete Eclipse data.");if(stagger==1)return linear;Bin.Require(stagger==2,"Invalid Eclipse stagger.");var result=new byte[size];int half=(size+1)/2;for(int i=0;i<size;i++)result[i]=linear[(i&1)==0?i/2:half+i/2];return result;}
        sealed class Bits {public byte[] Data;public int Position,Word;public int Read(int count){int value=0;while(count-->0){int bit=Word&1;Word>>=1;if(Word==0){Position-=2;int next=Bin.U16(Data,Position);bit=next&1;Word=(next>>1)|32768;}value=(value<<1)|bit;}return value;}}
        public static byte[] Decode(byte[] bytes,int address){int kind=Bin.U16(bytes,address),size=Bin.U16(bytes,address+2);Bin.Require(size>0&&size<=65535,"Invalid compressed asset size.");var output=new byte[size];if(kind==0){Buffer.BlockCopy(bytes,address+4,output,0,size);return output;}if(kind==8||kind==12)return Eclipse(bytes,address,size);Bin.Require(kind==4,"Unsupported native compression.");int end=address+2+Bin.U16(bytes,address+4);var bits=new Bits{Data=bytes,Position=end,Word=Bin.U16(bytes,end)};int p=size;while(p>0){int length,offset=0;if(bits.Read(1)==0){if(bits.Read(1)==0)length=bits.Read(3)+1;else{length=2;offset=bits.Read(8);}}else{int type=bits.Read(2);if(type==3)length=bits.Read(8)+9;else if(type==2){length=bits.Read(8)+5;offset=bits.Read(12);}else{length=type+3;offset=bits.Read(type+9);}}Bin.Require(length<=p,"Invalid compressed asset run.");if(offset==0){while(length-->0)output[--p]=(byte)bits.Read(8);}else{Bin.Require(p+offset<=size,"Invalid compressed asset reference.");while(length-->0){p--;output[p]=output[p+offset];}}}return output;}
    }
}

