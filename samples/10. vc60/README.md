## Visual C++ 6.0 Test Files

1. File -> New -> Win32 Console Application
2. Call the Project TestApp
3. A simple application

We want to generate the following samples

* CoffSymbolsOnly: Alt+F7 -> Link -> Category: Debug. Debug info: COFF format
* For splitting: Alt+F7 -> Link -> Category: Debug. Debug info: Both formats

`*.dbg` files are created by using `rebase.exe`. rebase.exe doesn't seem to like it if you try and rebase straight out of your output directory, so copy your `TestApp.exe` somewhere else and do rebase.exe. It doesn't hurt to run rebase.exe under a command prompt you've run `C:\Program Files (x86)\Microsoft Visual Studio\VC98\Bin\VCVARS32.BAT` in'

Make a copy of the files before splitting for the `CoffAndPdbSymbols_PreSplit` case

```
"C:\Program Files (x86)\Microsoft Visual Studio\VC98\Bin\rebase.exe" -b 0x400000 -x . TestApp.exe
```

Note that you aren't actually supposed to use the `-a` parameter. Either `-b` or `-i` must be specified, so we can just specify the initial base address. When you specify the output directory as `.` the `*.dbg` file will be placed in the same directory as the input file. Otherwise, if you do something like `C:\Temp` it will create a folder `C:\Temp\exe\TestApp.dbg` and also copy the PDB file into this directory (if there is one)

This directory contains the following samples

| Name            | Description
|-----------------|------------------------------------------------------------|
| CoffSymbolsOnly | Alt+F7 -> Link -> Category: Debug. Debug info: COFF format |
| CoffAndPdbSymbols_PreSplit | Alt+F7 -> Link -> Category: Debug. Debug info: Both formats. Contains `TestApp.exe` prior to splitting it with `rebase.exe` |
| CoffAndPdbSymbols_PostSplit | Alt+F7 -> Link -> Category: Debug. Debug info: Both formats. Contains `TestApp.exe`, `TestApp.pdb` and `TestApp.dbg` after splitting with `rebase.exe`. `TestApp.exe` is stripped, its debug info is copied to `TestApp.dbg` and a copy of the original `TestApp.pdb` is copied to the `rebase.exe` output folder |
| NB11_Split | Alt+F7 -> Link -> Customize -> Untick Use program database, Link -> Debug. Debug info: Microsoft format. This causes NB11 to be created. We then split this out using `rebase`. We get an error *unable to split symbols (0)* but it does successfully split |