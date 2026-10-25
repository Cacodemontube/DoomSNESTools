using System;
using System.Collections.Generic;
using System.IO;

namespace DoomSNESed {
    // A deliberately small native compiler: one rectangular sector, two BSP
    // leaves, six boundary segments. No PC node builder or PC-only features.
    public static class RoomCompiler {
        public static void Layout(Map m) {
            int x=m.RoomWidth/2,y=m.RoomHeight/2;
            m.Vertices=new List<Point> {new Point(-x,-y),new Point(-x,y),new Point(0,y),new Point(x,y),new Point(x,-y),new Point(0,-y)};
            m.Walls.Clear();m.Faces.Clear();
            for(int i=0;i<6;i++) { m.Walls.Add(new Wall {Start=i,End=(i+1)%6,Flags=i==3?0x801:1,Face=i*2,Type=i==3?11:0});m.Faces.Add(new Face {Solid=true,Offset=i*2,Near=0,Upper=m.RoomTexture}); }
        }
        static void Table(byte[] b,int table,int slot,int v) { Bin.W16(b,RomLayout.At(b,Rom.TableBank+table)+slot*2,v); }
        public static void Build(Rom rom,byte[] b,Map m) {
            Layout(m);Section a=rom.GetSection(m.Slot,"A"),s=rom.GetSection(m.Slot,"B");
            int pos=a.Start;
            // Upward partition at x=0; left and right child bounding boxes.
            int x=m.RoomWidth/2,y=m.RoomHeight/2;
            int[] node={0,0,0,1,y,-y,-x,0,0x8000,y,-y,0,x,0x8004};
            foreach(int n in node) {Bin.W16(b,pos,n);pos+=2;}
            Table(b,0x3314,m.Slot,0);
            int areas=pos;
            b[pos++]=3;Bin.W16(b,pos,0);pos+=2;b[pos++]=0;
            b[pos++]=3;Bin.W16(b,pos,33);pos+=2;b[pos++]=0;
            Table(b,0x334a,m.Slot,(areas&65535)-0x8000);
            int vertices=pos;
            foreach(Point v in m.Vertices) {Bin.W16(b,pos,v.X);Bin.W16(b,pos+2,v.Y);pos+=4;}
            Table(b,0x3380,m.Slot,vertices-Rom.VertexRam);Table(b,0x33b6,m.Slot,6);
            int lines=pos;
            for(int i=0;i<6;i++) {
                Wall w=m.Walls[i];Point p=m.Vertices[w.Start],q=m.Vertices[w.End];
                int angle=(int)(Math.Atan2(q.Y-p.Y,q.X-p.X)*65536/(Math.PI*2));angle=(angle+32)&0xffc0;
                Bin.W16(b,pos,Rom.VertexRam+w.Start*4);Bin.W16(b,pos+2,Rom.VertexRam+w.End*4);
                Bin.W16(b,pos+4,w.Flags);Bin.W16(b,pos+6,angle);Bin.W16(b,pos+8,i*2);b[pos+10]=(byte)w.Type;b[pos+11]=0;pos+=12;
            }
            Table(b,0x33ec,m.Slot,lines);Table(b,0x3422,m.Slot,6);
            int segs=pos;int[] order={5,0,1,2,3,4};
            foreach(int i in order) {
                Bin.W16(b,pos,Rom.VertexRam+i*4);Bin.W16(b,pos+2,Rom.VertexRam+((i+1)%6)*4);
                b[pos+4]=0x41;b[pos+5]=0;b[pos+6]=0;Bin.W16(b,pos+7,i*2);Bin.W16(b,pos+9,i);pos+=11;
            }
            Table(b,0x3458,m.Slot,segs-11);Table(b,0x348e,m.Slot,segs-1);Table(b,0x34c4,m.Slot,6);
            Table(b,0x34fa,m.Slot,pos);b[pos++]=0;
            Table(b,0x3530,m.Slot,pos);
            for(int i=0;i<6;i++) { b[pos++]=0;b[pos++]=(byte)m.RoomTexture; }
            Bin.Require(pos<=a.Start+a.Size && (a.Start>>16)==((pos-1)>>16),"New room exceeds this slot's geometry allocation.");
            pos=s.Start;Rom.WriteSector(b,pos,m.Sectors[0]);pos+=10;Bin.W16(b,pos,0x524c);pos+=2;
            Table(b,0x35d2,m.Slot,1);Table(b,0x3608,m.Slot,pos);
            byte[] objects=Rom.ObjectBytes(m.Things);Buffer.BlockCopy(objects,0,b,pos,objects.Length);pos+=objects.Length;
            Table(b,0x363e,m.Slot,pos);
            // Every cell shares a conservative six-line collision list. It is
            // small enough for the engine's 63-line movement limit. Offsets
            // are relative to each offset entry's high byte, as rlmove.a uses.
            int nx=m.RoomWidth/128+1,ny=m.RoomHeight/128+1,cells=nx*ny;
            Bin.W16(b,pos,-x);Bin.W16(b,pos+2,-y);b[pos+4]=(byte)nx;b[pos+5]=(byte)ny;pos+=6;
            int entries=pos,list=pos+cells*2;
            for(int i=0;i<cells;i++)Bin.W16(b,entries+i*2,list-(entries+i*2)-1);
            pos=list;for(int i=0;i<6;i++) {Bin.W16(b,pos,i);pos+=2;}Bin.W16(b,pos,-1);pos+=2;
            // Empty door list uses FF. Other platform lists begin with FF FF.
            Table(b,0x3674,m.Slot,pos);b[pos++]=255;
            foreach(int table in new[]{0x36aa,0x36e0,0x3716,0x374c})Table(b,table,m.Slot,pos);
            b[pos++]=255;b[pos++]=255;
            Bin.Require(pos<=s.Start+s.Size && (s.Start>>16)==((pos-1)>>16),"New room exceeds this slot's object/collision allocation. Reduce its dimensions or object count.");
        }
    }
}

