using ArcGIS.Core.Data;
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

        //Label Text Attributes
        private DateTime _dateTimeValid;
        private DateTime _dateTimeExpired;
        private string _staffComments;
        private string _additionalInformation;
        private string _uniqueDesignation;
        private string _type;
        private string _commonidentifier;
        private short? _speed;
        private string _higherFormation;
        private string _reinforced;
        private string _credibility;
        private string _reliability;
        
        #region Getters/Setters
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
                NotifyPropertyChanged(() => SymbolSet);
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
                NotifyPropertyChanged(() => SymbolEntity);
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
                //NotifyPropertyChanged(() => Identity);
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

        //For text labels
        public DateTime DateTimeValid
        {
            get
            {
                return _dateTimeValid;
            }
            set
            {
                if (value == null)
                {
                    _dateTimeValid = DateTime.Now;
                }
                else
                {
                    _dateTimeValid = value;
                }

                NotifyPropertyChanged(() => DateTimeValid);
            }
        }

        public DateTime DateTimeExpired
        {
            get
            {
                return _dateTimeExpired;
            }
            set
            {
                if (value == null)
                {
                    _dateTimeExpired = DateTime.Now;
                }
                else
                {
                    _dateTimeExpired = value;
                }
                NotifyPropertyChanged(() => DateTimeExpired);
            }
        }

        public string UniqueDesignation
        {
            get
            {
                return _uniqueDesignation;
            }
            set
            {
                _uniqueDesignation = value;
                NotifyPropertyChanged(() => UniqueDesignation);
            }
        }

        public string StaffComments
        {
            get
            {
                return _staffComments;
            }
            set
            {
                _staffComments = value;
                NotifyPropertyChanged(() => StaffComments);
            }
        }

        public string AdditionalInformation
        {
            get
            {
                return _additionalInformation;
            }
            set
            {
                _additionalInformation = value;
                NotifyPropertyChanged(() => AdditionalInformation);
            }
        }

        public string Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                NotifyPropertyChanged(() => Type);
            }
        }

        public string CommonIdentifier
        {
            get
            {
                return _commonidentifier;
            }
            set
            {
                _commonidentifier = value;
                NotifyPropertyChanged(() => CommonIdentifier);
            }
        }

        public short? Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                _speed = value;
                NotifyPropertyChanged(() => Speed);
            }
        }

        public string HigherFormation
        {
            get
            {
                return _higherFormation;
            }
            set
            {
                _higherFormation = value;
                NotifyPropertyChanged(() => HigherFormation);
            }
        }

        public string Reinforced
        {
            get
            {
                return _reinforced;
            }
            set
            {
                _reinforced = value;
                //NotifyPropertyChanged(() => Reinforced);
            }
        }

        public string Credibility
        {
            get
            {
                return _credibility;
            }
            set
            {
                _credibility = value;
                //NotifyPropertyChanged(() => Credibility);
            }
        }

        public string Reliability
        {
            get
            {
                return _reliability;
            }
            set
            {
                _reliability = value;
                //NotifyPropertyChanged(() => Reliability);
            }
        }

        #endregion

        private void GeneratePreviewSymbol()
        {
            // Step 1: Create a dictionary/map of well known attribute names to values
            Dictionary<string, string> attributeSet = GenerateAttributeSetDictionary();

            // Step 2: Set the SVG Home Folder
            // This should be within the git clone of joint-military-symbology-xml 
            // ex: C:\Github\joint-military-symbology-xml\svg\MIL_STD_2525D_Symbols

            // This is called in CheckSettings below, but you should call yourself if
            // reusing this method 
            //Utilities.SetImageFilesHome(@"C:\Projects\Github\joint-military-symbology-xml\svg\MIL_STD_2525D_Symbols");

            string militarySymbolsPath = System.IO.Path.Combine(ProSymbolUtilities.AddinAssemblyLocation(), "Images", "MIL_STD_2525D_Symbols");
            bool pathExists = Utilities.SetImageFilesHome(militarySymbolsPath);

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
                _symbolImage = null;
                NotifyPropertyChanged(() => SymbolImage);
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

        public void PopulateRowBufferWithAttributes(ref RowBuffer rowBuffer)
        {
            if (_identity != null && _identity != "")
            {
                rowBuffer["identity"] = _identity;
            }

            if (_symbolSet != null && _symbolSet != "")
            {
                rowBuffer["symbolset"] = Convert.ToInt32(_symbolSet);
            }

            if (_symbolEntity != null && _symbolEntity != "")
            {
                rowBuffer["symbolentity"] = Convert.ToInt32(_symbolEntity);
            }

            //Indicator / HQTFFD /

            if (_indicator != null && _indicator != "")
            {
                rowBuffer["indicator"] = _indicator;
            }

            //Echelon or Mobility

            if (_echelon != null && _echelon != "")
            {
                rowBuffer["echelon"] = _echelon;
            }

            if (_mobility != null && _mobility != "")
            {
                rowBuffer["mobility"] = _mobility;
            }

            //Statuses or Operation

            if (_operationalCondition != null && _operationalCondition != "")
            {
                rowBuffer["operationalcondition"] = _operationalCondition;
            }

            if (_statuses != null && _statuses != "")
            {
                rowBuffer["status"] = _statuses;
            }

            //Delta attributes
            if (_context != null && _context != "")
            {
                rowBuffer["context"] = _context;
            }

            if (_modifier1 != null && _modifier1 != "")
            {
                rowBuffer["modifier1"] = _modifier1;
            }

            if (_modifier2 != null && _modifier2 != "")
            {
                rowBuffer["modifier2"] = _modifier2;
            }

            //LABELS
            if (DateTimeValid != null)
            {
                rowBuffer["datetimevalid"] = DateTimeValid.ToString();
            }

            if (DateTimeExpired != null)
            {
                rowBuffer["datetimeexpired"] = DateTimeExpired.ToString();
            }
            
            if (UniqueDesignation != null && UniqueDesignation != "")
            {
                rowBuffer["uniquedesignation"] = UniqueDesignation;
            }

            if (StaffComments != null && StaffComments != "")
            {
                rowBuffer["staffcomment"] = StaffComments;
            }

            if (AdditionalInformation != null && AdditionalInformation != "")
            {
                rowBuffer["additionalinformation"] = AdditionalInformation;
            }

            if (Type != null && Type != "")
            {
                rowBuffer["type"] = Type;
            }

            if (CommonIdentifier != null && CommonIdentifier != "")
            {
                rowBuffer["commonidentifier"] = CommonIdentifier;
            }

            if (Speed != null)
            {
                //Short
                rowBuffer["speed"] = Speed;
            }

            if (HigherFormation != null && HigherFormation != "")
            {
                rowBuffer["higherFormation"] = HigherFormation;
            }

            if (Reinforced != null && Reinforced != "")
            {
                rowBuffer["reinforced"] = Reinforced;
            }

            if (Credibility != null && Credibility != "")
            {
                rowBuffer["credibility"] = Credibility;
            }

            if (Reliability != null && Reliability != "")
            {
                rowBuffer["reliability"] = Reliability;
            }
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
        
        public void ResetAttributes()
        {
            //Reset attributes
            SymbolSet = "";
            SymbolEntity = "";
            Echelon = "";
            Identity = "";
            OperationalCondition = "";
            Statuses = "";
            Mobility = "";
            Indicator = "";
            Context = "";
            Modifier1 = "";
            Modifier2 = "";

            //Reset label text attributes
            DateTimeValid = DateTime.Now;
            DateTimeExpired = DateTime.Now;
            UniqueDesignation = "";
            StaffComments = "";
            AdditionalInformation = "";
            Type = "";
            CommonIdentifier = "";
            Speed = null;
            HigherFormation = "";
            Reinforced = "";
            Credibility = null;
            Reliability = "";
        }
    }
}
