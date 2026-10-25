# v0.01 maintenance build: music transitions and sky selection

Mixed MIDI/MUS instrument banks used to overwrite shared SPC waveform,
instrument, envelope and song directories. Retail title/intermission and level
modules upload only their own changes to those tables, leaving some imported
pointers active. Each module now restores a shared baseline before its normal
uploads. One baseline chunk is reused across the module lists (raw for native campaign
patch compatibility, compressed for ROM-only packing);
contiguous uploads are grouped and superseded lists are discarded before ROM
allocation. Original tunes and imported note data are preserved.

Reopening an older converter ROM and exporting it repairs missing table resets
without requiring the original MIDI or PWAD. Keep a backup of your original ROM.
Opening a SNED project with its original base ROM and exporting also applies the
repair. A repaired DSE should be used with the same base ROM as its project.

Importing another score or opening instrument assignments now stops any active
preview first. Sample playback and its stream are disposed on Stop, preventing
an old preview from remaining active while selecting another instrument.

The ceiling colour picker lists Sky (255) first and shows a sky thumbnail.
The property dropdown labels it `255 / Sky`. Floors remain colour-only because
the native engine does not support a sky floor.

Validation includes the supplied Water World fixture, all 20 original music
module transitions, exact clean-versus-transition title/intermission PCM,
16 repeated Water World conversions and 40 muted Windows audio start/stop
cycles. These are software tests; physical SNES hardware has not been tested.
