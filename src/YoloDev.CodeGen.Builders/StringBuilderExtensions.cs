using System;
using System.Text;

namespace YoloDev.CodeGen.Builders
{
#if NETSTANDARD2_0
    static class StringBuilderExtensions
    {
        public static StringBuilder Append(this StringBuilder stringBuilder, ReadOnlySpan<char> span)
        {
            unsafe
            {
                fixed (char* ptr = span)
                {
                    stringBuilder.Append(ptr, span.Length);
                }
            }

            return stringBuilder;
        }
    }
#endif
}
