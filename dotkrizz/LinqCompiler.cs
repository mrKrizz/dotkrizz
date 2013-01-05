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
using System.Text.RegularExpressions;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using Microsoft.CSharp;
using System.Collections;

// Linq compiler, let's you provide the queries as a string
// in C#-dialect linq

namespace dotkrizz {
  public class LinqCompiler {
    private const string kExecuteMethodName = "Execute";
    private const string kQueryBoxClassName = "QueryBox";
    private const string kQueryPropertyName = "Query";
    private static readonly string kQueryBoxNamespace
        = typeof(LinqCompiler).Namespace + ".LinqCompiler";
    private const string kPlaceholderTypeParameter = "TPlaceholder{0}";
    private const string kPlaceholderScalarTypeParameter = "TPlaceholder{0}_Scalar";
    private const string kPlaceholderParameter = "placeholder_{0}";

    private static Dictionary<string, object> cache_
        = new Dictionary<string, object>();

    private IDictionary<string, object> parameters_
        = new Dictionary<string, object>();

    public object this[string key] {
      get {
        return parameters_[key];
      }
      set {
        parameters_[key] = value;
        query_box_ = null;
      }
    }

    public IList<Assembly> ExternalAssemblies {
      get;
      private set;
    }

    public string Query {
      get {
        if (query_box_ != null)
          return query_box_.GetProperty<string>(kQueryPropertyName);
        else
          return query_text_;
      }
      set {
        query_text_ = value;
        query_box_ = null;
      }
    }

    public LinqCompiler() {
      ExternalAssemblies = new List<Assembly>();
    }

    public LinqCompiler(string query) {
      ExternalAssemblies = new List<Assembly>();
      query_text_ = query;
    }

    public object EvaluateSingle(params IEnumerable[] placeholders) {
      return Evaluate<object>(placeholders);
    }

    public T EvaluateSingle<T>(params IEnumerable[] placeholders) {
      return Evaluate<T>(placeholders);
    }

    public IEnumerable EvaluateMany(params IEnumerable[] placeholders) {
      return Evaluate<IEnumerable>(placeholders);
    }

    public IEnumerable<T> EvaluateMany<T>(params IEnumerable[] placeholders) {
      return Evaluate<IEnumerable<T>>(placeholders);
    }

    protected T Evaluate<T>(IEnumerable[] placeholders) {
      var res = Evaluate(placeholders);

      if (res == null)
        return default(T);
      else if (typeof(T).IsAssignableFrom(res.GetType()))
        return (T)res;
      else
        return (T)Convert.ChangeType(res, typeof(T));
    }

    protected object Evaluate(IEnumerable[] placeholders) {
      if (query_box_ == null) {
        lock (cache_) {
          if (cache_.ContainsKey(QueryKey)) {
            query_box_ = cache_[QueryKey];
          } else {
            var results = Compile();

            var query_box_type = results.CompiledAssembly
                .GetType(kQueryBoxNamespace + "." + kQueryBoxClassName);

            query_box_ = Activator.CreateInstance(query_box_type);

            cache_.Add(QueryKey, query_box_);
          }
        }
      }

      foreach (var value in parameters_) {
        query_box_.SetProperty(value.Key, value.Value);
      }

      var type_parameters = placeholders.SelectMany(ph => new Type[] {
          ph.GetType(), ph.GetType().GetScalarType()
      }).ToArray();

      return query_box_.CallGeneric(kExecuteMethodName, type_parameters,
          placeholders.Cast<object>().ToArray());
      
      // pass {0} like arguments
    }

    private object query_box_ = null;
    private string query_text_ = "";

    private string QueryKey {
      get {
        var res = query_text_;
        foreach (var param in parameters_) {
          res += "%";
          res += param.Key;
          res += ":";
          res += param.Value.GetType().FullName;
        }
        return res;
      }
    }


    private CompilerResults Compile() {
      var provider = new CSharpCodeProvider(new Dictionary<string, string>() {
        { "CompilerVersion", "v3.5" }
      });

      var code_dom = BuildCodeDom();

      var compiler_params = new CompilerParameters((new string[]{
                "System.dll",
                "System.Core.dll",
                Assembly.GetExecutingAssembly().Location
            }.Union(ExternalAssemblies.Select(a => a.Location))).ToArray());

      compiler_params.CompilerOptions = "/t:library";
      compiler_params.GenerateInMemory = true;

      var results = provider.CompileAssemblyFromDom(compiler_params,
          new CodeCompileUnit[] { code_dom });

      if (results.Errors.HasErrors) {
        throw new LinqCompilerCompilationErrors(results.Errors.OfType<CompilerError>());
      }

      return results;
    }

    private static readonly Regex placeholder_re
        = new Regex(@"\{([0-9]+) *(: *([A-Za-z0-9.]*))?\}",
            RegexOptions.Compiled | RegexOptions.Singleline);

    public string[] PlaceholderTypes {
      get {
        var placeholders = from Match m in placeholder_re.Matches(Query)
                           from g in m.Groups.OfType<Group>().ToNumberedSet()
                           group g by m into y
                           let t = (from tx in y
                                    select tx.Value.Value).ToArray()
                           let w = new {
                             index = t[1],
                             type = t.Length > 3 && t[3] != "" ? t[3] : "object"
                           }
                           group w by w.index into z
                           orderby z.Key
                           select z.First().type;
        return placeholders.ToArray();
      }
    }

    public int NumberOfPlaceholders {
      get {
        return PlaceholderTypes.Length;
      }  
    }

    private CodeCompileUnit BuildCodeDom() {
      var @class = new CodeTypeDeclaration(kQueryBoxClassName) {
        TypeAttributes = TypeAttributes.Class | TypeAttributes.Sealed
                       | TypeAttributes.Public,

      };

      @class.Members.AddRange(parameters_.Select(value => new CodeMemberField(
          value.Value.GetType(), value.Key) {
            Attributes = MemberAttributes.Public
          }).ToArray());

      var execute_method = new CodeMemberMethod() {
        Attributes = MemberAttributes.Public | MemberAttributes.Final,
        ReturnType = new CodeTypeReference(typeof(object)),
        Name = kExecuteMethodName,
      };

      var query_body = Query;

      for (int i = 0; i < NumberOfPlaceholders; i++) {
        var vector_placeholder = new CodeTypeParameter(String.Format(
            kPlaceholderTypeParameter, i));
        var scalar_placeholder = new CodeTypeParameter(String.Format(
            kPlaceholderScalarTypeParameter, i));
        var placeholder_parameter = String.Format(kPlaceholderParameter, i);

        scalar_placeholder.Constraints.Add(
            new CodeTypeReference(" " + PlaceholderTypes[i]));

        vector_placeholder.Constraints.Add(new CodeTypeReference("IEnumerable",
            new CodeTypeReference(scalar_placeholder.Name)));

        execute_method.TypeParameters.AddRange(new CodeTypeParameter[] {
           vector_placeholder, scalar_placeholder });
        execute_method.Parameters.Add(new CodeParameterDeclarationExpression(
            vector_placeholder.Name, placeholder_parameter));
      }

      query_body = placeholder_re.Replace(query_body,
          m => String.Format(kPlaceholderParameter, m.Groups[1]));

      execute_method.Statements.Add(new CodeMethodReturnStatement(
          new CodeSnippetExpression(query_body)));

      @class.Members.Add(execute_method);

      var query_property = new CodeMemberProperty() {
        Attributes = MemberAttributes.Final | MemberAttributes.Public,
        HasGet = true,
        HasSet = false,
        Type = new CodeTypeReference(typeof(string)),
        Name = kQueryPropertyName
      };

      query_property.GetStatements.Add(
          new CodeMethodReturnStatement(
            new CodePrimitiveExpression(
              Query
            )
          ));


      @class.Members.Add(query_property);

      var @namespace = new CodeNamespace(kQueryBoxNamespace);
      @namespace.Imports.AddRange((new string[] {
        "System", "System.Linq", "System.Collections", "System.Collections.Generic"
      }).Select(n => new CodeNamespaceImport(n)).ToArray());
      @namespace.Types.Add(@class);

      var compile_unit = new CodeCompileUnit();
      compile_unit.Namespaces.Add(@namespace);

      return compile_unit;
    }


  }

  class LinqCompilerException : Exception {
    public LinqCompilerException(string msg)
      : base(msg) {
    }
  }

  class LinqCompilerCompilationErrors : LinqCompilerException {
    public LinqCompilerCompilationErrors(IEnumerable<CompilerError> errors)
      : base("An error occured while compiling the LINQ query: \n") {
      errors_ = errors;
    }

    IEnumerable<CompilerError> errors_;

    public override string Message {
      get {
        return base.Message + GetErrorMessages();
      }
    }

    private string error_messages_ = null;

    private string GetErrorMessages() {
      if (error_messages_ == null) {

        var builder = new StringBuilder();

        foreach (var error in errors_) {
          builder.AppendFormat("line {0}, {1}: {2}", error.Line, error.Column,
              error.ErrorText);
        }

        error_messages_ = builder.ToString();
      }

      return error_messages_;
    }
  }
}
