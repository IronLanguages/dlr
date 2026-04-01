// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// System.Threading.Lock was introduced in .NET 9.0; alias to System.Object on older targets
#if NET9_0_OR_GREATER
global using Lock = System.Threading.Lock;
#else
global using Lock = System.Object;
#endif
