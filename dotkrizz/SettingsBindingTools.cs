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
using System.Configuration;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Forms;

#if !PocketPC

namespace dotkrizz {
  public static class SettingsBindingTools {
    public static Binding DefineBinding(this ApplicationSettingsBase settings,
                                        string property_name,
                                        string setting_name) {
      return new Binding(property_name, settings, setting_name, true,
          DataSourceUpdateMode.OnPropertyChanged);
    }

    public static void BindTo(this ApplicationSettingsBase settings,
                              Control control, string property_name,
                              string setting_name) {
      control.DataBindings.Add(DefineBinding(settings, property_name,
          setting_name));
    }

    public static void BindWithConverterTo<S, T>(
        this ApplicationSettingsBase settings, Control control,
        string property_name, string setting_name,
        Converter<S, T> read_converter, Converter<T, S> write_converter) {
      var proxy = new BindingSettingProxy<S, T>(settings, setting_name,
          read_converter, write_converter);
      var binding = new Binding(property_name, proxy, "value", true,
          DataSourceUpdateMode.OnPropertyChanged);
      control.DataBindings.Add(binding);

      if (control.GetType().GetEvent(property_name + "Changed") == null) {
        if (control is Form) {
          (control as Form).FormClosed += delegate {
            binding.WriteValue();
          };
        } else {
          for (var parent = control.Parent; parent != null; parent = parent.Parent) {
            if (parent is Form) {
              (parent as Form).FormClosed += delegate {
                binding.WriteValue();
              };
            }
          }
        }
      }
    }

    public static void BindWithConverterTo<S>(
        this ApplicationSettingsBase settings, Control control,
        string property_name, string setting_name,
        Converter<S, S> read_converter, Converter<S, S> write_converter) {
      BindWithConverterTo<S, S>(settings, control, property_name, setting_name,
          read_converter ?? (Converter<S, S>)(x => x),
          write_converter ?? (Converter<S, S>)(x => x));
    }

    /// <summary>
    /// binds given setting to the given property of an instance of any class 
    /// if class also possesses propert_nameChanged event, it is used to keep
    /// the setting and property fully synced.
    /// If not, but class exposes Disposed, Closed or FormClosed event 
    /// the setting is update via hooking one of these
    /// Otherwise, settings are updated only just before Save() call on settings
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="whatever"></param>
    /// <param name="property_name"></param>
    /// <param name="setting_name"></param>
    public static void BindToWhatever(this ApplicationSettingsBase settings,
        object whatever, string property_name, string setting_name) {
      var property = whatever.GetType().GetFieldOrProperty(property_name,
          ReflectionTools.FieldPropertyConflictAction.ReturnProperty,
          BindingFlags.Public | BindingFlags.Instance);

      if (property == null)
        throw new ArgumentException(
            "{0} does not possess property {1}".Format(whatever.GetType(),
            property_name), "property_name");

      settings.PropertyChanged += (sender, e) => {
        if (e.PropertyName == setting_name)
          property.SetValue(whatever, settings[setting_name]);
      };

      foreach (string name in new string[] { property_name + "Changed", 
          "Disposed", "Closed", "FormClosed" }) {
        var event_ = whatever.GetType().GetEvent(name);
        if (event_ != null) {
          event_.AddEventHandler(whatever, new EventHandler((sender, e) => {
            settings[setting_name] = property.GetValue(whatever);
          }));
          return;
        }
      }

      settings.SettingsSaving += delegate {
        settings[setting_name] = property.GetValue(whatever);
      };
    }
  }

  internal class BindingSettingProxy<S, T> : INotifyPropertyChanged {
    public BindingSettingProxy(ApplicationSettingsBase settings,
                               string setting_name,
                               Converter<S, T> read_converter,
                               Converter<T, S> write_converter) {
      settings_ = settings;
      setting_name_ = setting_name;
      read_converter_ = read_converter;
      write_converter_ = write_converter;

      settings_.PropertyChanged += (sender, e) => {
        if (PropertyChanged != null)
          PropertyChanged(sender, e);
      };
    }

    public T value {
      get {
        if (read_converter_ != null)
          return read_converter_((S)settings_[setting_name_]);
        else
          return default(T);
      }
      set {
        if (write_converter_ != null)
          settings_[setting_name_] = write_converter_(value);
      }
    }

    private Converter<S, T> read_converter_;
    private Converter<T, S> write_converter_;
    private ApplicationSettingsBase settings_;
    private string setting_name_;

    #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion
  }

}

#endif
