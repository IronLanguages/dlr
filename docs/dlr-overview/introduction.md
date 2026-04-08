# 1 Introduction

The Dynamic Language Runtime (DLR) is a set of libraries built on the CLR to support dynamic language implementations on .NET. The DLR's mission is to enable an ecosystem of dynamic languages on .NET. A key value proposition of the .NET CLR is supporting multiple languages and allowing them to interoperate with each other. Dynamic languages have become very popular in the last several years. Customers want to use their favorite dynamic language and have great .NET interoperability for building applications and providing scripting for applications. The DLR makes it very easy to develop dynamic languages on .NET.

The DLR also has support for existing languages on .NET. If you already have a language implemented on .NET, you might want to add dynamic dispatch capabilities. As with C\# 4.0, this enables the language to support very nice expressions (syntactically light) when working with dynamic objects via COM, HTML DOM, or .NET reflection. The DLR provides an entry point for you to just use the fast dynamic dispatch.

The DLR has high-level support for library authors too. If you have a library for crawling through XML or working with JSON objects, you'd really like to enable your objects to appear as dynamic objects to C\# 4.0 and dynamic languages. This lets consumers write syntactically simpler and more natural code for accessing members, drilling into, and operating on your objects. The DLR provides two high-level helper objects so that library authors do not have to work at the level of language implementers to support dynamic operations.

The DLR provides three key components:

- language implementation services with language interoperability model

- dynamic language runtime services with fast dynamic dispatch and library support

- common hosting APIs across languages

Language implementers get great .NET interoperability. They also have several mechanisms for keeping their language true to its semantics and feel. See sections 1.1 and 2 for summaries of these components.

The key goals of the DLR are making it easy to

- port dynamic languages to .NET

- add dynamic features to your existing language

- author libraries whose objects support dynamic operations

- employ dynamic languages in your applications and frameworks.

The following sections provide an overview of the DLR, its overall architecture, and introductions to its key components.

<h2 id="key-dlr-advantages">1.1 Key DLR Advantages</h2>

**For language implementers the DLR lowers the bar considerably for porting a language to .NET**. Traditionally, implementers needed to build lexers, parsers, semantic analysis, optimization passes, code generation, runtime support, and so on. Virtual machines lowered the bar so that languages could emit a higher-level intermediate language instead of fully optimized machine code. The DLR essentially only requires languages to produce a bound abstract semantic tree (.NET Expression trees) and some runtime helpers if needed. The DLR and .NET do the rest of the work.

**Languages implemented using the DLR continually benefit from improvements lower down the stack**. Microsoft designed the .NET Framework to support a broad range of programming languages on the Common Language Runtime (CLR). The CLR provides shared services to these languages including garbage collection, just-in-time (JIT) compilation, a sandboxed security model, and support for tools integration. Sharing libraries and frameworks allows languages new to the CLR to build on the work of others. When .NET releases a new version with performance gains, for example, your language immediately benefits. When the DLR adds optimizations such as better compilation, language performance improves for everyone.

**In the Dynamic Language Runtime we provide common language interoperability, fast dynamic invocation, and some utilities**. The language interoperability story is based on a protocol for objects implemented in one language to be used by other languages. With dynamic typing, the object is king for determining if it can support a particular message or operation sent to it. Similarly the DLR enables dynamic objects to participate in a message passing protocol for negotiating how to perform abstract operations on any object. The fast dynamic dispatch is based on polymorphic inline caching. Dynamic objects can also participate in the fast dynamic invocation so that a particular call site can cache implementations of abstract operations from the calling language or from objects implemented by other languages.

**With common hosting APIs, applications can use any language supporting the DLR hosting model**. At a very high level, the DLR provides multiple script runtime environments per AppDomain, as well as remote script runtimes in other AppDomains. Hosts can inject global variable bindings and execute files or snippets of code in the context of those bindings. Hosts can create individual scopes of variable bindings and execute code in them. After executing code, hosts can extract scope variables or globals to hook up event handlers, command implementations, etc. Hosts can also invoke dynamic operations on dynamic objects living in the script runtimes.

<h2 id="open-source-projects">1.2 Open Source Projects</h2>

Most of our DLR languages and all the DLR source code are available on CodePlex and RubyForge. The sources are available under the Microsoft Public License, which is Open Software Initiative approved. The DLR and all of our open source languages are available for one-stop shopping at [www.codeplex.com/dlr](http://www.codeplex.com/dlr), where we will add more samples, specs, and getting started documentation over time. Microsoft currently provides two DLR languages, IronPython and IronRuby. IronPython is available open source at [www.codeplex.com/ironpython](http://www.codeplex.com/ironpython). IronRuby is available open source at <http://rubyforge.org/projects/ironruby>.

<h2 id="why-dynamic-languages">1.3 Why Dynamic Languages</h2>

This section introduces motivations for supporting Dynamic languages or adding dynamic features to a static language, which is occurring more and more these days. This debate with language designers is a classic religious battle. This section is not in any way a complete rhetoric for why you should embrace dynamic languages, and in no way does it try to say you should only use dynamic languages. This is just a brief treatment of some common reasons people cite for interest in dynamic languages.

Dynamic languages are one of those cyclic technologies that have become vogue again. They were hot in the 80s for scripting and in any startup claiming to do AI. They became hot again due to the web. The web is essentially built on dynamic languages and View Source. Due to the web, dynamic language are not only here to stay, but static languages are adopting dynamic features to make them more productive for web development and working with inherently dynamic modeling objects.

Many dynamic languages are popping up over the last 10+ years: JavaScript, PHP, Ruby, Python, ColdFusion, LUA, Cobra, Groovy, Newspeak, and more. Some popular dynamic languages (or those with dynamic features) have been around for quite a while: Perl, VB, Smalltalk, Lisp, and Scheme. While some of these aren't used as prevalently today as they were at one time, they are still popular with some programmers and in use today.

The communities around dynamic languages are very strong. They have very deep passions for their languages because they feel their languages lend themselves to a high degree of productivity. These programmers will use dynamic languages whenever possible. They will use them for infrastructure that maintains systems, scripting of applications, building whole applications, and so on.

A key productivity aspect of many dynamic languages is the ability to use a rapid feedback loop (REPL, or read-eval-print loop). This lets you enter snippets of code and hit enter to immediately see the results of executing the code. The ability to iteratively develop code by working with live objects is a very powerful mechanism for discovering how to use an API and experimenting with solutions to problems. Because dynamic languages are tolerant of unimplemented surface area, REPLs also support simultaneous top-down and bottom-up development. This means you can start with high-level functions and make calls to as-yet unimplemented functions. Then you can fill in underlying implementation or low-level utilities as you need them or further develop a branch of code.

Dynamic languages lend themselves to refactoring and making code changes more rapidly. Code is always evolving as implementation feeds back on design or as requirements change. You do not have to fix up static type declarations everywhere to make logical changes in your application. There are often a lot fewer textual changes to the code to update the logic. Dynamic languages also usually support features such as optional or named parameters that aid making changes to definitions and having a lighter-weight experience fixing up call sites.

Meta-programming with macros or syntactic flexibility are features often provided by dynamic languages. These are powerful mechanisms for defining domain-specific languages or extending a language in a more natural way than adding a library of functions. Macros can make code much more expressible, increasing productivity, in mature systems with domain-specific patterns or coding needs.

Dynamic languages make great glue code for snapping together applications or extensions from a palette of components. Due to the productivity of dynamic languages, ability to re-load code and keep running in a live runtime, and ability to work with types loosely, they make excellent scripting languages. Applications can host dynamic languages, provide an object model, and easily let customers extend the application with new commands and functionality.

Some common uses of dynamic languages:

- Scripting applications

- Building web sites

- Test harnesses

- Server farm maintenance

- One-off utilities or data crunching
