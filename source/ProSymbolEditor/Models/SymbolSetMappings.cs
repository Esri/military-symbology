using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSymbolEditor
{
    public class SymbolSetMappings
    {
        private Dictionary<string, string> _symbolSetMappings;

        public Dictionary<string, string> SymbolsDictionaryMapping
        {
            get
            {
                return _symbolSetMappings;
            }
        }

        public SymbolSetMappings()
        {
            //TODO refactor for points/lines/polygons
            _symbolSetMappings = new Dictionary<string, string>();
            _symbolSetMappings.Add("00", "Unknown");
            _symbolSetMappings.Add("01", "Air");
            _symbolSetMappings.Add("02", "AirMissile");
            _symbolSetMappings.Add("05", "Space");
            _symbolSetMappings.Add("06", "SpaceMissile");
            _symbolSetMappings.Add("10", "Units");
            _symbolSetMappings.Add("11", "Civilian");
            _symbolSetMappings.Add("15", "LandEquipment");
            _symbolSetMappings.Add("20", "Installations");
            _symbolSetMappings.Add("25", "ControlMeasuresPoints");
            _symbolSetMappings.Add("30", "SeaSurface");
            _symbolSetMappings.Add("35", "SeaSubsurface");
            _symbolSetMappings.Add("36", "MineWarfare");
            _symbolSetMappings.Add("40", "Activities");
            _symbolSetMappings.Add("45", "METOCPointsAtmospheric");
            _symbolSetMappings.Add("46", "METOCPointsOceanographic");
            //_symbolSetMappings.Add("47", "Meteorological Space");  //Unused
            _symbolSetMappings.Add("50", "SIGINT");
            _symbolSetMappings.Add("51", "SIGINT");
            _symbolSetMappings.Add("52", "SIGINT");
            _symbolSetMappings.Add("53", "SIGINT");
            _symbolSetMappings.Add("54", "SIGINT");
            _symbolSetMappings.Add("60", "Cyberspace");
        }


    }
}
