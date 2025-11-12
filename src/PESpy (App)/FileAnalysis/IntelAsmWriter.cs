using Iced.Intel;
using StringBuilder = System.Text.StringBuilder;

namespace PESpy
{
    internal unsafe class ByteCodeReader : CodeReader
    {
        internal byte* _pBytes;

        public override int ReadByte() => *(_pBytes++);
    }

    internal class IntelAsmWriter : FormatterOutput
    {
        private readonly Decoder _decoder;
        private readonly ByteCodeReader _reader;
        private readonly StringBuilder _builder;
        private readonly Formatter _formatter;

        internal IntelAsmWriter(int bitness, ISymbolResolver symbolResolver)
        {
            _reader = new ByteCodeReader();
            _decoder = Decoder.Create(bitness, _reader);
            _builder = new StringBuilder();
            _formatter = new MasmFormatter(symbolResolver);
            _formatter.Options.SpaceAfterOperandSeparator = true;
        }

        internal unsafe int Write(byte* pBytes, int rva, ref ValueStringBuilder.NonRef builder)
        {
            _reader._pBytes = pBytes;
            _decoder.IP = (ulong) rva;

            var instr = _decoder.Decode();

            _formatter.Format(instr, this);
            builder.Append(_builder);
            _builder.Clear();

            return instr.Length;
        }

        public override void Write(string text, FormatterTextKind kind)
        {
            switch (kind)
                case FormatterTextKind.Register:
                case FormatterTextKind.Operator:
                case FormatterTextKind.Punctuation:
                case FormatterTextKind.Keyword:
                case FormatterTextKind.LabelAddress:
                case FormatterTextKind.FunctionAddress:
                case FormatterTextKind.Prefix:
                case FormatterTextKind.SelectorValue:
                    _builder.Append(text);
}
