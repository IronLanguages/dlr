# 1 Introduction

This document demonstrates how to implement a very simple language, SymPL, using the Dynamic Language Runtime (DLR) as it ships in .NET 4.0. The reader should have some basic familiarity with language implementation concepts such as compiler components and runtime support. Mostly curiosity will be enough for reading this document. The Sympl implementation does not show production quality .NET interoperability such as IronPython has. For example, SymPL mostly just binds to .NET functions by matching argument and parameter counts and whether the runtime argument types are assignable to the parameter types. For serious language implementers, this document assumes you have deeper .NET awareness for how types and the CLR work. The goal for serious language implementers is to make you aware of how to get started building a language on the DLR.

The SymPL language implementation demonstrates the following:

- Using DLR Expression Trees (which include LINQ Expression Trees v1) for code generation

- Using DLR dynamic dispatch caching

- Building runtime binders

- Building dynamic objects and dynamic meta-objects

- Supporting dynamic language/object interoperability

- Very simple hosting with application supplied global objects

- Basic arithmetic and comparison operators with fast DLR dispatch caching

- Control flow

- Name binding within Sympl

- Method and top-level function invocation with fast DLR dispatch caching

- Very simple .NET interoperability for runtime method and operation binding

- Closures

- Assignment with various left-hand-side expressions

Before reading this document you may want to read dlr-overview.doc from [www.codeplex.com/dlr](http://www.codeplex.com/dlr) to get a general grasp of the DLR. You should read, either before or simultaneously with this document, the sites-binders-dynobj-interop.doc document from the same web site. This document refers to the latter now and then for more detailed background.

<h2 id="sources">1.1 Sources</h2>

All the source code for the DLR releases regularly on [www.codeplex.com/dlr](http://www.codeplex.com/dlr), with weekly source updates at least. The full sources for the language used in this document are under &lt;installdir&gt;\\languages\\sympl\\.

As a side note, the implementation was started in IronPython as an experiment in using the DLR functionality from a dynamic language. Using a dynamic language with a REPL would provide some productivity gains in exploring implementation techniques and .NET-isms. The experiment was beneficial for flushing out some implementation and design issues in IronPython and DLR. It showed where the dynamic language made some things easier. It also showed some things that were more straightforward if done in C\# or VB due to the nature of the DLR APIs and some hurdles created by IronPython itself being implemented on the DLR. Since the code for both are in the source tree, you can see some of the IronPython comments that note where the C\# implementation was easier.

<h2 id="walkthrough-organization">1.2 Walkthrough Organization</h2>

The walkthrough starts with Hello World and the entire infrastructure needed for it. There is a surprising amount required if you do not have a built-in print function. Having a built in print function would not be very interesting. The rest of the document flows as the Sympl implementation actually evolved. One might argue for a different organization, say, with all the runtime binder descriptions in one place. However, the fundamental point of this document is to walk you through how to start your own language.

There's a fairly natural order to how things were added that shows one good way to proceed. We build some fundamental infrastructure in a basic end-to-end scenario, Hello World. Then we add function definitions and invocations, some simple control flow, and some built-in types and constants. At this point, there's enough language to write a little library. Also, as we incrementally add instantiation, instance member access, and indexing, we can write interesting programs or tests. Later topics require more work and DLR concepts, so doing some easier features to flesh out Sympl after Hello World works well.
