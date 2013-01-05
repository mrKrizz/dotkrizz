// dotkrizz
//
// Copyright (c) 2009 Krzysztof Olczyk
//
// General-purpose .NET library 
//
// Developed together with and used by 
//
// Parrot application 
// 
// The project is the integral part
// of Master's Thesis:
//
// "Toolkit for Graphical User Interface Development 
//  with Markup Language Description"
//
//                       by Krzysztof Olczyk
// 
// presented at Technical University of Lodz


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

namespace dotkrizz {
  public abstract class VerbApplication {
    public VerbApplication(string [] args) {
      EnableVisualStyles = true;
      args_ = args;
      CommandLine = ParsedCommandLine.Parse(args);
    }

    public void Run() {
      var command_line = CommandLine.Clone();
      command_line.InvokeVerb += (sender, e) => {
        var invoke_verb_e = e;

        var single_instance_attr 
          = e.ImplementingMethod.GetSingleAttributeOrNull<SingleInstanceAttribute>();

        string qualifier = null;
        if (single_instance_attr != null) {
          qualifier 
              = single_instance_attr.Qualifier ?? e.Verb;
        }

        VBWinFormsApplication app = null;
        if (qualifier != null && internal_apps_.ContainsKey(qualifier)) {
          app = internal_apps_ [qualifier];
        } else {
          app = new VBWinFormsApplication();

          if (qualifier != null)
            internal_apps_.Add(e.Verb, app);

          app.IsSingleInstance = single_instance_attr != null; 

          app.Startup += (startup_sender, startup_e) => {
            var res = invoke_verb_e.CallMethod();
            if (res != null && res is Form)
              app.MainForm = res as Form;
            else
              startup_e.Cancel = true;
          };
          app.StartupNextInstance += (startup_sender, startup_e) => {
            var attr = invoke_verb_e.ImplementingMethod.GetSingleAttributeOrNull
                <SingleInstanceAttribute>();
            if (attr == null)
              return;
            if (attr.Behavior == SingleInstanceBehaviour.BringToFront) {
              startup_e.BringToForeground = true;
            } else if (attr.Behavior 
                == SingleInstanceBehaviour.CallAnotherInstanceMethod) {
              var method = e.Target.GetType().GetMethod(
                  attr.AnotherInstanceMethodName);
              if (method == null)
                throw new CommandLineException();
              var parsed_command_line 
                  = ParsedCommandLine.Parse(startup_e.CommandLine.ToArray());
              startup_e.BringToForeground = true;
              method.Invoke(e.Target, new object [] { parsed_command_line });
            }
          };
        }
        app.Run(args_);
        e.Handled = true;
      };
      command_line.ToExistingCommandLineObject(this);
    }

    public bool EnableVisualStyles {
      get;
      set;
    }

    public ParsedCommandLine CommandLine {
      get;
      private set;
    }

    private Dictionary<string, VBWinFormsApplication> internal_apps_
        = new Dictionary<string, VBWinFormsApplication>();

    private string [] args_;
  }

  internal class VBWinFormsApplication : WindowsFormsApplicationBase {
    public VBWinFormsApplication() {
      EnableVisualStyles = true;
      ShutdownStyle = ShutdownMode.AfterMainFormCloses;
    }

    public new Form MainForm {
      get;
      set;
    }

    public new bool IsSingleInstance {
      get {
        return base.IsSingleInstance;
      }
      set {
        base.IsSingleInstance = value;
      }
    }

    protected override void OnCreateMainForm() {
      base.OnCreateMainForm();
      base.MainForm = MainForm;
    }
  }

  public enum SingleInstanceBehaviour {
    IgnoreOtherInstances,
    BringToFront,
    CallAnotherInstanceMethod
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class SingleInstanceAttribute : Attribute {
    public SingleInstanceAttribute(SingleInstanceBehaviour behavior) {
      Behavior = behavior;
      Qualifier = null;
    }

    public SingleInstanceBehaviour Behavior {
      get;
      set;
    }

    public string Qualifier {
      get;
      set;
    }

    public string AnotherInstanceMethodName {
      get;
      set;
    }
  }
}
