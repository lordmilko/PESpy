## Microsoft C/C++ 7.0 Test Files

If you didn't let setup automatically mess with your AUTOEXEC.BAT and SYSTEM.INI, you need to
1. Copy C:\C700\INIT\AUTOEXEC.C70 to C:\C700\INIT\AUTOEXEC.BAT and run it. This will set your environment variables
2. Modify SYSTEM.INI to include the following under the [386enh] section
        device=C:\C700\BIN\VPFD.386
        device=C:\C700\BIN\CVW1.386
        device=C:\\C700\BIN\VMB.386
		
and also

        device=*VMCPD

if you already have items under C:\MSVC from Visual C++ 1.52, you need to remove these

Alternatively, as a hack you can just add in VPFD.386 and then you don't need to worry about removing the Visual C++ 1.52 ones

2. Run `cl /Zi TESTAPP.C`, which will generate an EXE with NB08 debugging information

Per https://bitsavers.trailing-edge.com/pdf/microsoft/msdos_c/Microsoft_C_7.0_1991/24778_Environment_and_Tools_199112.pdf CVPACK is automatically run by LINK. I was able to trick it into not running CVPACK by renaming the CVPACK EXE, leaving me with the original NB05 file

* Packed: NB08
* Unpacked: NB05