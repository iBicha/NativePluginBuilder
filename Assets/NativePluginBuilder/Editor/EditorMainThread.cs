using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;


namespace iBicha
{
    public class EditorMainThread
    {

        private static Queue<Action> queue = new Queue<Action>();

        public static void Run(Action action)
        {
            lock (queue)
            {
                queue.Enqueue(action);
                EditorApplication.update += Update;
            }
        }

        private static void Update()
        {
            lock (queue)
            {
                while (queue.Count > 0)
                {
                    Action action = queue.Dequeue();
                    if (action != null)
                    {
                        action();
                    }
                }
                EditorApplication.update -= Update;
            }
        }

    }

}
