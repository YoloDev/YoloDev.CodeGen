using System.Runtime.CompilerServices;

#if NETSTANDARD2_0
using System.Text;
#endif

namespace YoloDev.CodeGen.Builders;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string Concat(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
    {
        var sb = new StringBuilder(s1.Length + s2.Length);
        _ = sb.Append(s1);
        _ = sb.Append(s2);

        return sb.ToString();
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string Create(ReadOnlySpan<char> span)
        => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string Concat(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
        => string.Concat(s1, s2);
#endif
}
