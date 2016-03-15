using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSymbolEditor
{
    public class SymbolSetMapping
    {
        public string FeatureClassName { get; set; }
        public StyleItemType StyleItemType { get; set; }
        public string SymbolSet { get; set; }

        public SymbolSetMapping(string featureClassName, StyleItemType styleItemType, string symbolSet)
        {
            FeatureClassName = featureClassName;
            StyleItemType = styleItemType;
            SymbolSet = symbolSet;
        }


    }
}
