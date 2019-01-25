/*******************************************************************************
 * Copyright 2016 Esri
 * 
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 * 
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *   Unless required by applicable law or agreed to in writing, software
 *   distributed under the License is distributed on an "AS IS" BASIS,
 *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *   See the License for the specific language governing permissions and
 *   limitations under the License.
 ******************************************************************************/

using System.Collections.Generic;
using ArcGIS.Core.Geometry;

namespace ProSymbolEditor
{
    public class SymbolSetMappings
    {
        private List<SymbolSetMapping> SymbolSetMappings2525D 
        {
            get
            {
                return MilitaryOverlayDataModel.SymbolSetToFeatureClassMapping2525D;
            }
        }

        private List<SymbolSetMapping> SymbolSetMappingsAPP6D
        {
            get
            {
                return MilitaryOverlayDataModel.SymbolSetToFeatureClassMappingAPP6D;
            }
        }

        private List<SymbolSetMapping> SymbolSetMappings2525C
        {
            get
            {
                return MilitaryOverlayDataModel.SymbolSetToFeatureClassMapping2525C;
            }
        }

        public SymbolSetMappings()
        {
        }

        public string GetFeatureClassFromSymbolSet(string symbolSet, GeometryType geometryType)
        {
            if (string.IsNullOrEmpty(symbolSet))
                return "Units";

            foreach (SymbolSetMapping mapping in SymbolSetMappingsAPP6D)
            {
                if ((mapping.SymbolSetOrRegex == symbolSet) && (mapping.GeometryType == geometryType))
                {
                    return mapping.FeatureClassName;
                }
            }
            return "";
        }

        public string GetFeatureClassFromExtendedFunctionCode(string extendedFunctionCode, GeometryType geometryType)
        {
            if (string.IsNullOrEmpty(extendedFunctionCode))
                return "Units";

            foreach (SymbolSetMapping mapping in SymbolSetMappings2525C)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(extendedFunctionCode, mapping.SymbolSetOrRegex) &&
                                (mapping.GeometryType == geometryType))
                {
                    return mapping.FeatureClassName;
                }

            }
            return "";
        }

        public string GetFeatureClassFromMapping(DisplayAttributes displayAttributes, GeometryType geometryType)
        {
            if (displayAttributes == null)
                return string.Empty;

            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            {
                return GetFeatureClassFromExtendedFunctionCode(displayAttributes.ExtendedFunctionCode, geometryType);
            }
            else // 2525D or APPD
            {
                return GetFeatureClassFromSymbolSet(displayAttributes.SymbolSet, geometryType);
            }
        }

    }
}
