using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Diagnostics;
using System.Text;
using System;

namespace iBicha
{
	public class BackgroundProcess {

		public string Name;

		public delegate void ExitedDelegate(int exitCode, string outputData, string errorData);
		public event ExitedDelegate Exited;

		public event Action<string> OutputLine;
		public event Action<string> ErrorLine;

		public StringBuilder outputData;
		public StringBuilder errorData;

		private Process process;

		public BackgroundProcess nextProcess;
		public bool nextStopOnError;

		public BackgroundProcess(ProcessStartInfo startInfo) {
			outputData = new StringBuilder ();
			errorData = new StringBuilder ();
			process = new Process ();
			process.StartInfo = startInfo;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			process.EnableRaisingEvents = true;

			process.OutputDataReceived += Process_OutputDataReceived;
			process.ErrorDataReceived += Process_ErrorDataReceived;
			process.Exited += Process_Exited;
		}

		void Process_Exited (object sender, System.EventArgs e)
		{
			if (process.ExitCode != 0) {
				errorData.Insert (0, string.Format ("Exit code: {0}\n", process.ExitCode));
			}

			ExitedDelegate ExitedHandler = Exited;
			if (ExitedHandler != null) {
				EditorMainThread.Run (()=>{
					ExitedHandler(process.ExitCode, outputData.ToString().Trim(), errorData.ToString().Trim());
				});
			}

			if (nextProcess != null) {
				if (process.ExitCode == 0 || !nextStopOnError) {
					nextProcess.Start ();
				}
			}
		}

		void Process_ErrorDataReceived (object sender, DataReceivedEventArgs e)
		{
			errorData.AppendLine (e.Data);
			Action<string> ErrorLineHandler = ErrorLine;
			if (ErrorLineHandler != null) {
				EditorMainThread.Run (()=>{
					ErrorLineHandler(e.Data);
				});
			}

		}

		void Process_OutputDataReceived (object sender, DataReceivedEventArgs e)
		{
			outputData.AppendLine (e.Data);
			Action<string> OutputLineHandler = OutputLine;
			if (OutputLineHandler != null) {
				EditorMainThread.Run (()=>{
					OutputLineHandler(e.Data);
				});
			}
		}

		public BackgroundProcess(Process process) {
			this.process = process;
		}

		public void Start() {
			try {
				process.Start ();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

			} catch (Exception ex) {
				string err = string.Format ("Could not start process: {0}", ex.ToString ());
				errorData.AppendLine (err);
				Action<string> ErrorLineHandler = ErrorLine;
				if (ErrorLineHandler != null) {
					EditorMainThread.Run (()=>{
						ErrorLineHandler(err);
					});
				}
			}
		}

		public void Stop() {
			process.Kill ();
		}

		public void OnGui(){
			
		}

		public void StartAfter(BackgroundProcess backgroundProcess, bool stopOnError = true) {
			backgroundProcess.nextProcess = this;
			backgroundProcess.nextStopOnError = stopOnError;
		}
	}
}
