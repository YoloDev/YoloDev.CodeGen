using YoloDev.CodeGen.Builders.Pools;

namespace YoloDev.CodeGen.Builders;

public partial record TypeName
{
    readonly struct GenericParam
    {
        readonly Index _index;

        public GenericParam(Index index)
        {
            _index = index;
        }

        public static implicit operator GenericParam(int index)
            => new(index);

        public static implicit operator GenericParam(Index index)
            => new(index);
    }

    readonly struct GenericParamsArray
    {
        // Sentinel fixed-length arrays eliminate the need for a "count" field keeping this
        // struct down to just 4 fields. These are only used for their "Length" property,
        // that is, their elements are never set or referenced.
        static readonly GenericParam[] OneIndicesArray = new GenericParam[1];
        static readonly GenericParam[] TwoIndicesArray = new GenericParam[2];
        static readonly GenericParam[] ThreeIndicesArray = new GenericParam[3];

        readonly GenericParam _index0;
        readonly GenericParam _index1;
        readonly GenericParam _index2;

        // After construction, the first three elements of this array will never be accessed
        // because the indexer will retrieve those values from arg0, arg1, and arg2.
        readonly GenericParam[]? _arr;

        public GenericParamsArray(GenericParam index0)
        {
            _index0 = index0;
            _index1 = default;
            _index2 = default;

            _arr = OneIndicesArray;
        }

        public GenericParamsArray(GenericParam index0, GenericParam index1)
        {
            _index0 = index0;
            _index1 = index1;
            _index2 = default;

            _arr = TwoIndicesArray;
        }

        public GenericParamsArray(GenericParam index0, GenericParam index1, GenericParam index2)
        {
            _index0 = index0;
            _index1 = index1;
            _index2 = index2;

            _arr = ThreeIndicesArray;
        }

        public GenericParamsArray(GenericParam[] indices)
        {
            var len = indices.Length;
            _index0 = len > 0 ? indices[0] : default;
            _index1 = len > 1 ? indices[1] : default;
            _index2 = len > 2 ? indices[2] : default;

            _arr = len switch
            {
                0 => null,
                1 => OneIndicesArray,
                2 => TwoIndicesArray,
                3 => ThreeIndicesArray,
                _ => indices,
            };
        }

        public GenericParamsArray(ArrayBuilder<GenericParam>? builder)
        {
            var len = builder?.Count ?? 0;
            _index0 = len > 0 ? builder![0] : default;
            _index1 = len > 1 ? builder![1] : default;
            _index2 = len > 2 ? builder![2] : default;

            _arr = len switch
            {
                0 => null,
                1 => OneIndicesArray,
                2 => TwoIndicesArray,
                3 => ThreeIndicesArray,
                _ => builder!.ToArray(),
            };
        }

        public int Length
            => _arr switch
            {
                null => 0,
                var arr => arr.Length,
            };

        public GenericParam this[int index]
            => index == 0
            ? _index0
            : GetAtSlow(index);

        GenericParam GetAtSlow(int index)
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
