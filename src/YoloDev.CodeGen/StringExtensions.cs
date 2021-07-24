using System;
using System.Runtime.CompilerServices;

namespace YoloDev.CodeGen
{
    static class StringExtensions
    {
#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Create(ReadOnlySpan<char> span)
        {
            unsafe
            {
                fixed (char* ptr = span)
                {
                    return new string(ptr, 0, span.Length);
                }
            }
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string Create(ReadOnlySpan<char> span)
            => new string(span);
#endif

        public static LineEnumerator Lines(this string input, bool initialStartValue = true)
            => input.AsSpan().Lines(initialStartValue);

        public static LineEnumerator Lines(this ReadOnlySpan<char> input, bool initialStartValue = true)
            => new(input, initialStartValue);

        public enum LineItemKind
        {
            NewLine,
            Normal,
            Start,
        }

        public readonly ref struct LineItem
        {
            public static LineItem NewLine => new(ReadOnlySpan<char>.Empty, LineItemKind.NewLine);

            public ReadOnlySpan<char> Line { get; }

            public LineItemKind Kind { get; }

            public LineItem(ReadOnlySpan<char> line, bool isStart)
                : this(line, isStart ? LineItemKind.Start : LineItemKind.Normal)
            {
            }

            public LineItem(ReadOnlySpan<char> line, LineItemKind kind)
            {
                Line = line;
                Kind = kind;
            }

            public void Deconstruct(out ReadOnlySpan<char> line, out LineItemKind kind)
            {
                line = Line;
                kind = Kind;
            }
        }

        public ref struct LineEnumerator
        {
            bool _start;
            ReadOnlySpan<char> _rest;
            LineItem _current;
            int _index;

            public LineEnumerator(ReadOnlySpan<char> input, bool initialStartValue = true)
            {
                _start = initialStartValue;
                _rest = input;
                _current = default;
                _index = -2;
            }

            public void Fill(ReadOnlySpan<char> rest)
            {
                // we intentionally don't reset the `_start`.
                _rest = rest;
                _index = -2;
            }

            public LineEnumerator GetEnumerator()
                => this;

            public bool MoveNext()
            {
                while (_rest.Length != 0)
                {
                    // we've not gotten an index yet
                    if (_index == -2)
                    {
                        _index = _rest.IndexOfAny('\r', '\n');

                        if (_index == -1)
                        {
                            _current = new(_rest, _start);
                            _rest = ReadOnlySpan<char>.Empty;
                            return true;
                        }
                        else if (_index > 0)
                        {
                            _current = new(_rest.Slice(0, _index), _start);
                            _start = false;
                            return true;
                        }
                    }

                    if (_index < _rest.Length && _rest[_index] == '\r' && _rest[_index + 1] == '\n')
                    {
                        _rest = _rest.Slice(1); // skip \r in \r\n
                    }
                    else if (_rest[_index] == '\r')
                    {
                        _rest = _rest.Slice(1);
                        _index = -2;
                        continue;
                    }

                    _rest = _rest.Slice(1); // skip \n
                    _current = LineItem.NewLine;
                    _index = -2;
                    _start = true;
                    return true;
                }

                return false;
            }

            public LineItem Current => _current;
        }
    }
}

