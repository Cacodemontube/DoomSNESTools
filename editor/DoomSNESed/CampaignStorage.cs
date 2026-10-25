using System;
using System.Collections.Generic;
using System.Linq;
namespace DoomSNESed {
    public static class CampaignStorage {
        // The engine has fixed slot tables, including episode/menu entry points.
        // Keep a tiny valid native fallback, not the original disabled level.
        // Never reclaim graphics/audio that a modified base placed in map space.
        public static bool Strip(Rom rom,byte[] target,Map original,List<Section> free){
            Section a=rom.GetSection(original.Slot,"A"),b=rom.GetSection(original.Slot,"B");
            if(a.Size<512||b.Size<128||rom.Far(0x32a8,original.Slot)!=a.Start||rom.Far(0x3566,original.Slot)!=b.Start)return false;
            if(rom.Project.CampaignSlots.Any(slot=>slot!=original.Slot&&(rom.Far(0x32a8,slot)==a.Start||rom.Far(0x3566,slot)==b.Start)))return false;
            Map fallback=rom.MakeRoom(original.Slot,256,256);GeometryTools.Enable(fallback,fallback.RoomTexture);
            for(int p=a.Start;p<a.Start+a.Size;p++)target[p]=255;
            for(int p=b.Start;p<b.Start+b.Size;p++)target[p]=255;
            NativeBuildInfo info=NativeCompiler.Build(rom,target,fallback);
            int overhead=info.GeometryBytes+info.DataBytes;
            rom.Budget.Capacity+=a.Size+b.Size;rom.Budget.Maps+=overhead;
            rom.Budget.DisabledMaps++;rom.Budget.MapSlotOverhead+=overhead;
            rom.Budget.MapBytesReclaimed+=a.Size+b.Size-overhead;
            free.Add(new Section{Start=a.Start+info.GeometryBytes,Size=a.Size-info.GeometryBytes});
            free.Add(new Section{Start=b.Start+info.DataBytes,Size=b.Size-info.DataBytes});
            return true;
        }
        public static string Report(StorageSummary budget){return budget==null||budget.DisabledMaps==0?"":"Campaign storage: "+budget.DisabledMaps+" disabled original maps removed; "+budget.MapBytesReclaimed.ToString("N0")+" bytes reclaimed for graphics, music, sounds and text. Fixed-slot safety entries use "+budget.MapSlotOverhead.ToString("N0")+" bytes. Space is reused inside the ROM.\r\n";}
    }
}
