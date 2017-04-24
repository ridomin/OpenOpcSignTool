﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenVsixSignTool
{
    /// <summary>
    /// A builder to sign an OPC package.
    /// </summary>
    public class OpcPackageSignatureBuilder
    {
        private readonly OpcPackage _package;
        private readonly List<OpcPart> _enqueuedParts;

        internal OpcPackageSignatureBuilder(OpcPackage package)
        {
            _enqueuedParts = new List<OpcPart>();
            _package = package;
        }

        /// <summary>
        /// Enqueues a part that will be part of the package signature.
        /// </summary>
        /// <param name="part">The part to enqueue.</param>
        public void EnqueuePart(OpcPart part) => _enqueuedParts.Add(part);

        /// <summary>
        /// Dequeues a part from the signature builder. This file will not be part of the signature.
        /// </summary>
        /// <param name="part">The part to dequeue.</param>
        /// <returns>True if the file was dequeued, otherwise false.</returns>
        public bool DequeuePart(OpcPart part) => _enqueuedParts.Remove(part);

        /// <summary>
        /// Creates a signature from the enqueued parts.
        /// </summary>
        /// <param name="fileDigestAlgorithm">The hash algorithm used to digest the files. The recommended value is <see cref="HashAlgorithmName.SHA256"/>.</param>
        /// <param name="certificate">The certificate to sign with.</param>
        /// <returns></returns>
        public object Sign(HashAlgorithmName fileDigestAlgorithm, X509Certificate2 certificate)
        {
            var originFileUri = new Uri("package:///package/services/digital-signature/origin.psdor", UriKind.Absolute);
            string signatureUriRoot = "package:///package/services/digital-signature/xml-signature/";
            var originFileRelationship = _package.Relationships.FirstOrDefault(r => r.Type.Equals(OpcKnownUris.DigitalSignatureOrigin));

            OpcPart originFile;
            OpcPart signatureFile;
            //Create the origin file and relationship to the origin file if needed.
            if (originFileRelationship != null)
            {
                originFile = _package.GetPart(originFileRelationship.Target) ?? _package.CreatePart(originFileUri, OpcKnownMimeTypes.DigitalSignatureOrigin);
            }
            else
            {
                originFile = _package.GetPart(originFileUri) ?? _package.CreatePart(originFileUri, OpcKnownMimeTypes.DigitalSignatureOrigin);
                _package.Relationships.Add(new OpcRelationship(originFile.Uri, OpcKnownUris.DigitalSignatureOrigin));
            }

            var signatureRelationship = originFile.Relationships.FirstOrDefault(r => r.Type.Equals(OpcKnownUris.DigitalSignatureSignature));
            if (signatureRelationship != null)
            {
                signatureFile = _package.GetPart(signatureRelationship.Target) ?? _package.CreatePart(originFileUri, OpcKnownMimeTypes.DigitalSignatureSignature);
            }
            else
            {
                var target = new Uri(signatureUriRoot + certificate.GetCertHashString() + ".psdsxs", UriKind.Absolute);
                signatureFile = _package.GetPart(target) ?? _package.CreatePart(target, OpcKnownMimeTypes.DigitalSignatureSignature);
                originFile.Relationships.Add(new OpcRelationship(target, OpcKnownUris.DigitalSignatureSignature));
            }

            _package.Flush();
            var allParts = new List<OpcPart>(_enqueuedParts);
            allParts.Add(originFile);
            allParts.Add(_package.GetPart(_package.Relationships.DocumentUri));
            allParts.Add(_package.GetPart(originFile.Relationships.DocumentUri));
            var fileManifest = OpcSignatureManifest.Build(fileDigestAlgorithm, allParts);
            var signature = new OpcXmlSignature(fileManifest, certificate);
            return null;
        }
    }
}