using ArcGIS.Desktop.Framework.Contracts;
using MilitarySymbols;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ProSymbolEditor
{
    public class SymbolAttributeSet : PropertyChangedBase
    {
        //Base attributes
        private string _identity;
        private string _statuses;
        private string _operationalCondition;
        private string _echelon;
        private string _indicator;
        private string _mobility;

        //Delta Attributes
        private string _context;
        private string _modifier1;
        private string _modifier2;

        //Symbol Attributes
        private string _symbolSet;
        private string _symbolEntity;
        private string _selectedSymbolTags;
        private BitmapImage _symbolImage = null;

        public string SymbolSet
        {
            get
            {
                return _symbolSet;
            }
            set
            {
                _symbolSet = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string SymbolEntity
        {
            get
            {
                return _symbolEntity;
            }
            set
            {
                _symbolEntity = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string Identity
        {
            get
            {
                return _identity;
            }
            set
            {
                _identity = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string Echelon
        {
            get
            {
                return _echelon;
            }
            set
            {
                _echelon = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string OperationalCondition
        {
            get
            {
                return _operationalCondition;
            }
            set
            {
                _operationalCondition = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string Statuses
        {
            get
            {
                return _statuses;
            }
            set
            {
                _statuses = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string Indicator
        {
            get
            {
                return _indicator;
            }
            set
            {
                _indicator = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string Mobility
        {
            get
            {
                return _mobility;
            }
            set
            {
                _mobility = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string Context
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string Modifier1
        {
            get
            {
                return _modifier1;
            }
            set
            {
                _modifier1 = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string Modifier2
        {
            get
            {
                return _modifier2;
            }
            set
            {
                _modifier2 = value;
                GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
            }
        }

        public string SelectedSymbolTags
        {
            get
            {
                return _selectedSymbolTags;
            }
        }

        public BitmapImage SymbolImage
        {
            get
            {
                return _symbolImage;
            }
        }

        private void GeneratePreviewSymbol()
        {
            // Step 1: Create a dictionary/map of well known attribute names to values
            Dictionary<string, string> attributeSet = GenerateAttributeSetDictionary();

            // Step 2: Set the SVG Home Folder
            // This should be within the git clone of joint-military-symbology-xml 
            // ex: C:\Github\joint-military-symbology-xml\svg\MIL_STD_2525D_Symbols

            // This is called in CheckSettings below, but you should call yourself if
            // reusing this method 
            Utilities.SetImageFilesHome(@"C:\Projects\Github\joint-military-symbology-xml\svg\MIL_STD_2525D_Symbols");
            if (!Utilities.CheckImageFilesHomeExists())
            //if (!CheckSettings())
            {
                Console.WriteLine("No SVG Folder, can't continue.");
                return;
            }

            // Step 3: Get the Layered Bitmap from the Library
            const int width = 256, height = 256;
            Size exportSize = new Size(width, height);

            System.Drawing.Bitmap exportBitmap;

            bool success = Utilities.ExportSymbolFromAttributes(attributeSet, out exportBitmap, exportSize);

            if (success && exportBitmap != null)
            {
                _symbolImage = ProSymbolUtilities.BitMapToBitmapImage(exportBitmap);
                NotifyPropertyChanged(() => SymbolImage);
            }

            if (!success || (exportBitmap == null))
            {
                Console.WriteLine("Export failed!");
                return;
            }
        }


        public Dictionary<string, string> GenerateAttributeSetDictionary()
        {
            Dictionary<string, string> attributeSet = new Dictionary<string, string>();

            if (_identity != null && _identity != "")
            {
                attributeSet["identity"] = _identity;
            }

            if (_symbolSet != null && _symbolSet != "")
            {
                attributeSet["symbolset"] = _symbolSet;
            }

            if (_symbolEntity != null && _symbolEntity != "")
            {
                attributeSet["symbolentity"] = _symbolEntity;
            }

            if (_indicator != null && _indicator != "")
            {
                attributeSet["indicator"] = _indicator;
            }

            //Echelon or Mobility

            if (_echelon != null && _echelon != "")
            {
                attributeSet["echelon"] = _echelon;
            }

            if (_mobility != null && _mobility != "")
            {
                attributeSet["echelon"] = _mobility;
            }

            //Statuses or Operation

            if (_operationalCondition != null && _operationalCondition != "")
            {
                attributeSet["operationalcondition"] = _operationalCondition;
            }

            if (_statuses != null && _statuses != "")
            {
                attributeSet["operationalcondition"] = _statuses;
            }

            //Delta attributes
            if (_context != null && _context != "")
            {
                attributeSet["context"] = _context;
            }

            if (_modifier1 != null && _modifier1 != "")
            {
                attributeSet["modifier1"] = _modifier1;
            }

            if (_modifier2 != null && _modifier2 != "")
            {
                attributeSet["modifier2"] = _modifier2;
            }

            return attributeSet;
        }

        private void GenerateSelectedSymbolTagsString()
        {
            string statusOperation = "";
            if (_statuses != "")
            {
                statusOperation = _statuses;
            }
            else
            {
                statusOperation = _operationalCondition;
            }

            _selectedSymbolTags = "Standard Identity = " + _identity + ", Symbol Set = " + _symbolSet + ", Operational Condition = " + _operationalCondition + ", Status = " + _statuses +
                ", HQ / Task Force / Dummy = " + _indicator + ", Symbol Entity = " + _symbolEntity + ", Mobility = " + _mobility + 
                ", Echelon = " + _echelon + ", Context = " + _context + ", Modifier 1 = " + _modifier1 + ", Modifier 2 = " + _modifier2;
            NotifyPropertyChanged(() => SelectedSymbolTags);
        }



    }
}
