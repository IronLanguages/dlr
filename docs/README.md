# DLR Documents Guide

This directory contains historical DLR design documents, each document available in two formats: `.doc` and `.pdf`. Though various details about the DLR may no longer be accurate, the higher level concepts still stand.


## Document Index

| Topic | DOC | PDF | PDF pages | Best for | Summary |
| --- | --- | --- | ---: | --- | --- |
| DLR overview | [dlr-overview.doc](./dlr-overview.doc) | [dlr-overview.pdf](./dlr-overview.pdf) | 46 | Everyone new to the codebase | Broad introduction to the DLR's mission, architecture, hosting model, dynamic call sites, expression trees, and interoperability story. This is the orientation document that explains how the major subsystems fit together. |
| Hosting specification | [dlr-spec-hosting.doc](./dlr-spec-hosting.doc) | [dlr-spec-hosting.pdf](./dlr-spec-hosting.pdf) | 94 | Developers working on hosting APIs, embedding, or tooling | Detailed hosting model and API reference for `ScriptRuntime`, `ScriptScope`, `ScriptEngine`, `ScriptSource`, `CompiledCode`, and `ObjectOperations`. It also captures intended hosting scenarios, layering, and requirements from the original design. |
| Expression trees v2 specification | [expr-tree-spec.doc](./expr-tree-spec.doc) | [expr-tree-spec.pdf](./expr-tree-spec.pdf) | 173 | Developers changing compiler/runtime internals | Deep specification for the extended expression tree model used by the DLR, including reducible nodes, dynamic nodes, control flow, assignments, scoping, visitors, and API details. This is the most reference-heavy and least suitable as a first read. |
| Library author introduction | [library-authors-introduction.doc](./library-authors-introduction.doc) | [library-authors-introduction.pdf](./library-authors-introduction.pdf) | 19 | Developers focused on dynamic object support from application or library code | Practical introduction to using `ExpandoObject`, `DynamicObject`, and `IDynamicMetaObjectProvider` from the consumer side. It is short, example-driven, and the quickest way to understand how ordinary .NET code participates in DLR dynamic dispatch. |
| Sites, binders, and dynamic object interop | [sites-binders-dynobj-interop.doc](./sites-binders-dynobj-interop.doc) | [sites-binders-dynobj-interop.pdf](./sites-binders-dynobj-interop.pdf) | 40 | Developers touching binders, call-site caching, or dynamic object protocol | Focused explanation of dynamic call sites, binder responsibilities, cache levels, `DynamicMetaObject`, `DynamicMetaObjectBinder`, and interoperability conventions. This is the core conceptual bridge between the overview and the lower-level implementation work. |
| Sympl implementation walkthrough | [sympl.doc](./sympl.doc) | [sympl.pdf](./sympl.pdf) | 79 | Developers who learn best from an end-to-end example language | Step-by-step walkthrough of implementing a small language on the DLR. It covers hosting, globals, binders, expression tree generation, dynamic operations, interop, closures, control flow, and the resulting language design. It is the most concrete document in the set. |

## Recommended Study Plan

### Phase 1: Build the mental model

1. Start with [dlr-overview.pdf](./dlr-overview.pdf).
2. Read [library-authors-introduction.pdf](./library-authors-introduction.pdf).
3. Read [sites-binders-dynobj-interop.pdf](./sites-binders-dynobj-interop.pdf).

Why this order:

- `dlr-overview` gives the vocabulary and the architecture map.
- `library-authors-introduction` makes the object model concrete before the lower-level machinery.
- `sites-binders-dynobj-interop` then explains how the machinery actually works.

Expected outcome:

- You should understand what the DLR is responsible for.
- You should be comfortable with `DynamicObject`, `ExpandoObject`, `IDynamicMetaObjectProvider`, binders, and call sites.

### Phase 2: Connect concepts to a working implementation

4. Read [sympl.pdf](./sympl.pdf).

Why now:

- Sympl turns the abstract concepts into an end-to-end implementation.
- It is the best document for mapping the design vocabulary onto real compiler, binder, and runtime code.

Expected outcome:

- You should be able to follow how parsing, analysis, expression-tree generation, and runtime binding cooperate in a DLR language.

### Phase 3: Read the subsystem references you actually need

5. Read [dlr-spec-hosting.pdf](./dlr-spec-hosting.pdf) if you are working on embedding, scripting surfaces, REPL support, or tooling.
6. Read [expr-tree-spec.pdf](./expr-tree-spec.pdf) if you are changing compiler output, tree rewriting, code generation, or expression-tree semantics.

Why this order:

- These are both reference documents rather than onboarding documents.
- They are easier to digest after the architectural and example-driven material.

Expected outcome:

- You should have the detailed API and semantic background needed for subsystem-specific work.

## Fast Paths By Role

If you are mostly working on hosting and scripting APIs:

1. [dlr-overview.pdf](./dlr-overview.pdf)
2. [dlr-spec-hosting.pdf](./dlr-spec-hosting.pdf)
3. [sites-binders-dynobj-interop.pdf](./sites-binders-dynobj-interop.pdf)

If you are mostly working on dynamic binding or interoperability:

1. [dlr-overview.pdf](./dlr-overview.pdf)
2. [library-authors-introduction.pdf](./library-authors-introduction.pdf)
3. [sites-binders-dynobj-interop.pdf](./sites-binders-dynobj-interop.pdf)
4. [sympl.pdf](./sympl.pdf)

If you are mostly working on compiler or expression-tree internals:

1. [dlr-overview.pdf](./dlr-overview.pdf)
2. [sites-binders-dynobj-interop.pdf](./sites-binders-dynobj-interop.pdf)
3. [sympl.pdf](./sympl.pdf)
4. [expr-tree-spec.pdf](./expr-tree-spec.pdf)

## Notes for Current Contributors

- These documents are historical design material, so some names and scenarios reflect the original .NET 4 era rather than the current repository layout.
- When a document disagrees with the current implementation, treat the code as authoritative and use the document for design intent and terminology.
- For most readers, the first 10 to 20 pages of `expr-tree-spec` are much more valuable than reading the whole API reference straight through.