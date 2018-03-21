using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Diagnostics;
using System.Text;
using System;

namespace iBicha
{
	public class BackgroundProcess {

		public string Name { get; set; }

        public delegate void ExitedDelegate(int exitCode, string outputData, string errorData);
		public event ExitedDelegate Exited;

		public event Action<string> OutputLine;
		public event Action<string> ErrorLine;

        public StringBuilder OutputData { get; private set; }
        public StringBuilder ErrorData { get; private set; }

        public Process Process { get; private set; }
        public string LastLine { get; private set; }

        public BackgroundProcess nextProcess { get; private set; }
        private bool nextStopOnError;

		public BackgroundProcess(ProcessStartInfo startInfo) {
			OutputData = new StringBuilder ();
			ErrorData = new StringBuilder ();
			Process = new Process ();
			Process.StartInfo = startInfo;
			Process.StartInfo.UseShellExecute = false;
			Process.StartInfo.CreateNoWindow = true;
			Process.StartInfo.RedirectStandardOutput = true;
			Process.StartInfo.RedirectStandardError = true;

			Process.EnableRaisingEvents = true;

			Process.OutputDataReceived += Process_OutputDataReceived;
			Process.ErrorDataReceived += Process_ErrorDataReceived;
			Process.Exited += Process_Exited;
		}

		void Process_Exited (object sender, System.EventArgs e)
		{
			if (Process.ExitCode != 0) {
				ErrorData.Insert (0, string.Format ("Exit code: {0}\n", Process.ExitCode));
			}

			ExitedDelegate ExitedHandler = Exited;
			if (ExitedHandler != null) {
				EditorMainThread.Run (()=>{
					ExitedHandler(Process.ExitCode, OutputData.ToString().Trim(), ErrorData.ToString().Trim());
				});
			}

			if (nextProcess != null) {
				if (Process.ExitCode == 0 || !nextStopOnError) {
					nextProcess.Start ();
				}
			}
		}

		void Process_ErrorDataReceived (object sender, DataReceivedEventArgs e)
		{
			ErrorData.AppendLine (e.Data);
            LastLine = e.Data;
			Action<string> ErrorLineHandler = ErrorLine;
			if (ErrorLineHandler != null) {
				EditorMainThread.Run (()=>{
					ErrorLineHandler(e.Data);
				});
			}

		}

		void Process_OutputDataReceived (object sender, DataReceivedEventArgs e)
		{
            LastLine = e.Data;
			OutputData.AppendLine (e.Data);
			Action<string> OutputLineHandler = OutputLine;
			if (OutputLineHandler != null) {
				EditorMainThread.Run (()=>{
					OutputLineHandler(e.Data);
				});
			}
		}

		public BackgroundProcess(Process process) {
			this.Process = process;
		}

		public void Start() {
			try {
				Process.Start ();

				Process.BeginOutputReadLine();
				Process.BeginErrorReadLine();

				BackgroundProcessManager.Add(this);

			} catch (Exception ex) {
				string err = string.Format ("Could not start process: {0}", ex.ToString ());
				ErrorData.AppendLine (err);
				Action<string> ErrorLineHandler = ErrorLine;
				if (ErrorLineHandler != null) {
					EditorMainThread.Run (()=>{
						ErrorLineHandler(err);
					});
				}
			}
		}

		public void Stop() {
			Process.Kill ();
		}
        
		public void StartAfter(BackgroundProcess backgroundProcess, bool stopOnError = true) {
			backgroundProcess.nextProcess = this;
			backgroundProcess.nextStopOnError = stopOnError;
		}
	}
}
