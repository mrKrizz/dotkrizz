/**
Copyright (c) 2009 Krzysztof Olczyk. All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are
permitted provided that the following conditions are met:

   1. Redistributions of source code must retain the above copyright notice, this list of
      conditions and the following disclaimer.

   2. Redistributions in binary form must reproduce the above copyright notice, this list
      of conditions and the following disclaimer in the documentation and/or other materials
      provided with the distribution.

THIS SOFTWARE IS PROVIDED BY KRZYSZTOF OLCZYK ''AS IS'' AND ANY EXPRESS OR IMPLIED
WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL KRZYSZTOF OLCZYK OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Globalization;

namespace dotkrizz {
  public struct Number {
    private object value_;

    public Number (int value) {
      value_ = value;
    }

    [CLSCompliant(false)]
    public Number (uint value) {
      value_ = value;
    }

    public Number (long value) {
      value_ = value;
    }

    [CLSCompliant(false)]
    public Number (ulong value) {
      value_ = value;
    }

    public Number (short value) {
      value_ = value;
    }

    [CLSCompliant(false)]
    public Number (ushort value) {
      value_ = value;
    }

    public Number (byte value) {
      value_ = value;
    }

    public Number (float value) {
      value_ = value;
    }

    public Number (double value) {
      value_ = value;
    }

    public Number (decimal value) {
      value_ = value;
    }

    public Number (int? value) {
      value_ = value;
    }

    [CLSCompliant(false)]
    public Number (uint? value) {
      value_ = value;
    }

    public Number (long? value) {
      value_ = value;
    }

    [CLSCompliant(false)]
    public Number (ulong? value) {
      value_ = value;
    }

    public Number (short? value) {
      value_ = value;
    }

    [CLSCompliant(false)]
    public Number (ushort? value) {
      value_ = value;
    }

    public Number (byte? value) {
      value_ = value;
    }

    public Number (float? value) {
      value_ = value;
    }

    public Number (double? value) {
      value_ = value;
    }

    public Number (decimal? value) {
      value_ = value;
    }

    public Number (string value) {
      double @double;
      int int32;
      long int64;

      try {
        @double = Convert.ToDouble (value, CultureInfo.InvariantCulture);
      } catch (FormatException e) {
        throw new NumberException (e, "String '{0}' cannot be parsed as a number!", value);
      }
      try {
        int64 = Convert.ToInt64 (value);
        int32 = Convert.ToInt32 (value);
      } catch (FormatException) {
        value_ = @double;
        return;
      }

      if (@double != int64) {
        value_ = @double;
      } else if (int64 != int32) {
        value_ = int64;
      } else {
        value_ = int32;
      }
    }

    public Number (object value) {
      if (value == null) {
        value_ = null;
        return;
      }
      if (IsAllowedType (value.GetType ()))
        value_ = value;
      else
        throw new NumberException ("Number cannot be initialized with\r\n           a value of type {0}.", value.GetType ());
    }

    private static bool IsAllowedType (Type type) {
      var parametered_constructors = from c in typeof(Number).GetConstructors ()
        where c.IsPublic
        let p = c.GetParameters ().FirstOrDefault ()
        where p != null
        select p;
      var matching_constructor = parametered_constructors.FirstOrDefault (p => p.ParameterType.IsAssignableFrom (type));

      return matching_constructor != null;
    }

    public override string ToString () {
      if (value_ == null)
        return "null {" + typeof(Number).FullName + "}";
      else
        return value_.ToString ();
    }

    public Type CurrentType {
      get {
        if (value_ == null)
          return null;
        else
          return value_.GetType ();
      }
    }

    public TypeCode CurrentTypeCode {
      get { return Type.GetTypeCode (CurrentType); }
    }

    public void ChangeType (Type new_type) {
      if (IsAllowedType (new_type)) {
        value_ = Convert.ChangeType (value_, new_type);
      } else {
        throw new NumberException ("Cannot store number as {0}!", new_type);
      }
    }

    public static Number Null = new Number ((object)null);

    public static implicit operator Number (int? value) {
      return new Number (value);
    }

    public static implicit operator Number (long? value) {
      return new Number (value);
    }

    public static implicit operator Number (short? value) {
      return new Number (value);
    }

    public static implicit operator Number (float? value) {
      return new Number (value);
    }

    public static implicit operator Number (double? value) {
      return new Number (value);
    }

    public static implicit operator Number (string value) {
      return new Number (value);
    }

    public T GetValue<T> () {
      if (CurrentType == typeof(T))
        return (T)value_;
      else
        return (T)Convert.ChangeType (value_, typeof(T));
    }

    public static implicit operator string (Number number) {
      return number.ToString ();
    }

    public static implicit operator int (Number number) {
      return number.GetValue<int> ();
    }

    [CLSCompliant(false)]
    public static implicit operator uint (Number number) {
      return number.GetValue<uint> ();
    }

    public static implicit operator long (Number number) {
      return number.GetValue<long> ();
    }

    [CLSCompliant(false)]
    public static implicit operator ulong (Number number) {
      return number.GetValue<ulong> ();
    }

    public static implicit operator short (Number number) {
      return number.GetValue<short> ();
    }

    public static implicit operator byte (Number number) {
      return number.GetValue<byte> ();
    }

    [CLSCompliant(false)]
    public static implicit operator ushort (Number number) {
      return number.GetValue<ushort> ();
    }

    public static implicit operator float (Number number) {
      return number.GetValue<float> ();
    }

    public static implicit operator double (Number number) {
      return number.GetValue<double> ();
    }

    public static Dictionary<string, object> operator_delegates_cache = new Dictionary<string, object> ();

    private static T DoOperator<T> (Number a, Number b, Func<Expression, Expression, BinaryExpression> factory) {
      var common_type = GetCommonType (a.CurrentType, b.CurrentType);

      var cache_key = String.Format ("{0} {1}", factory.Method.Name, common_type);

      object operator_delegate;
      if (!operator_delegates_cache.TryGetValue (cache_key, out operator_delegate)) {

        if (common_type == null) {
          if (typeof(T) == typeof(Number)) {
            return (T)(object)Number.Null;
          } else {
            throw new NullReferenceException ();
          }
        }

        var param_a = Expression.Parameter (common_type, "a");
        var param_b = Expression.Parameter (common_type, "b");

        var body = factory (param_a, param_b);

        var operator_expr = Expression.Lambda (body, param_a, param_b);
        operator_delegate = operator_expr.Call ("Compile");

        lock (operator_delegates_cache) {
          if (!operator_delegates_cache.ContainsKey (cache_key))
            operator_delegates_cache.Add (cache_key, operator_delegate);
        }
      }

      if (typeof(T) == typeof(Number))
        return (T)(object)new Number (operator_delegate.Call ("Invoke", a.value_, b.value_));
      else
        return (T)operator_delegate.Call ("Invoke", a.value_, b.value_);
    }

    public static Number operator + (Number a, Number b) {
      return DoOperator<Number> (a, b, Expression.Add);
    }

    public static Number operator - (Number a, Number b) {
      return DoOperator<Number> (a, b, Expression.Subtract);
    }

    public static Number operator * (Number a, Number b) {
      return DoOperator<Number> (a, b, Expression.Multiply);
    }

    public static Number operator / (Number a, Number b) {
      return DoOperator<Number> (a, b, Expression.Divide);
    }

    public static Number operator % (Number a, Number b) {
      return DoOperator<Number> (a, b, Expression.Modulo);
    }

    public static Number operator | (Number a, Number b) {
      return DoOperator<Number> (a, b, Expression.Or);
    }

    public static Number operator & (Number a, Number b) {
      return DoOperator<Number> (a, b, Expression.And);
    }

    public static Number operator ^ (Number a, Number b) {
      return DoOperator<Number> (a, b, Expression.ExclusiveOr);
    }

    public static bool operator > (Number a, Number b) {
      return DoOperator<bool> (a, b, Expression.GreaterThan);
    }

    public static bool operator < (Number a, Number b) {
      return DoOperator<bool> (a, b, Expression.LessThan);
    }

    public static bool operator >= (Number a, Number b) {
      return DoOperator<bool> (a, b, Expression.GreaterThanOrEqual);
    }

    public static bool operator <= (Number a, Number b) {
      return DoOperator<bool> (a, b, Expression.LessThanOrEqual);
    }

    private static IDictionary<Type, Type> corresponding_signs = new Dictionary<Type, Type> {
      {
        typeof(ushort),
        typeof(int)
      },
      {
        typeof(uint),
        typeof(long)
      }
    };

    private static Type[] types_order = {
      typeof(byte),
      typeof(short),
      typeof(int),
      typeof(long),
      typeof(float),
      typeof(double)
    };

    private static Type GetCommonType (Type a, Type b) {
      if (a == null || b == null)
        return null;

      Type a1, b1;
      corresponding_signs.TryGetValue (a, out a1);
      corresponding_signs.TryGetValue (b, out b1);

      a1 = a1 ?? a;
      b1 = b1 ?? b;

      var a_i = Array.FindIndex (types_order, t => t == a1);
      var b_i = Array.FindIndex (types_order, t => t == b1);

      if (a_i > b_i)
        return a1;
      else
        return b1;
    }

    public override int GetHashCode () {
      if (value_ == null)
        return 0;
      return value_.GetHashCode();
    }

    public override bool Equals (object obj) {
      var value2 = obj != null && obj.GetType () == typeof(Number) ? ((Number)obj).value_ : obj;

      if (value_ == null)
        return value2 == null;

      if (value2 == null)
        return false;

      var common_type = GetCommonType (value_.GetType (), value2.GetType ());

      var a = Convert.ChangeType (value_, common_type);
      var b = Convert.ChangeType (value2, common_type);

      return a.Equals (b);
    }

    public static bool operator == (Number a, Number b) {
      return a.Equals (b);
    }

    public static bool operator != (Number a, Number b) {
      return !(a == b);
    }

  }

  public class NumberException : Exception {
    public NumberException (string msg, params object[] args) : base(String.Format (msg, args)) {
    }

    public NumberException (Exception inner_exception, string msg, params object[] args) : base(String.Format (msg, args), inner_exception) {
    }
  }
}
