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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

/* This module allows you to implement the interface 
   through delegate (!) instead of class.
   Say you have an interface ISomething:
       public interface ISomething {
           void Foo();
           int Bar(); 
       }

   Then you can get an implementation of it from a method, e.g:
       ISomething impl = ClassAdapter.AdaptDelegate<ISomething>(new InterfaceImplementation(this.cool_function));

   where cool_function is somethind like: 
   
   protected object cool_function(string method_name, object [] arguments)
   {
      if (method_name == "Foo")
      {
          // do something
          return null;
      }
      else if (method_name == "Bar") 
      {
         // do something else
         return 5;
      }
      return null;
   }

   It also serves the purpose similar to "proxy classes" in java. 
   You can provide the implementation by adapting an object 
   implementing interface IDynamicMethodDispatcher and/or IDynamicPropertyAccessor.
*/


namespace dotkrizz {
  public interface IDynamicMethodDispatcher {
    object Invoke(Type interface_type, string method_name, 
                  object [] arguments);
  }

  public interface IDynamicPropertyAccessor {
    void SetProperty(Type interface_type, string property_name, object value);
    object GetProperty(Type interfave_type, string property_name);
  }

  public class ClassAdapter {
    protected ClassAdapter(Type interface_type, object obj) {
      interface_type_ = interface_type;
      obj_ = obj;
    }

    protected object ProxyInvoke(string method_name, Type expected_return,
                                 params object [] arguments) {
      var method = obj_.GetType().GetMethod(method_name);
      var property = obj_.GetType().GetFieldOrProperty(method_name,
          ReflectionTools.FieldPropertyConflictAction.ReturnProperty,
          null);
      var other_property = obj_.GetType().GetFieldOrProperty("other",
          ReflectionTools.FieldPropertyConflictAction.ReturnProperty,
          null);

      object result = null;

      if (method != null)
        result = obj_.Call(method_name, arguments);
      else if (property != null
          && property.GetMemberType().IsSubclassOf(typeof(Delegate)))
        result = ((Delegate) property.GetValue(obj_)).DynamicInvoke(arguments);
      else if (obj_ is IDynamicMethodDispatcher)
        result = (obj_ as IDynamicMethodDispatcher).Invoke(interface_type_,
            method_name, arguments);
      else if (obj_.GetType().HasAllAttributes(typeof(CompilerGeneratedAttribute))
          && other_property != null
          && other_property.GetMemberType().IsSubclassOf(typeof(Delegate)))
        result = ((Delegate) other_property.GetValue(other_property)).DynamicInvoke(
          new object [] { method_name }.Concat(arguments));
      else
        throw new NotImplementedException();

      if (result == null)
        return expected_return.GetDefaultValue();
      else
        return result;
    }

    protected object ProxyGetProperty(string property_name) {
      if (obj_.GetType().GetProperty(property_name) != null)
        return obj_.GetProperty(property_name);
      else if (obj_ is IDynamicPropertyAccessor)
        return (obj_ as IDynamicPropertyAccessor).GetProperty(interface_type_,
            property_name);
      else
        throw new NotImplementedException();
    }

    protected void ProxySetProperty(string property_name, object value) {
      if (obj_.GetType().GetProperty(property_name) != null)
        obj_.SetProperty(property_name, value);
      else if (obj_ is IDynamicPropertyAccessor)
         (obj_ as IDynamicPropertyAccessor).SetProperty(interface_type_,
             property_name, value);
      else
        throw new NotImplementedException();
    }

    private Type interface_type_;
    private object obj_;

    public static I Adapt<I>(object obj) {
      var @class = new CodeTypeDeclaration(typeof(I).Name + "_Adapter") {
        Attributes = MemberAttributes.Final | MemberAttributes.Public
      };
      @class.BaseTypes.Add(typeof(ClassAdapter));
      @class.BaseTypes.Add(typeof(I));

      var construtor = new CodeConstructor() {
        Attributes = MemberAttributes.Public | MemberAttributes.Final,
      };
      
      construtor.Parameters.Add(new CodeParameterDeclarationExpression(
           typeof(object), "obj"));
      construtor.BaseConstructorArgs.Add(
          new CodeTypeOfExpression(typeof(I)));
      construtor.BaseConstructorArgs.Add(
          new CodeArgumentReferenceExpression("obj"));

      @class.Members.Add(construtor);

      foreach (var method in typeof(I).GetMethods().Where(mi => !mi.IsSpecialName)) {
        var proxy_method = new CodeMemberMethod() {
          Attributes = MemberAttributes.Final | MemberAttributes.Public,
          Name = method.Name,
          ReturnType = new CodeTypeReference(method.ReturnType)
        };
        proxy_method.Parameters.AddRange((from p in method.GetParameters()
                                          select new CodeParameterDeclarationExpression(
                                           p.ParameterType, p.Name)).ToArray());
        proxy_method.ImplementationTypes.Add(typeof(I));

        var @params = new List<CodeExpression>(proxy_method.Parameters.Count + 1);
        @params.Add(new CodePrimitiveExpression(method.Name));
        @params.Add(new CodeTypeOfExpression(
            new CodeTypeReference(method.ReturnType)));
        @params.AddRange(from CodeParameterDeclarationExpression p 
                             in proxy_method.Parameters
                         select (CodeExpression) 
                             new CodeArgumentReferenceExpression(p.Name));

        if (method.ReturnType != typeof(void)) {
          proxy_method.Statements.Add(
              new CodeMethodReturnStatement(
                  new CodeCastExpression(
                      method.ReturnType,
                      new CodeMethodInvokeExpression(
                          new CodeThisReferenceExpression(),
                          "ProxyInvoke",
                          @params.ToArray()
                      )
                  )
              )
          );
        } else {
          proxy_method.Statements.Add(
            new CodeMethodInvokeExpression(
                new CodeThisReferenceExpression(),
                "ProxyInvoke",
                @params.ToArray()
            )
          );
        }

        @class.Members.Add(proxy_method);
      }

      foreach (var property in typeof(I).GetProperties()) {
        var proxy_property = new CodeMemberProperty {
          Attributes = MemberAttributes.Final | MemberAttributes.Public,
          Name = property.Name,
          Type = new CodeTypeReference(property.PropertyType),
          HasGet = property.IsReadable(),
          HasSet = property.IsWritable()
        };

        proxy_property.ImplementationTypes.Add(typeof(I));

        if (proxy_property.HasGet) {
          proxy_property.GetStatements.Add(
              new CodeMethodReturnStatement(
                  new CodeCastExpression(
                      property.PropertyType,
                      new CodeMethodInvokeExpression(
                          new CodeThisReferenceExpression(),
                          "ProxyGetProperty",
                          new CodePrimitiveExpression(property.Name)
                      )
                  )
              )
          );
        }

        if (proxy_property.HasSet) {
          proxy_property.SetStatements.Add(
           new CodeMethodInvokeExpression(
               new CodeThisReferenceExpression(),
               "ProxySetProperty",
               new CodePrimitiveExpression(property.Name),
               new CodePropertySetValueReferenceExpression()
           )
         );
        }
         

        @class.Members.Add(proxy_property);
      }


      var compiler = new CSharpCodeProvider();

      return (I) Activator.CreateInstance(
          new CSharpCodeProvider().GenerateClass(@class, null), obj);
    }

    public static I AdaptDelegate<I>(InterfaceImplementation @delegate) {
      return Adapt<I>(new InterfaceToDelegateProxy(@delegate));
    }

    private class InterfaceToDelegateProxy : IDynamicMethodDispatcher {
      public InterfaceToDelegateProxy(InterfaceImplementation @delegate) {
        delegate_ = @delegate;
      }

      private InterfaceImplementation delegate_;

      #region IDynamicMethodDispatcher Members

      public object Invoke(Type interface_type, string method_name,
                           object [] arguments) {
        return delegate_(method_name, arguments);
      }

      #endregion
    }
  }

  public delegate object InterfaceImplementation(string method_name, 
                                                 object [] arguments);
}

