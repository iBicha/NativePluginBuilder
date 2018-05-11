using System.Diagnostics;
using UnityEngine;

namespace iBicha.Helpers
{
    public static class XCode
    {
        private static bool isInstalled;

        public static bool IsInstalled
        {
            get
            {
                if (isInstalled)
                {
                    return true;
                }

                if (UnityEditor.EditorPlatform != RuntimePlatform.OSXEditor)
                {
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "xcodebuild",
                    Arguments = "-version"
                };

                var backgroundProcess = new BackgroundProcess(startInfo) {Name = "Checking XCode"};
                backgroundProcess.Start();
                //I know this is bad, but it shouldn't take more than few ms. And it will be cached.
                //TODO: make async
                backgroundProcess.Process.WaitForExit();
                return isInstalled = backgroundProcess.Process.ExitCode == 0;
            }
        }
    }
}