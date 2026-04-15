using System;
using System.Management.Automation;
using System.Reflection;

namespace PESpy.PowerShell
{
    public abstract class FileCmdlet : PSCmdlet
    {
        //We need this to be in a non-generic base class so this isn't hooked up for every type
        //of FileCmdlet<T> we create
        static FileCmdlet()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var dir = System.IO.Path.GetDirectoryName(typeof(FileCmdlet<>).Assembly.Location);

            var fileName = args.Name.Split(',')[0];

            var path = System.IO.Path.Combine(dir, $"{fileName}.dll");

            if (!System.IO.File.Exists(path))
                return null;

            return Assembly.LoadFile(path);
        }

        protected bool HasParameter(string name) => MyInvocation.BoundParameters.ContainsKey(name);
    }
}
