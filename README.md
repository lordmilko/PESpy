# PESpy

PESpy is a .NET library, and standalone application, for visualizing Microsoft binary formats.

PESpy supports the following file types
* Portable Executable
* OBJ (COFF Based)
* DBG
* LIB (COFF Based)
* PDB (v1, v2, v7)
* Portable PDB
* New Executable
* Linear Executable

Every boy and his dog has written their own PE File parser. I don't just want a PE File parser however, I want a *visualizer*

* I want to see *everything* - all known data structures, no matter how obscure
* I want to know how they physically relate to one another. If I point at a random byte in the file, I want to know exactly what it is, what it's part of, and what's above and below it
* I don't want to have to figure out what all your crazy abstractions mean. I know what an `IMAGE_DYNAMIC_RELOCATION_TABLE` is; I don't know what "DVRT" is, and don't want to have to jump through hoops to figure out how *your* data model maps to the native data model at every turn

PESpy's struct definitions exact follow the names of their native counterparts everywhere that this is possible. `ImageDosHeader == IMAGE_DOS_HEADER`, `VsVersionInfo == VS_VERSIONINFO`, etc. In scenarios where this information is unavailable (such as the the `Reproducible` debug directory type), PESpy has chosen a name that hopefully should not be too controversial. On every type definition, PESpy makes clear which native type this structure corresponds to, or whether the name of this structure was synthesized due to no native structure being available.

