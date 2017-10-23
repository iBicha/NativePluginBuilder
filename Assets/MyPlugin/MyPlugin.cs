﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using AOT;


namespace iBicha.Example
{
    public class MyPlugin
    {
#if UNITY_EDITOR
		private const string libraryName = "MyPlugin";
		#elif UNITY_IOS || UNITY_IPHONE || UNITY_WEBGL
		private const string libraryName = "__Internal";
#else
		private const string libraryName = "MyPlugin";
#endif
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

    }
}
