using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace iBicha.Example {
	public class MyPluginExample : MonoBehaviour {



        public UnityEngine.UI.Text console;

		private static UnityEngine.UI.Text s_console;

		// Use this for initialization
		void Start () {
			s_console = console;

            Log(string.Format("GetTwo() returned:{0}", MyPlugin.GetTwo()));

			Log(string.Format("PassCallback() returned:{0}", MyPlugin.PassCallback(MyPlugin.Callback)));

            int[] array = { 0, 0, 0, 0, 0 };
            MyPlugin.FillWithOnes(array, array.Length);
            Log(string.Format("The content of array is:{0}", "[" + string.Join(",", array.Select(i => i.ToString()).ToArray()) + "]"));
        }
			
		public static void Log(string obj)
        {
            Debug.Log(obj);
			if (s_console != null)
            {
				s_console.text += obj + System.Environment.NewLine;
            }

        }
	}
}
