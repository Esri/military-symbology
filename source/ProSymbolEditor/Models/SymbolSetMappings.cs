using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
