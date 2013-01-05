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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// Certain file-related auxiliaries - validating file list by filter like *.txt,
// incrementing files like file0.txt,file1.txt,file2.txt

namespace dotkrizz {
  public static class FileTools {
    public static bool ValidateByFilter(string filter, params string[] files) {
      var defs = filter.Split('|').Where((x, i) => i % 2 == 1);

      var extensions = from def in defs
                       from wildchar in def.Split(';', ',')
                       let ext = Path.GetExtension(wildchar.Trim())
                       where ext != ".*"
                       orderby ext
                       select ext;

      var extensions_table = extensions.ToArray();
     
      foreach (string file in files) {
        if (!extensions_table.BSContains(Path.GetExtension(file)))
          return false;
      }
      return true;
    }

    public static bool ValidateByFilter(string filter, params FileInfo[] files) {
      return ValidateByFilter(filter, files.Select(fi => fi.Name).ToArray());
    }
    
    private static Regex counter_regex = new Regex(@"\(([0-9]*)\)$",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static FileInfo IncrementDuplicateCounterIfNeeded(this FileInfo file,
                                                             int max_counter) {
      if (!file.Exists)
        return file;

      string name_no_ext = file.Name.RemoveSuffix(file.Extension).TrimEnd();
      var counter_match = counter_regex.Match(name_no_ext);

      int counter = 0;
      if (counter_match.Success) {
        string counter_str = counter_match.Groups[1].Value;
        counter = Convert.ToInt32(counter_str);
        name_no_ext.Remove(name_no_ext.Length - counter_str.Length - 2);
      }

      do {
        counter++;
        var curr_file = new FileInfo(Path.Combine(file.DirectoryName,
            String.Format("{0}({1}){2}", name_no_ext, counter, file.Extension)));
        if (!curr_file.Exists)
          return curr_file;
      } while (counter < max_counter);

      return null;
    }
  }
}
