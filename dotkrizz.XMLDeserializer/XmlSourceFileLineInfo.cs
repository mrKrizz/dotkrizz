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
using System.Xml;
using System.Xml.Linq;

namespace dotkrizz.xml_deserializer {
  public interface IXmlSourceFileLineInfo {
    int LineNumber {
      get;
      set;
    }

    int LinePosition {
      get;
      set;
    }
  }

  public class SourceFileInfo : IXmlSourceFileLineInfo {
    public SourceFileInfo() {
    }

    public static IXmlSourceFileLineInfo FromXObject(XObject obj) {
      return FromXmlLineInfo(obj as IXmlLineInfo);
    }

    public static IXmlSourceFileLineInfo FromXmlLineInfo(IXmlLineInfo obj) {
      var result = new SourceFileInfo();
      result.LineNumber = obj.LineNumber;
      result.LinePosition = obj.LinePosition;
      return result;
    }

    #region IXmlLineInfo Members

    public int LineNumber {
      get;
      set;
    }

    public int LinePosition {
      get;
      set;
    }

    public bool HasLineInfo() {
      throw new NotImplementedException();
    }

    #endregion
  };
}
