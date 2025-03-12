using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Extensions;
public static class ObejctExtension
{
    public static string? ToInvariantString(this object value)
    {
        if (value is string @string)
            return @string;
        if (value is IConvertible convertible)
            return convertible.ToString(CultureInfo.InvariantCulture);
        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : TypeDescriptor.GetConverter(value.GetType()) is TypeConverter typeConverter
            ? typeConverter.ConvertToInvariantString(value)
            : value.ToString();
    }

    public static Tuple<T1, T2> ToTuple<T, T1, T2>(this T obj, Func<T, T1> selector1, Func<T, T2> selector2) => Tuple.Create(selector1(obj), selector2(obj));
}