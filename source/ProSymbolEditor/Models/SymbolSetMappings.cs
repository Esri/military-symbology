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
using ArcGIS.Core.Geometry;

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
            _symbolSetMappings.Add(new SymbolSetMapping("Unknown", GeometryType.Unknown, "00"));
            _symbolSetMappings.Add(new SymbolSetMapping("Air", GeometryType.Point, "01"));
            _symbolSetMappings.Add(new SymbolSetMapping("AirMissile", GeometryType.Point, "02"));
            _symbolSetMappings.Add(new SymbolSetMapping("Space", GeometryType.Point, "05"));
            _symbolSetMappings.Add(new SymbolSetMapping("SpaceMissile", GeometryType.Point, "06"));
            _symbolSetMappings.Add(new SymbolSetMapping("Units", GeometryType.Point, "10"));
            _symbolSetMappings.Add(new SymbolSetMapping("Civilian", GeometryType.Point, "11"));
            _symbolSetMappings.Add(new SymbolSetMapping("LandEquipment", GeometryType.Point, "15"));
            _symbolSetMappings.Add(new SymbolSetMapping("Installations", GeometryType.Point, "20"));
            _symbolSetMappings.Add(new SymbolSetMapping("ControlMeasuresPoints", GeometryType.Point, "25"));
            _symbolSetMappings.Add(new SymbolSetMapping("ControlMeasuresLines", GeometryType.Polyline, "25"));
            _symbolSetMappings.Add(new SymbolSetMapping("ControlMeasuresAreas", GeometryType.Polygon, "25"));
            _symbolSetMappings.Add(new SymbolSetMapping("SeaSurface", GeometryType.Point, "30"));
            _symbolSetMappings.Add(new SymbolSetMapping("SeaSubsurface", GeometryType.Point, "35"));
            _symbolSetMappings.Add(new SymbolSetMapping("MineWarfare", GeometryType.Point, "36"));
            _symbolSetMappings.Add(new SymbolSetMapping("Activities", GeometryType.Point, "40"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCPointsAtmospheric", GeometryType.Point, "45"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCLinesAtmospheric", GeometryType.Polyline, "45"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCAreasAtmospheric", GeometryType.Polygon, "45"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCPointsOceanographic", GeometryType.Point, "46"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCLinesOceanographic", GeometryType.Polyline, "46"));
            _symbolSetMappings.Add(new SymbolSetMapping("METOCAreasOceanographic", GeometryType.Polygon, "46"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "50"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "51"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "52"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "53"));
            _symbolSetMappings.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "54"));
            _symbolSetMappings.Add(new SymbolSetMapping("Cyberspace", GeometryType.Point, "60"));
        }

        public string GetFeatureClassFromMapping(string symbolSet, GeometryType geometryType)
        {
            foreach(SymbolSetMapping mapping in _symbolSetMappings)
            {
                if (mapping.SymbolSet == symbolSet && mapping.GeometryType == geometryType)
                {
                    return mapping.FeatureClassName;
                }
            }

            return "";
        }


    }
}
