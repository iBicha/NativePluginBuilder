using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

public class MyPluginExample : MonoBehaviour {

    private static StringBuilder console = new StringBuilder();
	// Use this for initialization
	void Start ()
	{
		

//		Log(string.Format("GetTwo() returned: {0}", MyPlugin.GetTwo()));
	}
		
	public static void Log(string obj)
    {
        Debug.Log(obj);
        console.Append(obj + "\n");
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), console.ToString());
    }

}
