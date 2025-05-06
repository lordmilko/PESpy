## Basic PDS 7.0

NB01

bc7 is the name the software installs to (and is also the name of the compiler), so we use the same name

Create a file `C:\TESTAPP.BAS` with the following text

```basic
PRINT "hello world"
```

Run `C:\BC7\BIN\QBX.EXE` and open `C:\TESTAPP.BAS`

Do Run -> Make EXE File. Ensure that under *Debug* CodeView Information is ticked. When you make the EXE, it will be created in the current directory. If you cd'd into C:\BC7\BIN first, it will be in there

This will create 2 files

```
TESTAPP.EXE
TESTAPP.OBJ
```

`TESTAPP.EXE` has a `NB01` signature at the end of the file.