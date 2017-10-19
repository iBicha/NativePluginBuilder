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

        [DllImport(libraryName)]
		public static extern int GetTwo();

    }
}
