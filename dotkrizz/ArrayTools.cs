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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Reflection;

// This module provides some auxiliary array functions 

namespace dotkrizz {
  public class Array<T> : IEnumerable<T>, ICollection<T>, IList<T>, ICloneable {
    public static T[] New(params T[] array) {
      return array;
    }

    public Array(params T[] array) {
      internal_ = array;
      IsSorted = false;
    }

    public T[] ToNativeArray() {
      IsSorted = false;
      return internal_;
    }

    public ReadOnlyCollection<T> ToReadOnly() {
      return Array.AsReadOnly(internal_);
    }

    public static implicit operator Array<T>(T[] array) {
      return new Array<T>(array);
    }

    public void Sort() {
      if (IsSorted)
        return;
      Array.Sort<T>(internal_);
      IsSorted = true;
    }

    public bool IsSorted {
      get;
      private set;
    }

    private T[] internal_;
    private bool sorted_;

    #region IEnumerable<T> Members

    public IEnumerator<T> GetEnumerator() {
      return Array.AsReadOnly(internal_).GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator() {
      return (internal_ as IEnumerable).GetEnumerator();
    }

    #endregion

    #region ICollection<T> Members

    public void Add(T item) {
      (internal_ as ICollection<T>).Add(item);
      CheckIfSorted(internal_.Length - 1);
    }

    public void Clear() {
      IsSorted = true;
      (internal_ as ICollection<T>).Clear();
    }

    public bool Contains(T item) {
      if (IsSorted)
        return internal_.BSContains(item);
      else
        return (internal_ as ICollection<T>).Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex) {
      Array.Copy(internal_, 0, array, arrayIndex, internal_.Length); 
    }

    public int Count {
      get {
        return internal_.Length;
      }
    }

    public bool IsReadOnly {
      get {
        return false;
      }
    }

    public bool Remove(T item) {
      return (internal_ as ICollection<T>).Remove(item);
    }

    #endregion

    #region IList<T> Members

    public int IndexOf(T item) {
      if (IsSorted)
        return Array.BinarySearch<T>(internal_, item);
      else
        return (internal_ as IList<T>).IndexOf(item);
    }

    public void Insert(int index, T item) {
      (internal_ as IList<T>).Insert(index, item);
      CheckIfSorted(index);
    }

    public void RemoveAt(int index) {
      (internal_ as IList<T>).RemoveAt(index);
    }

    public T this[int index] {
      get {
        if (index < 0 || index > internal_.Length)
          throw new IndexOutOfRangeException();
        return internal_[index];
      }
      set {
        if (index < 0 || index > internal_.Length)
          throw new IndexOutOfRangeException();
        internal_[index] = value;
        CheckIfSorted(index);
      }
    }

    #endregion

    #region ICloneable Members

    public object Clone() {
      T[] copied = new T[internal_.Length];
      Array.Copy(internal_, copied, internal_.Length);
      return new Array<T>(copied);
    }

    #endregion

    private void CheckIfSorted(int index) {
      T value = internal_[index];
      if (IsSorted) {
        var comparer = Comparer<T>.Default;
        if (index == 0)
          IsSorted = internal_.Length <= 1 || comparer.Compare(internal_[1], value) >= 0;
        else if (index == internal_.Length - 1)
          IsSorted = comparer.Compare(internal_[index - 1], value) <= 0;
        else
          IsSorted = comparer.Compare(internal_[index - 1], value) <= 0
              && comparer.Compare(internal_[index + 1], value) >= 0;
      }
    }
  }

  public static class ArrayTools {
    public static bool BSContains<T>(this T[] array, T value) {
      int res = Array.BinarySearch<T>(array, value);

      return res >= 0 && res < array.Length;
    }

    public static ReadOnlyCollection<T> ToReadOnly<T>(this T[] array) {
      return Array.AsReadOnly(array);
    }

    public static T[] ToSorted<T>(this T[] array) {
      var copy = (T[]) array.Clone();
      Array.Sort<T>(copy);
      return copy;
    }
  }
}
