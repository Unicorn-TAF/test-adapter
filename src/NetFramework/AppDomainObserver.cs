#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing.Attributes;

namespace Unicorn.TestAdapter.NetFramework
{
    /// <summary>
    /// Provides with ability to get <see cref="TestInfo"/> data from specified assembly in separate AppDomain.
    /// </summary>
    public class AppDomainObserver : MarshalByRefObject
    {
        internal static List<TestInfo> GetTestsInfoInIsolation(string source)
        {
            string pathToDll = Path.GetDirectoryName(source);
            UnicornAppDomainIsolation<AppDomainObserver> observerIsolation =
                new UnicornAppDomainIsolation<AppDomainObserver>( pathToDll, "Unicorn.TestAdapter Observer AppDomain");
            return observerIsolation.Instance.GetTests(source);
        }

        /// <summary>
        /// Gets list of <see cref="ITestInfo"/> from specified assembly in separate AppDomain.
        /// </summary>
        /// <param name="assembly">test assembly file</param>
        /// <returns>test info list</returns>
        private List<TestInfo> GetTests(string assembly)
        {
            var testsAssembly = Assembly.LoadFrom(assembly);
            var tests = TestsObserver.ObserveTests(testsAssembly);

            List<TestInfo> infos = new List<TestInfo>();

            foreach (var unicornTest in tests)
            {
                var testAttribute = unicornTest
                    .GetCustomAttribute(typeof(TestAttribute), true) as TestAttribute;

                if (testAttribute != null)
                {
                    infos.Add(new TestInfo(unicornTest));
                }
            }

            return infos;
        }
    }
}
#endif