# 2 Quick Language Overview

This is a high-level description of language features to set the stage for starting to discuss the implementation. For a more detailed description (no language lawyer formal specification :-)), see section . For quick view of the simple hosting and runtime API, see section .

SymPL stands for symbolic programming language. It is pronounced as "simple" and is usually written as "Sympl". It looks like a Lisp variant, but it lacks several of Lisp's semantics. For example, Sympl does NOT:

- evaluate identifiers by indirecting through a symbol's value cell (Sympl is like Common Lisp lexical variables)

- have symbols with distinct value and function value cells (only value cells)

- *read* all code into a list literal, have a \*readtable\*, read macros, etc.

- allow all identifiers to be redefined (Sympl has keywords)

Sympl has very minimal language features and simple .NET interoperability for instructional purposes only. Sympl has:

- pure expression-based semantics (easy with Expression Trees v2)

- a simplified module mechanism akin to python's

- hosting model for executing files and snippets within host-provided scopes

- top-level functions and global variables within file modules

- closures (free with the DLR)

- basic data types: int, string, double

- basic control flow: if, loop, function call

- basic arithmetic, Boolean, and comparison operations

- infix dot for accessing data and function members

- case-insensitive identifiers
