## Visual C++ 5.0 Test Files

File -> New -> Win32 Console Application

NB11 is generated for packed files made by Visual C++ 5 (it seems that cvpack runs automatically now as part of link without invoking cvpack.exe separately)

Visual C++ 5 really, really wants to use NB10 + PDBs; you can make it use NB11 as follows:

```
cl /c /Zi TestApp.cpp
link /DEBUG /pdb:none main.obj
```

This technically gives you an NB11 file, however you can get way more stuff in your file if you go to Link -> Customize and untick Use program database. You can still use /Zi