using System;
using UnityEditor;

namespace CMake.Instructions
{
    public static class GeneralInstructions
    {
        public static GenericInstruction MinimumRequiredVersion(string version)
        {
            return new GenericInstruction()
            {
                Command = $"cmake_minimum_required (VERSION {version})",
                Comment = "Specify the minimum version for CMake"
            };
        }

        public static GenericInstruction MinimumRequiredVersion(Version version)
        {
            return MinimumRequiredVersion(version.ToString());
        }

        public static GenericInstruction ProjectName(string name)
        {
            return new GenericInstruction()
            {
                Command = $"project (\"{name}\")",
                Comment = "Project's name"
            };
        }

        public static Set BuildType(Types.BuildType buildType)
        {
            if (buildType == Types.BuildType.Default)
                buildType = EditorUserBuildSettings.development ? Types.BuildType.Debug : Types.BuildType.Release;

            return new Set()
            {
                Var = "CMAKE_BUILD_TYPE",
                Value = buildType.ToString()
            };
        }
    }
}