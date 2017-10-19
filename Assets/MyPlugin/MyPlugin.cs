using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


namespace iBicha.Example
{
    public class MyPlugin
    {
        /* TODO: dll import examples
         * Different types
         * callbacks
         * unsafe code
         * locked memory pointers
         */
#if UNITY_EDITOR
		private const string libraryName = "MyPlugin";
#elif UNITY_IPHONE
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


        [DllImport(libraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FillWithOnes(int[] array, int length);

    }
}
