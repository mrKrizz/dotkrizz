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
using System.Text;
using System.Reflection;

// Certain EqualityComparer subclasses.
// - based on Comparison<T> delegate
// - based on direct comparision of given field in object

namespace dotkrizz {
  public static class ComparisionTools {
    public static IEqualityComparer<T> ToEqualityComparer<T>(
        this Comparison<T> comparison) {
      return new ComparisionBasedComparer<T>(comparison);
    }
  }

  public class ComparisionBasedComparer<T> : EqualityComparer<T> {
    public ComparisionBasedComparer(Comparison<T> comparision) {
      comparision_ = comparision;
    }

    public override bool Equals(T x, T y) {
      return comparision_(x, y) == 0;
    }

    public override int GetHashCode(T obj) {
      return obj.GetHashCode();
    }

    private Comparison<T> comparision_;
  }

  public class ByFieldComparer<T> : EqualityComparer<T> {
    public ByFieldComparer(MemberInfo field) {
      field_ = field;
    }

    public ByFieldComparer(string field_name) {
      var field_ = typeof(T).GetFieldOrProperty(field_name);
      if (field_ == null)
        throw new ArgumentException(String.Format(
            "{0} is not a valid name of {1}'s field or property!", field_name,
            typeof(T)), "field_name");
    }

    public override bool Equals(T x, T y) {
      object x_field = field_.GetValue(x);
      object y_field = field_.GetValue(y);
      return x_field == y_field;
    }

    public override int GetHashCode(T obj) {
      return field_.GetValue(obj).GetHashCode();
    }

    private MemberInfo field_;
  }
}
