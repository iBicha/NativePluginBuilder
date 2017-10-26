using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using AOT;
using System;
using System.Text;


namespace iBicha.Example
{
    public class MyPlugin
    {
		//TODO: embed version and build number in c# to compare with library, detecting old builds.
		private static string version;
		public static string Version {
			get {
				if (version == null) {
					IntPtr ptr = GetPluginVersion ();
					if (ptr != IntPtr.Zero) {
						version = Marshal.PtrToStringAnsi (ptr);
					}
				}
				return version;
			}
		}

		public static int BuildNumber {
			get {
				return GetPluginBuildNumber();
			}
		}

#if UNITY_EDITOR
		private const string libraryName = "MyPlugin";
#elif UNITY_IOS || UNITY_IPHONE || UNITY_WEBGL
		private const string libraryName = "__Internal";
#else
		private const string libraryName = "MyPlugin";
#endif

		//Return plugin version.
		[DllImport(libraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern IntPtr GetPluginVersion();

		//Return plugin build number.
		[DllImport(libraryName, CallingConvention = CallingConvention.Cdecl)]
		private static extern int GetPluginBuildNumber();

		//In this example, our c++ code returns 2.
		[DllImport(libraryName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetTwo();

        //We pass a C# delegate which will be called from c++ with a result.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CallbackDelegate(int result);
        [DllImport(libraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool PassCallback(CallbackDelegate callback);

        //Pass an array to the c++ code, and fill it with 1s.
        [DllImport(libraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FillWithOnes(int[] array, int length);


		[MonoPInvokeCallback(typeof(CallbackDelegate))]
		public static void Callback(int result) {
			MyPluginExample.Log(string.Format("The callback result is:{0}", result));
		}

		//WebGL specific javascript functions from MyPluginJS.jslib
#if UNITY_WEBGL
		const string WebGLPluginPrefix = "MyPlugin_";

        //Call window.confirm in the browser
        [DllImport("__Internal", EntryPoint = WebGLPluginPrefix + "confirm")]
        public static extern bool Confirm(string message);

        //Call window.prompt in the browser
        [DllImport("__Internal", EntryPoint = WebGLPluginPrefix + "prompt")]
        public static extern string Prompt(string message, string defaultInput = null);

        //Returns how many times Confirm has been called
        [DllImport ("__Internal", EntryPoint = WebGLPluginPrefix + "getConfirmCallCount")]
		public static extern int GetConfirmCallCount ();
#else
        public static bool Confirm(string message)
        {
            Debug.LogException(new System.PlatformNotSupportedException("Only available on WebGL"));
			return false;
        }

        public static string Prompt(string message, string defaultInput = null)
        {
            Debug.LogException(new System.PlatformNotSupportedException("Only available on WebGL"));
			return null;
        }

        public static int GetConfirmCallCount()
        {
            Debug.LogException(new System.PlatformNotSupportedException("Only available on WebGL"));
			return 0;
        }
#endif
    }
}
