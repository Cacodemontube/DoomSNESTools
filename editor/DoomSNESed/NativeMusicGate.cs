using System;using System.Linq;
namespace DoomSNESed {
    public static class NativeMusicGate {
        public const int StopAddress=0x1eb0;
        public static bool NeedsUpgrade(byte[] rom){byte[] driver=NativeCompression.Decode(rom,RomLayout.At(rom,0xed652));return driver.Length==6112&&Bin.U16(driver,0x1c47-0x700)==0x1024;}
        public static byte[] Driver(byte[] rom){byte[] driver=NativeCompression.Decode(rom,RomLayout.At(rom,0xed652));int table=0x1c37-0x700+8*2;Bin.Require(driver.Length==6112,"Unsupported SPC driver.");int prior=Bin.U16(driver,table);Bin.Require(prior==0||prior==0x1024||prior==StopAddress,"This modified SPC driver uses the converter's reserved stop command.");
            // E8 was unused. Install a bounded ownership check followed by KOFF.
            // Preserve the original note parser, BRR decoder and effects.
            byte[] stop={0x3f,0x94,0x17,0xf5,0x27,0x06,0x30,0x13,0xfd,0x7d,0x08,0x80,0x76,0xd0,0x00,0xd0,0x0a,0xf6,0xc4,0x17,0x8f,0x5c,0xf2,0x04,0xf3,0xc4,0xf3,0x5f,0x3a,0x18};
            // Dispatch handlers jump back to the parser after their operation.
            // 1024 looked like spare assembler text in the ROM, but is live
            // workspace after startup. Instrument processing overwrites it.
            // Restore that area when upgrading an older converter ROM.
            if(prior==0x1024){byte[] workspace={0x75,0x72,0x53,0x74,0x65,0x72,0x65,0x6f,0x50,0x61,0x6e,0x2b,0x58,0x2c,0x41,0x0d,0x0a,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x64,0x65};Buffer.BlockCopy(workspace,0,driver,0x1024-0x700,workspace.Length);}
            // Trailing diagnostic text: after the timer helper and before the
            // driver's constants at 1ed2. Never overlap live track workspace.
            Buffer.BlockCopy(stop,0,driver,StopAddress-0x700,stop.Length);Bin.W16(driver,table,StopAddress);
            // Timer 1 counts elapsed pulses, not a boolean. Preserve the original
            // tempo accumulator once per pulse. Discard startup backlog while tempo
            // is zero; the first song tick initializes it. The counter lives on
            // the stack, and the helper occupies unused text at 1e91..1eaf.
            byte[] original={0xe4,0xcb,0xf0,0x07,0x60,0x84,0xcc,0xc4,0xcc,0x90,0x03,0x3f,0xd4,0x16};
            int hook=0x733-0x700;Bin.Require(driver.Skip(hook).Take(14).SequenceEqual(original)||(driver[hook]==0x3f&&driver[hook+1]==0x91&&driver[hook+2]==0x1e),"Unsupported SPC timer routine.");
            byte[] catchup={0x2d,0xe4,0xcb,0xd0,0x04,0xae,0xe8,0x01,0x2d,0xe4,0xcb,0xf0,0x07,0x60,0x84,0xcc,0xc4,0xcc,0x90,0x03,0x3f,0xd4,0x16,0xae,0x9c,0xf0,0x03,0x2d,0x2f,0xeb,0x6f};
            Buffer.BlockCopy(catchup,0,driver,0x1e91-0x700,catchup.Length);Array.Clear(driver,hook,14);driver[hook]=0x3f;driver[hook+1]=0x91;driver[hook+2]=0x1e;return driver;}
        public static void Apply(byte[] source,byte[] target){byte[] raw=Driver(source),packed=NativeCompression.Encode(raw);Bin.Require(packed.Length<=0xeeb96-0xed652,"The note-stop driver does not fit its original ROM allocation.");Bin.Require(NativeCompression.Decode(packed,0).SequenceEqual(raw),"SPC driver compression verification failed.");Array.Clear(target,RomLayout.At(target,0xed652),0xeeb96-0xed652);Buffer.BlockCopy(packed,0,target,RomLayout.At(target,0xed652),packed.Length);}
    }
}

