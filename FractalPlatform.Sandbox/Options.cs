using System.Collections.Generic;

namespace FractalPlatform.Sandbox
{
    public class Options
    {
        public string BaseUrl { get; set; }

        public string AppName { get; set; }

		public string DeploymentKey { get; set; }

		public string UrlTag { get; set; }

        public List<string> Assemblies { get; set; }
    }
}
