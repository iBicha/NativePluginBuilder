using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace iBicha.Example {
	public class MyPluginExample : MonoBehaviour {

		// Use this for initialization
		void Start () {
            Debug.Log(string.Format("GetTwo() returned:{0}", MyPlugin.GetTwo()));
            Debug.Log(string.Format("PassCallback() returned:{0}", MyPlugin.PassCallback((result) => {
                Debug.Log(string.Format("The callback result is:{0}", result));
            })));
		}

	}
}
