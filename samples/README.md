## Samples

The following table lists CodeView versions and which OS + compiler was used to create them

The first CodeView debugger was developed in 1985 and first shipped with Microsoft C 4.0

*Bold* indicates the tool that generates the debug info. Items with no CV value do not introduce a new CodeView format, but are listed for the purpose of illustrating their compiler versions

| CV   | OS           | Compiler               | Year | CL      | LINK    | ILINK  | Packed | Sample          | Comments
|------|--------------|------------------------|------|---------|---------|--------|--------|-----------------|----------
| DNRB | DOS          | Microsoft C 4.0        | 1986 | 4.00    | *3.51*  | N/A    | No     | `C400\`         | C4 included the first version of CodeView, but curiously produces a signature "DNRB" that nobody has ever heard of. Even LINK4 does this. The signature doesn't repeat itself up above like OMF signatures are supposed to
| NB00 | DOS          | Microsoft C 5.0        | 1987 | 5.00    | *3.61*  | N/A    | No     | `C500\`         | Produced by linkers earlier than September 1989, including LINK 3.x, 4.0 and 5.00-5.03
| NB01 | DOS          | BASIC PDS 7.0          | 1989 | BC 7.00 | *5.05*  |        | No     | `bc7\`          |
| NB02 | DOS          | Microsoft C 6.0        | 1989 |         | *5.10*  | 1.20   | No     | `C600\`         |
| NB03 |              |                        |      |         |         |        |        |                 | I think IBM produced this; Microsoft tools explicitly don't support it
| NB04 |              |                        |      |         |         |        |        |                 | I think IBM produced this; Microsoft tools explicitly don't support it
| NB05 | DOS          | Microsoft C/C++ 7.0    | 1992 | 8.00c   | *5.30*  | 1.30   | No     | `C700\Unpacked` | I've seen conflicting information as to whether you need to use LINK 5.20 or 5.30
| NB06 |              |                        |      |         |         | *1.30* | No     |                 | Supposedly, incrementally linked by ILINK 1.30 and not cvpacked. C7 has ILink 1.30 but it always does a full link, and it didn't work. PWB indicates you can specify /INC to LINK but that doesn't work either, and PWB crashes when run under DOS inside Windows 3.11 (which is needed for cl.exe to work)
| NB07 | Windows 3.11 | QuickC for Windows 1.0 | 1991 | N/A     | *5.15*  | N/A    | Yes    |`qcwin\`         |
| NB08 | DOS          | Microsoft C/C++ 7.0    | 1992 |         | *5.30*  | 1.30   | Yes    |`C700\Packed`    | CodeView 4.00-4.05 after it has been packed. C/C++ 7.0 uses LINK 5.30, ILINK 1.30
|      |              | Visual C++ 1.00        |      | 8.00    | *5.50*  | ?      | ?      | N/A             |
| NB09 | Windows 3.11 | Visual C++ 1.52        | 1993 | 8.00c   | *5.60*  |        | Yes    |`vc152\`         | CodeView 4.10 after it has been packed. CL 8.00c, LINK 5.60
|      |              | Visual C++ 2.0         |      | 9.00    | *2.50*  | ?      | ?      | N/A
| NB10 | Windows 11   | Visual C++ 4           | 1995 | 10.00   | *3.00*  |        | N/A    |`vc40\`          | LINK is now the incremental linker (and perhaps took over ILINK's version numbers?)
| NB11 | Windows 11   | Visual C++ 5           | 1997 | 11.00   | *5.00*  |        | Yes    |`vc50\`          | Not sure if there's even a way to prevent packing anymore, it's either automatic, or implied by the NB version. You have to specify No PDB in link options. CodeView 5.0. Only NB10 (PDB 2.0) and RSDS (PDB 7.0) refer to PDBs
|      |              | Visual C++ 6           | 1998 | 12.00   | *6.00*  |        | ?      | N/A             |
| RSDS | Windows 7    | Visual Studio .NET     | 2002 | 13.00   | *7.00*  |        | N/A    |`vc70\`          | Visual C++ 7.0
|      |              | Visual Studio 2003     | 2003 | ?       | ?       |        | N/A    | N/A             | Visual C++ 7.1
|      |              | Visual Studio 2005     | 2005 | ?       | ?       |        | N/A    | N/A             | Visual C++ 8.0
|      |              | Visual Studio 2008     | 2008 | 15.00   | *9.00*  |        |        | N/A             | Visual C++ 9.0
|      |              | Visual Studio 2010     | 2010 | ?       | ?       |        | N/A    | N/A             | Visual C++ 10.0
|      |              | Visual Studio 2012     | 2012 | ?       | ?       |        | N/A    | N/A             | Visual C++ 11.0
|      |              | Visual Studio 2013     | 2013 | ?       | ?       |        | N/A    | N/A             | Visual C++ 12.0
|      |              | Visual Studio 2015     | 2015 | 19.00   | *14.00* |        | N/A    | N/A             | Visual C++ 14.0

Symbols sometimes refer to CodeView versions: C6/C7/C11/C13. C11 is VC5.x era, C13 is VC7.x era. C6 is "pre" C7; there is no explicit signature for C6. Note that C13 does not begin immediately with C7. ImpvVC70 can be C11

Packing refers to running CVPACK on the file. Some linkers run CVPACK implicitly and/or automatically

Microsoft C 4.0 (1986) has cl 4.00 and LINK 3.51 but curiously does not seem to generate NB00 (either with LINK or LINK4). It generates files with a DNRB signature at the end. CV.EXE can still debug them. Unlike regular OMF files, the DNRB signature does not repeat itself when you jump back to the end of the normal data in the file

QuickC 2.51 has ILINK 1.21 and QLINK 4.10

ilink in C 6.0 still produces NB02

Executables using OMF contain debug information at the end of the file: https://openwatcom.org/ftp/devel/docs/CodeView.pdf