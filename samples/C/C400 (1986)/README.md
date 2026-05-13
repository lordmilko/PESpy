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

`TESTAPP.EXE` has a `DNRB` signature at the end of the file, which is the original CodeView format

The `*.map` file emitted by default contains line numbers but no publics, which `mapsym.exe` (which I used from Visual C++ 5) rejects since you need to have at least one symbol. Linking with `link TESTAPP.OBJ /MAP` gives you a `*.map` file with publics...but no line numbers. As such, the `LineNumbers\` directory contains a hacked `*.map` file I synthesized by taking the output of `/MAP` and adding the line numbers from without `/MAP` to just before the end (it seems that the placement within the file matters; it ignored the line numbers when they were listed before the publics)