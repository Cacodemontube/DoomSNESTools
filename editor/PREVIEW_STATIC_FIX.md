# SPC preview cleanup and automatic claves — 2026-10-19

The Windows x64 converter and editor in `build` include these changes.

## Playback

Previously, `SpcPreviewPlayer.Dispose` cancelled rendering and reset waveOut
from the UI thread without waiting for the rendering task to finish. That
task could still render/submit another buffer while a different preview or
import was starting. A baseline Windows-device probe confirmed that the
playback task remained unfinished after Dispose returned.

The worker now exclusively resets/closes the audio device. Dispose cancels
and waits for its buffers, device and native APU session to be released.
Cancellation is checked again after native rendering, before submitting PCM.
Reusing a prepared WAVEHDR updates only its buffer length; it no longer
overwrites Windows-owned flags/linkage with a marshalled snapshot. Playback
is single-use, with repeated disposal supported. The converter cancels
pending preparation/queued UI callbacks before waiting for playback cleanup.

This fixes a reproducible lifecycle race. The exact audible static in
`Distort.mp4` was not reproduced locally, so the original import/play sequence
still needs a listening retest. No clipping limiter, sample replacement or
change to the SPC synthesis was introduced to hide distortion.

## Claves

The earlier GM percussion note 75 mapping worked when the selected home bank
contained BSN 56. A dominant melodic part could select a different bank and
silently fall back to another drum. Baseline probes returned patches 43, 98,
41 and 42 for claves in different mixed arrangements.

Automatic conversion now requires BSN 56 for GM percussion notes 75/76/77
(claves/high woodblock/low woodblock). The existing mixed-bank packer includes
the actual E1M2 BRR sample and envelopes when the home bank lacks it.
The native percussion pitch remains unchanged. This is GM channel-10
percussion; melodic GM Clavinet is a separate instrument.

## Validation

- Before/after Windows-device probe: playback task unfinished after Dispose
  in the old build, completed in the updated build.
- 40 muted Windows waveOut cycles: immediate stop, queued-buffer stop,
  repeated disposal, garbage collection pressure, natural completion.
- 24 pairs of interleaved fresh SPC sessions: chunked PCM exactly matches
  one contiguous reference read.
- Six mixed-instrument claves arrangements retain patch 56 through auto-fit;
  a broader manual probe covered 16 GM families.
- Existing pickup/claves, driver restart, instrument relocation, ROM/preview
  parity, native timing and steady percussion tests passed.
- Baseline exploration rendered 54 local MIDI files for 90 seconds apiece
  without sequence bytes being overwritten. This does not establish that
  every rendered waveform was free of audible distortion.

Evidence and previous binaries: `build-evidence/preview-static-20261019/`.
The native audio DLL and gameplay executable were not changed.
