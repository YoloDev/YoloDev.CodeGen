namespace YoloDev.CodeGen;

public static class CodeWriterExtensions
{
    static readonly Action<CodeWriter> EndSingleIndentAction = EndSingleIndent;
    static readonly Action<CodeWriter> EndBlockAction = EndBlock;
    static readonly Action<CodeWriter> EndBlockNoNewlineAction = EndBlockNoNewline;

    public static CodeScope Indent(this CodeWriter builder)
    {
        _ = builder.IncreaseIndent();
        return new(builder, EndSingleIndentAction);
    }

    public static CodeScope Block(this CodeWriter builder, bool newlineAfter = true)
    {
        _ = builder.Append('{')
            .AppendLine()
            .IncreaseIndent();

        return new CodeScope(builder, newlineAfter ? EndBlockAction : EndBlockNoNewlineAction);
    }

    public static CodeScope Block(this CodeWriter builder, string text, bool newlineAfter = true)
        => builder.Append(text).AppendLine().Block(newlineAfter);

    public static CodeScope Block(this CodeWriter builder, ReadOnlySpan<char> text, bool newlineAfter = true)
        => builder.Append(text).AppendLine().Block(newlineAfter);

    public static CodeScope BlockFormat(this CodeWriter builder, string format, object? arg0)
        => builder.AppendFormat(format, arg0).AppendLine().Block();

    public static CodeScope BlockFormat(this CodeWriter builder, string format, object? arg0, object? arg1)
        => builder.AppendFormat(format, arg0, arg1).AppendLine().Block();

    public static CodeScope BlockFormat(this CodeWriter builder, string format, object? arg0, object? arg1, object? arg2)
        => builder.AppendFormat(format, arg0, arg1, arg2).AppendLine().Block();

    public static CodeScope BlockFormat(this CodeWriter builder, string format, params object?[] args)
        => builder.AppendFormat(format, args).AppendLine().Block();

    public static CodeScope BlockFormat(this CodeWriter builder, IFormatProvider? provider, string format, object? arg0)
        => builder.AppendFormat(provider, format, arg0).AppendLine().Block();

    public static CodeScope BlockFormat(this CodeWriter builder, IFormatProvider? provider, string format, object? arg0, object? arg1)
        => builder.AppendFormat(provider, format, arg0, arg1).AppendLine().Block();

    public static CodeScope BlockFormat(this CodeWriter builder, IFormatProvider? provider, string format, object? arg0, object? arg1, object? arg2)
        => builder.AppendFormat(provider, format, arg0, arg1, arg2).AppendLine().Block();

    public static CodeScope BlockFormat(this CodeWriter builder, IFormatProvider? provider, string format, params object?[] args)
        => builder.AppendFormat(provider, format, args).AppendLine().Block();

    public static CodeScope BlockFormatted(this CodeWriter builder, FormattableString formattedString)
        => builder.AppendFormatted(formattedString).AppendLine().Block();

    static void EndSingleIndent(CodeWriter builder)
        => builder.DecreaseIndent();

    static void EndBlock(CodeWriter builder)
        => builder
            .DecreaseIndent()
            .Append('}')
            .AppendLine();

    static void EndBlockNoNewline(CodeWriter builder)
        => builder
            .DecreaseIndent()
            .Append('}');

    public ref struct CodeScope
    {
        Action<CodeWriter>? _dispose;
        CodeWriter? _builder;

        public CodeScope(CodeWriter builder, Action<CodeWriter> dispose)
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
