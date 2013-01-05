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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace dotkrizz {
  public class CommandLineException : Exception {
    public CommandLineException()
      : base() {
    }

    public CommandLineException(string message)
      : base(message) {
    }
  }

  public class InvokeVerbEventArgs : EventArgs  {
    public InvokeVerbEventArgs(string verb, object target,
                               MethodInfo implementing_method,
                               IEnumerable<object> arguments,
                               IEnumerable<MemberInfo> other_methods) {
      Verb = verb;
      ImplementingMethod = implementing_method;
      Arguments = arguments;
      Target = target;
      OtherMethods = other_methods;
      Handled = false;
    }

    public string Verb {
      get;
      private set;
    }

    public MethodInfo ImplementingMethod {
      get;
      private set;
    }

    public IEnumerable<object> Arguments {
      get;
      private set;
    }

    public object Target {
      get;
      private set;
    }

    public IEnumerable<MemberInfo> OtherMethods {
      get;
      private set;
    }

    /// <summary>
    /// If set to true, the delegate will not be called 
    /// </summary>
    public bool Handled {
      get;
      set;
    }

    /// <summary>
    /// Calls method associated with the verb with all its arguments 
    /// </summary>
    /// <returns></returns>
    public object CallMethod() {
      return ImplementingMethod.Invoke(Target, Arguments.ToArray());
    }
  }

  public delegate void InvokeVerbEvent(object sender, InvokeVerbEventArgs e);

  public class ParsedCommandLine {
    protected ParsedCommandLine(string [] arguments,
        params string [] switch_separator) {
      parameters = new List<string>();
      switches = new Dictionary<string, string>();

      var enumerator = arguments.GetEnumerator();
      bool stop_switches = false;
      while (enumerator.MoveNext()) {
        var current = enumerator.Current as string;
        if (!stop_switches && current.StartsWith("--")) {
          var name_value = Pair.Make(current.RemovePrefix("--").Split(
              switch_separator, 2, StringSplitOptions.RemoveEmptyEntries));
          switches.Add(name_value.first, name_value.second);
        } else if (!stop_switches && current.StartsWith("-")) {
          var stripped = current.RemovePrefix("-");
          if (stripped.Length == 0) {
            stop_switches = true; 
            continue;
          }
          switches.Add(stripped.Substring(0, 1), 
              stripped.Length > 1 ? stripped.Substring(1) : null);
        } else {
          parameters.Add(current);
        }
      }
    }

    protected ParsedCommandLine() {
    }

    public IList<string> parameters {
      get;
      private set;
    }

    public IDictionary<string, string> switches {
      get;
      private set;
    }

    public bool HasSwitch(string name) {
      return switches.ContainsKey(name);
    }

    public T GetSwitch<T>(string name) {
      if (switches.ContainsKey(name)) {
        object value = switches [name];
        try {
          return (T) Convert.ChangeType(value, typeof(T));
        } catch (InvalidCastException) {
          return default(T);
        } catch (FormatException) {
          return default(T);
        }
      }
      return default(T);
    }

    public object GetSwitch(Type type, string name) {
      return typeof(ParsedCommandLine).GetMethod("GetSwitch", 
          new Type [] { typeof(string) }).MakeGenericMethod(type).Invoke(this,
          new object [] { name });
    }

    public T GetSwitch<T>(params string [] names) {
      foreach (string name in names) {
        var value = GetSwitch<T>(name);
        if (value != null)
          return value;
      }
      return default(T);
    }

    public static ParsedCommandLine Parse(string [] command_line) {
      return new ParsedCommandLine(command_line, "=");
    }

    public static ParsedCommandLine Parse(string [] command_line,
        params string [] separators) {
      return new ParsedCommandLine(command_line, separators);
    }

    public event InvokeVerbEvent InvokeVerb;

    public void ToExistingCommandLineObject(object res) {
      Type type = res.GetType();

      var switch_members
          = type.GetAnnotatedMembers<CommandLineSwitchAttribute>();

      foreach (var switch_ in switch_members) {
        char short_name = switch_.Value.ShortName;
        string long_name = switch_.Value.LongName ?? switch_.Key.Name;

        var member = switch_.Key;
        var member_type = member.GetMemberType();
        var attribute = switch_.Value;

        object def = member_type.GetDefault();
        
        object val_long = GetSwitch(member_type, long_name);
        
        object val_short = (short_name != '\0')
            ? GetSwitch(member_type, new string(short_name, 1))
            : null;
        
        if (Comparer<object>.Default.Compare(val_long, def) != 0) {
          switch_.Key.SetValue(res, val_long);
        } else if (val_short != null
            && Comparer<object>.Default.Compare(val_short, def) != 0) {
          switch_.Key.SetValue(res, val_short);
        } else {
          if (switch_.Value.Optional) 
            switch_.Key.SetValue(res, null);
          else
            throw new CommandLineException("Missing non-optional argument");

        }
      }

      List<string> parameters_ = null;
      try {
        var params_info = type.GetAnnotatedMembers<CommandLineParametersAttribute>()
            .First().Key;
        parameters_ = new List<string>();
        params_info.SetValue(res, parameters_);
      } catch (Exception) {
      }
      var verbs = type.GetAnnotatedMembers<CommandLineVerbAttribute>();
      var enumerator = parameters.GetEnumerator();
      while (enumerator.MoveNext()) {
        var curr = enumerator.Current.ToLower();
        var ok_verbs = from verb in verbs 
                       where (verb.Value.Name ?? verb.Key.Name).ToLower() == curr
                       select verb.Key;

        var ok_verb = (MethodInfo) ok_verbs.FirstOrDefault();

        if (ok_verb == null) {
          if (parameters_ != null)
            parameters_.Add(enumerator.Current);
          else
            throw new CommandLineException("Unknown verb");
          continue;
        }

        var args = new object [ok_verb.GetParameters().Length];
        for (int i = 0; i < args.Length; i++) {
          if (enumerator.MoveNext())
            args [i] = Convert.ChangeType(enumerator.Current,
                ok_verb.GetParameters() [i].ParameterType);
          else
            throw new CommandLineException();
        }

        bool invoke_it = true;

        if (InvokeVerb != null) {
          var e = new InvokeVerbEventArgs(enumerator.Current, res, ok_verb,
              args, ok_verbs.Skip(1));
          InvokeVerb(this, e);
          invoke_it = !e.Handled;    
        }

        if (invoke_it)
          ok_verb.Invoke(res, args);                              
      }
    }

    public T ToCommandLineObject<T>() where T : new() {
      var res = new T();
      ToExistingCommandLineObject(res);
      return res;
    }

    public ParsedCommandLine Clone() {
      return new ParsedCommandLine() {
        parameters = this.parameters,
        switches = this.switches
      };
    }
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class CommandLineVerbAttribute : Attribute {
    public CommandLineVerbAttribute() {
      Name = null;
    }

    public string Name {
      get;
      set;
    }
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
  public class CommandLineSwitchAttribute : Attribute {
    public CommandLineSwitchAttribute() {
      LongName = null;
      ShortName = '\0';
      Optional = false;
    }

    public string LongName {
      get;
      set;
    }

    public char ShortName {
      get;
      set;
    }

    public bool Optional {
      get;
      set;
    }
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
  public class CommandLineParametersAttribute : Attribute {
    
  }
}
