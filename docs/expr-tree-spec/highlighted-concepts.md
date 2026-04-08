# 2 Highlighted Concepts

There are several high-level ideas or characteristics about the ET v2 design that are worth calling out. Think of these as high-order bit calls to make or top ten questions language implementers will be thinking as they approach a semantic model of code.

<h2 id="expression-based-model">2.1 Expression-based Model</h2>

One high-order bit to language design is whether to be expression-based. Do you have distinct notions of statements or control flow, or do you have a common concept of evaluating expressions where everything has a value. We decided to stay expression-based, so statements are modeled as having a result value and type.

There are several reasons for this design:

- Expression remains the base type for all ET nodes, and we avoid dual type hierarchies.

- Void is already allowed as a type, indicating there is no return value for an expression.

- Lambdas don’t change at all from v1 to v2.

- Being expression-based matches many languages (Lisp, Scheme, Ruby, F\#), and it does no harm when modeling other languages. They can easily make expressions be void returning.

Let's look at a couple of examples:

- BlockExpression has a value. By default its value is the last expression in the sequence, and its type is the same type as the last expression. This design also allows us to avoid another CommaExpression since BlockExpression now models this semantics as well.

- We model 'if' with ConditionalExpression, which returns the value of its consequent or alternative expression, whichever executes. The types of the branches must match the type of the ConditionalExpression. If there's no alternative expression, then we use a DefaultExpression with the matching type. Languages with distinct notions for statements often have a 'e1 ? e2 : e3' expression since their 'if' cannot return values, but they can model this with ConditionalExpression.

DefaultExpression serves two useful purposes in our expression-based model. First the Expression.Empty factory returns a DefaultExpression with Type void. This can be useful if you need an expression in a value-resulting position that matches the containing expressions result type. The second use of DefaultExpression is when you do have a non-void Expression in which you do need to sometimes return the "default(T)" value. Without this expression, you would have to generate a lot more ET to express "default(T)".

Often you do not need to use Expression.Empty to match a containing node's result type. There are expressions used in common patterns, typically control flow expression, where the result value is not used. For these common patterns, some nodes implicitly convert to void or squelch a result value. SwitchExpression, ConditionalExpression, TryExpression, BlockExpression, LambdaExpression, GotoExpression, and LabelExpression all automatically convert their result expression to void if they themselves have a void Type property (or delegate with result type in the case of lambdas).

<h2 id="reducible-nodes">2.2 Reducible Nodes</h2>

We have a tension for what level of abstraction to provide in our model (see section ). While we think Design Time Language Models have a distinct mission from Expression Trees v2, we would like to allow for smooth interoperability between them. We enable higher-level models (even language-specific models) that can reduce to a common set of ET v2 node types that all consumers can process. Programs can query an ET node as to whether it reduces, and if so, a program can call on the node to reduce itself. When a node reduces, it returns a semantically equivalent ET with a root node than can replace the original ET node.

Reductions are allowed to be partial. The resulting ET may include nodes that need to be further reduced. Typically the immediate result of reducing one node comprises only children that are common ET v2 node types. Expression.Compile only compiles common node types and those that reduce to common nodes. If a node type does not reduce or does not reduce to only common nodes, then it may still be useful as part of a library-specific set of extensions that model code. An example might be a Design Time Language Model for code that either has too many errors to reduce or that is part of a model too specific to tooling needs to bother reducing it to common nodes.

The ET v2 common set of nodes will include some reducible nodes. For example, for meta-programming goals, there will be higher-level iteration models. We'll include ForExpression for counter termination, ForEachExpression for member iterations, WhileExpression for test first iterations, and RepeatUntilExpression for test last iterations. We'll also include a fundamental LoopExpression to which the other iteration models reduce. Other examples include BinaryExpression with node kind AddAssign (compound assignment) or UnaryExpression with node kinds PreIncrementAssign and PostIncrementAssign.

Note, due to time constraints we cut the higher-level iteration node types for .NET 4.0, but they will likely show up soon in the DLR's codeplex open source project.

Common ET nodes with a given node kind are either reducible always, or never. That is, a node is not conditionally reducible based on other properties it has that may be different for different instantiations. For example, the GeneratorExpression nodes are always reducible. Regardless of the reducibility, the compiler may have direct support for the node kind, or the compiler may reduce the nodes. For example, when we add ForEachExpression, the compiler will likely directly compile it without reducing it.

<h2 id="bound-unbound-and-dynamic-nodes">2.3 Bound, Unbound, and Dynamic Nodes</h2>

There are three categories or states of being bound for modeling expressions. More commonly mathematicians or computer scientists think of only two, bound and unbound. For example, in the expression "for all x such that 0 &lt; x + y &lt; 10", 'x' is a bound variable while 'y' is a free reference or unbound variable. If 'y' were not present in the expression, the expression would be fully statically bound such that we could evaluate it. However, to evaluate the expression, we need to first bind 'y' to some value.

**An unbound ET node:**

- would need to be bound before executing it

- would represent syntax more than semantics

- would have a Type property that is null (see .NET 4.0 vs. V-next+1 note below)

Consider a language that supported LINQ-like expression and that also had late-bound member access (for example, if VB added late-bound LINQ). You would then need to model unbound trees for the lambda expression in the following pseudo-code:

> o.Where( lambda (x) =&gt; x &gt; 0 ) \#o had late bound semantics

To be able to execute an ET modeling this code, you would need to inspect the runtime type of 'o', search its 'Where' overloads, and pattern match for one that can take a delegate. Furthermore, you would need to match lambda expression to the delegate. The delegate needs take an argument and returns a value with some type. The delegate's type for 'x' needs to make sense to bind '&gt;' to an implementation taking the type of 'x', an operand assignable from integer, and returning the type of the delegate.

A key observation in this situation is that the late-bound node representing the call to 'Where' necessarily has language-specific binding information representing the lambda. The representation cannot be language-neutral semantically. It also can't even be just syntax in any common representation because you need the language that produced the ET to process the lambda representation in the presence of runtime type information while binding. Support for unbound ETs may not be a good solution or one worth trying to share across languages.

**NOTE:** Since no languages currently support late bound LINQ expressions, we won't actually allow Expression.Type to be null in .NET 4.0. We'll reconsider this in V-next+1 if we think ETs are useful to languages that need to represent unbound trees like the lambda expression in the example above.

**In the ET v2 model, a bound ET node:**

- has non-null Type property (that is, we statically know its type)

- could be dynamic expression

A dynamic expression often has a Type property that is Object, but its Type that is not null. It might not be Object as well. For example, in "if x &gt; y" the ET node for '&gt;' could be typed Boolean even if it is a dynamic node.

**The ET v2 model includes dynamically bound nodes that:**

- must be resolved at run time to determine how to perform the operation they model

- represented by DynamicExpression nodes

<h3 id="dynamicexpression-node">2.3.1 DynamicExpression Node</h3>

The DynamicExpression has binding information representing the metadata that further describes the expression beyond the ET node type and any children it has. For example, an ET representing the dynamic expression for comparing a variable to a string literal might have a flag in its binding information indicating whether the compilation unit had an option set to compare strings case-sensitively (which VB has). The binding information also encapsulates the language that created the ET node. The language determines the semantics of the dynamic expression at runtime when its binder searches for an implementation of the operation represented by the node.

One observation about the DynamicExpression node is that in some sense it marks a sub ET as being late-bound. These nodes need more information about what sort of operation they represent, unlike MethodCallExpression. Furthermore, they need to represent most of what all the other ET node types represent. This begs the question of having a dual representation for dynamic vs. fully bound static nodes. We went down the design path for quite a while using only the other node types with an optional BindingInfo property to mark the dynamic expressions.

It turned out in practice that while we thought we had a more economic design, the usage of the model with an optional BindingInfo property was too awkward:

- Hosted language scenarios need more arguments (often in the form of run-time context). For example, BinaryExpression Divide or Equal can require an extra context argument for the specific semantics in a module for Python or VB, meaning the operation does not fit the binary shape.

- A BinaryExpression with node kind Assign and a Left operand that is a MemberExpression is not intuitive as to where the extra dynamic metadata belongs. Does it go on the BinaryExpression or the MemberExpression, or both?

- Due to how we compile dynamic expression to use the DLR's fast dynamic dispatch caching, we need a delegate type to describe the dynamic call site. We don't generate the delegate types, and languages may need to control their creation such as pre-building some of them. This extra data also makes these expressions not fit the fully static nodes' shapes (number of operands or children), but all the dynamic expression do match shape (binding info, delegate type, arguments).

- The programming model for ET consumers always included a check for the BindingInfo Property to handle every node type. Since consumers want to handle dynamic nodes a specific way, using the ExpressionVisitor and other helper code, handling them now is a simple object-oriented method dispatch.

<h3 id="binding-information-in-expression-nodes">2.3.2 Binding Information in Expression Nodes</h3>

Expression nodes can have semantic hints or specifications that are more detailed than, for example, BinaryExpression Add on Int32s or MethodCallExpression with instance and name. These nodes can have MethodInfos attached to them to indicate the exact implementation of Add or resolution of method name to invoke. The MethodInfos serve two main purposes. The first is to be very exact when creating a node what the implementation is for the node's semantics. Language implementations should supply MethodInfos rather than leaving method resolution to the Expression factories because those factories may not resolve overloads in the same way the language would. The second job of the MethodInfos is to provide hints to LINQ providers that might interpret ETs. Those providers can have tables mapping to implementations of the node's semantics when they aren't actually compiling the ET and executing in a .NET run time.

This extra information is required with DynamicExpression nodes. They must have binding information that can be emitted when compiling. The binding information informs the run-time binder how to search for a correct implementation of the node's semantics, given the run-time operands that flow into the operation represented by the ET node. ETs use a CallSiteBinder as the binding information representation, not MethodInfo. In fact, the CallSiteBinder is also the run-time object used in the DLR's CallSites that manage the fast dynamic dispatch for dynamic operations. CallSiteBinder encapsulates both the binding information for the exact semantics of the node and the language that created the node, which governs the binding of the operation at run time.

Two design issues arise immediately from the choice to use CallSiteBinders vs. MethodInfos. The first is serializability. The ET design supports fully serializable ETs but doesn't enforce that they are always serializable. One reason we use the CallSiteBinders in the DynamicExpression is that they naturally fit exactly what the binding information is that the ET needs and help in hosted execution scenarios. If a language produces an ET as part of a hosting scenario to immediately execute the ET, then the binder can tuck away pointers to live data structures used by the language's engine. Languages can still produce ETs with DynamicExpressions that are serializable if they need to do so.

The second design issue is re-using MethodInfos by deriving custom implementations for use with DynamicExpression nodes. There's a nice consistency in representing the binding information as a MethodInfo, looking on at ETs only. However, the roles played by the MethodInfo are different than the binding information on DynamicExpression. It is more important to have the dynamic binding information be consistent across ETs, the DLR fast dynamic CallSites, and the DLR interoperability MetaObject protocol. Not only does MethodInfo have many members that would throw exceptions if used for dynamic binding information, it would be awkward to require creating an LCG method so that the MethodInfo was invocable. CallSiteBinders are best for detailed semantics capture in DynamicExpression nodes, not MethodInfos.

<h2 id="iteration-gotos-and-exits">2.4 Iteration, Goto's, and Exits</h2>

Representing iteration has a couple of interesting concepts and design points. How to represent iteration goes to the heart of what abstraction level to model for expressions (see section ). Whether to include Goto goes back ages in language design. ETs v2 provides a nice combination of high-level modeling and lower-level support if needed. There are some higher level modeling nodes so that you almost never need Goto, but we provide Goto for reducing some node types to more primitive control flow.

We provide GotoExpression because it is needed for C\#, VB, and other languages. The ET v2 GotoExpression started with simple, clean semantics designed into C\#. Basically this meant that the target label of the Goto must be lexically within the same function body, and you could only exit inner basic blocks to outer basic blocks. However, we need to accommodate more of VB's older Goto semantics, and we need a richer Goto for some of the ET transformations we do internally (see section 2.4.3).

GotoExpression has an optional Value expression. This allows GotoExpression to enable other node types to truly be expressions. The value expression's type must be ref assignable with the type of the GotoExpression's label target. See the following sub sections for more details.

GotoExpression has a Kind property with a value from GotoExpressionKind (Goto, Break, Continue, Return). This is explained further in the following sub sections. This is convenience for meta-programming purposes.

<h3 id="loopexpression-and-higher-level-iteration-node-types">2.4.1 LoopExpression and Higher-level Iteration Node Types</h3>

Given Goto, we provide a basic LoopExpression with explicit label targets for where to break to and where to continue to. Explict labels in LoopExpressions have multiple benefits. You can use GotoExpression inside the LoopExpression's Body expression and verify the jumps are to the right locations. Explicit labels support languages that can return from or break out of outer loops or scopes, such as JScript or Lisp. Explicit label targets make transformations easier to get right and allow for better error reporting when transformations are not right.

For meta-programming goals, there will be higher-level iteration models. We'll include ForExpression for counter termination, ForExachExpression for member iterations, WhileExpression for test first iterations, and RepeatUntilExpression for test last iterations. These will be common nodes that reduce to combinations of LoopExpression, BlockExpression, GotoExpression, etc.

Due to time constraints we cut the higher-level iteration node types for .NET 4.0, but they will likely show up soon in the DLR's codeplex open source project.

<h3 id="modeling-exits-labelexpression-and-gotoexpression-with-a-value">2.4.2 Modeling Exits, LabelExpression, and GotoExpression with a Value</h3>

As many have observed, lexical exits are ultimately just a Goto, proceeding to the end of the function and then returning. Sometimes when you leave a function, you leave one or more values on the stack. The ET model only supports a single return value inherently. Once we had to have Goto, there is an economy of design obtained by merging the models of explicit function return and Goto.

There are a couple of very nice benefits from merging lexical exits and Goto. The biggest is that by having Goto optionally carry a value to its target, more of our nodes became truly expression-based. This makes the overall model more consistent by more fully embracing the benefits of being expression-based. For example, LoopExpression is truly an expression that can be used anywhere because you can exit the loop with a value at the break label target.

The second benefit of merging Goto with lexical exits is that we've added explicit label targets to some nodes. This enables more Goto checking in factories and better error messages when compiling. For example, when you have a Goto expression inside a LoopExpression with kind Break, the expression compiler can ensure the Goto's target is the containing Loop's break label target. Also, when you form a GotoExpression, you have to supply the label target which has a Type property. The factory can ensure the Goto and the label target match by type, and the compiler can check the types match the type of the containing expression. Having label targets explicit in the tree also enables tree transformations to be safer and more reliable.

One quirk in the design is how to handle LabelExpression which has a LabelTarget that marks a destination in code for Goto. If a LabelExpression has non-void type, and execution gets to the target via Goto with a value of that type, we're consistent. What happens if I encounter the target location by straight line sequence, and how do we keep the IL value stack consistent? We solve this by adding an optional default value expression to a LabelExpression and by specifying the semantics of LabelExpression to place its target AFTER the default expression. In practice this works very naturally. For example, a LambdaExpression's Body can be a LabelExpression whose default value expression is the actual body of the function. Then the actual body of the function, an expression, is the value left on the stack if you naturally flow through the expressions in the body. If you exit via a GotoExpression (with node kind Return), you have a verifiable target at the end of the function and a verifiable value type that matches the return type from the Type property of the LambdaExpression's.

<h3 id="gotoexpression-capabilities">2.4.3 GotoExpression Capabilities</h3>

As stated, we expanded Goto capabilities beyond C\#'s. VB is not fully using the DLR yet, but when it does, we will need a more flexible Goto. If we do not allow more cases for GotoExpression, VB would need to produce a VBBlockExpression and their own VBGotoExpression that reduced to a complicated rewriting of the ET. It seems useful to provide the more general GotoExpression. However, VB would still need a special VBBlock to model their on\_error\_goto semantics, which seems too specific to a single language to generally model in common ET nodes.

ETs v2 limit Goto lexically within a function. ETs allow jumping into and out of the following:

- BlockExpressions

- ConditionalExpressions

- LoopExpressions

- SwitchExpressions

- TryExpressions (under certain situations)

- LabelExpressions

ETs allow some jumps relative to TryExpressions:

- jumping out of TryExpression’s body

- jumping out of a CatchBlock’s body

- jumping into TryExpression’s body from one of its own CatchBlocks

The above constitutes what ETs v2 allow. Just by way of examples, we do not allow jumping into the middle of argument expressions, such as those in binary operands, method calls, invocations, indexing, instance creation, etc. We do not allow jumping into or out of GeneratorExpressions. You could however jump within a BlockExpression used as an argument expression.

<h2 id="assignments-and-l-values">2.5 Assignments and L-values</h2>

We model assignments with BinaryExpression nodes that have an Assign node kind. The factory methods restrict the Left expression of the BinaryExpression to a fixed set of common ET node types that the compiler recognizes. This permitted ET node types are ParameterExpressions, MemberExpressions, and IndexExpressions.

For ref and out parameters, we also restrict ET node types, but we do allow a few more node types. We additionally allow BinaryExpressions with node kind ArrayIndex, MethodCallExpressions with MethodInfo that is Array.Get, and the new UnboxExpression. The first two are legacy from LINQ ETs v1, and we will obsolete them eventually (see section ). Expression.Compile handles write backs for these expressions passed to ref and out parameters. We explicitly do not support property accesses wrapped in conversion expressions due to ambiguities with how to convert back when writing back the out or ref value.

We considered CanWrite and CanRead flags on Expression, but cut them. This would allow assignment factories to check the Left expression. It turns out they weren't that useful. They felt more like form over function since you had to fill them in to call the assignment factory methods without error, but you'd get a nice error anyway from the compiler if you formed your tree incorrectly.

We considered supporting reducible Left expressions in the assignment factory methods, but cut them. The Left expression would have to reduce to the common recognized set of node types. If your assignment needed complicated logic with temps and a block of code, you would have to use a reducible node for the entire BinaryExpression (with Assign node kind). This design felt stilted, so we'll look at this again in v-next+1.

We believe in V-next+1 we can introduce a generalized l-value model. We would add pre and post expressions for temps set up and for write backs. We don't think this model would be overly complex. If added, then l-value positions could be any node type that was writable and had pre and post ETs for setting up temps and writing back values. We would still NOT support l-value positions with property accesses wrapped in conversion expressions.

<h3 id="indexed-locations">2.5.1 Indexed Locations</h3>

In ETs v2 we have IndexExpression that handles array access, indexers, and indexed properties. You can use these as l-values for assignments and ref/out parameters.

We will obsolete the LINQ v1 support for BinaryExpressions with node kind ArrayIndex and MethodCallExpressions with methodinfo Array.Get. We only support those for ref/out parameters for LINQ v1 compatibility, and we do not support them for the new BinaryExpression with node kind Assign. The plan, which could change, is as follows:

- In .NET 4.0, add bold red text to MSDN docs around ArrayIndex factories. Add bold red text to MethodCallExpression factories if methodinfo has ref/out parameters, and you’re using one of the two nodes (BinaryExpressions with node kind ArrayIndex or MethodCallExpressions with methodinfo Array.Get).

- In v-next+1, mark the factory methods as obsolete.

- In v-next+1, issue warnings when compiling ref/out parameters whose expressions are BinaryExpressions with node kind ArrayIndex or MethodCallExpressions with methodinfo Array.Get.

- In v-next+2, remove ArrayIndex factories.

- In v-next+2, throw exceptions where we issues warnings in v-next+1.

We also disallow ref and out arguments for IndexExpression. Neither VB nor C\# support these. They are not part of .NET CLS.

When creating IndexExpressions, you must supply PropertyInfo. Another clean up to the LINQ v1 trees is that you cannot use random get/set method pairs.

<h3 id="compound-assignment-or-in-place-binary-and-unary-operations">2.5.2 Compound Assignment or In-place Binary and Unary Operations</h3>

We represent operations such as "+=" and "++" as BinaryExpression or UnaryExpression nodes with distinct node kinds (for example, AddAssign). These nodes are reducible common nodes. They reduce to the appropriate binary assignment expressions or blocks with temps to ensure sub-expressions are evaluated at most once. Regarding dynamic expressions at run time, some languages will need to inspect the l-value expression for .NET types to handle events, properties with delegates, etc., the right way when binding the expression.

<h3 id="user-defined-setting-syntax">2.5.3 User-defined Setting Syntax</h3>

Languages that have user-defined setting forms for storable locations should provide language-specific reducible nodes. These nodes can reduce, for example, to a MethodCallExpression that takes the arguments defining the location and an argument for the value. We don't provide extensibility for this sort of language feature.

<h3 id="destructuring-assignment">2.5.4 Destructuring Assignment</h3>

Languages that support destructuring assignment should provide language-specific reducible nodes. Different languages handle destructuring bind in different ways (first-class carrier objects for the values, single vs. multiple kinds of carrier objects for values, runtime-internal carrier objects or calling conventions, etc.). A language-specific node will allow for better meta-programming consumers working with a given language. These nodes can reduce to an ET with exact semantics represented in common ET v2 nodes types.

<h2 id="array-access">2.6 Array Access</h2>

In ETs v2 we have IndexExpression that handles array access, indexers, and indexed properties. You can use these as l-values for assignments and ref/out parameters. See section .

We continue to support the LINQ v1 ArrayIndex factory methods that return BinaryExpression and MethodCallExpression for fetching array elements for backward compatibility. Eventually, we will obsolete them in lieu of IndexExpression.

<h2 id="blocks-scopes-variables-parameterexpression-and-explicit-lifting">2.7 Blocks, Scopes, Variables, ParameterExpression, and Explicit Lifting</h2>

ETs v2 model variable references with ParameterExpression nodes and a node kind value of Parameter. Initially for readability of code we chose to introduce a VariableExpression node type. It turns out in practice this meant much code that processed ETs had to be duplicated due to static typing in languages or had to use typeof() to dispatch to the right code if using Expression as a variable's type. Very little code actually needed to treat the declarations or references differently.

ETs v2 includes a BlockExpression node type that has an explicit list of variables. It creates a binding scope. Producers of ETs can use these to introduce new lexical variables, including temporaries. Blocks do not guarantee definite assignment. Languages need to do that when producing ETs. To initialize variables, BlockExpressions must explicitly include expressions in their body to set the variables. Definite assignment semantics is the concern of consumers of trees and compilers that enforce those semantics. Different languages have varying semantics here from definite assignment, to explicit unbound errors, to sentinel $Unassigned first class values. Lastly, recall ETs v2 is expression-based, so the value of a BlockExpression is the value of the last expression in its body.

Some languages have strong lexical semantics for unique binding of variables. For example, in Common Lisp or Scheme, each iteration around a loop or any basic block of code creates unique bindings for variables introduced in those basic blocks. Thus, returning a lambda would create unique closures environments for each iteration. Some languages, such as Python and F\#, move all variables to the implicit scope of their containing function. ETs v2 supports both models, all depending on where you create the BlockExpression in the ET and list variables. For the stronger lexical model, for example with the loop, place the BlockExpression inside the loop body and list variables there, instead of putting the Block Expression outside the loop or at the start of the function. See section for an example.

ETs v2 also supports explicit lifting of variables to support languages that provide explicit meta-programming of local variables. For example, Python has a "locals" keyword that returns a dictionary of the lexical variables within a function so that you can manipulate them or 'eval' code against your function's environment. You can use the RuntimeVariablesExpression to list the ParameterExpressions that you need explicitly lifted.

~~Annotations on variable references are an issue. Each reference aliases the one ParameterExpression object representing the introduction of the variable to a lambda or a scope. Therefore, annotating each reference with source location to accurately report null deferences, unassigned references, etc., has to be managed outside the ParameterExpression node. This is design legacy from ETs v1~~.

<h2 id="lambdas">2.8 Lambdas</h2>

Lambdas are modeled with LambdaExpression and Expression&lt;T&gt;. The latter derives from the former, and the T is a delegate type. LambdaExpression.Type holds the same T, and there is a ReturnType property that holds the type of value the T delegate would return. All lambdas created by the factory methods are actually Expression&lt;T&gt;. LambdaExpression provides the general base type for code that needs to process any lambda, or if you need to make a lambda with a computed delegate type at runtime. LambdaExpression supports two Compile methods that return a delegate of type LambdaExpression.Type, which can be invoked dynamically at run time.

Handling arbitrary returns or exits from lambdas has some interesting issues. Returns from a lambda can be arbitrarily deep in control constructs and blocks. Lexical exits represent a sort of non-local exit from nested control constructs. The expression that is the body of a lambda might have a particular type from the last sub expression it contains. Execution of the ET may never reach this last sub expression because of a Goto node. Even though we've added these sorts of control flow to ETs v2, they still keep the constraints ETs v1 had regarding LambdaExpression.Type and .Body.Type.

<h3 id="ensuring-returns-are-lexical-exits-with-matching-types">2.8.1 Ensuring Returns are Lexical Exits with Matching Types</h3>

We model returns with GotoExpression and LabelExpression. Due to how ETs v2 use these, we have the same verifiable properties discussed in section 2.4.2. We can verify exits are lexical. We can verify that the function's return type, its Body's type, and any lexical exits with result values all match in type. At one point in the ETs v2 design, we were not able to ensure the types invariant, and while the v1 behavior was not guaranteed, we didn't want to break that. Note, there is one case from v1 that counters this invariant property of matching types. When the lambda's delegate type has a void return, the body's type does not have to match since any resulting value is implicitly "converted to void" or squelched.

There is a factory method to create a LambdaExpression that only takes an Expression for the lambda's body and a parameters array. This factory method infers the lambda's return type from the body expression. For many ET node types, we only ensure a parent's Type property matches the sub expressions' Types that by default produce the result of the parent node. For example, when constructing a BlockExpression, the factory only checks that the last sub expression has the same type as the BlockExpression. However, you can get proper checking on lexical exits from lambdas at creation time (without waiting to compile) using LabelExpression as the lambda's body.

The best pattern for creating lambdas with returns is to make the LambdaExpression's Body be a LabelExpression. The LabelExpression's default value expression is the actual body of the function. Then the actual body of the function, an expression, is the value left on the stack if you naturally flow through the expressions in the body. If you exit via a GotoExpression, you provide a value expression and a label target. This pattern enables ETs with a verifiable target at the end of the function and that any return value has the appropriate matching type.

You could use a BlockExpression as the LambdaExpression.Body. You would then have to put a LabelExpression as the last expression in the block. If the lambda returned non-void, you would need to make the BlockExpression.Type match the LambdaExpression.ReturnType. To do this, you would need to make the LabelExpression.Type match the block's type, and you would need to fill in the LabelExpression's default value expression with a DefaultExpression, supplying the lambda's return type. This is the normal way to think about creating a target to use as the exit location for the function, but it is much more work than using a LabelExpression as the lambda's body itself.

When rewriting reducible nodes in a lambda, if you need to create a return from the lambda, you'll need to ensure the LambdaExpression's Body is a LabelExpression. If there isn't one already, your rewriting visitor will need to create the label target and then as you unwind in the visitor, replace the Body of the LambdaExpression with a LabelExpression using the same label target.

Lastly, what about languages that allow return targets with dynamic extent? Some languages allow closing over function return or block labels (that is, true non-local returns). If these languages do not require full continuations semantics, they could easily map these non-local return label closures to throws and catches that implement the semantics.

<h3 id="throw">2.8.2 Throw</h3>

We model Throw with a UnaryExpression. We had a ThrowExpression at one point. To be consistent with the "shape" aspect of re-using expression node types with the same kinds of children or properties, you can think of Throw as an operator with a single argument.

The Type property of the UnaryExpression with node kind Throw does not have to be void, which you may expect since the Throw never really returns. Using types other than void can be useful for ensuring a node has consistently typed children. For example, you might have a ConditionExpression with Type Foo, where the consequent returns a Foo, but the alternative is a Throw. Allowing the UnaryExpression with node kind Throw to have a non-Void Type, you do not have to artificially wrap your alternative in a BlockExpression and use a DefaultExpression in it.

CatchBlocks are helper objects that convey information to a TryExpression, like the MethodInfos or CallSiteBinder objects some Expression have. CatchBlock is not an expression as you might expect, primarily because they cannot appear anywhere any expression can appear. We could have thought of TryExpression as having a value from its Body, but sometimes its value comes from a CatchExpression. This would be analogous to ConditionalExpression. However, unlike a language like Lisp where Catch is a first-class expression, we felt the model we settled on is both expression-based and more amenable to .NET programmers.

<h3 id="recursion">2.8.3 Recursion</h3>

ETs avoid needing a Y-Combinator expression by simply using assignment. An ET can have a BlockExpression with a variable assigned to a LambdaExpression. The LambdaExpression's body can have a MethodCallExpression that refers to a ParameterExpression that refers to the variable bound in the outer scope. The compile effectively closes over the variable to create the recursive function.

<h3 id="tail-call">2.8.4 Tail Call</h3>

LambdaExpressions have a TailCall property that indicates whether compiling the lambda should attempt to use tail calls for any value returning expressions. If true, this is not a guarantee since some calls cannot be tailed called (for example, there may be write backs needed for some properties or ref args).

<h2 id="generators-codeplex-only">2.9 Generators (Codeplex only)</h2>

Cut from .NET 4.0, available on [www.codeplex.com/dlr](http://www.codeplex.com/dlr) .

Generators are first-class concepts in the ET v2 model. However, they are only available in the DLR's Codeplex project. The code can be re-used readily and ships in IronPython and IronRuby. The basic model is that you create the LambdaExpression with your enumerable return type and then use a GeneratorExpression inside the lambda to get the yielding state machine. The outer lambda can do any argument validation needed, and the GeneratorExpression reduces to code that closes over the lambda's variables. The body of the GeneratorExpression can have YieldExpressions in it. The generator node reduces to an ET that open codes the state machine necessary for returning values and re-entering the state machine to re-establish any dynamic context (try-catch, etc.).

The main reason for not shipping generators in .NET 4.0 is they have a couple of features that are specific to Python, and we don't have time to abstract the design cleanly. Python allows yield from finally blocks, and while we could define what that means, we'd need offer some extensibility here; for example, what happens when the yield occurs via a call to Dispose. Python requires 'yield' to be able to pass a value from the iteration controller into the GeneratorExpression body, which the generator may use to change the iteration state.

<h2 id="rich-static-call-site-modeling-v-next1">2.10 Rich Static Call Site Modeling (v-next+1)</h2>

Cut from .NET 4.0, planned for v-next+1.

We considered adding ComplexMethodCallExpression, ComplexIndexExpression, and ComplexNewExpression to model function calls with unsupplied arguments and named argument values. We need these eventually for call sites that do not simply supply required positional arguments. However, there are no language features in .NET 4.0 VS languages that demand we add this modeling in .NET 4.0, and there are no Codeplex languages we ship that would use the support if we added it. The reason no VS languages need this is that there are no plans to enhance the lambda syntax we have to permit control flow, assignments, or new language features inside of lambdas.

When we do add support for rich static call sites, we cannot add an ArgumentsInfo property to MethodCallExpression. This would break backward compatibility. If we added this extra property that is sometimes null, then when it wasn't null, the node would have different semantics than it did in ETs v1. ET v1 consumer code would not check for the new property, and sometimes the argument processing would be incorrect. We have a first-cut design on what we will add in v-next+1. The rest of this section outlines what we think is a nice general model.

These nodes would have two arrays, .ArgumentEvaluationOrder and . ArgumentDescriptions. The would have parallel elements. The first just contains the supplied expression in the appropriate order to evaluate them. It contains only as many expression as the number that appeared at the call site. The descriptions array contains ArgumentDescriptions, and each element describes the argument in the corresponding element of the first array. The descriptions detail whether the argument was positional or named, as well as in what position it was supplied. For example, in the expression (however kooky) "foo(,,3,x=5)", the arrays would be of length two, and the descriptions would describe the arguments in positions 2 and 3.

We will NOT model 'ref' and 'out' arguments in the ArgumentDescription objects for ComplexMethodCallExpression. You do not need these for correctness in producing an ET or for compiling it. If you parse a 'ref' argument in source at a call site, you can bake that information into the delegate type that is the T in the resulting dynamic CallSite&lt;T&gt; (see spec for "Sites, Binders, and Dynamic Object Interop"). If you do not parse 'ref', but your language's binder on sees an appropriate method at runtime with a 'ref' parameter, then you know to throw an exception. You only need the 'ref' and 'out' modeling for meta-programming convenience. Since 'out' is a single language oddity, and adding 'ref' for convenience only would require MethodCallExpression to have an Argument Description array and sometimes to be reducible, we will not model 'ref' and 'out' in ArgumentDescriptions.

<h2 id="expression-tree-where-possible">2.11 Expression "Tree" Where Possible</h2>

With all but two exceptions, Expression Trees truly are trees. However, there are technically two common ways in which they are a DAGs, which is still good, but they are not trees.

One case is ParameterExpression from ET v1 where the declaration site in the ET and the reference sites alias the ParameterExpression instances. This does not create any cycles, but it does mean we have pairs of vertices with more than one path joining them.

The other case is LabelExpression. We clearly want to avoid having BreakExpression or GotoExpression pointing to some other ET node such that traversing a tree would mean having to detect cycles. We do this by having LabelExpression nodes that point to LabelTargets. Then ET nodes, such as GotoExpression, can point to the LabelTarget of the LabelExpression marking the destination.

There are of course other ways to break the tree nature of the graph. You might alias the same MethodInfo in various places or ConstantExpressions. You might rewrite a tree and re-use sub ETs in different locations. None of this should be an issue since we don't have cycles, but purists should realize ETs are DAGs.

<h2 id="serializability">2.12 Serializability</h2>

The ability to serialize an ET is important. You may want to send the ET as a language-neutral semantic representation of code to a remote execution environment or to another app domain. You might store pre-parsed code snippets as ETs as a representation higher level than MSIL and more readily executable than source. Nothing should prevent the ability to save an ET to disk and reconstitute it.

It will always be possible to create an ET that does not serialize. In fact, in the hosted language scenarios, most produced ETs may not serialize because they are created for immediate execution. In this case it is fine to directly point at data needed for execution that may not serialize. For example, DynamicExpression nodes can hold onto rich binding information for use at runtime. They may have references to the ScriptRuntime in which they execute or other execution context information.

If a language does need to refer to a ScriptRuntime or scope objects for free references to variables, then the language can still create serializable ETs. The entry point to executable code produced from an ET is always a lambda (even if it is an outer most lambda wrapping the top-level forms of a file of script code). The language can create the LambdaExpression with parameters for the ScriptRuntime and/or variables scope chain through ScriptRuntime.Globals. Since the language is in control when it invokes the lambda at a later time, or in another context, it can pass in the execution context the code needs. Finally, if the language uses DynamicExpression, it needs to ensure its CallSiteBinders are serializable.

<h2 id="shared-visitor-support">2.13 Shared Visitor Support</h2>

ETs v2 provides a tree walker class you can derive from and customize. Customers often asked about tree walkers, and the DLR uses them a lot too. With the advent of Extension node kinds and reducibility, providing a walker model is even more important for saving work and having well-behaved extensions going forward. Without providing a walking mechanism out of the box, everyone would have to fully reduce extension nodes to walk them. Reducing is lossy for meta-programming because usually you can't go back to the original ET, especially if you're rewriting parts of the tree.

As an example, without the visitor mechanism, Extension node kinds inside of Quote (UnaryExpressions with node kind Quote) would be problematic. The Extensions would be black boxes, and Quote would be unable to substitute for ParameterExpressions in the black box. The Quote mechanism would need to fully reduce all nodes to substitute the ParameterExpressions. Then the quoted expression would not have the shape or structure that is expected when using Quote. This would make meta-programming with such expression work poorly.

The ExpressionVisitor class is abstract with two main entry points and many methods for sub classes to override. The entry points visit an arbitrary Expression or collection of Expressions. The methods for sub classes to override correspond to the node types. For example, if you only care to inspect or act on BinaryExpressions and ParameterExpressions, you'd override VisitBinary and VisitParameter. The methods you override all have default implementations that just visit their children. If the result of visiting a child produces a new node, then the default implementations construct a new node of the same kind, filling in the new children. If the child comes back identity equal, then the default just returns it.

As an Extension node kind author, you should override Expression.VisitChildren to visit your sub expressions. Furthermore, if any come back as new objects, you should reconstruct your node type with the new children, returning the new node as your result. By default VisitChildren reduces the expression to a common node and then calls the visitor on the result. As discussed above, this is not the best result for meta-programming purposes, so it is important that Extension nodes override this behavior.

<h2 id="design-impact-from-performance-improvements">2.14 Design Impact from Performance Improvements</h2>

We increased the surface area slightly for performance gains. The properties on Expression for fetching the node kind and Type properties from sub classes are now virtual. In many cases these needlessly took up a slot in the Expression object. Removing the backing fields for these members where possible saved about 50% of ET working set on real code processing in the DLR.

There are also several factory overloads to avoid having to allocate read-only collections. In typical usage before this change, a language would have an original array (of, say, expressions) and then call a factory to create an Expression with a read-only collection of children. The factory would have to allocate a new array to guarantee it being read-only. Then when someone fetched the Expression children, the node would wrap the array in a ReadOnlyCollection. We now use factories that take up to N children as individual arguments, create some clever sub classes of the node type, and leverage those for smart ReadOnlyCollection implementations. This saved about 70% of the read-only collection impact throughout real usage of ETs for the DLR.

<h2 id="annotations-debugsource-information-only">2.15 Annotations (Debug/Source Information Only)</h2>

We had a general annotation mechanism for a long time, but it kept presenting issues both in terms of the nature of the annotations and rewriting trees while correctly preserving annotations. We concluded that most annotations are short-lived for a single phase of processing, and they did not need node identity across tree rewrites. One common case of a persisted annotation, source locations, needed to span phases and rewrites, so we kept support for them specifically. We cut the general support for this release and will look at adding it back in a future release.

Note, all factory methods return new objects each time you call them. They return fresh objects so that you can associate them with unique annotations when that's needed. If you need caching for working set pressure or other performance turning (for example, re-using all Expression.Constant(1) nodes), then you need to provide that.

<h3 id="source-location-information">2.15.1 Source Location Information</h3>

We model source location information with a DebugInfoExpression that represents a point in the ET where there is debugging information (a la .NET sequence points). A later instance of this class with the IsClear property set to True clears the debugging information. This node type has properties for start and end location information. It also can point to SymbolDocumentInfo which contains file, document type, language, etc.

<h3 id="cut-general-annotations-support">2.15.2 CUT General Annotations Support</h3>

~~ETs v2 provide an annotation mechanism. This is useful for debug information and source locations. Different tools, ET producers and consumers, can tag ET nodes with access information or other metadata.~~

~~The information is immediately available via an Annotations object to which the ET node refers. We considered having the mechanism be completely outside the ETs. This was a more complicated model. It involved hash identities, aside tables, callback convention on transformations, etc. ET nodes do not have strong object identify in the presence of transformations. Now when tools transform ETs, the annotations on nodes naturally travel with them. They can also be modified or moved when creating new nodes.~~

~~Annotations instances are immutable. Of course, users can add an annotation member that is an indirection point, from which they can maintain mutable information. You might do this in some processing pass where an annotation changes, but you do not want to incur the cost of copying the sub ET from the node to the root of its containing tree over and over.~~

~~The information element in the Annotations object is keyed by a type. An Annotations object can have more than one element of a given type.~~

<h2 id="node-kind-and-operator-enum-values">2.16 Node Kind and Operator Enum Values</h2>

Each Expression has a NodeType property with a value from the ExpressionType enum. Each kind of ET node (kind, not node type) is uniquely identified by an ExpressionType value in the node's NodeType property. For example, a BinaryExpression node type could have NodeType values indicating different kinds of binary expressions, Add vs. Multiply.

For DynamicExpressions with the BinaryOperationBinder or UnaryOperationBinder, for example, the binder has an Operation property respresenting the abstract operation. We've re-used the ExpressionType enum for modeling the operation in the binder. This has the benefit of fewer types and consistency with operator modeling. The down side is that ExpressionType has elements that are only used either for binders or for ET node kinds.

<h2 id="tostring-method">2.17 ToString Method</h2>

Expression.ToString is for light weight debugging purposes only. We do not return strings with a helpful structural representation of the ET, as you may want when developing a language on the Dynamic Language Runtime or returning expressions from DLR MetaObjects. For that, there are helpers in the DLR code built on Codeplex.com, Expression.Dump and Expression.DumpExpression. These members that produce a form of pretty printed tree may move into v-next+1 or some other version of .NET when we have more time to bake them.

ToString does not try to return semantically accurate C\# or VB code in particular. We try to return terse strings loosely suggesting what an ET node contains for quick inspection only.
