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
        private List<SymbolSetMapping> _symbolSetMappings2525D;
        private List<SymbolSetMapping> _symbolSetMappings2525C;

        public List<SymbolSetMapping> SymbolsDictionaryMapping
        {
            get
            {
                return _symbolSetMappings2525D;
            }
        }

        public SymbolSetMappings()
        {
            _symbolSetMappings2525D = new List<SymbolSetMapping>();
            _symbolSetMappings2525D.Add(new SymbolSetMapping("Unknown", GeometryType.Unknown, "00"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("Air", GeometryType.Point, "01"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("AirMissile", GeometryType.Point, "02"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("Space", GeometryType.Point, "05"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("SpaceMissile", GeometryType.Point, "06"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("Units", GeometryType.Point, "10"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("Civilian", GeometryType.Point, "11"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("LandEquipment", GeometryType.Point, "15"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("Installations", GeometryType.Point, "20"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("ControlMeasuresPoints", GeometryType.Point, "25"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("ControlMeasuresLines", GeometryType.Polyline, "25"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("ControlMeasuresAreas", GeometryType.Polygon, "25"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("SeaSurface", GeometryType.Point, "30"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("SeaSubsurface", GeometryType.Point, "35"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("MineWarfare", GeometryType.Point, "36"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("Activities", GeometryType.Point, "40"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("METOCPointsAtmospheric", GeometryType.Point, "45"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("METOCLinesAtmospheric", GeometryType.Polyline, "45"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("METOCAreasAtmospheric", GeometryType.Polygon, "45"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("METOCPointsOceanographic", GeometryType.Point, "46"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("METOCLinesOceanographic", GeometryType.Polyline, "46"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("METOCAreasOceanographic", GeometryType.Polygon, "46"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "50"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "51"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "52"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "53"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "54"));
            _symbolSetMappings2525D.Add(new SymbolSetMapping("Cyberspace", GeometryType.Point, "60"));

            _symbolSetMappings2525C = new List<SymbolSetMapping>();
            _symbolSetMappings2525C.Add(new SymbolSetMapping("Air", GeometryType.Point, "^[S].[A].{7,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("Space", GeometryType.Point, "^[S].[P].{7,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("LandEquipment", GeometryType.Point, "^[S].[G].[E].{5,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("Installations", GeometryType.Point, "^[S].[G].[I].{5,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("Units", GeometryType.Point, "^[S].[GF].{7,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("ControlMeasuresPoints", GeometryType.Point, "^[G].{9,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("ControlMeasuresLines", GeometryType.Polyline, "^[G].{9,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("ControlMeasuresAreas", GeometryType.Polygon, "^[G].{9,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("SeaSurface", GeometryType.Point, "^[S].[S].{7,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("SeaSubsurface", GeometryType.Point, "^[S].[U].{7,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("Activities", GeometryType.Point, "^[OE].{9,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("METOCPoints", GeometryType.Point, "^[W].{9,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("METOCLines", GeometryType.Polyline, "^[W].{9,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("METOCAreas", GeometryType.Polygon, "^[W].{9,}"));
            _symbolSetMappings2525C.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "^[I].{9,}"));
        }

        public string GetFeatureClassFromSymbolSet(string symbolSet, GeometryType geometryType)
        {
            if (string.IsNullOrEmpty(symbolSet))
                return "Units";

            foreach (SymbolSetMapping mapping in _symbolSetMappings2525D)
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

            foreach (SymbolSetMapping mapping in _symbolSetMappings2525C)
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
            else // 2525D
            {
                return GetFeatureClassFromSymbolSet(displayAttributes.SymbolSet, geometryType);
            }
        }

    }
}
