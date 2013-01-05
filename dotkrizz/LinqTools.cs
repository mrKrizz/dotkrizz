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
using System.Collections;

namespace dotkrizz {
  public static class LinqTools {
    public static T MaxBy<T, S>(this IEnumerable<T> collection,
                                Func<T, S> selector) {
      var comparer = Comparer<S>.Default;
      T result = collection.First();
      foreach (T element in collection) {
        if (comparer.Compare(selector(element), selector(result)) > 0)
          result = element;
      }
      return result;
    }

    public static T MinBy<T, S>(this IEnumerable<T> collection,
                                Func<T, S> selector) {
      var comparer = Comparer<S>.Default;
      T result = collection.First();
      foreach (T element in collection) {
        if (comparer.Compare(selector(element), selector(result)) < 0)
          result = element;
      }
      return result;
    }

    public static T MaxOrDefault<T>(this IEnumerable<T> collection) {
      var enumerable_extensions = typeof(System.Linq.Enumerable);
      var max_method = enumerable_extensions.GetMethod("Max",
          new Type[] { collection.GetType() });
      try {
        return (T)max_method.Invoke(null, new object[] { collection });
      } catch (TargetInvocationException e) {
        if (e.InnerException != null 
            && e.InnerException is InvalidOperationException)
          return default(T);
        else
          throw;
      }
    }

    public static IEnumerable<int> UpTo(this int start, int stop, int step) {
      for (int i = start; i < stop; i += step)
        yield return i;
    }

    public static IEnumerable<int> UpTo(this int start, int stop) {
      for (int i = start; i < stop; i++)
        yield return i;
    }

    public delegate T GeneratorFunc<T>(T first, T prev);

    public static IEnumerable<T> GenerateSet<T>(this T start,
                                                GeneratorFunc<T> next_generator) {
      var current = start;
      while (true) {
        yield return current;
        try {
          current = next_generator(start, current);
        } catch (StopGenerateSet) {
          yield break;
        }
      }
    }

    public static IEnumerable<T> GenerateSetUntilDefault<T>(this T start,
           GeneratorFunc<T> next_generator) {

      for (var current = start; current != null && !current.Equals(default(T));
           current = next_generator(start, current)) {
        yield return current;
      }

    }

  /*  public static IEnumerable<T> SetSum<T>(this IEnumerable<T> first_set,
                                           params IEnumerable<T>[] sets) {
      foreach (var element in first_set)
        yield return element;
      foreach (var @set in sets) {
        foreach (var element in @set)
          yield return element;
      }
    }  */

    public static IEnumerable<KeyValuePair<int, T>> ToNumberedSet<T>
                                              (this IEnumerable<T> enumerable) {
      return enumerable.Select((x, i) => new KeyValuePair<int, T>(i, x));
    }

    public static IEnumerable<T> EveryNth<T>(this IEnumerable<T> enumerable,
                                             int step) {
      var enumerator = enumerable.GetEnumerator();

      for (int i = 0; enumerator.MoveNext(); i++) {
        if (i % step == 0)
          yield return enumerator.Current;
      }
    }

    public static IEnumerable<T> QueryOn<T>(this string query,
                                            params IEnumerable[] placeholders) {
      return new LinqCompiler(query).EvaluateMany<T>(placeholders);
    }

    public static string PrintOut<T>(this IEnumerable<T> enumerable, 
                                     string format, string separator,
                                     int per_fragment, string header,
                                     string footer) {
      var res = new StringBuilder(header ?? "");

      var length = enumerable.Count();
      var length_regular = length - length % per_fragment;
      var excess = length - length_regular;

      for (int i = 0; i < length_regular; i += per_fragment) {
        res.Append(String.Format(format, enumerable.Skip(i).Take(per_fragment)
            .Cast<object>().ToArray()));
        res.Append(separator);
      }

      if (excess > 0) {
        var arr = enumerable.Skip(length_regular).Take(excess).Cast<object>();
        arr = arr.Concat(("" as object).GenerateSet((a, b) => "").Take(per_fragment));
        Console.WriteLine(String.Format("\n XXXXXXX {0}\n", arr.Count()));
        res.Append(String.Format(format, arr.ToArray()));
      } else {
        res.Remove(res.Length - separator.Length, separator.Length);
      }

      res.Append(footer ?? "");

      return res.ToString();
    }

    public static string PrintOut<T>(this IEnumerable<T> enumerable, 
                                     string format, string separator,
                                     int per_fragment) {
      return PrintOut<T>(enumerable, format, separator, per_fragment, null, null);
    }

    public static string PrintOut<T>(this IEnumerable<T> enumerable,
                                     string format, string separator) {
      return PrintOut<T>(enumerable, format, separator, null, null);
    }

    public static string PrintOut<T>(this IEnumerable<T> enumerable, 
                                     string format, string separator,
                                     string header, string footer) {
      string body = enumerable.Aggregate("",
          (all, current) => all + String.Format(format, current) + separator);

      return (header ?? "") + body.Substring(0, body.Length - separator.Length) 
          + (footer ?? "");
    }
  }

  public class StopGenerateSet : Exception {
    public StopGenerateSet() {
    }
  }
}
