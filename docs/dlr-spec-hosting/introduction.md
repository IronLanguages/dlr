# 1 Introduction

One of the top DLR features is common hosting support for all languages implemented on the DLR. The primary goal is supporting .NET applications hosting the DLR’s ScriptRuntime and engines for the following high-level scenarios:

- SilverLight hosting in browsers

- MerlinWeb on the server

- Interaction consoles where the ScriptRuntime is possibly isolated in another app domain.

- Editing tool with colorization, completion, and parameter tips (may only work on live objects in v1)

- PowerShell, C\#, and VB.NET code using dynamic objects and operating on them dynamically in the same app domain

A quick survey of functionality includes:

- Create ScriptRuntimes locally or in remote app domains.

- Execute snippets of code.

- Execute files of code in their own execution context (ScriptScope).

- Explicitly choose language engines to use or just execute files to let the DLR find the right engine.

- Create scopes privately or publicly for executing code in.

- Create scopes, set variables in the scope to provide host object models, and publish the scopes for dynamic languages to import, require, etc.

- Create scopes, set variables to provide object models, and execute code within the scopes.

- Fetch dynamic objects and functions from scopes bound to names or execute expressions that return objects.

- Call dynamic functions as host command implementations or event handlers.

- Get reflection information for object members, parameter information, and documentation.

- Control how files are resolved when dynamic languages import other files of code.

Hosts always start by calling statically on the ScriptRuntime to create a ScriptRuntime. In the simplest case, the host can set globals and execute files that access the globals. In more advanced scenarios, hosts can fully control language engines, get services from them, work with compiled code, explicitly execute code in specific scopes, interact in rich ways with dynamic objects from the ScriptRuntime, and so on.
