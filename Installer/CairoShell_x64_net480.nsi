;--------------------------------
; CairoShell_64.nsi

!define ARCBITS 64
!define ARCNAME "x64"
!define NETTARGET "net480"
!define OUTNAME "CairoSetup_64bit_net480"
!define MUI_WELCOMEPAGE_TEXT "$(PAGE_Welcome_Text_${NETTARGET})"

!include "CairoShell.nsi"
