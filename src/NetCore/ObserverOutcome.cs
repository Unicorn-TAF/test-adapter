using System;
using System.Collections.Generic;
using Unicorn.Taf.Api;

namespace Unicorn.TestAdapter.NetCore
{
    /// <summary>
    /// Represents outcome of tests observer.
    /// </summary>
    [Serializable]
    public class ObserverOutcome : IOutcome
    {
        /// <summary>
        /// Gets list of observed test infos.
        /// </summary>
        public List<TestInfo> TestInfoList { get; set; } = new List<TestInfo>();
    }
}
