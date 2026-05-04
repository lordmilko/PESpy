using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using PESpy.View;
using ReFlow.FileAnalysis;

namespace PESpy.PowerShell
{
    /* Gets all exports in Windows 95 that return ERROR_CALL_NOT_IMPLEMENTED.
     * An export is considered to return ERROR_CALL_NOT_IMPLEMENTED if it ultimately calls
     * export #17 from kernel32.dll.
     * 
     * Each export within a given module points to a stub that matches the layout of the function.
     * That stub then forwards to kernel32 export #17, either via a direct call (when the export
     * is within kernel32.dll itself) or via an import (for exports outside of kernel32.dll)
     */
    [Cmdlet(VerbsCommon.Get, "Missing95Stub")]
    public class GetMissing95Stub : FileCmdlet<PEFile>
    {
        protected override void ProcessRecordEx()
        {
            //Do a sanity check as to whether this file even references ordinal #17

            //Are we kernel32.dll itself?
            var exportTable = File.ExportTable;

            var isKernel32 = false;
            ImageThunkData? importThunk = null;

            if (exportTable != null && exportTable.Name.IsValid && exportTable.Name.Value.EqualsIgnoreCase("kernel32.dll"))
            {
                isKernel32 = true;
            }
            else
            {
                //We had better be referencing kernel32.dll then

                var importTable = File.ImportTable;

                if (importTable == null)
                    return;

                foreach (var descriptor in importTable)
                {
                    if (descriptor.Name.IsValid && descriptor.Name.Value.EqualsIgnoreCase("kernel32.dll"))
                    {
                        //Check if we have an import against ordinal 17

                        if (descriptor.OriginalFirstThunk.IsValid && descriptor.FirstThunk.IsValid)
                        {
                            var ilt = descriptor.OriginalFirstThunk.Value;

                            for (var i = 0; i < ilt.Count; i++)
                            {
                                var thunk = ilt[i];

                                if (thunk.Kind == ImageThunkData.DataKind.Ordinal && thunk.Ordinal == 17)
                                {
                                    importThunk = descriptor.FirstThunk.Value[i];
                                    break;
                                }
                            }
                        }
                    }

                    if (importThunk != null)
                        break;
                }

                if (importThunk == null)
                    return;
            }

            using var accessor = FileAccessor.Create(File);

            FileAnalyzer.Analyze(accessor, IntelFileDisassembler.Instance, LocatorHttpPolicy.None);

            IList<XRef> xrefs;

            if (isKernel32)
            {
                var export17 = File.ExportTable.Exports[16];
                Debug.Assert(export17.Ordinal == 17);
                Debug.Assert(!export17.ForwardOrAddress.IsForward);

                //Get all xrefs to this export
                if (!accessor.TryGetTargetAddress(export17.ForwardOrAddress.Address, out var target17, out _))
                    throw new NotImplementedException();

                xrefs = accessor.GetXRefs(target17).ToArray().ToList();
            }
            else
            {
                //Get xrefs against the import thunk

                var localXrefs = accessor.GetXRefs(importThunk.Value.Offset);

                var results = new List<XRef>();

                foreach (var item in localXrefs)
                {
                    results.AddRange(accessor.GetXRefs(item.Other).ToArray());
                }

                xrefs = results;
            }

            //Get all stubs that call into export 17

            var stubEntities = new List<ViewEntity>(); //Informational only
            var stubAddrs = new HashSet<int>();

            foreach (var xref in xrefs)
            {
                if (xref.Kind == XRefKind.To && accessor.TryGetFunctionForAddress(xref.Other, out var exportEntity))
                {
                    stubAddrs.Add((int) exportEntity.TargetAddress);
                    stubEntities.Add(exportEntity);
                }
            }

            //Now get all exports that call into any of these stubs

            var missing = new List<ImageExportDirectory.Export>();

            foreach (var export in File.ExportTable.Exports)
            {
                if (!export.ForwardOrAddress.IsForward && accessor.TryGetTargetAddress(export.ForwardOrAddress.Address, out var target, out _))
                {
                    if (stubAddrs.Contains(target))
                    {
                        missing.Add(export);
                    }
                }
            }

            var name = System.IO.Path.GetFileName(Path);

            foreach (var item in missing)
            {
                var pso = new PSObject();
                pso.Properties.Add(new PSNoteProperty("Name", name));
                pso.Properties.Add(new PSNoteProperty("Path", Path));
                pso.Properties.Add(new PSNoteProperty("Export", item.Name.ToString()));
                WriteObject(pso);
            }
        }
    }
}
