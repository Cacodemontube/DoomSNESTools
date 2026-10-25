# v0.01 maintenance build: reclaim unchecked campaign maps

PWAD campaigns now remove original level data from unchecked map slots before
allocating imported graphics, music, sound and text. The fixed SNES map-slot
tables retain tiny valid fallback entries; normal progression still follows
only the selected campaign and ends with the episode 3 story text. A one-map
retail-base test recovers 928,746 bytes from 21 disabled levels while retaining
6,174 bytes of safe slot bookkeeping. Savings vary with the selected slots.

The storage meter and conversion report show the reclaimed space. A standard
ROM remains 2 MiB: the recovered bytes are available inside that ROM rather
than reducing the file size. Individual map limits, SPC RAM and native sample
slot limits still apply.

Disabled maps remain in the source ROM and SNED project so that selecting them
again restores them on the next build. The exported campaign contains neither
their original geometry nor their original objects. Original-ROM asset mode
and ordinary editor projects without a campaign selection preserve every map.

Modified base ROMs retain protected asset allocations and any map allocations
shared with an active slot. Only verified available regions can be reclaimed.
Patch records are split at native allocation/table boundaries so the existing
DoomSNESRecomp patch validator can accept the exports.
