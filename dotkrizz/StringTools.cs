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
using System.Text;
using System.Text.RegularExpressions;

namespace dotkrizz {
  public static class StringTools {
    public static string RemovePrefix(this String value,
        string prefix) {
      if (value.StartsWith(prefix)) {
        return value.Remove(0, prefix.Length);
      } else {
        return value;
      }
    }

    public static string RemoveSuffix(this String value,
        string suffix) {
      if (value.EndsWith(suffix)) {
        int start = value.LastIndexOf(suffix);
        return value.Remove(start, value.Length - start);
      } else {
        return value;
      }
    }

    public static string Capitalize(this string str) {
      return Char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    public static string ToTitleCase(this string str) {
      return String.Join(" ", str.Split(' ').Select(s => s.Capitalize()).ToArray());
    }

    public static bool IsAlnum(char c) {
      return Char.IsLetterOrDigit(c) || c == '_';
    }

    public static IEnumerable<string> SplitIntoWords(this String value) {
      var word = new StringBuilder();
      foreach (char c in value) {
        if (IsAlnum(c)) {
          word.Append(c);
        } else {
          if (word.Length > 0) {
            yield return word.ToString();
            word = new StringBuilder();
          }
          yield return new String(c, 1);
        }
      }
    }

    public static string Format(this String format, params object [] values) {
      return String.Format(format, values);
    }

    public static string Format(this String format, object value) {
      return String.Format(format, value);
    }

    public static string Format(this String format, object value1, object value2) {
      return String.Format(format, value1, value2);
    }

    public static string Escape(this String input) {
      StringBuilder result = new StringBuilder(input.Length);
      foreach (char c in input) {
        if (c == '\n')
          result.Append(@"\n");
        else if (c == '\r')
          result.Append(@"\r");
        else if (c == '\t')
          result.Append(@"\t");
        else if (c == '\\')
          result.Append(@"\\");
        else if (c == '"')
          result.Append(@"\""");
        else
          result.Append(c);
      }
      return result.ToString();
    }

    public static string Unescape(this String input) {
      StringBuilder result = new StringBuilder(input.Length);
      bool escaped = false;
      foreach (char c in input) {
        if (escaped) {
          if (c == 'n')
            result.Append("\n");
          else if (c == 'r')
            result.Append("\r");
          else if (c == 't')
            result.Append("\t");
          else if (c == '\\')
            result.Append("\\");
          else if (c == '"')
            result.Append("\"");
        } else {
          if (c == '\\') {
            escaped = true;
            continue;
          } else {
            result.Append(c);
          }
        }
        escaped = false;
      }
      return result.ToString();
    }

    private static char [] hex_digits = {'1', '2', '3', '4', '5', '6', '7', 
                                         '8', '9', '0', 'a', 'b', 'c', 'd',
                                         'e', 'f', 'A', 'B', 'C', 'D', 'E', 
                                         'F'};

    public static bool IsHexDigit(this Char c) {
      return hex_digits.Contains(c);
    }


    public static string Base64Encode(this string str) {
      return Convert.ToBase64String(Encoding.ASCII.GetBytes(str));
    }

    public static string Base64Decode(this string str) {
      byte[] bytes = Convert.FromBase64String(str);
      return Encoding.ASCII.GetString(bytes);
    }

    private static readonly Regex strip_html_regex = new Regex(@"<(.|\n)*?>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public static string StripHtml(this string str) {
      if (str == "" || str == null)
        return "";

      return strip_html_regex.Replace(str, "");
    }

  }

  
}
