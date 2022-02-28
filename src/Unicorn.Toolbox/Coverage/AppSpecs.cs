﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Unicorn.Toolbox.Coverage
{
    [DataContract]
    public class AppSpecs
    {
        [DataMember(Name = "name")]
        private string name;

        public string Name => name.ToUpper();

        [DataMember(Name = "modules")]
        public List<Module> Modules { get; set; }
    }
}
