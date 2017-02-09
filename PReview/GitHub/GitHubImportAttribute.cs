using GitHub.Services;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;

namespace PReview.GitHub
{
    public class GitHubImportAttribute : ImportAttribute
    {
        public GitHubImportAttribute(string assemblyName, string typeName) : base(FindType(assemblyName, typeName))
        {
        }

        static Type FindType(string assemblyName, string typeName)
        {
            var baseDir = Path.GetDirectoryName(typeof(IGitHubServiceProvider).Assembly.Location);
            var asmFile = Path.Combine(baseDir, assemblyName + ".dll");
            var asm = Assembly.LoadFrom(asmFile);
            return asm.GetType(typeName);
        }
    }
}
