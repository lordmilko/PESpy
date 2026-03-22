using System.Collections.Generic;

namespace PESpy.Tests
{
    public static class WellKnownTestModule
    {
        public static SymStoreKey aadauthhelper = new SymStoreKey("aadauthhelper.dll/29F887D25b000/aadauthhelper.dll", SymStoreKeyKind.PE);
        public static SymStoreKey AuthExt = new SymStoreKey("authext.dll/350DD84419000/authext.dll", SymStoreKeyKind.PE);
        public static SymStoreKey AzureAttest = new SymStoreKey("azureattest.dll/6250867848000/azureattest.dll", SymStoreKeyKind.PE);
        public static SymStoreKey cdd = new SymStoreKey("cdd.dll/76CEFE3B47000/cdd.dll", SymStoreKeyKind.PE);
        public static SymStoreKey coreclr = new SymStoreKey("coreclr.dll/674F3D104e7000/coreclr.dll", SymStoreKeyKind.PE);
        public static SymStoreKey crtdll = new SymStoreKey("crtdll.dll/30C91E2D27000/crtdll.dll", SymStoreKeyKind.PE);
        public static SymStoreKey ctl3d32 = new SymStoreKey("ctl3d32.dll/3005CC7211000/ctl3d32.dll", SymStoreKeyKind.PE);
        public static SymStoreKey DbgEng = new SymStoreKey("dbgeng.dll/2338A182626000/dbgeng.dll", SymStoreKeyKind.PE);
        public static SymStoreKey kdstub = new SymStoreKey("kdstub.dll/0EA20E6D13000/kdstub.dll", SymStoreKeyKind.PE);
        public static SymStoreKey kd_02_15b3 = new SymStoreKey("kd_02_15b3.dll/972BFA0319000/kd_02_15b3.dll", SymStoreKeyKind.PE);
        public static SymStoreKey kernel32 = new SymStoreKey("kernel32.dll/68379A20c4000/kernel32.dll", SymStoreKeyKind.PE);
        public static SymStoreKey notepad = new SymStoreKey("notepad.exe/A8673AF85a000/notepad.exe", SymStoreKeyKind.PE);
        public static SymStoreKey ntdll = new SymStoreKey("ntdll.dll/BCED4B82217000/ntdll.dll", SymStoreKeyKind.PE);
        public static SymStoreKey ntdllWin7 = new SymStoreKey("ntdll.dll/4CE7B96E13c000/ntdll.dll", SymStoreKeyKind.PE); //Has OMAP PDB
        public static SymStoreKey ntoskrnl = new SymStoreKey("ntoskrnl.exe/2C33C5081047000/ntoskrnl.exe", SymStoreKeyKind.PE);
        public static SymStoreKey mfc40 = new SymStoreKey("mfc40.dll/31E55C32e7000/mfc40.dll", SymStoreKeyKind.PE); //The 64-bit one also has ImageDebugMisc
        public static SymStoreKey mfc40u = new SymStoreKey("mfc40u.dll/3B7DFE9Ae9000/mfc40u.dll", SymStoreKeyKind.PE);
        public static SymStoreKey mscorlib = new SymStoreKey("mscorlib.dll/66D13820576000/mscorlib.dll", SymStoreKeyKind.PE);
        public static SymStoreKey SharedLibraryPDB = new SymStoreKey("SharedLibrary.pdb/13C8DCDB8CDE4CE1BB43877B7F8A40341/SharedLibrary.pdb", SymStoreKeyKind.PDB);
        public static SymStoreKey ShoulderTapView = new SymStoreKey("shouldertapview.dll/1456661987000/shouldertapview.dll", SymStoreKeyKind.PE);
        public static SymStoreKey _7z = new SymStoreKey("7z.dll/61C875601ad000/7z.dll", SymStoreKeyKind.PE);

        //This module is notable in that it provides both PDBv7 and Portable PDB symbols on the symbol server, depending on whether you
        //use the age 1 or -1
        public static SymStoreKey WinForms = new SymStoreKey("System.Windows.Forms.dll/AF8023C9d20000/System.Windows.Forms.dll", SymStoreKeyKind.PE);
        public static SymStoreKey WinFormsFullPDB = new SymStoreKey("System.Windows.Forms.pdb/1A553F89CEB44D2B91047C5262E602571/System.Windows.Forms.pdb", SymStoreKeyKind.PDB);
        public static SymStoreKey WinFormsPortablePDB = new SymStoreKey("System.Windows.Forms.pdb/1A553F89CEB44D2B91047C5262E60257FFFFFFFF/System.Windows.Forms.pdb", SymStoreKeyKind.PortablePDB);

        public static IEnumerable<SymStoreKey> EnumerateKeys()
        {
            var fields = typeof(WellKnownTestModule).GetFields();

            foreach (var field in fields)
            {
                yield return (SymStoreKey) field.GetValue(null);
            }
        }
    }
}
