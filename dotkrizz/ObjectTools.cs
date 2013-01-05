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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace dotkrizz {
  public static class ObjectTools {
    /// <summary>
    /// Assigns all the properties in current object from another objects.
    /// Only properties and fields that exist in both source object
    /// and destination object are, obviously, copied
    /// </summary>
    public static void AssignFrom(this object destination, object source, bool skip_unassignable_types) {
      Type dest_type = destination.GetType();
      Type source_type = source.GetType();

      var matching_fields = from d in dest_type.GetFieldsAndProperties()
                            join s in source_type.GetFieldsAndProperties()
                            on d.Name equals s.Name
                            where d.IsWritable() && s.IsReadable()
                            select new {
                              source = s,
                              destination = d
                            };

      foreach (var field in matching_fields) {
        try {
          field.destination.SetValueParsed(destination, field.source.GetValue(source));
        } catch (ArgumentException e) {
          if (!skip_unassignable_types)
            throw e;
        }
      }
    }

    public static void AssignFrom(this object destination, object source) {
      AssignFrom(destination, source, true);
    }

    public static object GetProperty(this object obj, string property) {
      return GetProperty<object>(obj, property);
    }

    public static E GetProperty<E>(this object obj, string property) {
      E result = default(E);

      DoActionOnProperty(obj, property, (property_info, @object) => {
        object value = property_info.GetValue(@object);

        if (value.GetType() == typeof(E)) {
          result = (E) value;
        } else if (typeof(E) == typeof(string)) {
          result = (E)(object)value.ToString();
        } else {
          result = (E) Convert.ChangeType(value, typeof(E));
        }
      });

      return result;
    }

    public static void SetProperty<T>(this T obj, string property, object value)
                                  where T : class {
      var value_to_set = value;

      DoActionOnProperty(obj, property, (property_info, @object) => {
        if (value_to_set != null) {
          var member_type = property_info.GetMemberType();
          if (member_type == typeof(string))
            value_to_set = value.ToString();
          else if (!member_type.IsAssignableFrom(value.GetType()))
            value_to_set = Convert.ChangeType(value, member_type);
        }

        property_info.SetValue(@object, value_to_set);
      });
    }

    public static object Call(this object obj, string method, params object [] @params) {
      object result = null;

      NavigateToMember(obj, method, (name, @object) => {
        var mi = @object.GetType().GetMethod(name);
        result = mi.Invoke(@object, @params);
      });

      return result;
    }

    public static object CallGeneric(this object obj, string method, 
                                     Type [] type_params,
                                     params object[] @params) {
      if (type_params == null || type_params.Length == 0)
        return Call(obj, method, @params);

      object result = null;

      NavigateToMember(obj, method, (name, @object) => {
        var mi_generic = @object.GetType().GetMethod(name);
        var mi = mi_generic.MakeGenericMethod(type_params);
        result = mi.Invoke(@object, @params);
      });

      return result;
    }

    public static void DoActionOnProperty(object obj, string property,
                                          Action<MemberInfo, object> action) {
      NavigateToMember(obj, property, (name, @object) => {
        var prop = obj.GetType().GetFieldOrProperty(name);
        action(prop, @object);
      });
    }

    public static void NavigateToMember(object obj, string member,
                                        Action<string, object> action) {
      if (obj == null)
        throw new ArgumentNullException();

      var path = member.Trim().Split('.');

      if (path.Length == 0)
        throw new ArgumentException();

      if (path.Length == 1) {
        action(path.First(), obj);
      } else {
        var prop = obj.GetType().GetFieldOrProperty(path.First());
        var new_obj = prop.GetValue(obj);
        NavigateToMember(new_obj, String.Join(".", path.Skip(1).ToArray()), action);
      }
    }

    public static T Unnullate<T>(this T? nullable, T value_for_null) where T : struct {
      return nullable.HasValue ? nullable.Value : value_for_null;
    }

    public static T Unnullate<T>(this T? nullable) where T : struct {
      return nullable.HasValue ? nullable.Value : default(T);
    }
  }
}
