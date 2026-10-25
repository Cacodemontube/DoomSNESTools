cmake_minimum_required(VERSION 3.16)
project(DoomSNESTools VERSION 0.1.0 LANGUAGES C)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
add_library(DoomSNESAudio SHARED
    native/src/doom_audio_preview.c
    native/src/snes/apu.c native/src/snes/spc.c native/src/snes/dsp.c
    native/src/snes/dsp_shadow.c native/src/snes/audio_shadow.c)
target_include_directories(DoomSNESAudio PRIVATE native/src)
set_target_properties(DoomSNESAudio PROPERTIES PREFIX "" OUTPUT_NAME "DoomSNESAudio")
if(MINGW)
    target_link_options(DoomSNESAudio PRIVATE -static-libgcc)
elseif(UNIX)
    target_link_libraries(DoomSNESAudio PRIVATE m)
endif()
