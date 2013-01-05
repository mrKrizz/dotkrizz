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
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace dotkrizz.web {
  public static class DescribedValues {
    public static IEnumerable<IDescribedValue> GetDescribedValues(this Type type) {
      if (type.IsEnum) {
        return from object val in Enum.GetValues(type)
               select GetEnumDescribedValue(val);
      } else if (type == typeof(bool)) {
        return from val in new bool[] { true, false }
               select GetDescribedValue<bool>(val) as IDescribedValue;
      } else {
        return null;
      }
    }

    public static IDescribedValue GetEnumDescribedValue(object value) {
      if (value == null) {
        return new NullDescribedValue();
      } else if (value.GetType().IsEnum) {
        var type = typeof(EnumDescribedValue<>).MakeGenericType(value.GetType());
        if (type != null)
          return (EnumDescribedValue)Activator.CreateInstance(type, value);
        else
          return new EnumDescribedValue(value);
      } else {
        return null;
      }
    }

    public static IDescribedValue<T> GetDescribedValue<T>(this T value) {
      if (value == null)
        return new NullDescribedValue<T>();
      else if (typeof(T).IsEnum)
        return new EnumDescribedValue<T>(value);
      else
        return new StandardDescribedValue<T>(value);
    }

    public static IDescribedValue GetDescribedValue(this object value) {
      var val_type = value.GetType();
      var methods = from m in typeof(DescribedValues).GetMethods()
                    where m.IsStatic && m.IsGenericMethodDefinition
                    select m;
      var method = methods.First();

      var specialized = method.MakeGenericMethod(val_type);

      return (IDescribedValue)specialized.Invoke(null, new object[] { value });
    }
  }

  public interface IDescribedValue {
    string Name {
      get;
    }

    string Text {
      get;
    }

    object Value {
      get;
    }
  }

  public interface IDescribedValue<T> : IDescribedValue {
    new T Value {
      get;
    }
  }

  abstract class BaseDescribedValue {
    public BaseDescribedValue(object value) {
      Value = value;
    }

    public override string ToString() {
      return Text;
    }

    public abstract string Text {
      get;
      protected set;
    }

    public virtual object Value {
      get;
      private set;
    }
  }

  class EnumDescribedValue : BaseDescribedValue, IDescribedValue {
    public EnumDescribedValue(object value) : base(value) {
      var type = value.GetType();

      if (!type.IsEnum)
        throw new ArgumentException();


      Name = Enum.GetName(type, value);
      var field = type.GetField(Name);

      var attr = (DescriptionAttribute)field.GetCustomAttributes(
          typeof(DescriptionAttribute), false).FirstOrDefault();
      if (attr != null) {
        Text = attr.Description;
      } else {
        Text = Name;
      }
    }

    public string Name {
      get;
      protected set;
    }

    public override string Text {
      get;
      protected set;
    }
  }

  class EnumDescribedValue<T> : EnumDescribedValue, IDescribedValue<T> {
    public EnumDescribedValue(T value)
      : base(value) {

    }

    public new T Value {
      get {
        return (T)base.Value;
      }
    }

    public static implicit operator T(EnumDescribedValue<T> described_enum) {
      return described_enum.Value;
    }
  }

  class NullDescribedValue : BaseDescribedValue, IDescribedValue {
    public NullDescribedValue()
        : base(null) {
     }

    public string Name {
      get {
        return "null";
      }
    }

    public override string Text {
      get {
        return "null";
      }
      protected set {
        throw new NotImplementedException();
      }
    }
  }

  class NullDescribedValue<T> : NullDescribedValue, IDescribedValue<T> {
    public new T Value {
      get {
        return default(T);
      }
    }
  }

  class StandardDescribedValue : BaseDescribedValue, IDescribedValue {
    public StandardDescribedValue(object value) : base(value) {
      
    }

    public override string Text {
      get {
        return Value.ToString();
      }
      protected set {
        throw new NotImplementedException();
      }
    }

    public string Name {
      get {
        return Value.ToString();
      }
    }
  }

  class StandardDescribedValue<T> : StandardDescribedValue, IDescribedValue<T> {
    public StandardDescribedValue(T value)
        : base(value) {
    }

    public new T Value {
      get {
        return (T)(this as IDescribedValue).Value;
      }
    }

  }
 
}
