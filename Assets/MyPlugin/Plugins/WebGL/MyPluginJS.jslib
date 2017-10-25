//A prefix to add to every function in the plugin
const PluginPrefix = "MyPlugin_";

//Plugin implementation
var MyPlugin = {
	confirm : function (messagePtr) {
		Helper.Static.confirmCallCount++;
		var message = Helper.ToJsString(messagePtr);
		return confirm(message);
	}, 
	getConfirmCallCount () {
		return Helper.Static.confirmCallCount;
	}
};

//Helper functions
var Helper = {
	//A static variable to store data between API calls
	Static: {
		confirmCallCount: 0
	},
	
	//Convert a Javascript string to a C# string
    ToCsString: function (str) 
    {
        if (typeof str === 'object') {
            str = JSON.stringify(str);
        }
        var bufferLength = lengthBytesUTF8(str) + 1;
        var buffer = _malloc(bufferLength);
        stringToUTF8(str, buffer, bufferLength);
        return buffer;
    },

    //Convert a C# string pointer to a Javascript string
	ToJsString: function (ptr) {
		return Pointer_stringify(ptr);
	},

	//Convert a C# json string pointer to a Javascript object
	ToJsObject: function (ptr) {
		var str = Pointer_stringify(ptr);
		try {
			return JSON.parse(str);
		} catch (e) {
			return null;
		}
	},

	//free allocated memory of a C# pointer
	FreeMemory: function (ptr) {
		_free(ptr);
	}
};

//Plugin merge function
function MergePlugin(plugin, prefix) {
	//prefix
	if(prefix) {
		for (var key in plugin) {
			if (plugin.hasOwnProperty(key)) {
				plugin[prefix + key] = plugin[key];
				delete plugin[key];
			}
		}
	}
	//helper
	if(Helper) {
		plugin.$Helper = Helper;
		autoAddDeps(plugin, '$Helper');
	}
	//merge
	mergeInto(LibraryManager.library, plugin);
}

MergePlugin(MyPlugin, PluginPrefix);