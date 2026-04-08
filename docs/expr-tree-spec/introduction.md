# 1 Introduction

In the .NET Framework 3.5 we created Expression Trees (ETs) to model code for LINQ expressions in C\# and VB. They were limited in .NET 3.5 to focus on LINQ requirements; for example, a LambdaExpression could not contain control flow, only a simple expression as its body. Looking forward, there are several reasons we'd like to extend ETs:

- We'd like to further develop a single semantic code model as a common currency for compilation tools.

- We're adding the Dynamic Language Runtime (DLR) into the .NET Framework, and it uses semantic trees to represent code as a means for making it easier to port new dynamic languages to .NET.

- The DLR also uses semantic trees to represent how to perform abstract operations in its dynamic call site caching mechanism.

- We want to support meta-programming going forward where customers can more readily get programs as data, manipulate the data, and emit new code as data based on the input code.

Obviously, to support the above goals, we need more in the ET model than v1 provided. We need to model control flow, assignment, recursion, etc., in addition to simple expressions. There is no plan in .NET 4.0 to add modeling for types/declarations, but we'll consider these for V-next+1 (that is, the next major release after .NET 4.0).

Some quick terminology:

- The term "ET" indicates a tree structure of instances of Expression (direct or indirect).

- The term "ET node" indicates a single instance of Expression (direct or indirect). The ET node could be the root of a tree.

- The term "ET node type" means the specific class of which the ET node is an instance.

- The term "ET kind" refers to the Expression.NodeType property or indicates the value of the ExpressionType enum. This is a legacy naming issue.

- The term "sub ET" indicates the root of a tree where the root is an interior or leaf node of some other ET.

<h2 id="relation-to-other-models">1.1 Relation to Other Models</h2>

We do not think we're creating Yet Another Code Model from Microsoft. We are extending LINQ ET v1. We understand that there is an issue with multiple code models already, given codeDOM and VS Code Model.

ETs have a distinct mission from Design Time Language Models (DTLMs). An example of a DTLM is the VS Code Model or the model used internally within VS for smart editor features. If you think about what they need to support, you can see they have very different requirements:

- DTLMs are necessarily highly language-specific at certain levels. We can build a good story for future DTLMs and ETs v2 interoperating smoothly (see Reducible Nodes in section 2.2).

- DTLMs must explicitly represent errors (that is, contain error nodes with syntax trees).

- DTLMs need ancillary information such as cached parser state, special nodes outside the semantically common ET nodes for representing partial knowledge of code, annotations on common ET nodes, links to editor text and view models, links to project system models, etc.

- DTLMs have many more unbound trees to represent what is clearly an identifier from syntax but still a name that cannot be resolved statically to a concrete reference.

- DTLMs have bound nodes referring to design time models or derivations of System.Type and MemberInfo for types partially defined in editor buffers.

- DTLMs need structural or syntactic fidelity to code, including line break info, whitespace info, etc.

<h2 id="review-from-linq-expression-trees-v1">1.2 Review from LINQ Expression Trees v1</h2>

ETs are a set of public classes that live in the System.Linq.Expressions namespace.

The Expression Tree API defines a set of classes which representation a number of node types. Combining nodes forms expression trees. The top level node of an expression tree represents an expression, and children trees represent sub expressions.

Each Expression has a NodeType property with a value from the ExpressionType enum. Each kind of ET node kind (not ET node *type*) is uniquely identified by an ExpressionType value in the node's NodeType property. For example, a BinaryExpression node type could have NodeType values indicating different kinds of binary expressions, Add vs. Multiply.

Expression trees represent semantics in that they represent specific language level expressions. However, expression trees are not tied to any specific language and constitute a language-independent representation of common expressions found in CLR languages. ETs are lossy in the sense that they do not exactly model the syntax of a language or how an expression was formed in a language.

<h2 id="design-goals">1.3 Design Goals</h2>

There are three groups of design goals: compatibility, abstraction level, and some explicit non-goals worth mentioning.

<h3 id="compatibility-with-v1">1.3.1 Compatibility with V1</h3>

The underlying design goal is to stay true to the design of LINQ expression trees v1. We can add new node types and add members to exiting node types. We cannot break existing invariants, such as when properties may be null or what a particular value means.

Existing consumers should continue to work on currently supported ET subsets. There will be no new semantics for current combinations of properties. If an ET v2 node looks like a node your program recognized before, then it can continue to process it in the same way.

Existing consumers who coded defensively for unknown nodes will fall into the same "don't care or don't grok" branches of their code. If they encounter an ET v2 node type or an extended ET v1 type with new combinations of values for properties, they will naturally fall into their defensive code handling these unknown nodes.

ETs v2 keep the immutable property they had in v1.

ETs v2 stay tree-structured with the exception of the ParameterExpression nodes.

ETs v2 keep the "shape-based" design of ETs v1. For example, whenever a node would have the same count of children, we tried to unify the nodes. This is why BinaryExpression has so many node kinds, and furthermore, why assignment is modeled as a BinaryExpression. You can see this too with Throw and Unbox modeled as UnaryExpression. The node kind distinction helps the meta-programmer see the exact semantics of the node.

<h3 id="model-abstraction-level">1.3.2 Model Abstraction Level</h3>

Going back to our motivation for ETs v2, we want to achieve a generally useful semantic code model that can serve as a common currency in compilation tools. We would also like to support end-user meta-programming where they can get programs as data, manipulate them, and return new programs. We clearly do not want a purely syntactic model that is necessarily very language-specific and not representative of semantics. We also do not want an MSIL model, which would be so far removed from source programs that meta-programming would be unbearable.

The ET v2 model should provide easy consumption so that they are able to preserve language-level concepts for meta-programming. For example, rather than simply have a bare LoopExpression with GotoExpressions, we need common iteration constructs modeled closer to languages. We should have ForExpression, ForEachExpression, WhileExpression, RepeatUntilExpression, etc., in addition to a fundamental LoopExpression.

The model should provide easy production support for new code. Tools will rewrite ETs. Given the number of programmers and their backgrounds who write these tools, they will not be comfortable with lower-level node types. They will likely produce bushier or expanded trees as more direct input to compilers. The end-user meta-programmers however will be most comfortable producing an ET closer to the languages they are familiar with. They will want higher-level ET modeling.

These constraints lead us to avoid nodes that are specific to a single language and that are specific to a single consumer. For example, we include MethodInfo or CallSiteBinder in nodes to specifically capture their intended semantics where they might be interpreted differently by different consumers. ~~We also include an annotations mechanism so that consumers can tailor common nodes to themselves when appropriate~~.

We resolve the tension of abstraction levels in two ways. One is to have some higher-level node types we find common across multiple languages, such as the iteration models mentioned above. The other is by providing an ET node reducing mechanism. This allows language-specific derived types from Expression to reduce to ETs that are more explicit in their semantics, comprised purely of common ET v2 nodes, and commonly understood by all consumers.

<h3 id="net-4.0-vs.-v-next1">1.3.3 .NET 4.0 vs. V-next+1</h3>

We can only add so much to ETs in .NET 4.0 due to resources and time line. We know we need to add everything needed to support IronPython, IronRuby, DLR JScript, and ETs generated for dynamic CallSites (see sites-binders-dynobj-interop.doc spec). In some places we can avoid adding new node types to the common ET nodes because languages can add extension nodes that reduce to a tree expressed in common nodes. We also aren't trying to add any modeling of declarations for types in .NET 4.0.

What we decide to add in .NET 4.0 is driven by needs of current languages so that they are not doing undue amounts of work to express certain language features. For example, we're adding an explicit scope expression (which we model with a BlockExpression that has an explicit list of variables). Languages do not have to jump through hoops to create scopes with runtime helpers, using LambdaExpression in weird ways to avoid forcing unneeded closures, or other techniques. Another example is adding a GlobalExpression node, which has the semantics of getting a variable value from a host-supplied scope. Hosted languages (IPy and IRuby) can work around not having a GlobalExpression, but in V-next+1 we'll want a general model of hosted execution as well as "natural" execution like C\# and VB on .NET.

Sometimes we've added node types because we know in V-next+1 we'll try to achieve full coverage of IL semantics (not just CLS semantics). We aren't adding everything now of course due to resources and time. However, we will add some node types now that we might otherwise avoid in .NET 4.0 because we know we'll need them eventually. For example, we're adding the UnboxExpression in .NET 4.0 to model the IL of passing an IntPtr to a boxed value type's value. IronPython could have worked around this in .NET 4.0, but adding this now will make their programming model for .NET interop cleaner sooner.

<h3 id="non-goal-design-time-language-models">1.3.4 Non-goal: Design Time Language Models</h3>

ETs have a mission that is distinct from Design Time Language Models (for example, VS Code Model). See section for several reasons or ways in which these two kinds of models are the same model.

We can provide a smooth transition from DTLMs to ETs as well as some interoperability. Using the reducible node mechanism in ETs v2, the DTLM types could all derive from Expression. They could have all the special properties and hook they need into the design time environment of a tool (project models, text models in editors, etc.). However, when a DTLM tree was complete, or error free, those node types could reduce to common ET v2 nodes representing a correct program's semantics. The reduced tree could have a very similar shape to the DTLM tree.
