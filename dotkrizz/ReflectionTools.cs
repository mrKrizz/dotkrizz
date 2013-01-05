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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;

namespace dotkrizz {
  /// <summary> 
  /// Extends certain System.Reflection classes with useful methods 
  /// </summary>
  public static class ReflectionTools {
    public static bool HasAnyAttribute(this ICustomAttributeProvider member, 
                                       params Type [] attributes) {
      foreach (var attribute in attributes) {
        if (member.GetCustomAttributes(attribute, true).Length > 0)
          return true;
      }
      return false;
    }

    public static bool HasAllAttributes(this ICustomAttributeProvider member,
                                   params Type [] attributes) {
      foreach (var attribute in attributes) {
        if (member.GetCustomAttributes(attribute, true).Length == 0)
          return false;
      }
      return true;
    }

    public static IEnumerable<T> GetCustomAttributes<T>(
        this ICustomAttributeProvider mi, bool inherit)
        where T : class {
      return mi.GetCustomAttributes(typeof(T), inherit).OfType<T>();
    }

    public static T GetSingleAttributeOrNull<T>(this ICustomAttributeProvider mi) 
        where T : class {
      var attributes = mi.GetCustomAttributes(typeof(T), true);
      if (attributes.Length > 0)
        return attributes [0] as T;
      else
        return default(T);
    }

    public static E GetSingleAttributePropertyValueOrGiven<T, E>(
            this ICustomAttributeProvider mi,
            string property_name, E default_value)
        where T : class
        where E : class {
      var attributes = mi.GetCustomAttributes(typeof(T), true);
      if (attributes.Length > 0) {
        var attr = attributes [0];
        var property = attr.GetType().GetFieldOrProperty(property_name,
            FieldPropertyConflictAction.ReturnProperty, 
            BindingFlags.Public | BindingFlags.Instance);
        if (property != null && property.IsReadable()) {
          return (property.GetValue(attr) ?? default_value) as E;
        } else {
          return default_value;
        }
      } else {
        return default_value;
      }
    }

    public static E GetSingleAttributePropertyValueOrNull<T, E>(
          this ICustomAttributeProvider mi, string property_name)
        where T : class
        where E : class {
      return GetSingleAttributePropertyValueOrGiven<T, E>(mi, property_name,
          default(E));
    }

    public static object GetValue(this MemberInfo mi, object obj) {
      if (mi is PropertyInfo)
        return (mi as PropertyInfo).GetValue(obj, null);
      else if (mi is FieldInfo)
        return (mi as FieldInfo).GetValue(obj);
      else
        throw new InvalidCastException();
    }

    public static void SetValue(this MemberInfo mi, object obj, object value) {
      if (mi is PropertyInfo)
        (mi as PropertyInfo).SetValue(obj, value, null);
      else if (mi is FieldInfo)
        (mi as FieldInfo).SetValue(obj, value);
      else
        throw new InvalidCastException();
    }

    public static Type GetMemberType(this MemberInfo mi) {
      if (mi is PropertyInfo)
        return (mi as PropertyInfo).PropertyType;
      else if (mi is FieldInfo)
        return (mi as FieldInfo).FieldType;
      else if (mi is MethodInfo)
        return (mi as MethodInfo).ReturnType;
      return null;
    }

    public static void SetValueParsed(this MemberInfo mi, object obj,
                                      object value) {
      var member_type = mi.GetMemberType();

      if (member_type.IsAssignableFrom(value.GetType())) {
        mi.SetValue(obj, value);
      } else if (member_type == typeof(bool)) {
        try {
          mi.SetValue(obj, Boolean.Parse(value.ToString()));
        } catch (FormatException) {
          mi.SetValue(obj, Convert.ToInt32(value) > 0);
        }
      } else if (member_type.IsPrimitive 
          || member_type == typeof(DateTime)) {
        mi.SetValue(obj, Convert.ChangeType(value, member_type,
            CultureInfo.InvariantCulture)); // CHECK IT
      } else if (member_type.IsEnum) {
        mi.SetValue(obj, Enum.Parse(member_type, value.ToString(), true));
      } else {
        throw new ArgumentException();
      }
    }

    public static Type GetGenericArgument(this Type type) {
      Type [] arguments = type.GetGenericArguments();
      if (arguments.Length > 0)
        return arguments [0];
      else
        return null;
    }

    /// <summary>
    /// Returns the type of array's item, collection's element or 
    /// itself if scalar is provided
    /// e.g. for int [] it's int
    ///      for List&lt;string&gt; it's string
    ///      for double it's double
    /// </summary>
    public static Type GetScalarType(this Type type) {
      if (type.IsArray) {
        return type.GetElementType();
      } else if (type.IsGenericType 
          && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
        return type.GetGenericArgument();      
      } else if (typeof(IEnumerable).IsAssignableFrom(type)) {
        try {
          return (from i in type.GetInterfaces()
                  where i.IsGenericType 
                      && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                  select i.GetGenericArgument()).First();
        } catch {
          throw new ArgumentException(@"GetScalarType cannot determine type for 
           this IEnumerable, use IEnumerable<T> instead");
        }
      } else {
        return type;
      }
    }

    public static bool IsConstruableFrom(this Type type,
        Type tested_type) {
      return type.GetConstructor(new Type [] { tested_type }) != null;
    }

    public static IEnumerable<MemberInfo> GetFieldsAndProperties(
        this Type type) {
      return type.GetFields().Cast<MemberInfo>().Union(
          type.GetProperties().Cast<MemberInfo>());
    }

    public enum FieldPropertyConflictAction {
      ReturnField,
      ReturnProperty,
      ReturnNull,
      ThrowException
    }

    public static MemberInfo GetFieldOrProperty(this Type type, string name,
        FieldPropertyConflictAction conflict_action, BindingFlags? flags) {
      FieldInfo field;
      PropertyInfo property;

      if (flags.HasValue) {
        field = type.GetField(name, flags.Value);
        property = type.GetProperty(name, flags.Value);
      } else {
        field = type.GetField(name);
        property = type.GetProperty(name);
      }

      if (field != null && property != null) {
        switch (conflict_action) {
          case FieldPropertyConflictAction.ReturnField:
            return field;
          case FieldPropertyConflictAction.ReturnProperty:
            return property;
          case FieldPropertyConflictAction.ReturnNull:
            return null;
          default:
            throw new ArgumentException(String.Format(
                "Type {0} has both property and field named {1}", type, name),
                "name");
        }
      } else if (field != null) {
        return field;
      } else  {
        return property;
      }
    }

    public static MemberInfo GetFieldOrProperty(this Type type, string name) {
      return GetFieldOrProperty(type, name,
          FieldPropertyConflictAction.ThrowException, null);
    }

    public static MemberInfo GetFieldOrProperty(this Type type, string name,
                                                BindingFlags flags) {
      return GetFieldOrProperty(type, name,
          FieldPropertyConflictAction.ThrowException, flags);
    }

    public static bool IsWritable(this MemberInfo mi) {
      if (mi is PropertyInfo) {
        return (mi as PropertyInfo).CanWrite;
      } else if (mi is FieldInfo) {
        return true;
      } else {
        return false;
      }
    }

    public static bool IsReadable(this MemberInfo mi) {
      if (mi is PropertyInfo) {
        return (mi as PropertyInfo).CanRead;
      } else if (mi is FieldInfo) {
        return true;
      } else {
        return false;
      }
    }

    /// <summary>
    /// Returns a collection of all subclasses of the given class defined
    /// within THE SAME assembly that the given type itself
    /// </summary>
    /// <param name="type">type examined.</param>
    /// <returns></returns>
    public static IEnumerable<Type> GetSubclasses(this Type type) {
      return from t in type.Assembly.GetTypes()  
             where t.IsSubclassOf(type)
             select t;
    }

    public static IEnumerable<T> EnumerateOnProperties<T>(this object obj) {
      var members = obj.GetType().GetFieldsAndProperties();

      return from member in members
             where member.IsReadable()
             select (T) member.GetValue(obj);
    }

    public static Type GetCommonType(ICollection<Type> types) {
      if (types.Count == 1)
        return types.First();

      int integers = types.Where(t => typeof(Int32).IsAssignableFrom(t))
          .Count();

      if (integers == types.Count)
        return typeof(Int32);

      int floats = types.Where(t => typeof(Double).IsAssignableFrom(t))
          .Count();

      if (floats + integers == types.Count)
        return typeof(Double);

      return null;
    }

    public static R Cast<R>(object obj) {
      return (R) obj;
    }

    public static object Cast(object obj, Type type) {
      if (obj.GetType() == type) {
        return obj;
      } else if (obj.GetType().IsPrimitive && type.IsPrimitive) {
        return Convert.ChangeType(obj, type, CultureInfo.InvariantCulture); // check
      } else {
        var method = typeof(ReflectionTools).GetMethod("Cast",
            new Type [] { typeof(object) });
        return method.MakeGenericMethod(type).Invoke(null, new object [] { obj });
      }
    }

    public static bool IsSerializable(this Type type) {
#if PocketPC
      return type.HasAnyAttribute(typeof(SerializableAttribute));
#else
      return type.IsSerializable;
#endif
    }

    public static T GetDefault<T>() {
      return default(T);
    }

    public static object GetDefaultValue(this Type type) {
      if (type == typeof(void))
        return null;
      return typeof(ReflectionTools).GetMethod("GetDefault").MakeGenericMethod(
          type).Invoke(null, new object [0]);
    }
  }
}
