# 3 ET Runtime

There are a few functions that are required by the code the ET compiler produces. When generating and compiling ETs for execution within the same process, there are no issues whatsoever. The following functions or any changes to them will be consistent within the version of the .NET Framework that you are using:

RuntimeOps.MergeRuntimeVariables(

IRuntimeVariables first, IRuntimeVariables second, int\[\] indexes)

RuntimeOps.Quote(Expression expression, object hoistedLocals,

object\[\] locals)

If you use LambdaExpression.CompileToMethod to generate a DLL with your code, then you cannot use UnaryExpression with node kind Quote, which requires the above functions. However, CompileToMethod may emit calls to these methods:

IRuntimeVariables RuntimeOps.CreateRuntimeVariables(

object\[\] data, long\[\] indexes)

IRuntimeVariables RuntimeOps.CreateRuntimeVariables()

We need to keep these around and working if the DLL the customer writes out is otherwise working in future version of the CLR.
