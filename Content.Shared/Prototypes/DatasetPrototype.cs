﻿using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes
{
    [Prototype("dataset")]
    public class DatasetPrototype : IPrototype, IIndexedPrototype
    {
        [DataField("id")]
        private string _id;
        public string ID => _id;

        [DataField("values")]
        private List<string> _values;
        public IReadOnlyList<string> Values => _values;
    }
}
