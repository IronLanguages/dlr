using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace HostingTest
{
    /// <summary>
    /// Base attribute to mark tests that require a specific language to be available.
    /// Tests marked with this attribute will be skipped if the language is not configured.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public abstract class RequiresLangAttribute : NUnitAttribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test;

        protected abstract string LangName { get; }

        protected abstract bool IsLangAvailable(HAPITestBase testBase);

        public void BeforeTest(ITest test)
        {
            if (test.Fixture is HAPITestBase testBase && !IsLangAvailable(testBase))
            {
                Assert.Ignore($"Test requires {LangName} to be available.");
            }
        }

        public void AfterTest(ITest test)
        {
            // Nothing to do after test
        }
    }

    /// <summary>
    /// Attribute to mark tests that require Ruby to be available.
    /// </summary>
    public class RequiresRubyAttribute : RequiresLangAttribute
    {
        protected override string LangName => "Ruby";

        protected override bool IsLangAvailable(HAPITestBase testBase) => testBase.IsRubyAvailable;
    }
}
