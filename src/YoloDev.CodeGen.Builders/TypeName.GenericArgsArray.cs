using System;

namespace YoloDev.CodeGen.Builders
{
    public partial record TypeName
    {
        readonly struct GenericArgsArray
        {
            // Sentinel fixed-length arrays eliminate the need for a "count" field keeping this
            // struct down to just 4 fields. These are only used for their "Length" property,
            // that is, their elements are never set or referenced.
            private static readonly TypeName[] _oneIndicesArray = new TypeName[1];
            private static readonly TypeName[] _twoIndicesArray = new TypeName[2];
            private static readonly TypeName[] _threeIndicesArray = new TypeName[3];

            readonly TypeName? _arg0;
            readonly TypeName? _arg1;
            readonly TypeName? _arg2;

            // After construction, the first three elements of this array will never be accessed
            // because the indexer will retrieve those values from arg0, arg1, and arg2.
            readonly TypeName[]? _arr;

            public GenericArgsArray(TypeName arg0)
            {
                _arg0 = arg0;
                _arg1 = default;
                _arg2 = default;

                _arr = _oneIndicesArray;
            }

            public GenericArgsArray(TypeName arg0, TypeName arg1)
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = default;

                _arr = _twoIndicesArray;
            }

            public GenericArgsArray(TypeName arg0, TypeName arg1, TypeName arg2)
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;

                _arr = _threeIndicesArray;
            }

            public GenericArgsArray(TypeName[] indices)
            {
                int len = indices.Length;
                _arg0 = len > 0 ? indices[0] : default;
                _arg1 = len > 1 ? indices[1] : default;
                _arg2 = len > 2 ? indices[2] : default;

                _arr = len switch
                {
                    0 => null,
                    1 => _oneIndicesArray,
                    2 => _twoIndicesArray,
                    3 => _threeIndicesArray,
                    _ => indices,
                };
            }

            public int Length
                => _arr is null
                ? 0
                : _arr.Length;

            public TypeName this[int index]
                => index == 0 && _arg0 is not null
                ? _arg0
                : GetAtSlow(index);

            TypeName GetAtSlow(int index)
            {
                if (index > Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (index == 1)
                    return _arg1!;

                if (index == 2)
                    return _arg2!;

                return _arr![index];
            }
        }
    }
}
