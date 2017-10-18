using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
namespace iBicha
{
    public class CMakeHelper
    {
        public static string GetCMakeVersion()
        {
            Process cmake = new Process();
			cmake.StartInfo.FileName = FindBinary("cmake");
            cmake.StartInfo.Arguments = "--version";
            cmake.StartInfo.UseShellExecute = false;
            //cmake.StartInfo.CreateNoWindow = true;
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


        public static bool Build(NativePlugin plugin, bool release = true, string arch=null)
        {
            //cmake --build . --clean-first
            StringBuilder argsBuilder = new StringBuilder();
            argsBuilder.AppendFormat("{0} ", "../CMake");
            argsBuilder.AppendFormat("-DPLUGIN_NAME:STRING={0} ", plugin.Name);
            argsBuilder.AppendFormat("-DSOURCE_FOLDER:PATH={0} ", plugin.sourceFolder);
            argsBuilder.AppendFormat("-DPLUGIN_BINARY_FOLDER:PATH={0} ", plugin.pluginBinaryFolderPath);
            argsBuilder.AppendFormat("-DPLUGIN_BINARY_FOLDER:PATH={0} ", plugin.pluginBinaryFolderPath);
            argsBuilder.AppendFormat("-DCMAKE_BUILD_TYPE={0} ", release ? "Debug" : "Release");
            if(!string.IsNullOrEmpty(arch))
            {
                argsBuilder.AppendFormat("-DARCH={0} ", arch);
            }
            //argsBuilder.AppendFormat("--build {0}", "../CMake");

            Process cmake = new Process();
			cmake.StartInfo.FileName = FindBinary("cmake");
            cmake.StartInfo.Arguments = argsBuilder.ToString();
            cmake.StartInfo.WorkingDirectory = plugin.buildFolder;
            cmake.StartInfo.UseShellExecute = false;
            //cmake.StartInfo.CreateNoWindow = true;
            cmake.StartInfo.RedirectStandardOutput = true;
            cmake.StartInfo.RedirectStandardError = true;

            try
            {
                cmake.Start();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return false;
                //throw ex;
            }
            string output = "";
            while (!cmake.StandardOutput.EndOfStream)
            {
                NativePluginBuilder.lastLogLine = cmake.StandardOutput.ReadLine();
                output += NativePluginBuilder.lastLogLine + System.Environment.NewLine;
            }

            string err = cmake.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(err))
            {
                UnityEngine.Debug.LogError(err);
            }
            UnityEngine.Debug.Log(output);

            cmake.WaitForExit();

            return cmake.ExitCode == 0;
        }

		private static string FindBinary(string command) {
			if (!IsOSX) {
				return command;
			}
			//temp hack
			return "/usr/local/bin/" + command;
		}


		public static bool IsOSX
		{
			get
			{
				#if UNITY_EDITOR_OSX
				return true;
				#else
				return false;
				#endif
			}
		}

    }

}
