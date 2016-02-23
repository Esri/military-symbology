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
        private string _featureClassName;
        private StyleItemType _styleItemType;
        private string _symbolSet;

        public SymbolSetMapping(string featureClassName, StyleItemType styleItemType, string symbolSet)
        {
            _featureClassName = featureClassName;
            _styleItemType = styleItemType;
            _symbolSet = symbolSet;
        }

        public string FeatureClassName
        {
            get
            {
                return _featureClassName;
            }
            set
            {
                _featureClassName = value;
            }
        }

        public StyleItemType StyleItemType
        {
            get
            {
                return _styleItemType;
            }
            set
            {
                _styleItemType = value;
            }
        }

        public string SymbolSet
        {
            get
            {
                return _symbolSet;
            }
            set
            {
                _symbolSet = value;
            }
        }
    }
}
