namespace PESpy.IL
{
    /* In the past, I was of the opinion that I wanted to try and use the native CLR types as much as possible,
     * which meant using System.Reflection.Emit.OpCode, despite the fact it wasn't very good. One of the biggest
     * issues with the OpCode class is that there isn't an easy to use enum type that can be used to switch on opcode
     * kinds. Internally, it has an "OpCodeValues" enum, which is a terrible name ("OpCodeKind" is a better fit) however
     * here we've opted to follow the enum found in dotnet/runtime's metadata typesystem object model */

    /// <summary>
    /// An enumeration of all of the operation codes that are used in the CLI Common Intermediate Language.
    /// </summary>
    public enum ILOpcode
    {
        #region III.3: Base Instructions

        /// <summary>
        /// Add two values, returning a new value<para/>
        /// III.3.1: add numeric values<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        add = 0x58,

        /// <summary>
        /// Add signed integer values with overflow check<para/>
        /// III.3.2: add integer values with overflow check<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        add_ovf = 0xd6,

        /// <summary>
        /// Add unsigned integer values with overflow check
        /// </summary>
        add_ovf_un = 0xd7,

        /// <summary>
        /// Bitwise AND of two integral values, returns an integral value<para/>
        /// III.3.3: bitwise AND<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        and = 0x5f,

        /// <summary>
        /// Return argument list handle for the current method<para/>
        /// III.3.4: get argument list<para/>
        /// ... -> ..., argListHandle
        /// </summary>
        arglist = 0x100,

        /// <summary>
        /// Branch to "target" (int32) if equal<para/>
        /// III.3.5: branch on equal<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        beq = 0x3b,

        /// <summary>
        /// Branch to "target" (int8) if equal, short form<para/>
        /// III.3.5: branch on equal<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        beq_s = 0x2e,

        /// <summary>
        /// Branch to "target" (int32) if greater than or equal to<para/>
        /// III.3.6: branch on greater than or equal to<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bge = 0x3c,

        /// <summary>
        /// Branch to "target" (int8) if greater than or equal to, short form<para/>
        /// III.3.6: branch on greater than or equal to<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bge_s = 0x2f,

        /// <summary>
        /// Branch to "target" (int32) if greater than or equal to (unsigned or unordered)<para/>
        /// III.3.7: branch on greater than or equal to, unsigned or ordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bge_un = 0x41,

        /// <summary>
        /// Branch to "target" (int8) if greater than or equal to (unsigned or unordered), short form<para/>
        /// III.3.7: branch on greater than or equal to, unsigned or ordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bge_un_s = 0x34,

        /// <summary>
        /// Branch to "target" (int32) if greater than<para/>
        /// III.3.8: branch on greater than<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bgt = 0x3d,

        /// <summary>
        /// Branch to "target" (int8) if greater than, short form<para/>
        /// III.3.8: branch on greater than<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bgt_s = 0x30,

        /// <summary>
        /// Branch to "target" (int32) if greater than (unsigned or unordered)<para/>
        /// III.3.9: branch on greater than, unsigned or unordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bgt_un = 0x42,

        /// <summary>
        /// Branch to "target" (int8) if greater than (unsigned or unordered), short form<para/>
        /// III.3.9: branch on greater than, unsigned or unordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bgt_un_s = 0x35,

        /// <summary>
        /// Branch to "target" (int32) if less than or equal to<para/>
        /// III.3.10: branch on less than or equal to<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        ble = 0x3e,

        /// <summary>
        /// Branch to "target" (int8) if less than or equal to, short form<para/>
        /// III.3.10: branch on less than or equal to<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        ble_s = 0x31,

        /// <summary>
        /// Branch to "target" (int32) if less than or equal to (unsigned or unordered)<para/>
        /// III.3.11: branch on less than or equal to, unsigned or unordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        ble_un = 0x43,

        /// <summary>
        /// Branch to "target" (int8) if less than or equal to (unsigned or unordered), short form<para/>
        /// III.3.11: branch on less than or equal to, unsigned or unordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        ble_un_s = 0x36,

        /// <summary>
        /// Branch to "target" (int32) if less than<para/>
        /// III.3.12: branch on less than<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        blt = 0x3f,

        /// <summary>
        /// Branch to "target" (int8) if less than, short form<para/>
        /// III.3.12: branch on less than<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        blt_s = 0x32,

        /// <summary>
        /// Branch to "target" (int32) if less than (unsigned or unordered)<para/>
        /// III.3.13: branch on less than, unsigned or unordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        blt_un = 0x44,

        /// <summary>
        /// Branch to "target" (int8) if less than (unsigned or unordered), short form<para/>
        /// III.3.13: branch on less than, unsigned or unordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        blt_un_s = 0x37,

        /// <summary>
        /// Branch to "target" (int32) if unequal or unordered<para/>
        /// III.3.14: branch on not equal or unordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bne_un = 0x40,

        /// <summary>
        /// Branch to "target" (int8) if unequal or unordered, short form<para/>
        /// III.3.14: branch on not equal or unordered<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        bne_un_s = 0x33,

        /// <summary>
        /// Branch to "target" (int32)<para/>
        /// III.3.15: unconditional branch<para/>
        /// ... -> ...
        /// </summary>
        br = 0x38,

        /// <summary>
        /// Branch to "target" (int8), short form<para/>
        /// III.3.15: unconditional branch<para/>
        /// ... -> ...
        /// </summary>
        br_s = 0x2b,
        break_ = 0x01,

        /// <summary>
        /// Branch to "target" (int32) if "value" is zero (false/null)<para/>
        /// III.3.17: branch on false, null, or zero<para/>
        /// ..., value -> ...
        /// </summary>
        brfalse = 0x39,

        /// <summary>
        /// Branch to "target" (int8) if "value" is zero (false/null), short form<para/>
        /// III.3.17: branch on false, null, or zero<para/>
        /// ..., value -> ...
        /// </summary>
        brfalse_s = 0x2c,

        /// <summary>
        /// Branch to "target" (int32) if "value" is non-zero (true/non-null)<para/>
        /// III.3.18: branch on non-false or non-null<para/>
        /// ..., value -> ...
        /// </summary>
        brtrue = 0x3a,

        /// <summary>
        /// Branch to "target" (int8) if "value" is non-zero (true/non-null), short form<para/>
        /// III.3.18: branch on non-false or non-null<para/>
        /// ..., value -> ...
        /// </summary>
        brtrue_s = 0x2d,

        /// <summary>
        /// Call method described by "method"<para/>
        /// III.3.19: call a method<para/>
        /// arg0, arg1 ... argN -> ..., retVal (not always returned)
        /// </summary>
        call = 0x28,

        /// <summary>
        /// Call method indicated on the stack with arguments described by "callsitedescr"<para/>
        /// III.3.20: indirect method call<para/>
        /// ..., arg0, arg1, argN, ftn -> ..., retVal (not always returned)
        /// </summary>
        calli = 0x29,

        /// <summary>
        /// Push 1 (of type int32) if "value1" equals "value2", else push 0<para/>
        /// III.3.21: compare equal<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        ceq = 0x101,

        /// <summary>
        /// Push 1 (of type int32) if "value1" > "value2", else push 0<para/>
        /// III.3.22: compare greater than<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        cgt = 0x102,

        /// <summary>
        /// Push 1 (of type int32) if "value1" > "value2", unsigned or unordered, else push 0<para/>
        /// III.3.23: compare greater than, unsigned or unordered<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        cgt_un = 0x103,

        /// <summary>
        /// Throw ArithmeticException if "value" is not a finite number<para/>
        /// III.3.24: check for a finite real number<para/>
        /// ..., value1 -> ..., value
        /// </summary>
        ckfinite = 0xc3,

        /// <summary>
        /// Push 1 (of type int32) if "value1" &lt; "value2", else push 0<para/>
        /// III.3.25: compare less than<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        clt = 0x104,

        /// <summary>
        /// Push 1 (of type int32) if "value1" &lt; "value2", unsigned or unordered, else push 0<para/>
        /// III.3.26: compare less than, unsigned or unordered<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        clt_un = 0x105,

        #region III.3.27: Conv

        /// <summary>
        /// Convert to int8, pushing int32 on stack<para/>
        /// III.3.27: data conversion<para/>
        /// ..., value -> ..., result
        /// </summary>
        conv_i1 = 0x67,

        /// <summary>
        /// Convert to int16, pushing int32 on stack
        /// </summary>
        conv_i2 = 0x68,

        /// <summary>
        /// Convert to int32, pushing int32 on stack
        /// </summary>
        conv_i4 = 0x69,

        /// <summary>
        /// Convert to int64, pushing int64 on stack
        /// </summary>
        conv_i8 = 0x6a,

        /// <summary>
        /// Convert to float32, pushing F on stack
        /// </summary>
        conv_r4 = 0x6b,

        /// <summary>
        /// Convert to float64, pushing F on stack
        /// </summary>
        conv_r8 = 0x6c,

        /// <summary>
        /// Convert to unsigned int8, pushing int32 on stack
        /// </summary>
        conv_u1 = 0xd2,

        /// <summary>
        /// Convert to unsigned int16, pushing int32 on stack
        /// </summary>
        conv_u2 = 0xd1,

        /// <summary>
        /// Convert to unsigned int32, pushing int32 on stack
        /// </summary>
        conv_u4 = 0x6d,

        /// <summary>
        /// Convert to unsigned int64, pushing int64 on stack
        /// </summary>
        conv_u8 = 0x6e,

        /// <summary>
        /// Convert to native int, pushing native int on stack
        /// </summary>
        conv_i = 0xd3,

        /// <summary>
        /// Convert to native unsigned int, pushing native int on stack
        /// </summary>
        conv_u = 0xe0,

        /// <summary>
        /// Convert unsigned integer to floating-point, pushing F on stack
        /// </summary>
        conv_r_un = 0x76,

        #endregion
        #region III.3.28: Conv_Ovf

        /// <summary>
        /// Convert to an int8 (on the stack as int32) and throw an exception on overflow<para/>
        /// III.3.28: data conversion with overflow detection<para/>
        /// ..., value -> ..., result
        /// </summary>
        conv_ovf_i1 = 0xb3,

        /// <summary>
        /// Convert to an int16 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_i2 = 0xb5,

        /// <summary>
        /// Convert to an int32 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_i4 = 0xb7,

        /// <summary>
        /// Convert to an int64 (on the stack as int64) and throw an exception on overflow
        /// </summary>
        conv_ovf_i8 = 0xb9,

        /// <summary>
        /// Convert to an unsigned int8 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_u1 = 0xb4,

        /// <summary>
        /// Convert to an unsigned int16 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_u2 = 0xb6,

        /// <summary>
        /// Convert to an unsigned int32 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_u4 = 0xb8,

        /// <summary>
        /// Convert to an unsigned int64 (on the stack as int64) and throw an exception on overflow
        /// </summary>
        conv_ovf_u8 = 0xba,

        /// <summary>
        /// Convert to a native int (on the stack as native int) and throw an exception on overflow
        /// </summary>
        conv_ovf_i = 0xd4,

        /// <summary>
        /// Convert to a native unsigned int (on the stack as native int) and throw an exception on overflow
        /// </summary>
        conv_ovf_u = 0xd5,

        #endregion
        #region III.3.29: Conv_Ovf_Un

        /// <summary>
        /// Convert unsigned to an int8 (on the stack as int32) and throw an exception on overflow.<para/>
        /// III.3.29: unsigned data conversion with overflow detection<para/>
        /// ..., value -> ..., result
        /// </summary>
        conv_ovf_i1_un = 0x82,

        /// <summary>
        /// Convert unsigned to an int16 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_i2_un = 0x83,

        /// <summary>
        /// Convert unsigned to an int32 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_i4_un = 0x84,

        /// <summary>
        /// Convert unsigned to an int64 (on the stack as int64) and throw an exception on overflow
        /// </summary>
        conv_ovf_i8_un = 0x85,

        /// <summary>
        /// Convert unsigned to an unsigned int8 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_u1_un = 0x86,

        /// <summary>
        /// Convert unsigned to an unsigned int16 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_u2_un = 0x87,

        /// <summary>
        /// Convert unsigned to an unsigned int32 (on the stack as int32) and throw an exception on overflow
        /// </summary>
        conv_ovf_u4_un = 0x88,

        /// <summary>
        /// Convert unsigned to an unsigned int64 (on the stack as int64) and throw an exception on overflow
        /// </summary>
        conv_ovf_u8_un = 0x89,

        /// <summary>
        /// Convert unsigned to a native int (on the stack as native int) and throw an exception on overflow
        /// </summary>
        conv_ovf_i_un = 0x8a,

        /// <summary>
        /// Convert unsigned to a native unsigned int (on the stack as native int) and throw an exception on overflow
        /// </summary>
        conv_ovf_u_un = 0x8b,

        #endregion

        /// <summary>
        /// Copy data from memory to memory<para/>
        /// III.3.30: copy data from memory to memory<para/>
        /// ..., destaddr, srcaddr, size -> ...
        /// </summary>
        cpblk = 0x117,

        /// <summary>
        /// Divide two values to return a quotient or floating-point result<para/>
        /// III.3.31: divide values<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        div = 0x5b,

        /// <summary>
        /// Divide two values, unsigned, returning a quotient<para/>
        /// III.3.32: divide integer values, unsigned<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        div_un = 0x5c,

        /// <summary>
        /// Duplicate the value on the top of the stack<para/>
        /// III.3.33: duplicate the top value of the stack<para/>
        /// ..., value -> ..., value, value
        /// </summary>
        dup = 0x25,

        /// <summary>
        /// End an exception handling filter clause<para/>
        /// III.3.34: end exception handling filter clause<para/>
        /// ..., value -> ...
        /// </summary>
        endfilter = 0x111,

        /// <summary>
        /// End finally clause of an exception block<para/>
        /// III.3.35: end the finally or fault clause of an exception block<para/>
        /// ... -> ...
        /// </summary>
        endfinally = 0xdc,

        /// <summary>
        /// Set all bytes in a block of memory to a given byte value<para/>
        /// III.3.36: initialize a block of memory to a value<para/>
        /// ..., addr, value, size -> ...
        /// </summary>
        initblk = 0x118,

        /// <summary>
        /// Exit current method and jump to the specified method<para/>
        /// III.3.37: jump to method<para/>
        /// ... -> ...
        /// </summary>
        jmp = 0x27,

        #region III.3.38: Ldarg

        /// <summary>
        /// Load argument numbered "num" (int16) onto the stack<para/>
        /// III.3.38: load argument onto the stack<para/>
        /// ... -> ..., value
        /// </summary>
        ldarg = 0x109,

        /// <summary>
        /// Load argument numbered "num" (int8) onto the stack, short form<para/>
        /// III.3.38: load argument onto the stack<para/>
        /// ... -> ..., value
        /// </summary>
        ldarg_s = 0x0e,

        /// <summary>
        /// Load argument 0 onto the stack
        /// </summary>
        ldarg_0 = 0x02,

        /// <summary>
        /// Load argument 1 onto the stack
        /// </summary>
        ldarg_1 = 0x03,

        /// <summary>
        /// Load argument 2 onto the stack
        /// </summary>
        ldarg_2 = 0x04,

        /// <summary>
        /// Load argument 3 onto the stack
        /// </summary>
        ldarg_3 = 0x05,

        #endregion

        /// <summary>
        /// Fetch the address of argument "argNum" (int16)<para/>
        /// III.3.39: load an argument address<para/>
        /// ... -> ..., address of argument number argNum
        /// </summary>
        ldarga = 0x10a,

        /// <summary>
        /// Fetch the address of argument "argNum" (int8), short form<para/>
        /// III.3.39: load an argument address<para/>
        /// ... -> ..., address of argument number argNum
        /// </summary>
        ldarga_s = 0x0f,

        #region III.3.40: Ldc

        /// <summary>
        /// Push "num" (int32) of type int32 onto the stack as int32<para/>
        /// III.3.40: load numeric constant<para/>
        /// ... -> ..., num
        /// </summary>
        ldc_i4 = 0x20,

        /// <summary>
        /// Push "num" (int64) of type int64 onto the stack as int64
        /// </summary>
        ldc_i8 = 0x21,

        /// <summary>
        /// Push "num" (float32) of type float32 onto the stack as F
        /// </summary>
        ldc_r4 = 0x22,

        /// <summary>
        /// Push "num" (float64) of type float64 onto the stack as F
        /// </summary>
        ldc_r8 = 0x23,

        /// <summary>
        /// Push 0 onto the stack as int32
        /// </summary>
        ldc_i4_0 = 0x16,

        /// <summary>
        /// Push 1 onto the stack as int32
        /// </summary>
        ldc_i4_1 = 0x17,

        /// <summary>
        /// Push 2 onto the stack as int32
        /// </summary>
        ldc_i4_2 = 0x18,

        /// <summary>
        /// Push 3 onto the stack as int32
        /// </summary>
        ldc_i4_3 = 0x19,

        /// <summary>
        /// Push 4 onto the stack as int32
        /// </summary>
        ldc_i4_4 = 0x1a,

        /// <summary>
        /// Push 5 onto the stack as int32
        /// </summary>
        ldc_i4_5 = 0x1b,

        /// <summary>
        /// Push 6 onto the stack as int32
        /// </summary>
        ldc_i4_6 = 0x1c,

        /// <summary>
        /// Push 7 onto the stack as int32
        /// </summary>
        ldc_i4_7 = 0x1d,

        /// <summary>
        /// Push 8 onto the stack as int32
        /// </summary>
        ldc_i4_8 = 0x1e,

        /// <summary>
        /// Push -1 onto the stack as int32
        /// </summary>
        ldc_i4_m1 = 0x15,

        /// <summary>
        /// Push "num" (int8) onto the stack as int32, short form
        /// </summary>
        ldc_i4_s = 0x1f,

        #endregion

        /// <summary>
        /// Push a pointer to a method referenced by "method", on the stack<para/>
        /// III.3.41: load method pointer<para/>
        /// ... -> ..., ftn
        /// </summary>
        ldftn = 0x106,

        /// <summary>
        /// Indirect load value of type int8 as int32 on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_i1 = 0x46,

        /// <summary>
        /// Indirect load value of type unsigned int8 as int32 on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_u1 = 0x47,

        /// <summary>
        /// Indirect load value of type int16 as int32 on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_i2 = 0x48,

        /// <summary>
        /// Indirect load value of type unsigned int16 as int32 on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_u2 = 0x49,

        /// <summary>
        /// Indirect load value of type int32 as int32 on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_i4 = 0x4a,

        /// <summary>
        /// Indirect load value of type unsigned int32 as int32 on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_u4 = 0x4b,

        /// <summary>
        /// Indirect load value of type int64 as int64 on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_i8 = 0x4c,

        /// <summary>
        /// Indirect load value of type native int as native int on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_i = 0x4d,

        /// <summary>
        /// Indirect load value of type float32 as F on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_r4 = 0x4e,

        /// <summary>
        /// Indirect load value of type float64 as F on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_r8 = 0x4f,

        /// <summary>
        /// Indirect load value of type object ref as O on the stack<para/>
        /// III.3.42: load value indirect onto the stack<para/>
        /// ..., addr -> ..., value
        /// </summary>
        ldind_ref = 0x50,

        #region III.3.43: Ldloc

        /// <summary>
        /// Load local variable of index "indx" (unsigned int16) onto stack<para/>
        /// III.3.43: load local variable onto the stack<para/>
        /// ... -> ..., value
        /// </summary>
        ldloc = 0x10c,

        /// <summary>
        /// Load local variable of index "indx" (unsigned int8) onto stack, short form
        /// </summary>
        ldloc_s = 0x11,

        /// <summary>
        /// Load local variable 0 onto stack
        /// </summary>
        ldloc_0 = 0x06,

        /// <summary>
        /// Load local variable 1 onto stack
        /// </summary>
        ldloc_1 = 0x07,

        /// <summary>
        /// Load local variable 2 onto stack
        /// </summary>
        ldloc_2 = 0x08,

        /// <summary>
        /// Load local variable 3 onto stack
        /// </summary>
        ldloc_3 = 0x09,

        #endregion

        /// <summary>
        /// Load address of local variable with index "indx" (unsigned int16)<para/>
        /// III.3.44: load local variable address<para/>
        /// ... -> ..., address
        /// </summary>
        ldloca = 0x10d,

        /// <summary>
        /// Load address of local variable with index "indx" (unsigned int8), short form<para/>
        /// III.3.44: load local variable address<para/>
        /// ... -> ..., address
        /// </summary>
        ldloca_s = 0x12,

        /// <summary>
        /// Push a null reference on the stack<para/>
        /// III.3.45: load a null pointer<para/>
        /// ... -> ..., null value
        /// </summary>
        ldnull = 0x14,

        /// <summary>
        /// Exit a protected region of code ("target": int32)<para/>
        /// III.3.46:  exit a protected region of code<para/>
        /// ... ->
        /// </summary>
        leave = 0xdd,

        /// <summary>
        /// Exit a protected region of code, short form ("target": int8)<para/>
        /// III.3.46:  exit a protected region of code<para/>
        /// ... ->
        /// </summary>
        leave_s = 0xde,

        /// <summary>
        /// Allocate space from the local memory pool<para/>
        /// III.3.47: allocate space in the local dynamic memory pool<para/>
        /// size -> address
        /// </summary>
        localloc = 0x10f,

        /// <summary>
        /// Multiply values<para/>
        /// III.3.48: multiply values<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        mul = 0x5a,

        /// <summary>
        /// Multiply signed integer values. Signed result shall fit in same size<para/>
        /// III.3.49: multiply integer values with overflow check<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        mul_ovf = 0xd8,

        /// <summary>
        /// Multiply unsigned integer values. Unsigned result shall fit in same size<para/>
        /// III.3.49: multiply integer values with overflow check<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        mul_ovf_un = 0xd9,

        /// <summary>
        /// Negate value<para/>
        /// III.3.50: negate<para/>
        /// ..., value -> ..., result
        /// </summary>
        neg = 0x65,

        /// <summary>
        /// Do nothing<para/>
        /// III.3.51: no operation<para/>
        /// ... -> ...
        /// </summary>
        nop = 0x00,

        /// <summary>
        /// Bitwise complement<para/>
        /// III.3.52: bitwise complement<para/>
        /// ..., value -> ..., result
        /// </summary>
        not = 0x66,

        /// <summary>
        /// Bitwise OR of two integer values, returns an integer<para/>
        /// III.3.53: bitwise OR<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        or = 0x60,

        /// <summary>
        /// Pop "value" from the stack<para/>
        /// III.3.54: remove the top element of the stack<para/>
        /// ..., value -> ...
        /// </summary>
        pop = 0x26,

        /// <summary>
        /// Remainder when dividing one value by another<para/>
        /// III.3.55: compute remainder<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        rem = 0x5d,

        /// <summary>
        /// Remainder when dividing one unsigned value by another<para/>
        /// III.3.56: compute integer remainder, unsigned<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        rem_un = 0x5e,

        /// <summary>
        /// Return from method, possibly with a value<para/>
        /// III.3.57: return from method<para/>
        /// retVal on callee evaluation stack (not always present) -> ..., retVal on caller evaluation stack (not always present)
        /// </summary>
        ret = 0x2a,

        /// <summary>
        /// Shift an integer left (shifting in zeros), return an integer<para/>
        /// III.3.58: shift integer left<para/>
        /// ..., value, shiftAmount -> ..., result
        /// </summary>
        shl = 0x62,

        /// <summary>
        /// Shift an integer right (shift in sign), return an integer<para/>
        /// III.3.59: shift integer right<para/>
        /// ..., value, shiftAmount -> ..., result
        /// </summary>
        shr = 0x63,

        /// <summary>
        /// Shift an integer right (shift in zero), return an integer<para/>
        /// III.3.60: shift integer right, unsigned<para/>
        /// ..., value, shiftAmount -> ..., result
        /// </summary>
        shr_un = 0x64,

        /// <summary>
        /// Store "value" (unsigned int16) to the argument numbered num<para/>
        /// III.3.61: store a value in an argument slot<para/>
        /// ..., value -> ...
        /// </summary>
        starg = 0x10b,

        /// <summary>
        /// Store "value" (unsigned int8) to the argument numbered num, short form<para/>
        /// III.3.61: store a value in an argument slot<para/>
        /// ..., value -> ...
        /// </summary>
        starg_s = 0x10,

        /// <summary>
        /// Store value of type int8 into memory at address<para/>
        /// III.3.62: store value indirect from stack<para/>
        /// ..., addr, val -> ...
        /// </summary>
        stind_i1 = 0x52,

        /// <summary>
        /// Store value of type int16 into memory at address<para/>
        /// III.3.62: store value indirect from stack<para/>
        /// ..., addr, val -> ...
        /// </summary>
        stind_i2 = 0x53,

        /// <summary>
        /// Store value of type int32 into memory at address<para/>
        /// III.3.62: store value indirect from stack<para/>
        /// ..., addr, val -> ...
        /// </summary>
        stind_i4 = 0x54,

        /// <summary>
        /// Store value of type int64 into memory at address<para/>
        /// III.3.62: store value indirect from stack<para/>
        /// ..., addr, val -> ...
        /// </summary>
        stind_i8 = 0x55,

        /// <summary>
        /// Store value of type float32 into memory at address<para/>
        /// III.3.62: store value indirect from stack<para/>
        /// ..., addr, val -> ...
        /// </summary>
        stind_r4 = 0x56,

        /// <summary>
        /// Store value of type float64 into memory at address<para/>
        /// III.3.62: store value indirect from stack<para/>
        /// ..., addr, val -> ...
        /// </summary>
        stind_r8 = 0x57,

        /// <summary>
        /// Store value of type native int into memory at address<para/>
        /// III.3.62: store value indirect from stack<para/>
        /// ..., addr, val -> ...
        /// </summary>
        stind_i = 0xdf,

        /// <summary>
        /// Store value of type object ref (type O) into memory at address<para/>
        /// III.3.62: store value indirect from stack<para/>
        /// ..., addr, val -> ...
        /// </summary>
        stind_ref = 0x51,

        #region III.3.63: Stloc

        /// <summary>
        /// Pop a value from stack into local variable "indx" (unsigned int16)<para/>
        /// III.3.63: pop value from stack to local variable<para/>
        /// ..., value -> ...
        /// </summary>
        stloc = 0x10e,

        /// <summary>
        /// Pop a value from stack into local variable "indx" (unsigned int8), short form
        /// </summary>
        stloc_s = 0x13,

        /// <summary>
        /// Pop a value from stack into local variable 0
        /// </summary>
        stloc_0 = 0x0a,

        /// <summary>
        /// Pop a value from stack into local variable 1
        /// </summary>
        stloc_1 = 0x0b,

        /// <summary>
        /// Pop a value from stack into local variable 2
        /// </summary>
        stloc_2 = 0x0c,

        /// <summary>
        /// Pop a value from stack into local variable 3
        /// </summary>
        stloc_3 = 0x0d,

        #endregion

        /// <summary>
        /// Subtract value2 from value1, returning a new value<para/>
        /// III.3.64: subtract numeric values<para/>
        /// ..., value1, value2 -> ...
        /// </summary>
        sub = 0x59,

        /// <summary>
        /// Subtract native int from a native int. Signed result shall fit in same size<para/>
        /// III.3.65: subtract integer values, checking for overflow<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        sub_ovf = 0xda,

        /// <summary>
        /// Subtract native unsigned int from a native unsigned int. Unsigned result shall fit in same size<para/>
        /// III.3.65: subtract integer values, checking for overflow<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        sub_ovf_un = 0xdb,
        switch_ = 0x45,

        /// <summary>
        /// Bitwise XOR of integer values, returns an integer<para/>
        /// III.3.67: bitwise XOR<para/>
        /// ..., value1, value2 -> ..., result
        /// </summary>
        xor = 0x61,

        #endregion
        #region III.4: Object Model Instructions

        /// <summary>
        /// Convert a boxable value to its boxed form<para/>
        /// III.4.1: convert a boxable value to its boxed form<para/>
        /// ..., val -> ..., obj
        /// </summary>
        box = 0x8c,

        /// <summary>
        /// Call a method associated with an object<para/>
        /// III.4.2: call a method associated, at runtime, with an object<para/>
        /// ..., obj, arg1, ..., argN -> ..., returnVal (not always returned)
        /// </summary>
        callvirt = 0x6f,

        /// <summary>
        /// Cast "obj" to "typeTok"<para/>
        /// III.4.3: cast an object to a class<para/>
        /// ..., obj -> ..., obj2
        /// </summary>
        castclass = 0x74,

        /// <summary>
        /// Copy a value type from "src" to "dest"<para/>
        /// III.4.4: copy a value from one address to anot her<para/>
        /// ..., dest, src -> ...
        /// </summary>
        cpobj = 0x70,

        /// <summary>
        /// Initialize the value at address "dest"<para/>
        /// III.4.5: initialize the value at an address<para/>
        /// ..., dest -> ...
        /// </summary>
        initobj = 0x115,

        /// <summary>
        /// Test if "obj" is an instance of "typeTok", returning null or an instance of that class or interface<para/>
        /// III.4.6: test if an object is an instance of a class or interface<para/>
        /// ..., obj -> ..., result
        /// </summary>
        isinst = 0x75,

        /// <summary>
        /// Load the element at "index" onto the top of the stack<para/>
        /// III.4.7: load element from array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem = 0xa3,

        /// <summary>
        /// Load the element with type int8 at "index" onto the top of the stack as an int32<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_i1 = 0x90,

        /// <summary>
        /// Load the element with type int16 at "index" onto the top of the stack as an int32<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_i2 = 0x92,

        /// <summary>
        /// Load the element with type int32 at "index" onto the top of the stack as an int32<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_i4 = 0x94,

        /// <summary>
        /// Load the element with type int64 at "index" onto the top of the stack as an int64<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_i8 = 0x96,

        /// <summary>
        /// Load the element with type unsigned int8 at "index" onto the top of the stack as an int32<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_u1 = 0x91,

        /// <summary>
        /// Load the element with type unsigned int16 at "index" onto the top of the stack as an int32<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_u2 = 0x93,

        /// <summary>
        /// Load the element with type unsigned int32 at "index" onto the top of the stack as an int32<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_u4 = 0x95,

        /// <summary>
        /// Load the element with type float32 at "index" onto the top of the stack as an F<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_r4 = 0x98,

        /// <summary>
        /// Load the element with type float64 at "index" onto the top of the stack as an F<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_r8 = 0x99,

        /// <summary>
        /// Load the element with type native int at "index" onto the top of the stack as a native int<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_i = 0x97,

        /// <summary>
        /// Load the element at "index" onto the top of the stack as an O. The type of the O is the same as the element type of the array pushed on the CIL stack<para/>
        /// III.4.8: load an element of an array<para/>
        /// ..., array, index -> ..., value
        /// </summary>
        ldelem_ref = 0x9a,

        /// <summary>
        /// Load the address of element at "index" onto the top of the stack<para/>
        /// III.4.9: load address of an element of an array<para/>
        /// ..., array, index -> ..., address
        /// </summary>
        ldelema = 0x8f,

        /// <summary>
        /// Push the value of "field" of object (or value type) "obj", onto the stack<para/>
        /// III.4.10: load field of an object<para/>
        /// ..., obj -> ..., value
        /// </summary>
        ldfld = 0x7b,

        /// <summary>
        /// Push the address of "field" of object "obj" on the stack<para/>
        /// III.4.11: load field address<para/>
        /// ..., obj -> ..., address
        /// </summary>
        ldflda = 0x7c,

        /// <summary>
        /// Push the "length" (of type native unsigned int) of array on the stack<para/>
        /// III.4.12: load the length of an array<para/>
        /// ..., array -> ..., length
        /// </summary>
        ldlen = 0x8e,

        /// <summary>
        /// Copy the value stored at address "src" to the stack<para/>
        /// III.4.13: copy a value from an address to the stack<para/>
        /// ..., src -> ..., val
        /// </summary>
        ldobj = 0x71,

        /// <summary>
        /// Push the value of "field" on the stack<para/>
        /// III.4.14: load static field of a class<para/>
        /// ... -> ..., value
        /// </summary>
        ldsfld = 0x7e,

        /// <summary>
        /// Push the address of the static field, "field", on the stack<para/>
        /// III.4.15: load static field address<para/>
        /// ... -> ..., address
        /// </summary>
        ldsflda = 0x7f,

        /// <summary>
        /// Push a string object for the literal "string"<para/>
        /// III.4.16: load a literal string<para/>
        /// ... -> ..., string
        /// </summary>
        ldstr = 0x72,

        /// <summary>
        /// Convert metadata "token" to its runtime representation<para/>
        /// III.4.17: load the runtime representation of a metadata token<para/>
        /// ... -> ..., RuntimeHandle
        /// </summary>
        ldtoken = 0xd0,

        /// <summary>
        /// Push address of virtual method "method" on the stack<para/>
        /// III.4.18: load a virtual method pointer<para/>
        /// ..., object -> ..., ftn
        /// </summary>
        ldvirtftn = 0x107,

        /// <summary>
        /// Push a typed reference to "ptr" of type "class" onto the stack<para/>
        /// III.4.19: push a typed reference on the stack<para/>
        /// ..., ptr -> ..., typedRef
        /// </summary>
        mkrefany = 0xc6,

        /// <summary>
        /// Create a new array with elements of type "etype"<para/>
        /// III.4.20: create a zero-based, one-dimensional array<para/>
        /// ..., numElems -> ..., array
        /// </summary>
        newarr = 0x8d,

        /// <summary>
        /// Allocate an uninitialized object or value type and call "ctor"<para/>
        /// III.4.21: create a new object<para/>
        /// ..., arg1, ... argN -> ..., obj
        /// </summary>
        newobj = 0x73,

        /// <summary>
        /// Push the type token stored in a typed reference<para/>
        /// III.4.22: load the type out of a typed reference<para/>
        /// ..., TypedRef -> ..., type
        /// </summary>
        refanytype = 0x11d,

        /// <summary>
        /// Push the address stored in a typed reference<para/>
        /// III.4.23: load the address out of a typed reference<para/>
        /// ..., TypedRef -> ..., address
        /// </summary>
        refanyval = 0xc2,

        /// <summary>
        /// Rethrow the current exception<para/>
        /// III.4.24: rethrow the current exception<para/>
        /// ... -> ...
        /// </summary>
        rethrow = 0x11a,
        sizeof_ = 0x11c,

        /// <summary>
        /// Replace array element at "index" with the "value" on the stack<para/>
        /// III.4.26: store element to array<para/>
        /// ..., array, index, value -> ...
        /// </summary>
        stelem = 0xa4,

        /// <summary>
        /// Replace "array" element at "index" with the int8 value on the stack<para/>
        /// III.4.27: store an element of an array<para/>
        /// ..., array, index, value -> ...,
        /// </summary>
        stelem_i1 = 0x9c,

        /// <summary>
        /// Replace "array" element at "index" with the int16 value on the stack<para/>
        /// III.4.27: store an element of an array<para/>
        /// ..., array, index, value -> ...,
        /// </summary>
        stelem_i2 = 0x9d,

        /// <summary>
        /// Replace "array" element at "index" with the int32 value on the stack<para/>
        /// III.4.27: store an element of an array<para/>
        /// ..., array, index, value -> ...,
        /// </summary>
        stelem_i4 = 0x9e,

        /// <summary>
        /// Replace "array" element at "index" with the int64 value on the stack<para/>
        /// III.4.27: store an element of an array<para/>
        /// ..., array, index, value -> ...,
        /// </summary>
        stelem_i8 = 0x9f,

        /// <summary>
        /// Replace "array" element at "index" with the float32 value on the stack<para/>
        /// III.4.27: store an element of an array<para/>
        /// ..., array, index, value -> ...,
        /// </summary>
        stelem_r4 = 0xa0,

        /// <summary>
        /// Replace "array" element at "index" with the float64 value on the stack<para/>
        /// III.4.27: store an element of an array<para/>
        /// ..., array, index, value -> ...,
        /// </summary>
        stelem_r8 = 0xa1,

        /// <summary>
        /// Replace "array" element at "index" with the native int value on the stack<para/>
        /// III.4.27: store an element of an array<para/>
        /// ..., array, index, value -> ...,
        /// </summary>
        stelem_i = 0x9b,

        /// <summary>
        /// Replace "array" element at "index" with the ref value on the stack<para/>
        /// III.4.27: store an element of an array<para/>
        /// ..., array, index, value -> ...,
        /// </summary>
        stelem_ref = 0xa2,

        /// <summary>
        /// Replace the "value" of "field" of the object "obj" with "value"<para/>
        /// III.4.28: store into a field of an object<para/>
        /// ..., obj, value -> ...,
        /// </summary>
        stfld = 0x7d,

        /// <summary>
        /// Store a value of type "typeTok" at an address<para/>
        /// III.4.29: store a value at an address<para/>
        /// ..., dest, src -> ...
        /// </summary>
        stobj = 0x81,

        /// <summary>
        /// Replace the value of "field" with "val".<para/>
        /// III.4.30: store a static field of a class<para/>
        /// ..., val -> ...
        /// </summary>
        stsfld = 0x80,
        throw_ = 0x7a,

        /// <summary>
        /// Extract a value-type from "obj", its boxed representation<para/>
        /// III.4.32: convert boxed value type to its raw form<para/>
        /// ..., obj -> ..., valueTypePtr
        /// </summary>
        unbox = 0x79,

        /// <summary>
        /// Extract a value-type from obj, its boxed representation<para/>
        /// III.4.33: convert boxed type to value<para/>
        /// ..., obj -> ..., value or obj
        /// </summary>
        unbox_any = 0xa5,

        #endregion

        prefix1 = 0xfe,
        unaligned = 0x112,
        volatile_ = 0x113,
        tail = 0x114,
        constrained = 0x116,
        no = 0x119,
        readonly_ = 0x11e
    }
}
