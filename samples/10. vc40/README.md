## Visual C++ 4.0 Test Files

### Normal

File -> New -> Project Workspace -> Console Application
File -> New -> Text File -> main.cpp
Insert -> Files Into Project -> main.cpp

(note that you can see the files in the project by switching the sidebar on the left from ClassView to FileView)

Enter in the following text

```c
#include <stdio.h>

int main(int argc, char** argv)
{
    printf("hello world\n");
    return 0;
}
```

Build -> Rebuild All

### CVXD32

This folder contains the results of compiling the CVXD32 sample from the Windows 95 DDK, which demonstrates how to write a VXD in C

This sample consists of two programs:
* cvxdsamp.vxd - a VXD driver
* con_samp.exe - a test program that runs the VXD driver and makes an API call

con_samp is a PE file with NB09

cvxdsamp.sym gets created by mapsym.exe. [SYM files are derived from MAP files](https://win-archaeology.fandom.com/wiki/.SYM_Format)

### MIPS

"C:\Program Files (x86)\qemu\qemu-img.exe" create -f qcow2 nt4_server_mips.qcow2 4G
"C:\Program Files (x86)\qemu\qemu-system-mips64el.exe" -M magnum -m 128 -net nic -net user -global ds1225y.filename=nvram -bios NTPROM.RAW  -hda nt4_server_mips.qcow2 -cdrom winnt40wks_sp1_en.iso

Install Visual C++ 4.0 for MIPS

The GlobalPointerTableDirectory is created when an *.obj file contains a .sdata section, which occurs as a result of doing

#pragma data_seg(".sdata")

int a = 1;

.sdata is shared memory

my a = 1 wasn't actually set at the rva it indicated