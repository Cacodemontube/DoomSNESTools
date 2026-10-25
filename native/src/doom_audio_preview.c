/* Converter preview: execute the retail SPC driver on the same APU/DSP core
 * as gameplay. No ROM or soundbank is embedded in this library. */
#include "snes/apu.h"
#include "audio_trace.h"
#include <stdint.h>
#include <string.h>
uint64_t g_apu_timer0_total_ticks;
#ifdef _WIN32
#define API __declspec(dllexport)
#else
#define API
#endif
void audio_trace_on_sample(int16_t l,int16_t r,int d,uint32_t n){(void)l;(void)r;(void)d;(void)n;}
void audio_trace_on_reg_write(uint8_t a,uint8_t v){(void)a;(void)v;}
void audio_trace_on_consume(uint64_t i,uint32_t n,uint32_t a){(void)i;(void)n;(void)a;}
void audio_trace_on_cpu_port_apply(uint8_t p,uint8_t v){(void)p;(void)v;}
void audio_trace_on_spc_port_read(uint8_t p,uint8_t v){(void)p;(void)v;}
void audio_trace_on_spc_port_write(uint8_t p,uint8_t v){(void)p;(void)v;}
void audio_trace_on_shadow_div(double a,double b){(void)a;(void)b;}
void audio_trace_on_faithful_div(double a){(void)a;}
void audio_trace_on_echo_div(double a){(void)a;}
void audio_trace_on_brr_compare(uint16_t b,uint8_t h,uint8_t s,int c,int r,int old,int older){(void)b;(void)h;(void)s;(void)c;(void)r;(void)old;(void)older;}
static void advance(Apu *a,unsigned cycles){while(cycles--)apu_cycle(a);}
static int command(Apu *a,unsigned serial,unsigned cmd,unsigned parameter){
    a->inPorts[0]=parameter;a->inPorts[1]=parameter>>8;
    a->inPorts[3]=cmd;a->inPorts[2]=serial;
    for(unsigned i=0;i<102400;i++){apu_cycle(a);if(a->outPorts[2]==serial)return 1;}return 0;
}
/* A persistent renderer lets the converter fill short playback buffers without
 * emulating the whole requested preview before the first sample is heard. */
typedef struct PreviewSession { Apu *apu; unsigned serial; uint64_t frames; } PreviewSession;
API void *DoomAudioOpen(const uint8_t *driver,int length,const uint8_t *ram,int song){
    if(!driver||length!=6112||!ram||song<0||song>106)return NULL;
    PreviewSession *s=calloc(1,sizeof(*s));if(!s)return NULL;
    s->apu=apu_init();if(!s->apu){free(s);return NULL;}Apu *a=s->apu;apu_reset(a);
    memcpy(a->ram,ram,65536);memcpy(a->ram+0x700,driver,length);
    a->romReadable=false;a->spc->pc=0x700;a->spc->sp=0xff;a->cpuCyclesLeft=0;
    advance(a,32000);a->dsp->sampleRead=a->dsp->sampleWrite;s->serial=4;
    if(!command(a,2,2,0)||!command(a,3,6,song)||!command(a,4,18,0x7f01)){
        apu_free(a);free(s);return NULL;
    }return s;
}
API int DoomAudioRead(void *session,int frames,int16_t *pcm,int *levels,int capacity){
    PreviewSession *s=session;if(!s||!pcm||frames<1||frames>32000)return -1;Apu *a=s->apu;
    if(levels&&capacity>0)memset(levels,0,capacity*sizeof(*levels));
    for(int i=0;i<frames;i++){
        advance(a,32);
        if(levels){unsigned bucket=(i/640)*8;if(bucket+8<=(unsigned)capacity)for(int c=0;c<8;c++){
            int v=a->dsp->channel[c].sampleOut,l=a->dsp->channel[c].volumeL,r=a->dsp->channel[c].volumeR;
            if(v<0)v=-v;if(l<0)l=-l;if(r<0)r=-r;v=v*(l>r?l:r)/128;
            if(v>levels[bucket+c])levels[bucket+c]=v;
        }}dsp_getSamples(a->dsp,pcm+i*2,1);
    }s->frames+=frames;return frames;
}
API int DoomAudioCommand(void *session,int cmd,int parameter){
    PreviewSession *s=session;if(!s||cmd<0||cmd>76||parameter<0||parameter>65535)return 0;
    s->serial=(s->serial+1)&127;if(!s->serial)s->serial=1;
    return command(s->apu,s->serial,cmd,parameter);
}
/* Diagnostic snapshot used by restart regressions, never shared with playback. */
API int DoomAudioState(void *session,uint8_t *ram,uint8_t *dsp,int *registers){
    PreviewSession *s=session;if(!s||!ram||!dsp||!registers)return 0;Apu *a=s->apu;
    memcpy(ram,a->ram,65536);memcpy(dsp,a->dsp->ram,128);
    registers[0]=a->spc->pc;registers[1]=a->spc->sp;registers[2]=a->ram[0xcb];
    registers[3]=a->outPorts[2];return 1;
}
API void DoomAudioClose(void *session){PreviewSession *s=session;if(s){apu_free(s->apu);free(s);}}
/* mode 0: song; mode 1: effect. Returns stereo frames, or a negative error.
 * pitch output is the first active DSP voice's hardware playback rate.
 * Diagnostic bit 1 suppresses echo; bit 2 returns final active-voice count
 * through rate instead of pitch. */
API int DoomAudioRenderLevels(const uint8_t *driver,int driver_size,const uint8_t *ram,
    int mode,int id,int frames,int16_t *pcm,int *rate,int *levels,int capacity){
    if(!driver||driver_size!=6112||!ram||!pcm||frames<1||frames>32000*600||id<0||id>106)return -1;
    Apu *a=apu_init();if(!a)return -2;apu_reset(a);
    memcpy(a->ram,ram,65536);memcpy(a->ram+0x700,driver,driver_size);
    a->romReadable=false;a->spc->pc=0x700;a->spc->sp=0xff;a->cpuCyclesLeft=0;
    advance(a,32000);a->dsp->sampleRead=a->dsp->sampleWrite;
    if(!command(a,2,2,0)||!command(a,3,(mode&1)?4:6,(unsigned)id)||!command(a,4,18,0x7f01)){apu_free(a);return -3;}
    if(rate)*rate=0;
    for(int i=0;i<frames;i++){
        /* Bit 1 requests dry output for note-release diagnostics. Normal
         * previews retain the original SPC echo/reverb. */
        if(mode&2)a->dsp->echoVolumeL=a->dsp->echoVolumeR=0;
        advance(a,32);
        if(levels){int bucket=(i/640)*8;if(bucket+8<=capacity)for(int c=0;c<8;c++){int value=a->dsp->channel[c].sampleOut;int l=a->dsp->channel[c].volumeL,r=a->dsp->channel[c].volumeR;if(value<0)value=-value;if(l<0)l=-l;if(r<0)r=-r;value=value*(l>r?l:r)/128;if(value>levels[bucket+c])levels[bucket+c]=value;}}
        if(rate&&!*rate)for(int c=0;c<8;c++)if(a->dsp->channel[c].gain&&a->dsp->channel[c].pitch){*rate=(a->dsp->channel[c].pitch*32000+2048)/4096;break;}
        dsp_getSamples(a->dsp,pcm+i*2,1);
    }
    if((mode&4)&&rate){*rate=0;for(int c=0;c<8;c++)if(a->dsp->channel[c].gain&&(a->dsp->channel[c].volumeL||a->dsp->channel[c].volumeR))(*rate)++;}
    apu_free(a);return frames;
}
API int DoomAudioRender(const uint8_t *driver,int driver_size,const uint8_t *ram,int mode,int id,int frames,int16_t *pcm,int *rate){return DoomAudioRenderLevels(driver,driver_size,ram,mode,id,frames,pcm,rate,0,0);}
/* Offline reference analysis: observe original sequence reads without replacing
 * the retail sequencer. Each record is cycle/PC/address/value/X/Y/A/return-PC.
 * At key-off (1794), value/Y/A report voice, owner and voice flags. */
API int DoomAudioScan(const uint8_t *driver,const uint8_t *ram,int song,int origin,int frames,int *records,int capacity){
    if(!driver||!ram||!records||frames<1||frames>32000*600||capacity<1)return -1;
    Apu *a=apu_init();apu_reset(a);memcpy(a->ram,ram,65536);memcpy(a->ram+0x700,driver,6112);a->romReadable=false;a->spc->pc=0x700;a->spc->sp=255;a->cpuCyclesLeft=0;
    advance(a,32000);if(!command(a,2,2,0)){apu_free(a);return -2;}
    a->inPorts[0]=song;a->inPorts[1]=0;a->inPorts[2]=3;a->inPorts[3]=6;
    int count=0;for(int cycle=0;cycle<frames*32;cycle++){
        if(a->cpuCyclesLeft==0){Spc *s=a->spc;unsigned pc=s->pc,op=a->ram[pc],dp=s->p?256:0,addr=0;
            if(op==0xf7){unsigned p=dp+a->ram[(pc+1)&65535];addr=(a->ram[p]|a->ram[dp+((p+1)&255)]<<8)+s->y;}
            if(op==0xe7){unsigned p=(a->ram[(pc+1)&65535]+s->x)&255;addr=(a->ram[dp+p]|a->ram[dp+((p+1)&255)]<<8);}
            if(op==0xf5||op==0xf6||op==0xe5){addr=(a->ram[(pc+1)&65535]|a->ram[(pc+2)&65535]<<8)+(op==0xf5?s->x:op==0xf6?s->y:0);}
            if(pc==0x18b0||pc==0x1794)addr=origin;addr&=65535;if(addr>=origin&&addr<0xb490&&count<capacity){int *r=records+count++*8;r[0]=cycle;r[1]=pc;r[2]=addr;r[3]=pc==0x1794?a->ram[0x627+s->x]:pc==0x18b0?s->a:a->ram[addr];r[4]=s->x;r[5]=pc==0x1794?a->ram[0xd0+(a->ram[0x627+s->x]&7)]:s->y;r[6]=pc==0x1794?a->ram[0x246+(a->ram[0x627+s->x]&7)]:s->a;r[7]=a->ram[0x100+((s->sp+1)&255)]|(a->ram[0x100+((s->sp+2)&255)]<<8);}
        }apu_cycle(a);if(a->dsp->sampleWrite-a->dsp->sampleRead>4096)a->dsp->sampleRead=a->dsp->sampleWrite;
    }apu_free(a);return count;
}
