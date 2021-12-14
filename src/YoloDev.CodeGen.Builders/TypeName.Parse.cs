using System.Diagnostics.CodeAnalysis;
using YoloDev.CodeGen.Builders.Pools;

namespace YoloDev.CodeGen.Builders;

public partial record TypeName
{
    public static bool TryParse(string fullName, bool valueType, [NotNullWhen(true)] out TypeName? result)
        => TryParseNormalized(NormalizeFullName(fullName, out var @namespace), @namespace, valueType, out result);

    public static bool TryParse(ReadOnlySpan<char> fullName, bool valueType, [NotNullWhen(true)] out TypeName? result)
        => TryParseNormalized(NormalizeFullName(fullName, out var @namespace), @namespace, valueType, out result);

    public static TypeName Parse(string fullName, bool valueType)
        => TryParse(fullName, valueType, out var result)
        ? result
        : throw new ArgumentException($"'{fullName}' is not a valid type name.", nameof(fullName));

    public static TypeName Parse(ReadOnlySpan<char> fullName, bool valueType)
        => TryParse(fullName, valueType, out var result)
        ? result
        : throw new ArgumentException($"'{StringExtensions.Create(fullName)}' is not a valid type name.", nameof(fullName));

    static bool TryParseNormalized(string fullName, Range @namespace, bool valueType, [NotNullWhen(true)] out TypeName? typeName)
    {
        var span = fullName.AsSpan();

        if (span.IsEmpty)
        {
            typeName = null;
            return false;
        }

        Range typePart;
        var namespaceLength = @namespace.GetOffsetAndLength(span.Length).Length;
        typePart = namespaceLength == 0
            ? 0..
            : (namespaceLength + 1)..;

        ArrayBuilder<GenericParam>? genericsArrayBuilder = null;
        try
        {
            var rest = span[typePart];
            while (!rest.IsEmpty)
            {
                var symbolIndex = rest.IndexOf('<');
                if (symbolIndex == -1)
                {
                    break;
                }

                var index = symbolIndex + 1;
                var len = rest.Length;
                genericsArrayBuilder ??= ArrayBuilder<GenericParam>.GetInstance();
                genericsArrayBuilder.Add(index);

                while (index < len && rest[index++] is ',')
                {
                    genericsArrayBuilder.Add(index);
                }

                if (rest[index - 1] is not '>')
                {
                    typeName = null;
                    return false;
                }

                rest = rest[index..];
            }

            var genericParams = new GenericParamsArray(genericsArrayBuilder);
            throw new NotImplementedException();
            //typeName = new(fullName, @namespace, typePart, genericParams, new GenericArgsArray(), valueType);
            //return true;
        }
        finally
        {
            genericsArrayBuilder?.Free();
        }
    }
}
