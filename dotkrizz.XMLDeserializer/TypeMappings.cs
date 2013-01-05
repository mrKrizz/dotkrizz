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
using System.Reflection;
using System.Xml.Serialization;
using dotkrizz;

namespace dotkrizz.xml_deserializer {
  internal class TypeMappings {
    private TypeMappings(Type type) {
      type_ = type;
    }

    private static Dictionary<Type, TypeMappings> mappings
        = new Dictionary<Type, TypeMappings>();

    public static TypeMappings Create(Type type) {
      if (mappings.ContainsKey(type))
        return mappings [type] ;
      TypeMappings new_mapping = new TypeMappings(type);
      new_mapping.Init();
      mappings.Add(type, new_mapping);
      return new_mapping;
    }

    protected virtual void Init() {
      var members = (from MemberInfo mi in type_.GetFields()
                     select mi).Union(
                     from MemberInfo mi in type_.GetProperties()
                     select mi);

      element_mappings = ProcessMapping(members,
         member => member.HasAnyAttribute(typeof(XmlElementAttribute)),
         new Func<MemberInfo, string>(ExtractNameFromElementMapping));

      attribute_mappings = ProcessMapping(members,
         member => member.HasAnyAttribute(typeof(XmlAttributeAttribute)),
         new Func<MemberInfo, string>(ExtractNameFromAttributeMapping));

      array_mappings = ProcessMapping(members,
         member => member.HasAnyAttribute(typeof(XmlArrayAttribute)),
         new Func<MemberInfo, string>(ExtractNameFromArrayMapping));

      var xmltext_mappings = ProcessMapping(members,
          member => member.HasAnyAttribute(typeof(XmlTextAttribute)),
          delegate {
            return "";
          });
      if (xmltext_mappings.Count > 1)
        throw new DuplicatedElements(typeof(XmlTextAttribute), type_);
      xmltext_mapping = (from element in xmltext_mappings
                         select element.Value).FirstOrDefault();

      var any_element_mappings = ProcessMapping(members,
         member => member.HasAnyAttribute(typeof(XmlAnyElementAttribute)),
         delegate {return "";});
      if (any_element_mappings.Count > 1)
        throw new DuplicatedElements(typeof(XmlAnyElementAttribute), type_);
      all_elements_mapping = (from element in any_element_mappings
                               select element.Value).FirstOrDefault();

      var any_attributes_mappings = ProcessMapping(members,
        member => member.HasAnyAttribute(typeof(XmlAnyAttributeAttribute)),
        delegate {return "";});
      if (any_attributes_mappings.Count > 1)
        throw new DuplicatedElements(typeof(XmlAnyElementAttribute), type_);
      all_attributes_mapping = (from attribute in any_attributes_mappings
                                 select attribute.Value).FirstOrDefault();         
    }

    private static Dictionary<string, MemberInfo> ProcessMapping(
        IEnumerable<MemberInfo> members, Predicate<MemberInfo> filter,
        Func<MemberInfo, string> key_extractor) {
      return
        members.Where(el => filter(el)).ToDictionary(mi => key_extractor(mi));
    }

    private string ExtractNameFromElementMapping(MemberInfo mi) {
      var attribute = mi.GetSingleAttributeOrNull<XmlElementAttribute>();
      if (attribute == null)
        throw new Unexpected();
      return attribute.ElementName != "" ? attribute.ElementName : mi.Name;
    }

    private string ExtractNameFromAttributeMapping(MemberInfo mi) {
      var attribute = mi.GetSingleAttributeOrNull<XmlAttributeAttribute>();
      if (attribute == null)
        throw new Unexpected();
      return attribute.AttributeName != "" ? attribute.AttributeName : mi.Name;
    }

    private string ExtractNameFromArrayMapping(MemberInfo mi) {
      var attribute = mi.GetSingleAttributeOrNull<XmlArrayAttribute>();
      if (attribute == null)
        throw new Unexpected();
      return attribute.ElementName != "" ? attribute.ElementName : mi.Name;
    }

    public Dictionary<string, MemberInfo> element_mappings {
      get;
      private set;
    }
    public Dictionary<string, MemberInfo> attribute_mappings {
      get;
      private set;
    }
    public MemberInfo all_attributes_mapping {
      get;
      private set;
    }
    public MemberInfo all_elements_mapping {
      get;
      private set;
    }
    public MemberInfo xmltext_mapping {
      get;
      private set;
    }
    public Dictionary<string, MemberInfo> array_mappings {
      get;
      private set;
    }

    protected Type type_;
  }
}
