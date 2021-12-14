using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace YoloDev.CodeGen;

/// <summary>
/// An indention aware code writer.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public class CodeWriter
{
    static readonly ReadOnlyMemory<char> Spaces = new string(' ', 48).AsMemory();

    readonly int _indentation = 4;
    readonly StringBuilder _codeWriter;

    public CodeWriter(int indentation = 4)
        : this(new StringBuilder(), indentation)
    { }

    public CodeWriter(StringBuilder stringBuilder, int indentation = 4)
    {
        _codeWriter = stringBuilder;
        _indentation = indentation;
    }

    public int IndentLevel { get; private set; }

    public override string ToString()
        => _codeWriter.ToString();

    public CodeWriter IncreaseIndent(int count = 1)
    {
        IndentLevel += count;
        return this;
    }

    public CodeWriter DecreaseIndent(int count = 1)
    {
        IndentLevel -= count;
        return this;
    }

    public CodeWriter Append(ReadOnlySpan<char> text)
    {
        foreach (var (value, kind) in text.Lines())
        {
            if (kind == StringExtensions.LineItemKind.NewLine)
            {
                _ = _codeWriter.Append('\n');
            }
            else
            {
                if (kind == StringExtensions.LineItemKind.Start)
                    MaybeIndent();

                _ = _codeWriter.Append(value);
            }
        }

        return this;
    }

    public CodeWriter AppendFormat(string format, object? arg0)
        => AppendFormatHelper(null, format, new ParamsArray(arg0));

    public CodeWriter AppendFormat(string format, object? arg0, object? arg1)
        => AppendFormatHelper(null, format, new ParamsArray(arg0, arg1));

    public CodeWriter AppendFormat(string format, object? arg0, object? arg1, object? arg2)
        => AppendFormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));

    public CodeWriter AppendFormat(string format, params object?[] args)
    {
        if (args == null)
        {
            // To preserve the original exception behavior, throw an exception about format if both
            // args and format are null. The actual null check for format is in AppendFormatHelper.
            var paramName = (format == null) ? nameof(format) : nameof(args);
            throw new ArgumentNullException(paramName);
        }

        return AppendFormatHelper(null, format, new ParamsArray(args));
    }

    public CodeWriter AppendFormat(IFormatProvider? provider, string format, object? arg0)
        => AppendFormatHelper(provider, format, new ParamsArray(arg0));

    public CodeWriter AppendFormat(IFormatProvider? provider, string format, object? arg0, object? arg1)
        => AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1));

    public CodeWriter AppendFormat(IFormatProvider? provider, string format, object? arg0, object? arg1, object? arg2)
        => AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));

    public CodeWriter AppendFormat(IFormatProvider? provider, string format, params object?[] args)
    {
        if (args == null)
        {
            // To preserve the original exception behavior, throw an exception about format if both
            // args and format are null. The actual null check for format is in AppendFormatHelper.
            var paramName = (format == null) ? nameof(format) : nameof(args);
            throw new ArgumentNullException(paramName);
        }

        return AppendFormatHelper(provider, format, new ParamsArray(args));
    }

    public CodeWriter AppendFormatted(FormattableString formattableString)
        => AppendFormatHelper(null, formattableString.Format, new ParamsArray(formattableString.GetArguments()));

    public CodeWriter Append(char chr)
    {
        MaybeIndent();
        _ = _codeWriter.Append(chr);

        return this;
    }

    public CodeWriter Append(string text)
        => Append(text.AsSpan());

    public CodeWriter AppendLine()
    {
        _ = _codeWriter.Append('\n');
        return this;
    }

    public CodeWriter AppendLine(string text)
        => Append(text).AppendLine();

    [DoesNotReturn]
    static void FormatError()
        => throw new FormatException("Invalid format string");

    // Undocumented exclusive limits on the range for Argument Hole Index and Argument Hole Alignment.
    const int IndexLimit = 1000000; // Note:            0 <= ArgIndex < IndexLimit

    CodeWriter AppendFormatHelper(IFormatProvider? provider, string format, ParamsArray args)
    {
        if (format == null)
        {
            throw new ArgumentNullException(nameof(format));
        }

        if (provider == null)
        {
            provider = CultureInfo.InvariantCulture;
        }

        _ = _codeWriter.EnsureCapacity(format.Length + (args.Length * 8));

        var pos = 0;
        var len = format.Length;
        var ch = '\x0';
        var start = true;

        ICustomFormatter? cf = null;
        if (provider != null)
        {
            cf = (ICustomFormatter?)provider.GetFormat(typeof(ICustomFormatter));
        }

        while (true)
        {
            if (pos < len)
                _ = _codeWriter.EnsureCapacity(len - pos);

            while (pos < len)
            {
                ch = format[pos];
                pos++;

                // Is it a closing brace?
                if (ch == '}')
                {
                    // Check next character (if there is one) to see if it is escaped. eg }}
                    if (pos < len && format[pos] == '}')
                    {
                        pos++;
                    }
                    else
                    {
                        // Otherwise treat it as an error (Mismatched closing brace)
                        FormatError();
                    }
                }

                // Is it an opening brace?
                else if (ch == '{')
                {
                    // Check next character (if there is one) to see if it is escaped. eg {{
                    if (pos < len && format[pos] == '{')
                    {
                        pos++;
                    }
                    else
                    {
                        // Otherwise treat it as the opening brace of an Argument Hole.
                        pos--;
                        break;
                    }
                }

                // Is it a carriage return?
                else if (ch == '\r')
                {
                    // Check next character (if there is one) to see if it is a line feed
                    if (pos < len && format[pos] == '\n')
                    {
                        pos++;
                        ch = '\n';
                    }
                    else
                    {
                        // We simply skip over carriage returns.
                        continue;
                    }
                }

                // If it's neither then treat the character as just text.
                if (ch == '\n')
                {
                    start = true;
                }
                else if (start)
                {
                    MaybeIndent();
                    start = false;
                }
                _ = _codeWriter.Append(ch);
            }

            //
            // Start of parsing of Argument Hole.
            // Argument Hole ::= { Index (, WS* Alignment WS*)? (: Formatting)? }
            //
            if (pos == len)
            {
                break;
            }

            //
            //  Start of parsing required Index parameter.
            //  Index ::= ('0'-'9')+ WS*
            //
            pos++;

            // If reached end of text then error (Unexpected end of text)
            // or character is not a digit then error (Unexpected Character)
            if (pos == len || (ch = format[pos]) < '0' || ch > '9')
                FormatError();

            var index = 0;
            do
            {
                index = (index * 10) + ch - '0';
                pos++;
                // If reached end of text then error (Unexpected end of text)
                if (pos == len)
                {
                    FormatError();
                }
                ch = format[pos];
                // so long as character is digit and value of the index is less than 1000000 ( index limit )
            }
            while (ch >= '0' && ch <= '9' && index < IndexLimit);

            // If value of index is not within the range of the arguments passed in then error (Index out of range)
            if (index >= args.Length)
            {
                throw new FormatException("Format index out of range");
            }

            // Consume optional whitespace.
            while (pos < len && (ch = format[pos]) == ' ')
                pos++;
            // End of parsing index parameter.

            //
            // Start of parsing of optional formatting parameter.
            //
            var arg = args[index];

            ReadOnlySpan<char> itemFormatSpan = default; // used if itemFormat is null

            // Is current character a colon? which indicates start of formatting parameter.
            if (ch == ':')
            {
                pos++;
                var startPos = pos;

                while (true)
                {
                    // If reached end of text then error. (Unexpected end of text)
                    if (pos == len)
                    {
                        FormatError();
                    }
                    ch = format[pos];

                    if (ch == '}')
                    {
                        // Argument hole closed
                        break;
                    }
                    else if (ch == '{')
                    {
                        // Braces inside the argument hole are not supported
                        FormatError();
                    }

                    pos++;
                }

                if (pos > startPos)
                {
                    itemFormatSpan = format.AsSpan(startPos, pos - startPos);
                }
            }
            else if (ch != '}')
            {
                // Unexpected character
                FormatError();
            }

            // Construct the output for this arg hole.
            pos++;
            string? s = null;
            string? itemFormat = null;

            if (cf != null)
            {
                if (itemFormatSpan.Length != 0)
                {
                    itemFormat = StringExtensions.Create(itemFormatSpan);
                }
                s = cf.Format(itemFormat, arg, provider);
            }

            if (s == null)
            {
                if (arg is IFormattable formattableArg)
                {
                    if (itemFormatSpan.Length != 0)
                    {
                        itemFormat ??= StringExtensions.Create(itemFormatSpan);
                    }

                    s = formattableArg.ToString(itemFormat, provider);
                }
                else if (arg != null)
                {
                    s = arg.ToString();
                }
            }

            // Append it to the final output of the Format String.
            if (s == null)
            {
                s = string.Empty;
            }

            foreach (var (value, kind) in s.Lines(start))
            {
                if (kind == StringExtensions.LineItemKind.NewLine)
                {
                    _ = _codeWriter.Append('\n');
                    start = true;
                }
                else
                {
                    if (kind == StringExtensions.LineItemKind.Start)
                        MaybeIndent();

                    _ = _codeWriter.Append(value);
                    start = false;
                }
            }
            // Continue to parse other characters.
        }

        return this;
    }

    void MaybeIndent()
    {
        if (_codeWriter.Length == 0 || _codeWriter[^1] == '\n')
        {
            var indent = IndentLevel * _indentation;
            AppendIndentation(indent);
        }
    }

    void AppendIndentation(int spaces)
    {
        var span = Spaces.Span;
        while (spaces > span.Length)
        {
            _ = _codeWriter.Append(span);
            spaces -= span.Length;
        }

        if (spaces > 0)
        {
            _ = _codeWriter.Append(span[..spaces]);
        }
    }
}
