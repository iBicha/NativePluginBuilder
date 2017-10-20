using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace iBicha.Example {
	public class MyPluginExample : MonoBehaviour {

        public UnityEngine.UI.Text console;
		// Use this for initialization
		void Start () {
            Log(string.Format("GetTwo() returned:{0}", MyPlugin.GetTwo()));

            Log(string.Format("PassCallback() returned:{0}", MyPlugin.PassCallback((result) => {
                Log(string.Format("The callback result is:{0}", result));
            })));

            int[] array = { 0, 0, 0, 0, 0 };
            MyPlugin.FillWithOnes(array, array.Length);
            Log(string.Format("The content of array is:{0}", "[" + string.Join(",", array.Select(i => i.ToString()).ToArray()) + "]"));
        }

        private void Log(string obj)
        {
            Debug.Log(obj);
            if (console != null)
            {
                console.text += obj + System.Environment.NewLine;
            }

        }
	}
}
