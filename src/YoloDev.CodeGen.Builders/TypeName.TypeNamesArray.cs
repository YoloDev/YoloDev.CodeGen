using System;
using YoloDev.CodeGen.Builders.Pools;

namespace YoloDev.CodeGen.Builders
{
    public partial record TypeName
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks></remarks>
        readonly struct TypeNamesArray
        {
            // Sentinel fixed-length arrays eliminate the need for a "count" field keeping this
            // struct down to just 4 fields. These are only used for their "Length" property,
            // that is, their elements are never set or referenced.
            private static readonly Range[] _oneIndicesArray = new Range[1];
            private static readonly Range[] _twoIndicesArray = new Range[2];
            private static readonly Range[] _threeIndicesArray = new Range[3];

            readonly Range _index0;
            readonly Range _index1;
            readonly Range _index2;

            // After construction, the first three elements of this array will never be accessed
            // because the indexer will retrieve those values from arg0, arg1, and arg2.
            readonly Range[] _arr;

            public TypeNamesArray(ArrayBuilder<Range> builder)
            {
                builder.ReverseContents();

                var len = builder.Count;
                _index0 = len > 0 ? builder[0] : default;
                _index1 = len > 1 ? builder[1] : default;
                _index2 = len > 2 ? builder[2] : default;

                _arr = len switch
                {
                    0 => throw new InvalidOperationException("There should always be at least 1 type-name"),
                    1 => _oneIndicesArray,
                    2 => _twoIndicesArray,
                    3 => _threeIndicesArray,
                    _ => builder.ToArray(),
                };
            }

            public int Length
                => _arr.Length;

            public Range this[int index]
                => index == 0
                ? _index0
                : GetAtSlow(index);

            Range GetAtSlow(int index)
            {
                if (index == 1)
                    return _index1;
                if (index == 2)
                    return _index2;
                if (_arr is null)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _arr[index];
            }
        }
    }
}
