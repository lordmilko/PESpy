# PESpy

PESpy is a Portable Executable parser for .NET

*All I want is a parser that lets me visualize an entire Portable Executable file, as it truly is*

Every boy and his dog has written their own PE File parser

* Most PE File parsers just cover the "most common" data structures the use cases they're interested in
* Some will *leave out* certain structures like `IMAGE_DOS_HEADER` because they say "they're not important, you don't need that"
* You might get a few here or there that cover some more advanced use cases, but they almost never go all the way; I can count on one hand the number of parsers I've seen that even bother to include .NET/ECMA 335 metadata
* And invariably, almost *everyone* will attempt to introduce their own fancy abstractions over the native data types. I know what an `IMAGE_DYNAMIC_RELOCATION_TABLE` is. I don't know what "DVRT" is, nor which data structure it's supposed to correspond to.

PESpy's struct definitions exact follow the names of their native counterparts everywhere that this is possible. `ImageDosHeader == IMAGE_DOS_HEADER`, `VsVersionInfo == VS_VERSIONINFO`, etc. In scenarios where this information is unavailable (such as the the `Reproducible` debug directory type), PESpy has chosen a name that hopefully should not be too controversial. On every type definition, PESpy makes clear which native type this structure corresponds to, or whether the name of this structure was synthesized due to no native structure being available.

