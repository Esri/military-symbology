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
