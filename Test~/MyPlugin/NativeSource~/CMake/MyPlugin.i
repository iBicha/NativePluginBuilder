%module MyPlugin

// Add necessary symbols to generated header
%{
#include "Plugin.h"
%}

// Process symbols in header
%include "..\Source\Plugin.h"
