using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Unicorn.Taf.Core.Testing.Attributes;

namespace Unicorn.TestAdapter
{
    /// <summary>
    /// Represents single test info.
    /// </summary>
    [Serializable]
    public class TestInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestInfo"/> class based on <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="methodInfo"><see cref="MethodInfo"/> of the test</param>
        public TestInfo(MethodInfo methodInfo)
        {
            MethodName = methodInfo.Name;
            ClassPath = methodInfo.ReflectedType.FullName;

            TestAttribute titleAttribute = methodInfo.GetCustomAttribute<TestAttribute>();
            Title = string.IsNullOrEmpty(titleAttribute.Title) ? MethodName : titleAttribute.Title;

            Disabled = methodInfo.IsDefined(typeof(DisabledAttribute), true);

            AuthorAttribute authorAttribute = methodInfo.GetCustomAttribute<AuthorAttribute>(true);
            Author = authorAttribute != null ? authorAttribute.Author : null;

            IEnumerable<CategoryAttribute> categoryAttributes = methodInfo.GetCustomAttributes<CategoryAttribute>(true);

            Categories = new ReadOnlyCollection<string>(categoryAttributes.Select(a => a.Category).ToList());

            TestParametersCount = 
                methodInfo.IsDefined(typeof(TestDataAttribute), true) ? methodInfo.GetParameters().Length : 0;
        }

        /// <summary>
        /// Gets test method name.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Gets test mathod class path.
        /// </summary>
        public string ClassPath { get; }

        /// <summary>
        /// Gets test title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets a value indicating whether test is disabled or not.
        /// </summary>
        public bool Disabled { get; }

        /// <summary>
        /// Gets test author.
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Gets collection of test categories.
        /// </summary>
        public ReadOnlyCollection<string> Categories { get; }

        /// <summary>
        /// Gets count of test parameters.
        /// </summary>
        public int TestParametersCount { get; }
    }
}
