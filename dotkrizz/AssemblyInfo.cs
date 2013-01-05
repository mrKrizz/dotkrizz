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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

// This module lets you access some information stored normally in autogeneated
// AssemblyInfo.cs file through assembly attributes. 

namespace dotkrizz {
  public static class AssemblyInfo {
    public static string AssemblyTitle {
      get {
        return Assembly.GetCallingAssembly()
            .GetSingleAttributePropertyValueOrNull<AssemblyTitleAttribute, string>(
            "Title") ?? Path.GetFileNameWithoutExtension(Assembly.GetCallingAssembly().GetName()
            .CodeBase);
      }
    }

    public static string AssemblyVersion {
      get {
        return Assembly.GetCallingAssembly().GetName().Version.ToString();
      }
    }

    public static string AssemblyDescription {
      get {
        return Assembly.GetCallingAssembly()
            .GetSingleAttributePropertyValueOrNull<AssemblyDescriptionAttribute, string>(
            "Description") ?? "";
      }
    }

    public static string AssemblyProduct {
      get {
        return Assembly.GetCallingAssembly()
           .GetSingleAttributePropertyValueOrNull<AssemblyProductAttribute, string>(
           "Product") ?? "";
      }
    }

    public static string AssemblyCopyright {
      get {
        return Assembly.GetCallingAssembly()
           .GetSingleAttributePropertyValueOrNull<AssemblyCopyrightAttribute, string>(
           "Copyright") ?? "";
      }
    }

    public static string AssemblyCompany {
      get {
        return Assembly.GetCallingAssembly()
           .GetSingleAttributePropertyValueOrNull<AssemblyCompanyAttribute, string>(
           "Company") ?? "";
      }
    }
  }
}
