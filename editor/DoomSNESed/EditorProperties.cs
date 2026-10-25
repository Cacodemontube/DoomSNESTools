using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace DoomSNESed {
    public static class Context { public static Rom Rom; }
    public class ThingTypeConverter:StringConverter {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext c) {return true;}
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext c) {return true;}
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c) {return new StandardValuesCollection(Context.Rom.ObjectNames.Take(64).Where((s,i)=>i==0 || i>=5).ToArray());}
    }
    public class WallTextureConverter:Int32Converter {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext c) {return true;}
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext c) {return true;}
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c) {return new StandardValuesCollection(Context.Rom.WallTextures);}
        public override object ConvertTo(ITypeDescriptorContext c,CultureInfo culture,object value,Type destination){if(destination==typeof(string)&&value is int)return NativeWallCatalog.Label((int)value);return base.ConvertTo(c,culture,value,destination);}
        public override object ConvertFrom(ITypeDescriptorContext c,CultureInfo culture,object value){var text=value as string;if(text!=null&&text.Contains("/"))value=text.Substring(0,text.IndexOf('/')).Trim();return base.ConvertFrom(c,culture,value);}
    }
    public class FlatTextureConverter:Int32Converter {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext c) {return true;}
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext c) {return true;}
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c) {return new StandardValuesCollection(Context.Rom.FlatTextures);}
    }
    public class FloorColorConverter:FlatTextureConverter {
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c) {return new StandardValuesCollection(Context.Rom.FlatTextures.Where(id=>id!=255).ToArray());}
    }
    [TypeConverter(typeof(FriendlyEnumConverter))]public enum SectorEffect {Normal=0,RandomBlink=1,BlinkHalfSecond=2,BlinkOneSecond=3,DamageAndBlink=4,MediumDamage=5,LowDamage=7,LightOscillation=8,Secret=9,ExitDamage=11,SyncedOneSecondBlink=12,SyncedHalfSecondBlink=13,HighDamage=16}
    public class ThingProperties {
        protected readonly Thing t; public ThingProperties(Thing value) {t=value;}
        [Category("Object"),TypeConverter(typeof(ThingTypeConverter)),DisplayName("Type")]
        public string Kind {get{return Context.Rom.ObjectName(t.Type);}set {
            int type=Array.IndexOf(Context.Rom.ObjectNames,value);Bin.Require(type==0 || (type>=5 && type<64),"Unsupported object type.");
            Thing template=Context.Rom.Project.Maps.SelectMany(m=>m.Things).FirstOrDefault(x=>x.Type==type);
            int flags=template==null?(type<=14?0xe8:type==49?0xa8:0x80):template.Flags&0xe8;
            t.Type=type;t.Flags=flags|(t.Flags&7);if(type==0)t.Flags|=7;
        }}
        [Category("Position")]public int X {get{return t.X;}set{t.X=value;}}
        [Category("Position")]public int Y {get{return t.Y;}set{t.Y=value;}}
        [Category("Position"),Description("Native angle for player/teleport starts. Monsters have no stored facing angle in this ROM format.")]
        public int AngleDegrees {get{return t.Angle*360/65536;}}
        [Category("Skills")]public bool Easy {get{return(t.Flags&1)!=0;}set{Set(1,value);}}
        [Category("Skills")]public bool Medium {get{return(t.Flags&2)!=0;}set{Set(2,value);}}
        [Category("Skills")]public bool Hard {get{return(t.Flags&4)!=0;}set{Set(4,value);}}
        [Category("Object"),ReadOnly(true)]public bool MultiplayerOnly {get{return(t.Flags&16)!=0;}}
        void Set(int bit,bool enabled) {Bin.Require(t.Type!=0 || enabled,"Player start must appear at every skill.");t.Flags=enabled?t.Flags|bit:t.Flags&~bit;}
    }
    public class FacingThingProperties:ThingProperties {
        public FacingThingProperties(Thing value):base(value) {}
        [Category("Position")]public new int AngleDegrees {get{return base.AngleDegrees;}set{t.Angle=((value%360+360)%360)*65536/360;}}
    }
    public class SectorProperties {
        protected readonly Sector s;public SectorProperties(Sector sector) {s=sector;}
        [Category("Heights"),Description("Existing heights are locked because platform tables contain precomputed destinations.")]
        public int Floor {get{return s.Floor;}}
        [Category("Heights")]public int Ceiling {get{return s.Ceiling;}}
        [Category("Surfaces"),DisplayName("Floor colour"),TypeConverter(typeof(FloorColorConverter))]public int FloorTexture {get{return s.FloorTexture;}set{Bin.Require(value>=1&&value<=254,"Floor colours are 1..254; 255 is ceiling sky.");s.FloorTexture=value;}}
        [Category("Surfaces"),DisplayName("Ceiling colour / sky"),TypeConverter(typeof(FlatTextureConverter))]public int CeilingTexture {get{return s.CeilingTexture;}set{Bin.Require(value>=1&&value<=255,"Ceiling colours are 1..255; 255 is sky.");s.CeilingTexture=value;}}
        [Category("Lighting"),DisplayName("Darkness (native)"),Description("Reality Engine darkness: 0 is brightest, 247 darkest.")]public int Darkness {get{return s.Darkness;}set{s.Darkness=value;}}
        [Category("Lighting"),Description("0..247, higher is brighter.")]public int Brightness {get{return 247-s.Darkness;}set{Bin.Require(value>=0&&value<=247,"Brightness is 0..247.");s.Darkness=247-value;s.NeighborDarkness=s.Darkness;}}
        [Category("Lighting")]public int NeighborDarkness {get{return s.NeighborDarkness;}set{s.NeighborDarkness=value;}}
        [Category("Behavior")]public SectorEffect Effect {get{return(SectorEffect)s.Type;}set{s.Type=(int)value;}}
        [Category("Behavior")]public int Tag {get{return s.Tag;}}
    }
    public class RoomProperties:SectorProperties {
        readonly Map m;public RoomProperties(Map map):base(map.Sectors[0]) {m=map;}
        [Category("Heights")]public new int Floor {get{return s.Floor;}set{s.Floor=value;}}
        [Category("Heights")]public new int Ceiling {get{return s.Ceiling;}set{s.Ceiling=value;}}
        [Category("Room")]public int Width {get{return m.RoomWidth;}set{Bin.Require(value>=256 && value<=4096 && value%128==0,"Width must be 256..4096, in steps of 128.");m.RoomWidth=value;RoomCompiler.Layout(m);}}
        [Category("Room")]public int Height {get{return m.RoomHeight;}set{Bin.Require(value>=256 && value<=4096 && value%128==0,"Height must be 256..4096, in steps of 128.");m.RoomHeight=value;RoomCompiler.Layout(m);}}
        [Category("Surfaces"),TypeConverter(typeof(WallTextureConverter))]public int WallTexture {get{return m.RoomTexture;}set{m.RoomTexture=value;RoomCompiler.Layout(m);}}
    }
    public class FaceProperties {
        readonly Face f;public FaceProperties(Face face) {f=face;}
        [Category("Surface")]public bool Solid {get{return f.Solid;}}
        [Category("Surface"),TypeConverter(typeof(WallTextureConverter)),Description("Middle texture on a solid wall; upper texture on a portal.")]
        public int UpperOrMiddle {get{return f.Upper;}set{f.Upper=value;}}
        [Category("Surface"),TypeConverter(typeof(WallTextureConverter))]public int Lower {get{return f.Lower;}set{Bin.Require(!f.Solid,"Solid walls have only a middle texture.");f.Lower=value;}}
        [Category("Links")]public int NearSector {get{return f.Near;}}
        [Category("Links")]public int FarSector {get{return f.Far;}}
    }
    [TypeConverter(typeof(FriendlyEnumConverter))]public enum NativeAction {
        None=0,DoorOpenWaitCloseUse=1,DoorOpenStayWalkOnce=2,DoorCloseWalkOnce=3,FloorRaiseToCeilingWalkOnce=5,
        StairsRaise8Use=7,StairsRaise8WalkOnce=8,DonutUse=9,LiftLowerWaitRaiseWalkOnce=10,ExitLevelUse=11,LightFullWalkOnce=13,
        FloorRaise32Use=14,DoorCloseWaitOpenWalkOnce=16,FloorRaiseNextUse=18,FloorLowerHighestWalkOnce=19,FloorRaiseNextChangeUse=20,LiftLowerWaitRaiseUseOnce=21,
        FloorRaiseNextWalkOnce=22,FloorLowerLowestUse=23,BlueDoorUse=26,YellowDoorUse=27,RedDoorUse=28,TaggedDoorOpenWaitCloseUseOnce=29,FloorRaise64WalkOnce=30,
        DoorOpenStayUse=31,BlueDoorOpenStayUse=32,RedDoorOpenStayUse=33,YellowDoorOpenStayUse=34,LightDarkWalkOnce=35,FloorLowerHighestPlus8WalkOnce=36,
        FloorLowerAndChangeWalkOnce=37,FloorLowerLowestWalkOnce=38,TeleportWalkOnce=39,TaggedDoorCloseUseRepeat=42,ExitSecretUse=51,ExitLevelWalk=52,
        FloorRaiseToCeilingMinus8WalkOnce=56,FloorRaise24WalkOnce=58,FloorRaise24ChangeWalkOnce=59,TaggedDoorOpenUseRepeat=61,LiftLowerWaitRaiseUseRepeat=62,
        TaggedDoorOpenWaitCloseUseRepeat=63,FloorLowerHighestPlus8UseRepeat=70,CrusherWalkRepeat=73,CrusherStopWalkRepeat=74,TaggedDoorCloseWalkRepeat=75,
        CrusherFastWalkRepeat=77,FloorLowerLowestWalkRepeat=82,TaggedDoorOpenWalkRepeat=86,MovingFloorStartWalkRepeat=87,LiftLowerWaitRaiseWalkRepeat=88,
        MovingFloorStopWalkRepeat=89,TaggedDoorOpenWaitCloseWalkRepeat=90,FloorRaiseToCeilingWalkRepeat=91,TeleportWalkRepeat=97,FloorLowerHighestPlus8WalkRepeat=98,FloorLowerHighestUse=102
    }
    public class EditableSectorProperties:SectorProperties {
        readonly Map map;readonly int index;public EditableSectorProperties(Map m,int sector):base(m.Sectors[sector]){map=m;index=sector;}
        [Category("Identity"),DisplayName("Internal sector number"),ReadOnly(true),Description("Zero-based sector index used by the native SNES map. This is separate from the sector tag; structural map edits can renumber sectors.")]
        public int InternalSectorNumber {get{return index;}}
        void Enable(){GeometryTools.Enable(map,Context.Rom.MakeRoom(map.Slot,1024,1024).RoomTexture);}
        [Category("Heights"),DisplayName("Floor height"),Description("World units. Editing heights rebuilds the native geometry and platform tables.")]
        public new int Floor {get{return s.Floor;}set{Bin.Require(value>=-4096&&value<=s.Ceiling,"Floor must be between -4096 and the ceiling.");Enable();s.Floor=value;}}
        [Category("Heights"),DisplayName("Ceiling height")]
        public new int Ceiling {get{return s.Ceiling;}set{Bin.Require(value>=s.Floor&&value<=4096,"Ceiling must be between the floor and 4096.");Enable();s.Ceiling=value;}}
        [Category("Behavior"),DisplayName("Sector tag"),Description("Link a switch or walk trigger to this sector using the same nonzero tag.")]
        public new int Tag {get{return s.Tag;}set{Bin.Require(value>=0&&value<=255,"Native tags are 0..255.");Enable();s.Tag=value;}}
        [Category("Behavior"),DisplayName("Motion preset"),Description("Door closes the ceiling and adds use actions on adjoining portals. Lift, moving floor, stairs and crusher assign a tag and a starter action; inspect the linedef action to customize it.")]
        public SectorMotion Motion {get{return s.Motion;}set{Enable();GeometryTools.Preset(map,index,value);}}
    }
    public class VertexProperties {
        readonly Map map;readonly Point p;public VertexProperties(Map m,int index){map=m;p=m.Vertices[index];}
        void Enable(){GeometryTools.Enable(map,Context.Rom.MakeRoom(map.Slot,1024,1024).RoomTexture);}
        [Category("Vertex")]public int X {get{return p.X;}set{Bin.Require(Math.Abs(value)<=8192,"Keep coordinates within -8192..8192.");Enable();p.X=value;}}
        [Category("Vertex")]public int Y {get{return p.Y;}set{Bin.Require(Math.Abs(value)<=8192,"Keep coordinates within -8192..8192.");Enable();p.Y=value;}}
    }
    public class LineProperties {
        readonly Map map;readonly Wall wall;public LineProperties(Map m,int index){map=m;wall=m.Walls[index];}
        void Enable(){GeometryTools.Enable(map,Context.Rom.MakeRoom(map.Slot,1024,1024).RoomTexture);}
        [Category("Behavior"),DisplayName("Action"),Description("Native SNES line action. Use means press Use/Open; Walk means cross the line. Tagged actions need a matching sector tag.")]
        public NativeAction Action {get{return(NativeAction)wall.Type;}set{Bin.Require(Enum.IsDefined(typeof(NativeAction),value),"Choose a supported SNES action from the list.");Bin.Require(EngineActions.Doors[(int)value]!=1 || GeometryTools.Far(map,wall)>=0,"A manual door needs a two-sided boundary into a door sector.");Enable();wall.Type=(int)value;}}
        [Category("Behavior"),DisplayName("Target tag")]
        public int Tag {get{return wall.Tag;}set{Bin.Require(value>=0&&value<=255,"Tags must be 0..255.");Enable();wall.Tag=value;}}
        [Category("Collision"),Description("Block passage even when there is a sector on both sides.")]
        public bool Impassable {get{return(wall.Flags&8)!=0;}set{Enable();wall.Flags=value?wall.Flags|8:wall.Flags&~8;}}
        [Category("Automap")]public bool Secret {get{return(wall.Flags&4)!=0;}set{Enable();wall.Flags=value?wall.Flags|4:wall.Flags&~4;}}
        [Category("Collision"),Description("Two-sided state follows the linked back sector. Set Back sector to create or remove a portal."),ReadOnly(true)]public bool DoubleSided {get{return GeometryTools.Far(map,wall)>=0;}}
        [Category("Automap")]public bool Hidden {get{return(wall.Flags&32)!=0;}set{Enable();wall.Flags=value?wall.Flags|32:wall.Flags&~32;}}
        [Category("Surfaces"),TypeConverter(typeof(WallTextureConverter)),DisplayName("Front upper / middle")]
        public int FrontTexture {get{return GeometryTools.Front(map,wall).Upper;}set{Enable();GeometryTools.Front(map,wall).Upper=value;}}
        [Category("Surfaces"),TypeConverter(typeof(WallTextureConverter)),DisplayName("Front lower")]
        public int FrontLower {get{return GeometryTools.Front(map,wall).Lower;}set{Enable();GeometryTools.Front(map,wall).Lower=value;}}
        [Category("Surfaces"),TypeConverter(typeof(WallTextureConverter)),DisplayName("Back upper")]
        public int BackTexture {get{Face face=GeometryTools.Back(map,wall);return face==null?GeometryTools.Front(map,wall).Upper:face.Upper;}set{Enable();Face face=GeometryTools.Back(map,wall);Bin.Require(face!=null,"This line has only one side.");face.Upper=value;}}
        [Category("Surfaces"),TypeConverter(typeof(WallTextureConverter)),DisplayName("Back lower")]
        public int BackLower {get{Face face=GeometryTools.Back(map,wall);return face==null?GeometryTools.Front(map,wall).Lower:face.Lower;}set{Enable();Face face=GeometryTools.Back(map,wall);Bin.Require(face!=null,"This line has only one side.");face.Lower=value;}}
        [Category("Surfaces"),DisplayName("Texture X offset")]
        public int OffsetX {get{return wall.OffsetX;}set{Enable();wall.OffsetX=(value%256+256)%256;}}
        [Category("Surfaces"),DisplayName("Texture Y offset")]
        public int OffsetY {get{return wall.OffsetY;}set{Enable();wall.OffsetY=(value%256+256)%256;}}
        [Category("Links"),DisplayName("Front sector")]public int NearSector {get{return GeometryTools.Near(map,wall);}}
        [Category("Links"),DisplayName("Back sector")]public int FarSector {get{return GeometryTools.Far(map,wall);}set{Bin.Require(value>=-1&&value<map.Sectors.Count&&value!=GeometryTools.Near(map,wall),"Back sector must be another sector index, or -1 for a solid wall.");Enable();Face front=GeometryTools.Front(map,wall);front.Far=value;front.Solid=value<0;if(value<0){wall.BackFace=-1;wall.Flags|=1;}else{Face back=GeometryTools.Back(map,wall);if(back==null){back=GeometryTools.AddFace(map,value,front.Near,front.Upper);wall.BackFace=back.Offset;}back.Near=value;back.Far=front.Near;back.Solid=false;wall.Flags&=~1;}}}
    }
}
