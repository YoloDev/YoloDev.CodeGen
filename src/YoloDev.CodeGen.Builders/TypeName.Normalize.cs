using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace YoloDev.CodeGen.Builders;

public partial record TypeName
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string NormalizeFullName(string fullName, out Range @namespace)
        => NormalizeFullName(fullName.AsSpan(), out var normalized, out @namespace)
        ? normalized
        : fullName;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string NormalizeFullName(ReadOnlySpan<char> fullName, out Range @namespace)
        => NormalizeFullName(fullName, out var normalized, out @namespace)
        ? normalized
        : StringExtensions.Create(fullName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool NormalizeFullName(ReadOnlySpan<char> fullName, [NotNullWhen(true)] out string? normalized, out Range @namespace)
    {
        var needsGlobal = !fullName.StartsWith("global::".AsSpan(), StringComparison.Ordinal);

        var lastDot = fullName.LastIndexOf('.');
        @namespace = lastDot == -1
            ? 0..0
            : 0..lastDot;

        var specialSymbolIndex = fullName.IndexOfAny('`', '+');
        if (specialSymbolIndex == -1)
        {
            if (needsGlobal)
            {
                normalized = StringExtensions.Concat("global::".AsSpan(), fullName);
                return true;
            }

            normalized = null;
            return false;
        }

        if (needsGlobal)
        {
            normalized = StringExtensions.Concat("global::".AsSpan(), fullName);
            fullName = normalized.AsSpan();
        }

        normalized = NormalizeSlowPath(fullName, specialSymbolIndex);
        return true;

        static string NormalizeSlowPath(ReadOnlySpan<char> fullName, int specialSymbolIndex)
        {
            var builder = new StringBuilder(fullName.Length + 10);

            do
            {
                _ = builder.Append(fullName[..specialSymbolIndex]);
                var symbol = fullName[specialSymbolIndex];

                if (symbol == '+')
                {
                    // simple replacement of + -> .
                    _ = builder.Append('.');
                    fullName = fullName[(specialSymbolIndex + 1)..];
                    specialSymbolIndex = fullName.IndexOfAny('`', '+');
                    continue;
                }

                var arity = ParseArity(ref fullName, specialSymbolIndex);
                specialSymbolIndex = fullName.IndexOfAny('`', '+');

                _ = builder.Append('<');
                for (var i = 1; i < arity; i++)
                    _ = builder.Append(',');
                _ = builder.Append('>');
            }
            while (specialSymbolIndex != -1);

            _ = builder.Append(fullName);
            return builder.ToString();
        }

        static int ParseArity(ref ReadOnlySpan<char> fullName, int index)
        {
            Debug.Assert(fullName[index] == '`');
            var len = fullName.Length;

            var arity = 0;
            while (index < len && fullName[++index] is >= '0' and <= '9' and var ch)
            {
                arity = (arity * 10) + ch - '0';
            }

            fullName = fullName[index..];
            return arity;
        }
    }
}
