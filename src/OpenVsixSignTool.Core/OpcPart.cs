using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// Represents a part inside of a package.
    /// </summary>
    public class OpcPart : IEquatable<OpcPart>
    {
        internal OpcRelationships _relationships;
        private readonly OpcPackageFileMode _mode;
        private readonly string _path;
        private ZipArchiveEntry _entry;
        private readonly OpcPackage _package;
        private MemoryStream _virtualStream;

        public Uri Uri { get; }
        internal bool IsVirtual { get; set; }


        internal OpcPart(OpcPackage package, string path, ZipArchiveEntry entry, OpcPackageFileMode mode, bool isVirtual)
        {
            Uri = new Uri(OpcPackage.BasePackageUri, path);
            _package = package;
            _path = path;
            _entry = entry;
            _mode = mode;
            IsVirtual = isVirtual;
        }

        public bool Equals(OpcPart other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return Uri.Equals(other.Uri);
        }

        public Stream Open()
        {
            if (_entry == null && !IsVirtual)
            {
                throw new InvalidOperationException("No zip entry for a non-virtual part.");
            }
            if (IsVirtual)
            {
                if (_virtualStream == null)
                {
                    _virtualStream = new MemoryStream();
                }
                else
                {
                    _virtualStream.Position = 0;
                }
                return new VirtualizedMemoryStream(_virtualStream);
            }
            else
            {
                return _entry.Open();
            }
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case OpcPart part: return Equals(part);
                default: return false;
            }
        }

        public OpcRelationships Relationships
        {
            get
            {
                if (_relationships == null)
                {
                    _relationships = ConstructRelationships();
                }
                return _relationships;
            }
        }

        public string ContentType
        {
            get
            {
                var extension = Path.GetExtension(_path)?.TrimStart('.');
                return _package.ContentTypes.FirstOrDefault(ct => string.Equals(ct.Extension, extension, StringComparison.OrdinalIgnoreCase))?.ContentType ?? OpcKnownMimeTypes.OctetString;
            }
        }

        private string GetRelationshipFilePath()
        {
            return Path.Combine(Path.GetDirectoryName(_path), "_rels/" + Path.GetFileName(_path) + ".rels").Replace('\\', '/');
        }

        private OpcRelationships ConstructRelationships()
        {
            var path = GetRelationshipFilePath();
            var entry = _package._archive.GetEntry(path);
            var readOnlyMode = _mode != OpcPackageFileMode.ReadWrite;
            var location = new Uri(OpcPackage.BasePackageUri, path);
            if (entry == null || IsVirtual)
            {
                return new OpcRelationships(location, readOnlyMode);
            }
            else
            {
                using (var stream = entry.Open())
                {
                    return new OpcRelationships(location, XDocument.Load(stream, LoadOptions.PreserveWhitespace), readOnlyMode);
                }
            }
        }

        public override int GetHashCode() => Uri.GetHashCode();

        internal void Materialize()
        {
            if (!IsVirtual)
            {
                return;
            }
            var archiveEntry = _package._archive.CreateEntry(_path);
            if (_virtualStream != null)
            {
                _virtualStream.Position = 0;
                using (var zipStream = archiveEntry.Open())
                {
                    _virtualStream.CopyTo(zipStream);
                }
            }
            _entry = archiveEntry;
            IsVirtual = false;
            _virtualStream?.Dispose();
            _virtualStream = null;
        }

        private class VirtualizedMemoryStream : Stream
        {
            private readonly MemoryStream _stream;

            public VirtualizedMemoryStream(MemoryStream stream)
            {
                _stream = stream;
            }

            public override bool CanRead => _stream.CanRead;

            public override bool CanSeek => _stream.CanSeek;

            public override bool CanWrite => _stream.CanWrite;

            public override long Length => _stream.Length;

            public override long Position
            {
                get => _stream.Position;
                set => _stream.Position = value;
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _stream.Write(buffer, offset, count);
            }
        }
    }
}