﻿using System;

namespace OpenVsixSignTool.Core
{
    public static class UriHelpers
    {
        private static Uri PackageBaseUri = new Uri("package:///", UriKind.Absolute);
        private static Uri RootedPackageBaseUri = new Uri("package:", UriKind.Absolute);


        /// <summary>
        /// Converts a package URI to a path within the package zip file.
        /// </summary>
        /// <param name="partUri">The URI to convert.</param>
        /// <returns>A string to the path in a zip file.</returns>
        public static string ToPackagePath(this Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(PackageBaseUri, partUri);
            var pathUri = new Uri(absolute.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped), UriKind.Absolute);
            var resolved = PackageBaseUri.MakeRelativeUri(pathUri);
            return resolved.ToString();
        }

        public static string ToQualifiedPath(this Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(RootedPackageBaseUri, partUri);
            var pathUri = new Uri(absolute.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.Unescaped), UriKind.Absolute);
            var resolved = RootedPackageBaseUri.MakeRelativeUri(pathUri);
            return resolved.ToString();
        }

        public static Uri ToQualifiedUri(this Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(RootedPackageBaseUri, partUri);
            var pathUri = new Uri(absolute.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.Unescaped), UriKind.Absolute);
            return RootedPackageBaseUri.MakeRelativeUri(pathUri);
        }

        public static bool EqualOrContainedBy(this Uri uri, Uri parent)
        {
            var seperator = new char[] { '/' };
            //These should be canonicalized by the Uri class already.
            var fullPath = uri.ToPackagePath();
            var parentPath = parent.ToPackagePath();
            var fullPathComponents = fullPath.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            var parentPathComponents = parentPath.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            if (fullPathComponents.Length < parentPathComponents.Length)
            {
                return false;
            }
            for(var i = 0; i < parentPathComponents.Length; i++)
            {
                if (!parentPathComponents[i].Equals(fullPathComponents[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
