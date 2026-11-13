using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NETFRAMEWORK
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// メソッドが指定した値まりを返す場合、引数が null になる可能性があることを示します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class MaybeNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; }
        public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
    }

    /// <summary>
    /// メソッドが指定した値まりを返す場合、引数が null にならないことを示します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class NotNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; }
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
    }
}
#endif
