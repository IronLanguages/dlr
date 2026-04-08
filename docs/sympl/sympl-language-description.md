# 21 SymPL Language Description

The following sub sections contain very brief descriptions of language features and semantics. Some sections have more details, such as the description of **Import**, but most loosely describe the construct, such as try-catch.

<h2 id="high-level">21.1 High-level</h2>

Sympl is expression-based. The last expression executed in a function produces the return value of the function. All control flow constructs have result values. The executed branch of an **If** provides the value of **If**. If the **break** from a loop has a value, it is the result of the **loop** expression; otherwise, nil is.

There is support for top-level functions and lambda expressions. There is no **flet**, but you can get recursive lambdas by letting a variable be nil, then setting it to the result of a lambda expression, which can refer to the variable for recursive calls.

Sympl is a case-INsensitive language for identifiers.

Sympl does not demonstrate class definitions. See section for more information.

<h2 id="lexical-aspects">21.2 Lexical Aspects</h2>

Identifiers may contain any character except the following:

> ( ) " ; , ' @ \\ .

Sympl should disallow backquote in case adding macro support later would be interesting. For now it allows backquote in identifiers since .NET raw type names allow that character. Like many languages, Sympl could provide a mapping from simpler names to those names that include backquote (for example, map List to List\`1). Doing so didn't seem to add any teaching about the DLR, and Sympl identifiers are flexible enough to avoid the work now.

Due to the lack of infix operators (other than period), there's no issue with expressions like "a+b", which is an identifier in Sympl.

An identifier may begin with a backslash, when quoting a keyword for use as an identifier.

Sympl has integer and floats (.NET doubles) for numeric literals.

Doubles aren't in yet.

Strings are delimited by double quotes and use backslash as an escape character.

Apostrophes quote list literals, symbols, integers, and other literals. It is only necessary to quote lists and symbols (distinguishes them from identifiers).

Comments are line-oriented and begin with a semi-colon.

<h2 id="built-in-types">21.3 Built-in Types</h2>

Sympl has these built-in types:

- Integers -- .NET int32 (no bignums)

- Floats -- .NET doubles NOT ADDED YET

- Strings -- .NET immutable strings

- Boolean -- **true** and **false** literal keywords for .NET interop. Within Sympl, anything that is not **nil** or **false** is true.

- Lists -- Cons cell based lists (with First and Rest instead of Car and Cdr :-))

- Symbols -- interned names in a Sympl runtime instance

- Lambdas -- .NET dynamic methods via Expression Trees v2 Lambda nodes

Lists and symbols were added as a nod to the name SymPL (Symbolic Programming Language), but they show having a language specific runtime object representation that might need to be handled specially with runtime helper functions or binding rules. They also provided a nice excuse for writing a library of functions that actually do something, as well as showing importing Sympl libraries.

<h2 id="control-flow">21.4 Control Flow</h2>

Control flow in Sympl consists of function call, lexical exits, conditionals, loops, and try/catch.

<h3 id="function-call">21.4.1 Function Call</h3>

A function call has the following form (parentheses are literal, curlies group, asterisks are regular expression notation, and square brackets indicate optional syntax):

(expr \[{.id \| . invokemember}\* .id\] expr\*)

Invokemember :: (id expr\*)

A function call evaluates the first expression to get a value:

> (foo 2 "three")
>
> (bar)
>
> ((lambda (x) (print x)) 5)

The first expression may be a dotted expression. If an identifier follows the dot, it must be a member of the previously obtained value, evaluating left to right. If a period is followed by invoke member syntax, the identifier in the invoke member syntax must name a member of the previously obtained value, and the member must be callable.

These two expressions are equivalent, but the first is preferred for style:

(obj.foo.bar x y)

obj.foo.(bar x y)

The second should only be used in this sort of situation:

obj.(foo y).bar ;;bar is property

(obj.(foo y).bar ...) ;;bar is method or callable member

((obj.foo y) . bar ...) ;; also works but odd nested left parens

The first form, (obj.foo.bar.baz x y), has the following semantics when baz is a method (where tmp holds the implicit 'this'):

(let\* ((tmp obj.foo.bar))

(tmp.baz x y))

The form has these semantics if baz is property with a callable value:

(let\* ((tmp obj.foo.bar.baz))

(tmp x y)) ;; no implicit 'this'

Sympl prefers implicit this invocation over a callable value in that it first tries to invoke the member with an implicit this, and then tries to call with just the arguments passed.

<h3 id="conditionals">21.4.2 Conditionals</h3>

**If** expressions have the following form (parentheses are literal and square brackets indicate optional syntax):

(if expr expr \[expr\])

The first expr is the test condition. If it is neither nil nor false, then the second (or consequent) expr executes to produce the value of the **If**. If the condition is false or nil, and there is no third expr, then the **If** returns false; otherwise, it executes the third (or alternative) expr to produce the value of the **If**.

<h3 id="loops">21.4.3 Loops</h3>

There is one **loop** expression which has the form (plus is regular expression notation):

(loop expr+)

Loops may contain **break** expressions, which have the following forms (square brackets indicate optional syntax):

(break \[expr\])

Break expressions do not return. They transfer control to the end of the loop. Break exits the loop and produces a value for the loop if it has an argument. Otherwise, the loop returns nil.

Sympl may consider adding these too (with break and continue as well):

(for (\[id init-expr \[step-expr\]\]) ;;while is (for () (test) ...)

(test-expr \[result-expr\])

expr\*)

(foreach (id seq-expr \[result-expr\]) expr\*)

<h3 id="trycatchfinally-and-throw">21.4.4 Try/Catch/Finally and Throw</h3>

Still need to add Try/Catch/Finally/Throw. These are pretty easy, direct translations like 'if' and 'loop' since Expression Trees v2 directly supports these expressions.

These have semantics as supported by DLR Expression Trees. A **try** has the following form (parentheses are literal, asterisks are regular expression notation, and square brackets indicate optional syntax):

(try &lt;expr&gt;

\[(catch (&lt;var&gt; &lt;type&gt;) &lt;body&gt;)\] \*

\[(finally &lt;body&gt;)\] )

<h2 id="built-in-operations">21.5 Built-in Operations</h2>

These are the built-in operations in Sympl, all coming in the form of keyword forms as the examples for each shows:

- Function definition: **defun** keyword form. Sympl uses the **defun** keyword form to define functions. It takes a name as the first argument and a list of parameter names as the second. These are non-evaluated contexts in the Sympl code. The rest of a **defun** form is a series of expressions. The last expression to execute in a function is the return value of the function. Sympl does not currently support a **return** keyword form, but you'll see the implementation is plumbed for its support.

> (defun name (param1 param2) (do-stuff param1) param2)

- Assignment: **set** keyword form

> (set x 5)
>
> (set (elt arr 0) "bill") ; works for aggregate types, indexers, and Sympl lists
>
> (set o.bar 3)

- arithmetic: **+**, **-**, **\***, **/** keyword forms, where each requires two arguments

> (\* (+ x 5) (- y z))

- Boolean: **and**, **or**, **not** keyword forms. For each operand, any value that is not **nil** or **false**, it is true. **And** is conditional, so it is equivalent to (if e1 e2). **Or** is conditional, so it is equivalent to (let\* ((tmp1 e1)) (if tmp1 tmp1 (let\* ((tmp2 e2)) (if tmp2 tmp2)))).

- Comparisons: **=**, **!=**, **&lt;**, **&gt;**, **eq** keyword forms. All but **eq** have the semantics of Expression Trees v2 nodes. **Eq** returns true if two objects are reference equal, and for integers, returns true if they are numerically the same value.

- Indexing: **elt** keyword form

> (elt "bill" 2)
>
> (elt '(a b c) 1)
>
> (elt dot-net-dictionary "key")

- Object instantiation: **new** keyword form

> (new system.text.stringbuilder "hello world!")
>
> (set types (system.array.createinstance system.type 1))
>
> (set (elt types 0) system.int32)
>
> (new (system.collections.generic.list\`1.MakeGenericType types))

- Object member access: uses infix dot/period syntax

> o.foo
>
> o.foo.bar
>
> (set o.blah 5)

<h2 id="globals-scopes-and-import">21.6 Globals, Scopes, and Import</h2>

The hosting object, Sympl, has a Globals dictionary. It holds globals the host makes available to an executing script. It also holds names for namespace and types added from assemblies when instantiating the Sympl hosting object. For example, if mscorlib.dll is passed to Sympl, then Sympl.Globals binds "system" to an ExpandoObject, which in turn binds "io" to an ExpandoObject, which in turn binds "textreader" to a model of the TextReader type.

<h3 id="file-scopes-and-import">21.6.1 File Scopes and Import</h3>

There is an implicit scope per file. Free references in expressions resolve to the file's implicit scope. Bindings are created in a file's scope by setting identifiers that do not resolve to a lexical scope. There also is an **import** expression that binds file scope variables to values brought into the file scope from the host's Sympl.Globals table or from loading other files.

**Import** has the following form (parentheses are literal, curlies group, asterisks are regular expression notation, and square brackets indicate optional syntax):

(import id\[.id\]\* \[{id \| (id \[id\]\*)} \[{id \| (id \[id\]\*)}\]\] )

The first ID must be found in the Sympl.Globals table available to the executing code, or the ID must indicate a filename (with implicit extension .sympl) in the executing file's directory. If the first argument to **import** is a sequence of dotted IDs, then they must evaluate to an object via Sympl.Globals. The effect is that **import** creates a new file scope variable with the same name as the last dotted identifier and the value it had in the dotted expression.

If the second argument is supplied, it is a member of the result of the first expression, and the effect is that this member is imported and assigned to a file scope variable with the same name. If the second argument is a list of IDs, then each is a member of the first argument resulting in a new variable created for each one with the corresponding name.

If the third argument is supplied, it must match the count of IDs in the second argument. The third set of IDs specifies the names of the variables to create in the file's scope, setting each to the values of the corresponding members from the second list.

If the first ID is not found in Sympl.Globals, and it names a file in the executing file's directory, then that file is executed in its own file scope. Then in the file's scope that contains the **import** expression, **import** creates a variable with the name ID and the imported file's scope as the value. A file's scope is a dynamic object you can fetch members from. The second and third arguments have the same effect as specified above, but the member values come from the file's scope rather than an object fetched from Sympl.Globals.

The IDs may be keywords without any backslash quoting. Note, that if such an identifier is added to Globals, referencing locations in code will need to be quoted with the backslash.

**Import** returns nil.

<h3 id="lexical-scoping">21.6.2 Lexical Scoping</h3>

Sympl has strongly lexically scoped identifiers for referencing variables. Some variables have indefinite extent due to closures. Variables are introduced via function parameters or **let\*** bindings. Function parameters can be referenced anywhere in a function where they are not shadowed by a **let\*** binding. **Let\*** variables can be referenced within the body of the **let\*** expression. For example,

(defun foo (x)

(system.console.writeline x)

(let ((x 5))

(system.console.writeline x))

(set x 10))

(system.console.writeline x))

(system.console.writeline x))

(foo 3)

prints 3, then 5, then 10, then 3 .

Each time execution enters a **let\*** scope, there are distinct bindings to new variables semantically. For example, if a **let\*** were inside a **loop**, and you saved closures in each iteration of the loop, they would close over distinct variables.

<h3 id="closures">21.6.3 Closures</h3>

Sympl has closure support. If you have a **lambda** expression in a function or within a **let\***, and you reference a parameter or let binding from within the lambda, Sympl closes over that binding. If a **let\*** were inside a **loop**, and you saved closures in each iteration of the loop, they would close over distinct variables. The great thing about Expression Trees is this is free to the language implementer!

<h2 id="why-no-classes">21.7 Why No Classes</h2>

Sympl does not demonstrate classes. Sympl could have showed using an ExpandoObject to describe class members, and used a derivation of DynamicObject to represent instances since DynamicObject can support invoke member. Sympl could have stayed simple by requiring static class members to be access via classname.staticmember. It also could require an explicit 'self' or 'this' parameter on any instance methods (and using self.instancemember). This may have been cute, but it wouldn't have demonstrated anything real on the path to implementing good .NET interop.

Real languages need to use .NET reflection to emit real classes into a dynamic assembly. You need to do this to derive from .NET types and to pass your class instances into .NET static libraries. Performance is also better when you can burn some members into class fields or properties for faster access.

<h2 id="keywords">21.8 Keywords</h2>

The following are keywords in Sympl:

- Import

- Defun, Lambda

- Return (not currently used)

- Let\*, Block

- Set

- New

- +, -, \*, /

- =, !=, &lt;, &gt;

- Or, And, Not

- If

- Loop, Break, Continue (continue not currently used)

- Try, Catch, Finally, Throw (not currently used)

- Elt

- List, Cons, First, Rest

- Nil, True, False

<h2 id="example-code-mostly-from-test.sympl">21.9 Example Code (mostly from test.sympl)</h2>

(import system.windows.forms)

(defun nconc (lst1 lst2)

(if (eq lst2 nil)

lst1

(if (eq lst1 nil)

lst2

(block (if (eq lst1.Rest nil)

(set lst1.Rest lst2)

(nconc lst1.Rest lst2))

lst1))))

(defun reverse (l)

(let\* ((reverse-aux nil))

(set reverse-aux

(lambda (remainder result)

(if remainder

(reverse-aux remainder.Rest

(cons remainder.First result))

result)))

(reverse-aux l nil)))

(import system)

(system.console.WriteLine "hey")

(defun print (x)

(if (eq x nil)

(system.console.writeline "nil")

(system.console.writeline x))

x\)

(print nil)

(print 3)

(print (print "cool"))

(set blah 5)

(print blah)

(defun foo2 (x)

(print x)

(let\* ((x "let x")

(y 7))

;; shadow binding local names

(print x)

(print y)

(set x 5)

(print x))

(print x)

(print blah)

;; shadow binding global names

(let\* ((blah "let blah"))

(print blah)

(set blah "bill")

(print blah))

(print blah)

(set blah 17))

((lambda (z) (princ "non ID expr fun: ") (print z)) "yes")

(set closure (let\* ((x 5))

(lambda (z) (princ z) (print x))))

(closure "closure: ")

(print nil)

(print true)

(print false)

(print (list x alist (list blah "bill" (list 'dev "martin") 10) 'todd))

(if (eq '(one).Rest nil) ;\_getRest nil)

(print "tail was nil"))

(if (eq '(one two) nil)

(print "whatever")

(print "(one two) is not nil"))

;; Sympl library of list functions.

(import lists)

(set steve (cons 'steve 'grunt))

(set db (list (cons 'bill 'pm) (cons 'martin 'dev) (cons 'todd 'test)

steve))

(print (lists.assoc 'todd db))

(print (lists.member steve db))

(let\* ((x '(2 6 8 9 4 10)))

(print

(loop

(if (eq x.First 9)

(break x.Rest)

(set x x.Rest)))))

(set x (new System.Text.StringBuilder "hello"))

(x.Append " world!")

(print (x.ToString))

(print (x.ToString 0 5))

(set y (new (x.GetType) (x.ToString)))

(print (y.ToString))

(print y.Length)
