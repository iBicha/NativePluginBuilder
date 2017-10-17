using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
namespace iBicha
{
    public class CMake
    {
        public static string GetCMakeVersion()
        {
            Process cmake = new Process();
            cmake.StartInfo.FileName = "cmake";
            cmake.StartInfo.Arguments = "--version";
            cmake.StartInfo.UseShellExecute = false;
            cmake.StartInfo.CreateNoWindow = true;
            cmake.StartInfo.RedirectStandardOutput = true;
            cmake.StartInfo.RedirectStandardError = true;

            cmake.Start();

            string output = cmake.StandardOutput.ReadToEnd();
            string err = cmake.StandardError.ReadToEnd();
            cmake.WaitForExit();

            if (cmake.ExitCode != 0)
            {
                throw new System.Exception(string.Format("CMake exited with code {0} and error: {1}", cmake.ExitCode, err));
            }

            output = output.ToLower();

            if (output.Contains("version"))
            {
                output = output.Substring(output.IndexOf("version") + "version".Length).Trim().Split(' ')[0];
            }

            return output;
        }


        public static bool Build(NativePlugin plugin)
        {
            StringBuilder argsBuilder = new StringBuilder();
            argsBuilder.Append("../CMake ");
            argsBuilder.AppendFormat("-DPLUGIN_NAME:STRING={0} ", plugin.Name);
            argsBuilder.AppendFormat("-DSOURCE_FOLDER:PATH={0} ", plugin.sourceFolder);

            Process cmake = new Process();
            cmake.StartInfo.FileName = "cmake";
            cmake.StartInfo.Arguments = argsBuilder.ToString();
            cmake.StartInfo.WorkingDirectory = plugin.buildFolder;
            
            cmake.StartInfo.UseShellExecute = false;
            cmake.StartInfo.CreateNoWindow = true;
            cmake.StartInfo.RedirectStandardOutput = true;
            cmake.StartInfo.RedirectStandardError = true;

            cmake.Start();
            while (!cmake.StandardOutput.EndOfStream)
            {
                UnityNativePluginBuilder.lastLogLine = cmake.StandardOutput.ReadLine();
            }
            
            string err = cmake.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(err))
            {
                UnityEngine.Debug.LogError(err);
            }
            cmake.WaitForExit();

            return cmake.ExitCode == 0;
        }
    }

}
