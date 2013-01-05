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
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Xml.Serialization;
using System.Text;

namespace dotkrizz.xml_deserializer {
  public class XmlSerializer<T> where T : class {
    public XmlSerializer(Type type) {
      if (!typeof(T).IsAssignableFrom(type) || !type.IsSerializable() ) {
        throw new ArgumentException(ErrorMessages.kInvalidType);
      }
      type_ = type;
    }

    public XmlSerializer() {
      if (!typeof(T).IsSerializable()) {
        throw new ArgumentException(ErrorMessages.kInvalidType);
      }
      type_ = typeof(T);
    }

    public T Deserialize(XmlTextReader input) {
      bool found_first_element = false;
      while (!found_first_element && input.Read()) {
        found_first_element = input.NodeType == XmlNodeType.Element;
      }
      if (!found_first_element)
        throw new UnexpectedXMLTermination();

      var parsed = XElement.Load(input, 
          LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);

      ConstructorInfo constructor
                = type_.GetConstructor(new Type[] {});
      T obj = constructor.Invoke(null) as T;

      ObjectDeserializer deserializer = new ObjectDeserializer(type_);
      deserializer.Deserialize(parsed, obj);

      return obj;
    }

    public T Deserialize(TextReader input) {
      return Deserialize(new XmlTextReader(input));
    }

    public T Deserialize(Stream input) {
      return Deserialize(new XmlTextReader(input));
    }

    private Type type_;
  }

  public class XmlSerializer : XmlSerializer<object> {
  }
}
