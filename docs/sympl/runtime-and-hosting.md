# 22 Runtime and Hosting

Sympl provides very basic hosting, essentially execute file and execute string. You can instantiate a Sympl runtime with a list of assemblies whose namespaces and types will be available to Sympl code. You can execute files in a host-provided scope or module, or let Sympl create a new scope for each execution. You can execute strings with Sympl expressions in a host-provided scope, or one previously obtained from executing a file.

There is a host globals table. Sympl programs can import names from the globals table, making those names become file module globals. When you execute a file, it name (without directories or extension) becomes an entry in the host globals table. Other Sympl programs can then import that name to access libraries of Sympl code.

<h2 id="class-summary">22.1 Class Summary</h2>

This class represents a Sympl runtime. It is not intentionally thread-safe or necessarily hardened in any way.

public ExpandoObject ExecuteFile(string filename)

public ExpandoObject ExecuteFile(string filename,

string globalVar)

ExecuteFile reads the file and executes its contents in a fresh scope or module. Then it adds a variable in the Globals table based on the file's name (no directory or extension), so that importing can refer to the name to access the file's module. When globalVar is supplied, this is the name entered into the Globals table instead of the file's base name. These functions also add a variable to the module called "\_\_file\_\_" with the file's full pathname, which importing uses to load file names relative to the current file executing.

public void ExecuteFileInScope(string filename,

ExpandoObject moduleEO)

ExecuteFile is just like ExecuteFile except that it uses the scope provided, and it does not add a name to Globals.

public object ExecuteExpr(string expr\_str,

ExpandoObject moduleEO)

ExecuteExpr reads the string for one expression, then executes it in the provided scope or module. It returns the value of the expression.

public static ExpandoObject CreateScope()

CreateScope returns a new module suitable for executing files and expression in.
