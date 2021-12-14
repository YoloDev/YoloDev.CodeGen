namespace YoloDev.CodeGen.Builders.Pools;

sealed partial class ArrayBuilder<T>
{
    /// <summary>
    /// struct enumerator used in foreach.
    /// </summary>
    internal struct Enumerator
    {
        readonly ArrayBuilder<T> _builder;
        int _index;

        public Enumerator(ArrayBuilder<T> builder)
        {
            _builder = builder;
            _index = -1;
        }

        public T Current => _builder[_index];

        public bool MoveNext()
        {
            _index++;
            return _index < _builder.Count;
        }
    }
}
