## Microsoft C 4.0 Test Files

## Installation

Manually copy the files off the floppies to C:\C400. Some disks have files with the same name (e.g. README.doc). Skip these

## Compile

Microsoft C 4.0 uses K&R C. so you can't do a normal `int argc, char** argv`. Instead, you declare args in a [crazy way](https://gunkies.org/wiki/C_programming_language)

Create `C:\TESTAPP.C` with

```c
#include <stdio.h>

int main(argc, argv)
int argc;
char** argv;
{
    printf("hello world\n");
    return 0;
}
```

Do

```
cd C:\C400
cl /Zi C:\TESTAPP.C
```
This will create 3 files

```
C:\C400\TESTAPP.EXE
C:\C400\TESTAPP.MAP
C:\C400\TESTAPP.OBJ
```

`TESTAPP.EXE` has a `DNRB` signature at the end of the file. I don't currently know what this means (perhaps BRND? Borland?)