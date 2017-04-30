using System;
using System.IO;
using Xunit;

namespace OpenVsixSignTool.Core.Tests
{
    public class OpcPackageVirtualizationTests : PackageTestBase
    {
        [Fact]
        public void ShouldNotWriteToZipForContentTypesUntilPackageClose()
        {
            using (var package = ShadowCopyPackage(@"sample\OpenVsixSignToolTest.vsix", out var path, OpcPackageFileMode.ReadWrite))
            {
                using (Watchdog.Watch(path))
                {
                    package.ContentTypes.Add(new OpcContentType("pdf", "application/pdf", OpcContentTypeMode.Default));
                }
            }
        }

        [Fact]
        public void ShouldNotWriteToZipForNewPartsUntilPackageClose()
        {
            using (var package = ShadowCopyPackage(@"sample\OpenVsixSignToolTest.vsix", out var path, OpcPackageFileMode.ReadWrite))
            {
                using (Watchdog.Watch(path))
                {
                    var part = package.CreatePart(new Uri("/test.txt", UriKind.Relative), "text/plain");
                    using (var stream = part.Open())
                    {
                        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
                    }
                }
            }
        }
    }

    internal class Watchdog : IDisposable
    {
        private readonly FileSystemWatcher _watcher;

        private Watchdog(string path)
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
            _watcher.Changed += (s, o) =>
            {
                Assert.True(false, "File unexpectedly changed on disk.");
            };
            _watcher.Error += delegate
            {
                Assert.True(false, "Couldn't watch file for changes.");
            };
            _watcher.EnableRaisingEvents = true;
        }

        public static IDisposable Watch(string path) => new Watchdog(path);

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
    }
}
