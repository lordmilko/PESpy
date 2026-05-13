## Microsoft C 6.0 Test Files

`C:\C600\BIN\NEW-VARS.BAT`
`cl.exe /Zi TESTAPP.C`

/Zi will include symbolic information

Tiny is generated with

`cl /AT /Zi TESTAPP.C`

Note that by default cl will want to use emulated math with the small memory model, which means it will need `SLIBCE.LIB` to be installed. During setup, ensure you specify to include emulated math

https://www.infania.net/misc/kbarchive/kb/028/Q28173/index.html

Information about the general format of these files can be found here: https://www.pcjs.org/documents/books/mspl13/c/ctoolkit/

`Symbols\TESTAPP.EXE` has NB02 data at the end of it. `Tiny\TESTAPP.DBG` is just the NB02 data

VXD samples are the EBIOS sample from the Windows 3.1 DDK. The DDK comes with masm5, but not nmake, so I ran the following

```
C:\C600\BIN\NEW-VARS.BAT
set PATH=%PATH%;C:\DDK\WIN31\DDK386\TOOLS
```
By default, this sample only generates a *.sym file. To make it also generate CodeView I made the following changes

* In `MAKEFILE` add `-Zi` after the `-Mx` parameter
* In `EBIOS.LNK` add `/CODEVIEW` at the end of the line for `ebios.386`

Generates an LE file with NB00 CodeView information. The linker is LINK386, which is listed as being version 1