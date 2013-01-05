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
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dotkrizz {
  /// <summary>
  /// The code in this file is based on code by Florian Stinglmayr
  /// http://www.codeguru.com/csharp/csharp/cs_misc/userinterface/article.php/c9327
  /// </summary>

  public class SystemMenuException : Exception {
  }

  public class NoSystemMenuException : SystemMenuException {
  }

  public class FormHandleNotCreatedException : SystemMenuException {
  }

  /// <summary>
  /// A class that helps to manipulate the system menu
  /// by Krzysztof Olczyk, inspired on implementation
  /// by Florian "nohero" Stinglmayr, although modified much.
  /// </summary>
  public class SystemMenu {
    public SystemMenu(Form form) {
      form_ = form;
      wnd_proc_ = new API.WndProcDelegate(WndProc);
    }

    ~SystemMenu() {
      if (original_wndproc_ != IntPtr.Zero)
        API.SetWindowLong(handle_, API.GWL.GWL_WNDPROC, original_wndproc_);
    }

    // Insert a separator at the given position index starting at zero.
    public bool InsertSeparator(int position) {
      return API.InsertMenu(Handle, (uint)position, API.MenuFlags.MF_SEPARATOR
          | API.MenuFlags.MF_BYPOSITION, 0, "");
    }

    // Simplified InsertMenu(), that assumes that Pos is relative
    // position index starting at zero
    public bool InsertMenu(int position, string text, EventHandler handler) {
      handlers_.Add(handler);
      uint id = (uint) handlers_.Count - 1 + InitialId;
      return API.InsertMenu(Handle, (uint)position, API.MenuFlags.MF_BYPOSITION
          | API.MenuFlags.MF_STRING, id, text);
    }

    // Appends a seperator
    public bool AppendSeparator() {
      return API.AppendMenu(Handle, API.MenuFlags.MF_SEPARATOR, 0, null);
    }

    // This uses the ItemFlags.mfString as default value
    public bool AppendMenu(string text, EventHandler handler) {
      handlers_.Add(handler);
      uint id = (uint)handlers_.Count - 1 + InitialId;
      return API.AppendMenu(Handle, API.MenuFlags.MF_STRING, id, text);
    }
  
    // Reset's the window menu to it's default
    public void Reset() {
      handle_ = API.GetSystemMenu(form_.Handle, true);
    }

    [CLSCompliant(false)]
    protected IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
      if (msg == (uint)API.WindowMessages.WM_SYSCOMMAND) {
        var id = wParam.ToInt32();
        if (id >= InitialId && id < InitialId + handlers_.Count) {
          var handler = handlers_ [(int)(id - InitialId)];
          if (handler != null)
            handler(this, new EventArgs());
        }
      }

      return API.CallWindowProc(original_wndproc_, hWnd, msg, wParam, lParam);
    }

    private void RetrieveHandle() {
      if (!form_.IsHandleCreated)
        throw new FormHandleNotCreatedException();
      handle_ = API.GetSystemMenu(form_.Handle, false);
      if (handle_ == IntPtr.Zero)
        throw new NoSystemMenuException();
    }

    private void SubClass() {
      IntPtr wndproc = Marshal.GetFunctionPointerForDelegate(wnd_proc_);
      original_wndproc_ = API.SetWindowLong(form_.Handle, API.GWL.GWL_WNDPROC,
          wndproc);

      if (original_wndproc_ == IntPtr.Zero)
        throw new Win32Exception(Marshal.GetLastWin32Error());

      form_.FormClosing += delegate {
        API.SetWindowLong(handle_, API.GWL.GWL_WNDPROC, original_wndproc_);
      };
    }

    private IntPtr handle_ = IntPtr.Zero; // Handle to the System Menu
    private Form form_ = null;
    private List<EventHandler> handlers_ = new List<EventHandler>();
    private IntPtr original_wndproc_ = IntPtr.Zero;
    private API.WndProcDelegate wnd_proc_ = null;
    private const uint InitialId = 0x100;

    public IntPtr Handle {
      get {
        if (handle_ == IntPtr.Zero) {
          RetrieveHandle();
          SubClass();
        }
        return handle_;
      }
    }

    private static class API {
      [Flags]
      public enum MenuFlags : uint {
        MF_STRING = 0,
        MF_BYPOSITION = 0x400,
        MF_SEPARATOR = 0x800,
        MF_REMOVE = 0x1000,
      }

      public enum WindowMessages : uint {
        WM_SYSCOMMAND = 0x0112
      }

      public enum GWL : int {
        GWL_WNDPROC = (-4),
        GWL_HINSTANCE = (-6),
        GWL_HWNDPARENT = (-8),
        GWL_STYLE = (-16),
        GWL_EXSTYLE = (-20),
        GWL_USERDATA = (-21),
        GWL_ID = (-12)
      }

      public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg,
          IntPtr wParam, IntPtr lParam);

      [DllImport("user32.dll")]
      public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

      [DllImport("user32.dll", CharSet = CharSet.Auto)]
      public static extern bool AppendMenu(IntPtr hMenu, MenuFlags uFlags,
                                           uint uIDNewItem, string lpNewItem);

      [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      public static extern bool InsertMenu(IntPtr hmenu, uint position,
          MenuFlags flags, uint item_id,
          [MarshalAs(UnmanagedType.LPTStr)] string item_text);

      [DllImport("user32.dll", SetLastError = true)]
      public static extern IntPtr SetWindowLong(IntPtr hWnd, GWL nIndex,
                                         IntPtr dwNewLong);

      [DllImport("user32.dll")]
      public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc,
          IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
  }

  public static class SystemMenuTools {
    private static Dictionary<Form, SystemMenu> created_menus
        = new Dictionary<Form,SystemMenu>();
    public static SystemMenu GetSystemMenu(this Form form) {
      if (created_menus.ContainsKey(form))
        return created_menus [form];
      var menu = new SystemMenu(form);
      created_menus.Add(form, menu);
      form.Disposed += delegate {
        created_menus.Remove(form);
      };
      return menu;
    }
  }
}
