#if NET || NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing.Attributes;
using Unicorn.TestAdapter.NetCore;

namespace Unicorn.TestAdapter
{
    public class LoadContextObserver : IDataCollector
    {
        /// <summary>
        /// Gets <see cref="IOutcome"/> from specified assembly in separate assembly load context.
        /// </summary>
        /// <param name="assembly">test assembly</param>
        /// <returns>launch info as <see cref="IOutcome"/></returns>
        public IOutcome CollectData(Assembly assembly)
        {
            IEnumerable<MethodInfo> tests = TestsObserver.ObserveTests(assembly);
            ObserverOutcome outcome = new ObserverOutcome();

            foreach (var unicornTest in tests)
            {
                TestAttribute testAttribute = unicornTest.GetCustomAttribute<TestAttribute>(true);

                if (testAttribute != null)
                {
                    outcome.TestInfoList.Add(new TestInfo(unicornTest));
                }
            }

            return outcome;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static List<TestInfo> GetTestsInfoInIsolation(string source)
        {
            UnicornAssemblyLoadContext observerContext = new UnicornAssemblyLoadContext(Path.GetDirectoryName(source));

            observerContext.Initialize(typeof(IDataCollector));

            try
            {
                Type observerType = observerContext.GetAssemblyContainingType(typeof(LoadContextObserver))
                    .GetTypes()
                    .First(t => t.Name.Equals(typeof(LoadContextObserver).Name));

                IDataCollector observer = Activator.CreateInstance(observerType) as IDataCollector;

                AssemblyName assemblyName = AssemblyName.GetAssemblyName(source);
                Assembly testAssembly = observerContext.GetAssembly(assemblyName);

                IOutcome iTestInfo = observer.CollectData(testAssembly);

                //Outcome transition between load contexts.
                byte[] bytes = LoadContextSerialization.Serialize(iTestInfo);
                ObserverOutcome outcome = LoadContextSerialization.Deserialize<ObserverOutcome>(bytes);

                return outcome.TestInfoList;
            }
            finally
            {
                observerContext.Unload();
            }
        }
    }
}
#endif