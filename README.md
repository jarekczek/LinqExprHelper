# LinqExprHelper
Helper library to deal with Linq Expressions

# Functionality

Everything is in main source file, [LinqExprHelper.cs](LinqExprHelper.cs).

* `LinqExprHelper.NewExpr()` -
Assign new lambda expression with var statement

* `LinqExprHelper.UnifyParametersByName()` -
unify parameters of a linq expression by name, even when they come
from different scopes. Workaround the exception:
"System.InvalidOperationException : variable 'x' of type
'System.Int32' referenced from scope '', but it is not defined"

* `LinqExprHelper.ReplacePar()` -
Combine linq expressions easily.

# Samples

[Unit tests](LinqExprHelperTests.cs) included, that demonstrate usage.
