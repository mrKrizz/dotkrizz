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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;

namespace dotkrizz {
  public static class VariousTools {
    public static string GenerateCode(this CodeDomProvider provider, 
                                      CodeCompileUnit unit) {
      var writer = new StringWriter();

      provider.GenerateCodeFromCompileUnit(unit, writer, 
          new CodeGeneratorOptions() {
            BlankLinesBetweenMembers = true,
            BracingStyle = "block",
            ElseOnClosing = true,
            IndentString = "  ",
           VerbatimOrder = true
      });

      return writer.ToString();
    }

    public static StringCollection ToStringCollection(this string [] arr) {
      var res = new StringCollection();
      res.AddRange(arr);
      return res;
    }

    public static Type GenerateClass(this CodeDomProvider provider,
                                     CodeTypeDeclaration type_decl,
                                     GenerateClassOptions options) {
      options = options ?? new GenerateClassOptions();
      
      var @namespace = new CodeNamespace("DynamicClassNamespace");

      @namespace.Types.Add(type_decl);
      @namespace.Imports.AddRange(options.imports.Select(
          im => new CodeNamespaceImport(im)).ToArray());

      var compile_unit = new CodeCompileUnit();
      compile_unit.Namespaces.Add(@namespace);

      var compiler_options = new CompilerParameters() {
        GenerateExecutable = false,
        GenerateInMemory = true,
        IncludeDebugInformation = false,
      };

      compiler_options.ReferencedAssemblies.AddRange(options.assemblies);
      compiler_options.ReferencedAssemblies.Add(
          Assembly.GetExecutingAssembly().Location);
      compiler_options.ReferencedAssemblies.Add(
          Assembly.GetCallingAssembly().Location);
      compiler_options.ReferencedAssemblies.Add(
          Assembly.GetEntryAssembly().Location);

      var res = provider.CompileAssemblyFromDom(compiler_options, compile_unit);

      if (!res.Errors.HasErrors) {
        return res.CompiledAssembly.GetType(
            @namespace.Name + "." + type_decl.Name, true);
      } else {
        throw new CompilationErrors(res.Errors);
      }
    }

    public static string BuildErrorMsg(this CompilerError error) {
      return String.Format("{0}, {1} - Error {2}: {3}", error.Line,
          error.Column, error.ErrorNumber, error.ErrorText);
    }

    public static string BuildErrorMsg(this CompilerErrorCollection errors) {
      var res = new StringBuilder();

      foreach (CompilerError error in errors) {
        res.Append(error.BuildErrorMsg());
        res.Append("\n");
      }

      return res.ToString();
    }
  }

  public class GenerateClassOptions {
    public string [] imports = new string[0];
    public string [] assemblies = new string [0];
  }

  public class CompilationErrors : Exception {
    public CompilationErrors(CompilerErrorCollection errors)
        : base(errors.BuildErrorMsg()) {
      Errors = errors;
    }

    public CompilerErrorCollection Errors {
      get;
      private set;
    }

  }
}
