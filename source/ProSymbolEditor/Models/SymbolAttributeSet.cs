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
        private string _context;
        private string _identity;
        private string _symbolSet;
        private string _symbolEntity;
        private string _modifier1;
        private string _modifier2;
        private string _echelon;
        private string _indicator;
        private string _operationalCondition;
        private string _selectedSymbolTags;
        private BitmapImage _symbolImage = null;

        //public event PropertyChangedEventHandler PropertyChanged;

        //private string _hqTaskForceDummy;
        //private string _amplifierDescriptor;
        //private string _entity;
        //private string _entityType;
        //private string _sector1Modifier;
        //private string _sector2Modifier;

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

            //attributeSet["context"]      = "1";
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

            //attributeSet["modifier1"]    = "01";
            //attributeSet["modifier2"]    = "01";

            if (_echelon != null && _echelon != "")
            {
                attributeSet["echelon"] = _echelon;
            }

            //attributeSet["indicator"]    = "7";

            if (_operationalCondition != null && _operationalCondition != "")
            {
                attributeSet["operationalcondition"] = _operationalCondition;
            }
            
            return attributeSet;
        }

        private void GenerateSelectedSymbolTagsString()
        {
            _selectedSymbolTags = "identity = " + _identity + ", symbolset = " + _symbolSet + ", symbolentity = " + _symbolEntity + 
                ", echolon = " + _echelon + ", operationalcondition = " + _operationalCondition;
            NotifyPropertyChanged(() => SelectedSymbolTags);
        }



    }
}
