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

namespace dotkrizz {
  public class Pair<T1, T2> : IEquatable<Pair<T1, T2>>,
                              IComparable<Pair<T1, T2>> {
    public T1 first {
      get;
      set;
    }

    public T2 second {
      get;
      set;
    }

    public Pair() {
      this.first = default(T1);
      this.second = default(T2);
    }

    public Pair(T1 first, T2 second) {
      this.first = first;
      this.second = second;
    }

    public static Pair<E1, E2> Make<E1, E2>(E1 first, E2 second) {
      return new Pair<E1, E2>(first, second);
    }

    public static Pair<E, E> Make<E>(E [] array) {
      if (array == null || array.Length > 2 || array.Length == 0)
        throw new ArgumentException();
      return new Pair<E, E>(array [0], array.Length == 2 ? array [1] : default(E));
    }

    public override bool Equals(object obj) {
      if (obj is Pair<T1, T2>)
        return (this as IEquatable<Pair<T1, T2>>).Equals(obj as Pair<T1, T2>);
      else
        return false;
    }

    public override int GetHashCode() {
      try {
        return first.GetHashCode() ^ second.GetHashCode();
      } catch (NullReferenceException) {
        if (first != null)
          return first.GetHashCode();
        else if (second != null)
          return second.GetHashCode();
        else
          return 0;
      }
    }

    #region IEquatable<Pair<T1,T2>> Members

    bool IEquatable<Pair<T1, T2>>.Equals(Pair<T1, T2> other) {
      return EqualityComparer<T1>.Default.Equals(first, other.first)
          && EqualityComparer<T2>.Default.Equals(second, other.second);
    }

    #endregion

    #region IComparable<Pair<T1,T2>> Members

    int IComparable<Pair<T1, T2>>.CompareTo(Pair<T1, T2> other) {
      int first_result = Comparer<T1>.Default.Compare(first, other.first);
      int second_result = Comparer<T2>.Default.Compare(second, other.second);
      return (first_result * 100 / int.MaxValue) * 100 
          + (second_result * 100 / int.MaxValue);
    }

    #endregion
  }

  public class Pair : Pair<object, object> {
  }
}
