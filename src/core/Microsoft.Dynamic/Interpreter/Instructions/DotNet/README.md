# .NET interpreter instructions

The files in this folder implement interpreter instructions used by the `LightCompiler`. They are copied from the expression interpreter in [.NET](https://github.com/dotnet/runtime) (`System.Linq.Expressions.Interpreter`) and are covered by its MIT license, see [LICENSE.TXT](LICENSE.TXT).

## Provenance

Copied from commit [`76b5b76`](https://github.com/dotnet/runtime/tree/76b5b76ceb08baa587178f8654df1a964547930b).

| File | Upstream path |
|---|---|
| `AddInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/AddInstruction.cs` |
| `AndInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/AndInstruction.cs` |
| `DecrementInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/DecrementInstruction.cs` |
| `DivInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/DivInstruction.cs` |
| `EqualInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/EqualInstruction.cs` |
| `ExclusiveOrInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/ExclusiveOrInstruction.cs` |
| `GreaterThanInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/GreaterThanInstruction.cs` |
| `GreaterThanOrEqualInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/GreaterThanOrEqualInstruction.cs` |
| `IncrementInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/IncrementInstruction.cs` |
| `LeftShiftInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/LeftShiftInstruction.cs` |
| `LessThanInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/LessThanInstruction.cs` |
| `LessThanOrEqualInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/LessThanOrEqualInstruction.cs` |
| `ModuloInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/ModuloInstruction.cs` |
| `MulInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/MulInstruction.cs` |
| `NegateInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/NegateInstruction.cs` |
| `NotEqualInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/NotEqualInstruction.cs` |
| `NotInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/NotInstruction.cs` |
| `OrInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/OrInstruction.cs` |
| `RightShiftInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/RightShiftInstruction.cs` |
| `SubInstruction.cs` | `src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/Interpreter/SubInstruction.cs` |
| `LICENSE.TXT` | `LICENSE.TXT` |

### Local changes

The copied files are unmodified, except that:

- `#nullable enable` is added.
- The `System.Dynamic.Utils` using and the `System.Linq.Expressions.Interpreter` namespace are replaced with `Microsoft.Scripting.Utils` and `Microsoft.Scripting.Interpreter`.
