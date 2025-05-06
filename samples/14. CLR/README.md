## CLR Test Files

### NGEN

These are the steps that I took; it's possible you don't need to use .NET Framework. When I tried to use an EXE previously ngen got upset about it

1. Create a new Class Library (.NET Framework) at `C:\TestLib`
2. Compile for Debug
3. Open a command prompt and do

```
cd C:\windows\Microsoft.NET\Framework64\v4.0.30319
ngen install C:\TestLib\TestLib\bin\Release\TestLib.dll
ngen display C:\TestLib\TestLib\bin\Release\TestLib.dll
```

it should hopefully say that the DLL was successfully installed

Open `C:\windows\assembly\NativeImages_v4.0.30319_64\TestLib\<random>\` and copy the path to `TestLib.ni.dll`

The NGEN'd image will have two RSDS `IMAGE_DEBUG_TYPE_CODEVIEW` entries in it
* One for `TestLib.ni.pdb`
* One pointing to the original PDB path

Despite the fact a GUID has been generated, `TestLib.ni.pdb` does not actually get automatically created. It must be manually created using `ngen createPDB`

`ngen createPDB` has two modes of operation
1. `ngen createPDB <pathToNativeImage> <directoryToStorePDB>`
2. `ngen createPDB <pathToNativeImage> <directoryToStorePDB> /lines <originalPDBDirectory>`

When `/lines <originalPDBDirectory` is specified, the only meaningful difference in the resulting PDB has file checksums and a list of the original input files. If `/lines` is not specified, there is a single "unknown" file (which is also present when you *do* specify `/lines`)

The PDB that is created by NGEN has a GUID that matches the original GUID that was listed in the `TestLib.ni.pdb` `IMAGE_DEBUG_TYPE_CODEVIEW` entry, so it would seem that NGEN is somehow telling the PDB what GUID to use.

### R2R

```
cd \
mkdir TestApp
cd TestApp
dotnet new console
dotnet publish -c Release -r win-x64 -p:PublishReadyToRun=true
```
Output files are in `C:\TestApp\bin\Release\net9.0\win-x64\publish`
* `TestApp.exe` is a single file host
* `TestApp.dll` is our actual R2R library that gets executed by the single file host