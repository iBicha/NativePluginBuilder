using System.Text;

namespace Swig
{
    public class SwigInterface
    {
        public string PluginName { get; set; }
        public string HeaderFile { get; set; }

        public SwigInterface(string pluginName, string headerFile)
        {
            PluginName = pluginName;
            HeaderFile = headerFile;
        }

        //Todo: make a template in the resources folder
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"%module {PluginName}");
            sb.AppendLine();
            sb.AppendLine("// Add necessary symbols to generated header");
            sb.AppendLine("%{");
            sb.AppendLine($"#include \"{HeaderFile}\"");
            sb.AppendLine("%}");
            sb.AppendLine();
            sb.AppendLine("// Process symbols in header");
            sb.AppendLine($"%include \"{HeaderFile}\"");
            return sb.ToString();
        }
    }
}