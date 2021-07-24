using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace YoloDev.CodeGen
{
    /// <summary>
    /// An indention aware code builder.
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class CodeBuilder
    {
        static readonly ReadOnlyMemory<char> Indentation = new string(' ', 48).AsMemory();

        readonly int _indentation = 4;
        readonly StringBuilder _stringBuilder;

        public CodeBuilder(int indentation = 4)
            : this(new StringBuilder(), indentation)
        { }

        public CodeBuilder(StringBuilder stringBuilder, int indentation = 4)
        {
            _stringBuilder = stringBuilder;
            _indentation = indentation;
        }

        public int IndentLevel { get; private set; }

        public override string ToString()
            => _stringBuilder.ToString();

        public CodeBuilder IncreaseIndent(int count = 1)
        {
            IndentLevel += count;
            return this;
        }

        public CodeBuilder DecreaseIndent(int count = 1)
        {
            IndentLevel -= count;
            return this;
        }

        public CodeBuilder Append(ReadOnlySpan<char> text)
        {
            foreach (var (value, kind) in text.Lines())
            {
                if (kind == StringExtensions.LineItemKind.NewLine)
                {
                    _stringBuilder.Append('\n');
                }
                else
                {
                    if (kind == StringExtensions.LineItemKind.Start)
                        MaybeIndent();

                    _stringBuilder.Append(value);
                }
            }

            return this;
        }

        public CodeBuilder AppendFormat(string format, object? arg0)
            => AppendFormatHelper(null, format, new ParamsArray(arg0));

        public CodeBuilder AppendFormat(string format, object? arg0, object? arg1)
            => AppendFormatHelper(null, format, new ParamsArray(arg0, arg1));

        public CodeBuilder AppendFormat(string format, object? arg0, object? arg1, object? arg2)
            => AppendFormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));

        public CodeBuilder AppendFormat(string format, params object?[] args)
        {
            if (args == null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in AppendFormatHelper.
                string paramName = (format == null) ? nameof(format) : nameof(args);
                throw new ArgumentNullException(paramName);
            }

            return AppendFormatHelper(null, format, new ParamsArray(args));
        }

        public CodeBuilder AppendFormat(IFormatProvider? provider, string format, object? arg0)
            => AppendFormatHelper(provider, format, new ParamsArray(arg0));

        public CodeBuilder AppendFormat(IFormatProvider? provider, string format, object? arg0, object? arg1)
            => AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1));

        public CodeBuilder AppendFormat(IFormatProvider? provider, string format, object? arg0, object? arg1, object? arg2)
            => AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));

        public CodeBuilder AppendFormat(IFormatProvider? provider, string format, params object?[] args)
        {
            if (args == null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in AppendFormatHelper.
                string paramName = (format == null) ? nameof(format) : nameof(args);
                throw new ArgumentNullException(paramName);
            }

            return AppendFormatHelper(provider, format, new ParamsArray(args));
        }

        public CodeBuilder AppendFormatted(FormattableString formattableString)
            => AppendFormatHelper(null, formattableString.Format, new ParamsArray(formattableString.GetArguments()));

        public CodeBuilder Append(char chr)
        {
            MaybeIndent();
            _stringBuilder.Append(chr);

            return this;
        }

        public CodeBuilder Append(string text)
            => Append(text.AsSpan());

        public CodeBuilder AppendLine()
        {
            _stringBuilder.Append('\n');
            return this;
        }

        public CodeBuilder AppendLine(string text)
            => Append(text).AppendLine();

        [DoesNotReturn]
        static void FormatError()
        {
            throw new FormatException("Invalid format string");
        }

        // Undocumented exclusive limits on the range for Argument Hole Index and Argument Hole Alignment.
        const int IndexLimit = 1000000; // Note:            0 <= ArgIndex < IndexLimit

        CodeBuilder AppendFormatHelper(IFormatProvider? provider, string format, ParamsArray args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (provider == null)
            {
                provider = CultureInfo.InvariantCulture;
            }

            _stringBuilder.EnsureCapacity(format.Length + args.Length * 8);

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
                    _stringBuilder.EnsureCapacity(len - pos);

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
                    _stringBuilder.Append(ch);
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
                    index = index * 10 + ch - '0';
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
                        _stringBuilder.Append('\n');
                        start = true;
                    }
                    else
                    {
                        if (kind == StringExtensions.LineItemKind.Start)
                            MaybeIndent();

                        _stringBuilder.Append(value);
                        start = false;
                    }
                }
                // Continue to parse other characters.
            }

            return this;
        }

        void MaybeIndent()
        {
            if (_stringBuilder.Length == 0 || _stringBuilder[_stringBuilder.Length - 1] == '\n')
            {
                var indent = IndentLevel * _indentation;
                AppendIndentation(indent);
            }
        }

        void AppendIndentation(int spaces)
        {
            var span = Indentation.Span;
            while (spaces > span.Length)
            {
                _stringBuilder.Append(span);
                spaces -= span.Length;
            }

            if (spaces > 0)
            {
                _stringBuilder.Append(span.Slice(0, spaces));
            }
        }
    }
}
