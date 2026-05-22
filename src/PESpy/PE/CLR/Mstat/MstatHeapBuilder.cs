using System;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    internal struct MstatHeapBuilder
    {
        public MstatHeap MstatHeap;
        public ModelHeap ModelHeap;

        private mdTypeRef[] _systemTypes;

        enum SystemTypeKind
        {
            Void,
            Boolean,
            Char,
            I1,
            U1,
            I2,
            U2,
            I4,
            U4,
            I8,
            U8,
            R4,
            R8,
            String,
            Object,
            TypedByRef,
            I,
            U,

            Max
        }

        public MstatHeapBuilder(MstatHeap mstatHeap, ModelHeap modelHeap)
        {
            MstatHeap = mstatHeap;
            ModelHeap = modelHeap;

            _systemTypes = new mdTypeRef[(int) SystemTypeKind.Max];
        }

        public mdTypeRef GetSystemTypeRef(CorElementType corElementType)
        {
            //Get the typeref of a special type defined in System.Private.CoreLib

            var index = corElementType switch
            {
                <= CorElementType.String => (SystemTypeKind) corElementType,
                CorElementType.Object => SystemTypeKind.Object,
                CorElementType.TypedByRef => SystemTypeKind.TypedByRef,
                CorElementType.I => SystemTypeKind.I,
                CorElementType.U => SystemTypeKind.U
            };

            var existing = _systemTypes[(int) index];

            if (!existing.IsNil)
                return existing;

            AssemblyRefRow row = default;

            var found = false;

            //First, find the AssemblyRef that corresponds to System.Private.CoreLib
            foreach (var assemblyRef in ModelHeap.AssemblyRefTable)
            {
                if (assemblyRef.Name.GetString() == "System.Private.CoreLib"u8)
                {
                    row = assemblyRef;
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new NotImplementedException();

            //Now, find the TypeRef whose scope is the assembly ref implying it's
            //top level) and has the required name of the CorElementType

            var targetScope = ResolutionScopeTag.CreateIndex((int) row.RowIndex, TableKind.AssemblyRef);
            var targetName = corElementType switch
            {
                CorElementType.Void       => "Void"u8,
                CorElementType.Boolean    => "Boolean"u8,
                CorElementType.Char       => "Char"u8,
                CorElementType.I1         => "SByte"u8,
                CorElementType.U1         => "Byte"u8,
                CorElementType.I2         => "Int16"u8,
                CorElementType.U2         => "UInt16"u8,
                CorElementType.I4         => "Int32"u8,
                CorElementType.U4         => "UInt32"u8,
                CorElementType.I8         => "Int64"u8,
                CorElementType.U8         => "UInt64"u8,
                CorElementType.R4         => "Single"u8,
                CorElementType.R8         => "Double"u8,
                CorElementType.String     => "String"u8,
                CorElementType.Object     => "Object"u8,
                CorElementType.TypedByRef => "TypedReference"u8,
                CorElementType.I          => "IntPtr"u8,
                CorElementType.U          => "UIntPtr"u8,
            };

            foreach (var typeRef in ModelHeap.TypeRefTable)
            {
                if (typeRef.ResolutionScope != targetScope)
                    continue;

                //Compare the type name first, since that's more rare than the namespace
                if (typeRef.TypeName.GetString() != targetName || typeRef.TypeNamespace.GetString() != "System"u8)
                    continue;

                var token = (mdTypeRef) typeRef.RowIndex;

                _systemTypes[(int) index] = token;

                return token;
            }

            throw new NotImplementedException();
        }
    }
}
