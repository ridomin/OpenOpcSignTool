using System;
using System.Collections.Generic;
using System.IO;

namespace OpenVsixSignTool.Core.Tests
{
    public abstract class PackageTestBase : IDisposable
    {
        private readonly List<string> _shadowFiles = new List<string>();

        protected OpcPackage ShadowCopyPackage(string packagePath, out string path, OpcPackageFileMode mode = OpcPackageFileMode.Read)
        {
            var temp = Path.GetTempFileName();
            _shadowFiles.Add(temp);
            File.Copy(packagePath, temp, true);
            path = temp;
            return OpcPackage.Open(temp, mode);
        }


        public void Dispose()
        {
            void CleanUpShadows()
            {
                _shadowFiles.ForEach(File.Delete);
            }
            CleanUpShadows();
        }
    }
}
