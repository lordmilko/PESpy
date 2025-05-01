## Microsoft C 5.0 Test Files

Create a file `C:\TESTAPP.C` with the following text

```c
#include <stdio.h>

int main(int argc, char** argv)
{
    printf("hello world\n");
    return 0;
}
```

Do

```
C:\C500\BIN\SAMPLE\NEW-VARS.BAT
cl /Zi TESTAPP.C
```

Invoking `NEW-VARS.BAT` will mess up your `PATH`, so you'll need to restart once you're done

This will create 3 files

```
C:\TESTAPP.EXE
C:\TESTAPP.MAP
C:\TESTAPP.OBJ
```

`TESTAPP.EXE` has a `NB00` signature at the end of the file. CodeView was first introduced in Microsoft C 4.0 so I don't understand why we get NB00 in Microsoft C 5.0 and DNRB in Microsoft C 4.0