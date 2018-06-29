// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace HostingTest {

    internal class PythonCodeSnippets : CodeSnippetCollection{

        internal PythonCodeSnippets() {

            AllSnippets = new CodeSnippet[]{
                     new CodeSnippet(
                        CodeType.Null, "Null Code",
                        null),

                     new CodeSnippet(
                        CodeType.Junk, "Junk Code",
                        "@@3skjdhfkshdfk"),

                     new CodeSnippet(
                        CodeType.Comment, "Comment only code",
                        "#this is a test comment"),

                     new CodeSnippet(
                        CodeType.WhiteSpace1, "WhiteSpace only",
                        "            "),

                     new CodeSnippet(
                       CodeType.ValidExpressionWithMethodCalls, "Valid Expresion Using Method",
                       @"eval('eval(\'2+2\')')"),

                     new CodeSnippet(
                        CodeType.ValidStatement1, "Valid Statement",
                        @"if 1>0 : 
    print 1001"),

                    /// <summary>
                    ///  Test Bug : this is not an expression - This is a statement!
                    /// </summary>
                     new CodeSnippet(
                        CodeType.InCompleteExpression1, "Incomplete expression",
                        "print("),

                    /// <summary>
                    ///  Test Bug : this is not an expression - This is a statement!
                    /// </summary>
                     new CodeSnippet(
                        CodeType.InCompleteExpression2, "Incomplete expression",
                        "a = 2+"),

                     new CodeSnippet(
                        CodeType.InCompleteStatement1, "Incomplete statement",
                        "if"),

                     new CodeSnippet(
                        CodeType.Interactive1, "Interactive Code",
                        "<add valid interactive code>"),

                     new CodeSnippet(
                        CodeType.OneLineAssignmentStatement, "Interactive Code",
                        "x =  1+2"),

                     new CodeSnippet(
                        CodeType.LinefeedTerminatorRStatement, "Interactive Code",
                        "x =  1+2\ry= 3+4"),

                    /// <summary>
                    /// A python expression with classic functional language paradigms calling map with
                    /// a lambda function that multiplies the input value by -1.
                    /// </summary>
                     new CodeSnippet(
                        CodeType.CallingFuncWithLambdaArgsToMap, "A python expression with classic functional language paradigms using lambda and map",
                        @"map(lambda x: x * -1, range(0,-10, -1))"
                        ),

                        new CodeSnippet(
                            CodeType.MethodWithThreeArgs,
                            "Simple method with three args",
@"def concat( a, b, c): 
    return str(a + b + c)"),
                        
                    /// <summary>
                    /// Simple FooClass to test ScriptSource.Invocate(...)
                    /// </summary>
                     new CodeSnippet(
                        CodeType.SimpleFooClassDefinition, "Simple Foo class used to test calling member method after execution",
@"class FooClass:
     'A simple test class'
     def __init__(self):
         self.someInstanceAttribute = 42
     def f(self):return 'Hello World'
     def concat(self, a, b, c): 
        return str(a + b + c)
     def add(self, a, b):
        return a + b

fooTest = FooClass()
def bar(): return fooTest.f()"),

                    /// <summary>
                    ///  Rot13 function definition 
                    /// </summary>
                     new CodeSnippet(
                        CodeType.Rot13Function, "Defined Rot13 function",
                        @"
def rot13(transstr):
    chklst = list(transstr)
    nlst   = list()
    lookup = list('NOPQRSTUVWXYZABCDEFGHIJKLMnopqrstuvwxyzabcdefghijklm')
    for i in range(chklst.Length()):
        rchr = 0
        if(chklst[i].isalpha()):
            if(chklst[i].isupper()):
                rchr = lookup[ord(chklst[i]) % ord('A')]
            else:
                rchr = lookup[ord(chklst[i]) % ord('a') + 26]
        else:
            rchr = chklst[i];
        nlst.append(rchr)
    return ''.join(nlst)
"),

                    /// <summary>
                    ///  Test Bug : this is not an expression - This is a statement!
                    /// </summary>
                     new CodeSnippet(
                      CodeType.ValidExpression1, "Interactive Code",
                      "x =  1+2"),
                    /// <summary>
                    /// Valid code snippet with both expressions and statements
                    /// </summary>
                     new CodeSnippet(
                      CodeType.ValidMultiLineMixedType, "Valid Code",
@"
def increment(arg):
    local = arg + 1
    local2 =local
    del local2
    return local

global1 = increment(3)
global2 = global1"),

                     new CodeSnippet(
                        CodeType.Valid1, "Valid Code",
@"
def increment(arg):
    local = arg + 1
    local2 =local
    del local2
    return local

global1 = increment(3)
global2 = global1"),

                     new CodeSnippet(
                        CodeType.BrokenString, "Broken String",
                        "a = \"a broken string'"),

                     new CodeSnippet(
                        CodeType.SimpleMethod, "Simple method",
                        "def pyf(): return 42"),

                     new CodeSnippet(
                        CodeType.FactorialFunc, "Factorial function",
@"def fact(x):
    if (x == 1):
        return 1
    return x * fact(x - 1)"),

                     new CodeSnippet(
                        CodeType.ImportFutureDiv, "TrueDiv function",
@"from __future__ import division
r = 1/2"),

                     new CodeSnippet(
                        CodeType.ImportStandardDiv, "LegacyZeroResultFromOneHalfDiv function",
                        @"r = 1/2"),

                     new CodeSnippet(
                        CodeType.SevenLinesOfAssignemtStatements, "Very simple code example to be used for testing ScriptSource CodeReader method",
                         @"a1=1
a2=2
a3=3
a4=4
a5=5
a6=6
a7=7"),

                     new CodeSnippet(
                        CodeType.UpdateVarWithAbsValue, "Give a variable set to a negative number -1 and then re-assign abs value of itself",
                         @"
test1 = -10
test1 = abs(test1)"),
                  
                     new CodeSnippet(
                        CodeType.SimpleExpressionOnePlusOne, "A very simple expression 1 + 1",
                        "1+1" ),

                     new CodeSnippet(
                        CodeType.IsEvenFunction, "A function that returns true or false depending on if a number is even or not",
                        "def iseven(n): return 1 != n % 2"),

                    new CodeSnippet(
                        CodeType.IsOddFunction, "A function that returns true or false depending on if a number is odd or not",
                        "def isodd(n): return 1 == n % 2;"),

                     new CodeSnippet(
                        CodeType.MethodWithDocumentationAttached, "A very simple method with docs attached",
@"def doc():
     """"""This function does nothing""""""
     return"),
                     new CodeSnippet(
                        CodeType.SmallDotNetObjectForDocTest, "A .Net Object to use to verify we can see attached documentation",
                        "from System.Runtime.Remoting import ObjectHandle"),
            
                     new CodeSnippet(
                        CodeType.TimeZoneDotNetObjectForDocTest, "Will this work",
                        "from System import TimeZone"),
                     
                     new CodeSnippet(
                        CodeType.NegativeOneAssignedToX, "Interactive Code",
                        "x = 1"),
                     
                     new CodeSnippet(
                        CodeType.ImportCPythonDateTimeModule, "Import a CPython DateTime Module",
                        "import datetime\ndate=datetime.datetime"),
                     
                     new CodeSnippet(
                        CodeType.ImportDotNetAssemblyDateTimeModule, "Import .Net DateTime for an individual assembly",
                        "import clr\nfrom System import DateTime\nDotNetDate=DateTime")


            };
        }
    }

}

