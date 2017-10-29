using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace iBicha
{
	public class LinuxBuilder : PluginBuilderBase {
		public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			throw new System.NotImplementedException ();
		}

		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);
		}

	}
}