using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace HostingTest
{
    /// <summary>
    /// A simple ExpectedExceptionAttribute
    /// </summary>
    /// <remarks>
    /// In NUnit 3.0 <c>ExpectedExceptionAttribute</c> was removed in favor of <c>Assert.Throws</c>.
    /// This attribute is provided here for tests that were originally written using the old attribute
    /// and have not yet been converted to use the new assertion style.
    /// </remarks>
    /// <seealso href="https://github.com/nunit/nunit-csharp-samples/blob/master/ExpectedExceptionExample/ExpectedExceptionAttribute.cs"/>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ExpectedExceptionAttribute : NUnitAttribute, IWrapTestMethod
    {
        private readonly Type _expectedExceptionType;

        public ExpectedExceptionAttribute(Type type)
        {
            _expectedExceptionType = type;
        }

        public TestCommand Wrap(TestCommand command)
        {
            return new ExpectedExceptionCommand(command, _expectedExceptionType);
        }

        private class ExpectedExceptionCommand : DelegatingTestCommand
        {
            private readonly Type _expectedType;

            public ExpectedExceptionCommand(TestCommand innerCommand, Type expectedType)
                : base(innerCommand)
            {
                _expectedType = expectedType;
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                Type caughtType = null;

                try
                {
                    innerCommand.Execute(context);
                }
                catch (Exception ex)
                {
                    if (ex is NUnitException)
                        ex = ex.InnerException;
                    caughtType = ex.GetType();
                }

                if (caughtType == _expectedType)
                    context.CurrentResult.SetResult(ResultState.Success);
                else if (caughtType != null)
                    context.CurrentResult.SetResult(ResultState.Failure,
                        $"Expected {_expectedType.Name} but got {caughtType.Name}");
                else
                    context.CurrentResult.SetResult(ResultState.Failure,
                        $"Expected {_expectedType.Name} but no exception was thrown");

                return context.CurrentResult;
            }
        }
    }
}
