using System;
using System.Diagnostics;

namespace YoloDev.CodeGen.Builders
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public partial record TypeName
    {
        readonly string _fullName;
        readonly Range _global;
        readonly Range _namespace;
        readonly TypeNamesArray _typeName;
        readonly GenericParamsArray _genericParams;
        readonly GenericArgsArray _genericArgs;
        readonly bool _valueType;

        TypeName(
            string fullName,
            Range global,
            Range @namespace,
            TypeNamesArray typeName,
            GenericParamsArray genericParams,
            GenericArgsArray genericArgs,
            bool valueType)
        {
            _fullName = fullName;
            _global = global;
            _namespace = @namespace;
            _typeName = typeName;
            _genericParams = genericParams;
            _genericArgs = genericArgs;
            _valueType = valueType;
        }

        public override string ToString()
            => _fullName;

        string GetDebuggerDisplay()
            => ToString();
    }
}
