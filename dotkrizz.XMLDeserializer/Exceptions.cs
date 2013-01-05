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

namespace dotkrizz.xml_deserializer {
  public static class ErrorMessages {
    public const string kInvalidType = "Invalid type for serializer class.";
  }

  public class KrizzXMLDeserializerException : Exception {
    public KrizzXMLDeserializerException(string message) :
      base(message) {
    }
  }

  public class UnexpectedXMLTermination : KrizzXMLDeserializerException {
    public const string kMessage = "Unexpected termination of XML file";
    public UnexpectedXMLTermination()
        : base(kMessage) {
    }
  }

  public class UnexpectedXMLElement : KrizzXMLDeserializerException {
    public const string kMessage = "Unexpected element {0} at line {1}, {2}";
    public UnexpectedXMLElement(object element, int line, int column) :
      base(String.Format(kMessage, element.ToString(), line, column)) {
    }
  }

  public class TypeWrongfullyAnnotated : KrizzXMLDeserializerException {
    public const string kAnyAttributeType = "Field/property for any attribute"
        + " must be derived from ICollection<XmlAttribute>"
        + " or ICollection<XAttribute>";
    public TypeWrongfullyAnnotated(string msg)
      : base(msg) {
    }
  }

  public class DuplicatedElements : TypeWrongfullyAnnotated {
    public const string kMessage
      = "There is allowed only one occurance of {0} in {1}.";
    public DuplicatedElements(object element, object container) :
      base(String.Format(kMessage, element.ToString(), container.ToString())) {
    }
  }

  public class Unexpected : KrizzXMLDeserializerException {
    public Unexpected() : base("Unexpected situation!") {
    }
  }
}
