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
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Reflection;
using dotkrizz;

namespace dotkrizz.xml_deserializer {
  internal class ObjectDeserializer {
    public ObjectDeserializer(Type type) {
      type_ = type;
      mappings_ = TypeMappings.Create(type);
    }

    public void Deserialize(XElement element, object obj) {
      DeserializeAttributes(element, obj);
      DeserializeChildElements(element, obj);
      if (mappings_.xmltext_mapping != null) {
        mappings_.xmltext_mapping.SetValueParsed(obj, element.Value.Trim());
      }
      if (obj is IXmlSourceFileLineInfo) {
        var source_file_info = obj as IXmlSourceFileLineInfo;
        source_file_info.LineNumber = (element as IXmlLineInfo).LineNumber;
        source_file_info.LinePosition = (element as IXmlLineInfo).LinePosition;
      }
    }

    public void DeserializeAttributes(XElement element, object obj) {
      foreach (var attribute in element.Attributes()) {
        string attribute_name = attribute.Name.ToString();
        if (mappings_.attribute_mappings.ContainsKey(attribute_name)) {
          var mi = mappings_.attribute_mappings [attribute_name];
          
          mi.SetValueParsed(obj, attribute.Value);
          
          var store_info 
              = mi.GetSingleAttributeOrNull<StoreLineInfoAttribute>();
          if (store_info != null) {
            MemberInfo info_member = obj.GetType().GetMember(
                store_info.FieldName).FirstOrDefault();
            if (info_member == null)
              throw new TypeWrongfullyAnnotated("");
            info_member.SetValue(obj,
                                SourceFileInfo.FromXObject(attribute));
          }
        } else {
          if (mappings_.all_attributes_mapping != null) {
            Type collection_type
                = mappings_.all_attributes_mapping.GetMemberType();
            
            if (typeof(ICollection<XAttribute>).
                IsAssignableFrom(collection_type)) {
              AddToCollection(obj, mappings_.all_attributes_mapping, attribute);
            } else {
              throw new TypeWrongfullyAnnotated(
                  TypeWrongfullyAnnotated.kAnyAttributeType);
            }

          } else {
            throw new UnexpectedXMLElement(attribute,
                (attribute as IXmlLineInfo).LineNumber,
                (attribute as IXmlLineInfo).LinePosition);
          }
        }
      }
    }

    private void SetValueFromChild(XElement child, object obj, MemberInfo mi) {
      var child_type = mi.GetMemberType().GetScalarType();
      object child_obj;
      if (child_type == typeof(string)) {
        child_obj = child.Value;
      } else {
        ObjectDeserializer deserializer
          = new ObjectDeserializer(child_type);
        ConstructorInfo constructor
            = child_type.GetConstructor(new Type[] {});
        child_obj = constructor.Invoke(null);
        deserializer.Deserialize(child, child_obj);
      }

      if (typeof(ICollection).IsAssignableFrom(mi.GetMemberType())) {
        AddToCollection(obj, mi, child_obj);
      } else {
        mi.SetValue(obj, child_obj);
      }
    }

    public void DeserializeChildElements(XElement element, object obj) {
      foreach (var child in element.Elements()) {
        string child_name = child.Name.ToString();
        if (mappings_.element_mappings.ContainsKey(child_name)) {
          MemberInfo mi = mappings_.element_mappings [child_name];
          Type child_type = mi.GetMemberType();
          if (child_type.IsPrimitive
              || typeof(string).IsAssignableFrom(child_type)) {
            mi.SetValueParsed(obj, child.Value);
          } else if (mi.GetMemberType().IsSerializable()) {
            SetValueFromChild(child, obj, mi);
          } else {
            throw new NotImplementedException();
          }
          var store_info
              = mi.GetSingleAttributeOrNull<StoreLineInfoAttribute>();
          if (store_info != null) {
            MemberInfo info_member = obj.GetType().GetMember(
                store_info.FieldName, BindingFlags.NonPublic 
                | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
            if (info_member == null)
              throw new TypeWrongfullyAnnotated("Field " + store_info.FieldName
                + " cannot be set.");
            info_member.SetValue(obj,
                                 SourceFileInfo.FromXObject(child));
          }
        } else if (mappings_.array_mappings.ContainsKey(child_name)) {
          MemberInfo mi = mappings_.array_mappings [child_name];
          Type child_type = mi.GetMemberType().GetScalarType();
          var array_item = mi.GetSingleAttributeOrNull<XmlArrayItemAttribute>();
          string element_name = array_item != null
              ? array_item.ElementName : child_type.Name;
          foreach (var subchild in child.Elements()) {
            if (subchild.Name.ToString() != element_name)
              throw new UnexpectedXMLElement(subchild,
                  (subchild as IXmlLineInfo).LineNumber,
                  (subchild as IXmlLineInfo).LinePosition);
            SetValueFromChild(subchild, obj, mi);
          }
        } else {
          if (mappings_.all_elements_mapping != null) {
            Type collection_type
                = mappings_.all_elements_mapping.GetMemberType();

            if (typeof(ICollection<XElement>).
                IsAssignableFrom(collection_type)) {
              AddToCollection(obj, mappings_.all_elements_mapping, child);
            } else {
              throw new TypeWrongfullyAnnotated(
                  TypeWrongfullyAnnotated.kAnyAttributeType);
            }

          } else {
            throw new UnexpectedXMLElement(child,
                (child as IXmlLineInfo).LineNumber,
                (child as IXmlLineInfo).LinePosition);
          }
        }
      }
    }

    // Warning: No checks are done whether value is compatible with collection
    //          nor whether collection is a collection at all
    private static void AddToCollection(object obj, MemberInfo mi,
                                        object value) {
      object collection = mi.GetValue(obj);
      if (collection == null) {
        ConstructorInfo constructor 
            = mi.GetMemberType().GetConstructor(new Type[] {});
        mi.SetValue(obj, collection = constructor.Invoke(null));
      }
      mi.GetMemberType().InvokeMember("Add", BindingFlags.InvokeMethod,
          Type.DefaultBinder, collection, new object [] { value });
    }

    private Type type_;

    static ObjectDeserializer() {
      
    }

    private TypeMappings mappings_;
  }
}
