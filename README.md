# Unity Native Plugin Builder
Unity Native Plugin Builder is an Editor extension to automate the creation and the cross platform compilation of native Unity plugins.

Based on the CMake build system, you can create native c++ plugins, manage multiple build options and configurations, compile code, and copy binaries to the correct plugin folder in the Unity project, all with a press of a button.

## Prerequisite software
Make sure to install [CMake](https://cmake.org/download/ "CMake download page"), if not installed already.

## Supported platforms
Multiple plugins with multiple builds are supported.

These platforms are supported (when available):

* Android
* iOS
* Standalone (Windows, OS X, Linux)
* WebGL

We are looking into supporting more platforms (Universal Windows, Console, etc)

## Getting started
1. Added NativePluginBuilder to your `Assets` folder
2. Go to `Window -> Native Plugin Builder`
3. Go to the settings tab, and make sure that there is a CMake verion in the editor.
3. Go to the plugins tab, and create a new plugin
4. A new folder is now created. Add the `MyPluginExample.cs` script to a `GameObject`
5. Build the plugin for the Editor (The default build options should work for the editor)  
6.Hit play!

## The editor
Using the editor you can create and define multiple plugins each with multiple build options.

<img src="https://raw.github.com/iBicha/UnityNativePluginBuilder/master/Screenshots/screen1.png" height="500">

* The name of the plugin will be used as the name of the library and the name of the C# class. Make sure it's a valid indentifier. It can only be set in creation time.
* The version and the build number are accessible to your c++ code through `PLUGIN_VERSION` and `PLUGIN_BUILD_NUMBER` defines
* Make sure the paths are valid (They should be, by default):
  * Source Folder should point to your c++ files location
  * Build Folder is where the build scripts will be created. If you are using version control, this folder should usually be ignored.
  * Plugin Folder is where the binaries will be after compilation
* You can add the `PluginAPI` folder in the include search paths, so you can use headers such as `IUnityInterface.h`. Simply check the `Include Plugin API` option.
* You can also add your custom defines, that will be accessible from c++.
* You can add multiple build options for different platforms and architectures
* The `Build` button will compile your plugin for all the *active* build options
* The `Clean` button will "TRY" to delete all folders and files from the Build folder
* The `Remove` button will delete the asset file for the plugin settings. The source code will not be deleted.
* Each running process responsible for a build task, will show up in the status bar at the bottom, which you can cancel with the `Stop button`
* If you have more than one plugin, a `Build All` and `Clean All` option will be available.


## Contribution:

PRs and issues are always welcome.
This project is in early stages, and does not implement complex features, such as the ones supported by CMake. But it will grow more and more, as developers meet specific needs for their c++ projects.
If you see a missing feature or an error, please do report it.
