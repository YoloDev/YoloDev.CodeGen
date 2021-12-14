#if NETSTANDARD2_0
using System.Text;

namespace YoloDev.CodeGen.Builders;

static class StringBuilderExtensions
{
    public static StringBuilder Append(this StringBuilder stringBuilder, ReadOnlySpan<char> span)
    {
        unsafe
        {
            fixed (char* ptr = span)
            {
                _ = stringBuilder.Append(ptr, span.Length);
            }
        }

        return stringBuilder;
    }
}
#endif
