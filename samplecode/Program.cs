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
using dotkrizz;
using dotkrizz.xml_deserializer;
using dotkrizz.web;
using System.Windows.Forms;

namespace TestDemo {
  public class X {
    public int x;
  }

  public struct Y {
    public int y;
  }

  public interface ISth {
    void DoSth(int x);
    int sum(int x, int y);
    int mult(int x, int y);

    double prop {
      get;
      set;
    }
  }

  public class SomeClass : IDynamicMethodDispatcher, IDynamicPropertyAccessor {
    public void DoSth(int x) {
      MessageBox.Show(x.ToString()); 
    }

    public int sum(int a, int b) {
      return a + b;
    }

    #region IDynamicMethodDispatcher Members

    public object Invoke(Type interface_type, string method_name,
                         object [] arguments) {
      if (method_name == "mult") {
        return (int) arguments [0] * (int) arguments [1];
      } else {
        throw new NotImplementedException();
      }
    }

    #endregion

    #region IDynamicPropertyAccessor Members

    public void SetProperty(Type interface_type, string property_name, object value) {
      if (property_name == "prop")
        MessageBox.Show(String.Format("Value set: {0}!", value));
    }

    public object GetProperty(Type interfave_type, string property_name) {
      throw new NotImplementedException();
    }

    #endregion
  }

  public static class Program {
    static void Main(string[] args) {
      var cls = new SomeClass();
      ISth intf = ClassAdapter.Adapt<ISth>(cls);

      intf.DoSth(intf.sum(10, intf.mult(20, 5)));

      intf.prop = 102;

      ClassAdapter.Adapt<ISth>(new {
        DoSth = (Action<int>) delegate(int x) {
          MessageBox.Show("Yoohooo " + x);
        }
      }).DoSth(0);

      var delimpl = ClassAdapter.AdaptDelegate<ISth>((name, a) => {
        MessageBox.Show("Invoked method " + name);
        return null;
      });

      delimpl.DoSth(10);
      delimpl.mult(10, 20);

      var str = "hello";

      var sz = str.GetProperty<int>("Length");

      Console.WriteLine(String.Format("hello size: {0}", sz));

      str = "<b> Hello <i>World</i> </b>";

      Console.Write("Before: ");
      Console.Write(str);

      var res = str.StripHtml();

      Console.Write(" After: ");
      Console.WriteLine(res);




      var filter = "Text file (*.txt)|*.txt|whatever |*.rtf";

      bool r1 = FileTools.ValidateByFilter(filter, "me.txt");
      bool r2 = FileTools.ValidateByFilter(filter, "ku.rtf");
      bool r3 = FileTools.ValidateByFilter(filter, "me.txt", "ka.rtf");
      bool r4 = FileTools.ValidateByFilter(filter, "me.txa", "ka.txt");

      Console.WriteLine(String.Format("r1: {0} r2: {1} r3: {2} r4: {3}", r1, r2, r3, r4));


      var foo = new X();

      foo.SetProperty("x", 10);
      Console.WriteLine(foo.x);


      var foo2 = new Y();
      foo2.y = 211;

      var zzz = foo2.GetProperty<int>("y");

      Console.WriteLine(zzz);


      var one_to_hundred = 0.UpTo(100).ToArray();

      Console.WriteLine(one_to_hundred.PrintOut("{0}", ",", "1-100: ", ";"));

      
    /*  var ints = new int[] { 5, 4, 1, 22, 55, 2 };
      var doubles = new double[] { 1.2, 5, 7, 3, 5, 6 };
      var res5 = @"from a in {0 : int}
                   from b in {1}
                   select new {
                     a,
                     b,
                     sum = a + b
                   }".QueryOn<double>(ints, doubles);   */


      var linq = new LinqCompiler("from t in src where t > val select t * 2");
      linq["val"] = 6;
      linq["src"] = new int[] { 5, 2, 7, 8, 1, 13, 27 };

      var ress = linq.EvaluateMany<int>();

      Console.WriteLine(ress.PrintOut("{0}", ";"));

      linq["src"] = new double[] { 5, 2.3, 7.1, 8, 1, 13, 27 };

      var ress2 = linq.EvaluateMany<double>();

      var linq2 = new LinqCompiler("Math.Sqrt(x)");
      linq2["x"] = 25;

      var rez = linq2.EvaluateSingle<int>();

     /* var linq3 = new LinqCompiler("from x in {0} select x");
      var res3 = linq3.EvaluateMany<double>(new double[] { 2, 3, 4 });

      var res3a = linq3.EvaluateMany<string>((IEnumerable<string>)new string [] {"dupa", "plecy"});

      var linq4 = new LinqCompiler("from x in {0} select x.ToString()");

      var res4 = linq4.EvaluateMany<string>(0.UpTo(10));*/


      ////////////////////////////////

      Number n1 = 3;
      Number n2 = 5.5;
      Number n3 = "5.8";
      Number n4 = "3";
      Number n5 = Number.Null;

      string str_n3 = n3;
      uint uint_n4 = n4;

      var test = n1 == n4;

      var sum_n = n4 + n2;

      var diff_n = n3 - 0.8;

      var mult = n1 * n4;

      var compr = n3 > n4 & n3 > n4;
      var comp2 = n2 < n1;
    }
  }
}
