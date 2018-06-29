﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Security;

namespace Microsoft.Scripting.Metadata {
    public static class MetadataExtensions {
        public static bool IsNested(this TypeAttributes attrs) {
            switch (attrs & TypeAttributes.VisibilityMask) {
                case TypeAttributes.Public:
                case TypeAttributes.NotPublic:
                    return false;

                default:
                    return true;
            }
        }

        public static bool IsForwarder(this TypeAttributes attrs) {
            return (attrs & (TypeAttributes)0x00200000) != 0;
        }

        public static AssemblyName GetAssemblyName(this AssemblyRef assemblyRef) {
            return CreateAssemblyName(assemblyRef.Name, assemblyRef.Culture, assemblyRef.Version, assemblyRef.NameFlags, assemblyRef.GetPublicKeyOrToken());
        }

        public static AssemblyName GetAssemblyName(this AssemblyDef assemblyDef) {
            return CreateAssemblyName(assemblyDef.Name, assemblyDef.Culture, assemblyDef.Version, assemblyDef.NameFlags, assemblyDef.GetPublicKey());
        }

        private static AssemblyName CreateAssemblyName(MetadataName name, MetadataName culture, Version version, AssemblyNameFlags flags, byte[] publicKeyOrToken) {
            var result = new AssemblyName();

            result.Name = name.ToString();
            if (!culture.IsEmpty) {
                result.CultureInfo = new CultureInfo(culture.ToString());
            }

            result.Version = version;
            result.Flags = flags;

            if (publicKeyOrToken.Length != 0) {
                if ((result.Flags & AssemblyNameFlags.PublicKey) != 0) {
                    result.SetPublicKey(publicKeyOrToken);
                } else {
                    result.SetPublicKeyToken(publicKeyOrToken);
                }
            }

            return result;
        }

        public static MetadataTables GetMetadataTables(this Module module) {
            return MetadataTables.OpenFile(module.FullyQualifiedName);
        }
    }
}
