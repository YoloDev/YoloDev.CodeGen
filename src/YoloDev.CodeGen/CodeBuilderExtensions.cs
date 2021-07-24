using System;

namespace YoloDev.CodeGen
{
    public static class CodeBuilderExtensions
    {
        readonly static Action<CodeBuilder> _endSingleIndent = EndSingleIndent;
        readonly static Action<CodeBuilder> _endBlock = EndBlock;
        readonly static Action<CodeBuilder> _endBlockNoNewline = EndBlockNoNewline;

        public static CodeScope Indent(this CodeBuilder builder)
        {
            builder.IncreaseIndent();
            return new(builder, _endSingleIndent);
        }

        public static CodeScope Block(this CodeBuilder builder, bool newlineAfter = true)
        {
            builder.Append('{')
                .AppendLine()
                .IncreaseIndent();

            return new CodeScope(builder, newlineAfter ? _endBlock : _endBlockNoNewline);
        }

        public static CodeScope Block(this CodeBuilder builder, string text, bool newlineAfter = true)
            => builder.Append(text).AppendLine().Block(newlineAfter);

        public static CodeScope BlockFormat(this CodeBuilder builder, string format, object? arg0)
            => builder.AppendFormat(format, arg0).AppendLine().Block();

        public static CodeScope BlockFormat(this CodeBuilder builder, string format, object? arg0, object? arg1)
            => builder.AppendFormat(format, arg0, arg1).AppendLine().Block();

        public static CodeScope BlockFormat(this CodeBuilder builder, string format, object? arg0, object? arg1, object? arg2)
            => builder.AppendFormat(format, arg0, arg1, arg2).AppendLine().Block();

        public static CodeScope BlockFormat(this CodeBuilder builder, string format, params object?[] args)
            => builder.AppendFormat(format, args).AppendLine().Block();

        public static CodeScope BlockFormat(this CodeBuilder builder, IFormatProvider? provider, string format, object? arg0)
            => builder.AppendFormat(provider, format, arg0).AppendLine().Block();

        public static CodeScope BlockFormat(this CodeBuilder builder, IFormatProvider? provider, string format, object? arg0, object? arg1)
            => builder.AppendFormat(provider, format, arg0, arg1).AppendLine().Block();

        public static CodeScope BlockFormat(this CodeBuilder builder, IFormatProvider? provider, string format, object? arg0, object? arg1, object? arg2)
            => builder.AppendFormat(provider, format, arg0, arg1, arg2).AppendLine().Block();

        public static CodeScope BlockFormat(this CodeBuilder builder, IFormatProvider? provider, string format, params object?[] args)
            => builder.AppendFormat(provider, format, args).AppendLine().Block();

        public static CodeScope BlockFormatted(this CodeBuilder builder, FormattableString formattedString)
            => builder.AppendFormatted(formattedString).AppendLine().Block();

        private static void EndSingleIndent(CodeBuilder builder)
            => builder.DecreaseIndent();

        private static void EndBlock(CodeBuilder builder)
        {
            builder.DecreaseIndent()
                .Append('}')
                .AppendLine();
        }

        private static void EndBlockNoNewline(CodeBuilder builder)
        {
            builder.DecreaseIndent()
                .Append('}');
        }

        public ref struct CodeScope
        {
            Action<CodeBuilder>? _dispose;
            CodeBuilder? _builder;

            public CodeScope(CodeBuilder builder, Action<CodeBuilder> dispose)
            {
                _builder = builder;
                _dispose = dispose;
            }

            public void Dispose()
            {
                if (_dispose is { } dispose
                    && _builder is { } builder)
                {
                    _dispose(builder);
                }

                _dispose = null;
                _builder = null;
            }
        }
    }
}
