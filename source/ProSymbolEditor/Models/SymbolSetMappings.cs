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
using ArcGIS.Desktop.Mapping;

namespace ProSymbolEditor
{
    public class SymbolSetMappings
    {
        private List<SymbolSetMapping> _symbolSetMappings;

        public List<SymbolSetMapping> SymbolsDictionaryMapping
        {
            get
            {
                return _symbolSetMappings;
            }
        }

        public SymbolSetMappings()
        {
            _symbolSetMappings = new List<SymbolSetMapping>();
            _symbolSetMappings.Add(new SymbolSetMapping("Unknown", StyleItemType.Unknown, "00"));
            _symbolSetMappings.Add(new SymbolSetMapping("Air", StyleItemType.PointSymbol, "01"));
            _symbolSetMappings.Add(new SymbolSetMapping("AirMissile", StyleItemType.PointSymbol, "02"));
            _symbolSetMappings.Add(new SymbolSetMapping("Space", StyleItemType.PointSymbol, "05"));
            _symbolSetMappings.Add(new SymbolSetMapping("SpaceMissile", StyleItemType.PointSymbol, "06"));
            _symbolSetMappings.Add(new SymbolSetMapping("Units", StyleItemType.PointSymbol, "10"));
            _symbolSetMappings.Add(new SymbolSetMapping("Civilian", StyleItemType.PointSymbol, "11"));
            _symbolSetMappings.Add(new SymbolSetMapping("LandEquipment", StyleItemType.PointSymbol, "15"));
            _symbolSetMappings.Add(new SymbolSetMapping("Installations", StyleItemType.PointSymbol, "20"));
            _symbolSetMappings.Add(new SymbolSetMapping("ControlMeasuresPoints", StyleItemType.PointSymbol, "25"));
            _symbolSetMappings.Add(new SymbolSetMapping("ControlMeasuresLines", StyleItemType.LineSymbol, "25"));
            _symbolSetMappings.Add(new SymbolSetMapping("ControlMeasuresAreas", StyleItemType.PolygonSymbol, "25"));
            _symbolSetMappings.Add(new SymbolSetMapping("SeaSurface", StyleItemType.PointSymbol, "30"));
            _symbolSetMappings.Add(new SymbolSetMapping("SeaSubsurface", StyleItemType.PointSymbol, "35"));
            _symbolSetMappings.Add(new SymbolSetMapping("MineWarfare", StyleItemType.PointSymbol, "36"));
            _symbolSetMappings.Add(new SymbolSetMapping("Activities", StyleItemType.PointSymbol, "40"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCPointsAtmospheric", StyleItemType.PointSymbol, "45"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCLinesAtmospheric", StyleItemType.LineSymbol, "45"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCAreasAtmospheric", StyleItemType.PolygonSymbol, "45"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCPointsOceanographic", StyleItemType.PointSymbol, "46"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCLinesOceanographic", StyleItemType.LineSymbol, "46"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCAreasOceanographic", StyleItemType.PolygonSymbol, "46"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", StyleItemType.PointSymbol, "50"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", StyleItemType.PointSymbol, "51"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", StyleItemType.PointSymbol, "52"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", StyleItemType.PointSymbol, "53"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", StyleItemType.PointSymbol, "54"));
            _symbolSetMappings.Add(new SymbolSetMapping("Cyberspace", StyleItemType.PointSymbol, "60"));
        }

        public string GetFeatureClassFromMapping(string symbolSet, StyleItemType geometryType)
        {
            foreach(SymbolSetMapping mapping in _symbolSetMappings)
            {
                if (mapping.SymbolSet == symbolSet && mapping.StyleItemType == geometryType)
                {
                    return mapping.FeatureClassName;
                }
            }

            return "";
        }


    }
}
