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

using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using System.Web.Script.Serialization;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace ProSymbolEditor
{
    /// <summary>
    /// This is the model for the entire set of information known about a military symbol
    /// It is also how a symbol is serialized as a "favorite"
    /// </summary>
    [DisplayName("Symbol Attributes")]
    public class SymbolAttributeSet : PropertyChangedBase
    {
        public SymbolAttributeSet()
        {
            Initialize();
        }

        public SymbolAttributeSet(Dictionary<string, string> fieldValues)
        {
            Initialize();

            SetPropertiesFromFieldAttributes(fieldValues);
        }

        private void Initialize()
        {
            //Used to make a SymbolAttributeSet from field data in a feature
            DisplayAttributes = new DisplayAttributes();
            DisplayAttributes.PropertyChanged += Attributes_PropertyChanged;

            LabelAttributes = new LabelAttributes();
            LabelAttributes.PropertyChanged += LabelAttributes_PropertyChanged;

            StandardVersion = ProSymbolUtilities.StandardString;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || (GetType() != obj.GetType()))
                return false;

            SymbolAttributeSet compareObj = obj as SymbolAttributeSet;

            if ((DisplayAttributes == null) || (LabelAttributes == null))
                return false;

            return DisplayAttributes.Equals(compareObj.DisplayAttributes)
             && LabelAttributes.Equals(compareObj.LabelAttributes);
        }

        public override int GetHashCode()
        {
            if ((DisplayAttributes == null) || (LabelAttributes == null))
                return 0;
            else
                return DisplayAttributes.GetHashCode() ^ LabelAttributes.GetHashCode();
        }

        [ScriptIgnore]
        public Dictionary<string, string> AttributesDictionary
        {
            get
            {

                Dictionary<string, string> dict = new Dictionary<string, string>();

                dict.Add("SymbolType", this.Name);

                foreach (var prop in DisplayAttributes.GetType().GetProperties())
                {
                    string key = prop.Name;
                    object value = prop.GetValue(DisplayAttributes, null);
                    if (value == null)
                        continue;

                    string valueAsString = value.ToString();

                    // Skip non-value types
                    if (valueAsString.StartsWith("ProSymbolEditor"))
                        continue;

                    // If debug needed:
                    // System.Diagnostics.Trace.WriteLine(string.Format("{0}={1}", key, valueAsString));

                    if (string.IsNullOrEmpty(valueAsString) || dict.ContainsKey(key))
                        continue;

                    dict.Add(key, valueAsString);
                }

                foreach (var prop in LabelAttributes.GetType().GetProperties())
                {
                    string key = prop.Name;
                    object value = prop.GetValue(LabelAttributes, null);
                    if (value == null)
                        continue;

                    string valueAsString = value.ToString();

                    // Skip non-value types
                    if (valueAsString.StartsWith("ProSymbolEditor"))
                        continue;

                    // If debug needed:
                    // System.Diagnostics.Trace.WriteLine(string.Format("{0}={1}", key, valueAsString));

                    if (string.IsNullOrEmpty(valueAsString) || dict.ContainsKey(key))
                        continue;

                    dict.Add(key, valueAsString);
                }

                return dict;
            }
        }

        #region Getters/Setters

        [ExpandableObject]
        public DisplayAttributes DisplayAttributes { get; set; }

        [ExpandableObject]
        public LabelAttributes LabelAttributes { get; set; }

        public string Name 
        {
            get
            {
                return ProSymbolUtilities.TagsToSymbolName(SymbolTags);
            }
        }

        public string FavoriteId { get; set; }

        private string _standardVersion = string.Empty;
        public string StandardVersion {
            get { return _standardVersion; }
            set
            {
                if (value != _standardVersion)
                {
                    _standardVersion = value;
                    NotifyPropertyChanged(() => StandardVersion);
                }
            }
        }

        /// <summary>
        /// These are the symbol tags retrieved from a military feature style file or favorite
        /// See the style file documentation: https://github.com/Esri/military-features-data
        /// IMPORTANT/TRICKY: 
        /// Because all information known about a symbol comes from these style/favorite tags, key
        /// tags have a particular position in the tag list so they can be found, this order is:
        /// GEOMETRY_TYPE=tags[-3] (3rd to last list item)
        /// SYMBOL_NAME=tags[-2] (2nd to last list item)
        /// SYMBOL_ID=tags[-1] (last list item)
        /// General tag format: {other tags};GEOMETRY_TYPE;SYMBOL_NAME;SYMBOL_ID
        /// Example tags: Infantry; Land Unit; POINT; Land Unit : Infantry : 10110110
        /// If you are modifying or overwriting these tags, the tag order for these last 3 tags must 
        /// be maintained because several places in the code depend on this information: 
        /// ex when loading a new style item or new favorite.
        /// </summary>
        public string SymbolTags { get; set; }

        public bool IsValid
        {
            get
            {
                if (DisplayAttributes == null)
                    return false;

                // Check any properties that indicate this object is not initialized/valid
                if (string.IsNullOrEmpty(DisplayAttributes.SymbolSet) &&
                    string.IsNullOrEmpty(DisplayAttributes.ExtendedFunctionCode))
                    return false;

                return true;
            }
        }

        private BitmapImage _symbolImage = null;

        [ScriptIgnore]
        public BitmapImage SymbolImage
        {
            get
            {
                if (_symbolImage == null)
                    return UnknownSymbolImage;

                return _symbolImage;
            }
        }

        private BitmapImage _unknownSymbolImage = null;

        [ScriptIgnore]
        public BitmapImage UnknownSymbolImage
        {
            get
            {
                if (_unknownSymbolImage == null)
                {
                    Uri oUri = new Uri(@"pack://application:,,,/MilitarySymbolEditor;component/Images/UnknownSymbol.png");
                    _unknownSymbolImage = new BitmapImage(oUri);
                }

                return _unknownSymbolImage;
            }
        }

        #endregion

        private System.Threading.Tasks.Task<System.Windows.Media.ImageSource> GetBitmapImageAsync(Dictionary<string, object> attributes)
        {
            if (attributes == null)
                return null;

            return ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() => {
                try
                {
                    string standard = "mil" + ProSymbolUtilities.StandardString.ToLower();
                    ArcGIS.Core.CIM.CIMSymbol symbol = ArcGIS.Desktop.Mapping.SymbolFactory.Instance.GetDictionarySymbol(standard, attributes);

                    if (symbol == null)
                        return null;

                    // IMPORTANT + WORKAROUND + TRICKY:
                    // Pro SDK does not directly provide a way to set a PATCH_SIZE > 64 pixels
                    // However you can do this if the value is negative (-1) but it transforms/flips the image
                    // Therefore we flip the image back in:
                    // Views\MilitarySymbolDockpane.xaml.cs - Image.RenderTransform
                    // ViewModels\MilitarySymbolDockpaneViewModel.cs - GetClipboardImage
                    // If this ever gets changed/fixed in ProSDK, you must remove the flip there
                    const int PATCH_SIZE = -256;  // negative value is a workaround
                    var si = new ArcGIS.Desktop.Mapping.SymbolStyleItem()
                    {
                        Symbol = symbol,
                        PatchHeight = PATCH_SIZE,
                        PatchWidth = PATCH_SIZE
                    };
                    return si.PreviewImage;
                }
                catch (Exception ex)
                {
                    return null;
                }
             });
        }

        public async void GeneratePreviewSymbol()
        {
            // Step 1: Create a dictionary/map of well known attribute names to values
            Dictionary<string, object> attributeSet = GenerateAttributeSetDictionary();

            if ((attributeSet == null) || (attributeSet.Count == 0))
            {
                _symbolImage = null;
                return;
            }

            _symbolImage = await GetBitmapImageAsync(attributeSet) as BitmapImage;

            NotifyPropertyChanged(() => SymbolImage);
        }

        public Dictionary<string, object> GenerateAttributeSetDictionary()
        {
            Dictionary<string, object> attributeSet = new Dictionary<string, object>();

            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.ExtendedFunctionCode))
                {
                    attributeSet["extendedfunctioncode"] = DisplayAttributes.ExtendedFunctionCode;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Identity))
                {
                    attributeSet["affiliation"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Status))
                {
                    attributeSet["status"] = DisplayAttributes.Status;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator))
                {
                    attributeSet["hqtffd"] = DisplayAttributes.Indicator;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon))
                {
                    attributeSet["echelonmobility"] = DisplayAttributes.Echelon;
                }
            }
            else // 2525D
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.Identity))
                {
                    attributeSet["identity"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolSet))
                {
                    attributeSet["symbolset"] = DisplayAttributes.SymbolSet;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolEntity))
                {
                    attributeSet["symbolentity"] = DisplayAttributes.SymbolEntity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator))
                {
                    attributeSet["indicator"] = DisplayAttributes.Indicator;
                }

                //Echelon or Mobility

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon))
                {
                    attributeSet["echelon"] = DisplayAttributes.Echelon;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Mobility))
                {
                    attributeSet["echelon"] = DisplayAttributes.Mobility;
                }

                //Statuses or Operation

                if (!string.IsNullOrEmpty(DisplayAttributes.OperationalCondition))
                {
                    attributeSet["operationalcondition"] = DisplayAttributes.OperationalCondition;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Status))
                {
                    attributeSet["operationalcondition"] = DisplayAttributes.Status;
                }

                //Delta attributes
                if (!string.IsNullOrEmpty(DisplayAttributes.Context))
                {
                    attributeSet["context"] = DisplayAttributes.Context;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier1))
                {
                    attributeSet["modifier1"] = DisplayAttributes.Modifier1;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier2))
                {
                    attributeSet["modifier2"] = DisplayAttributes.Modifier2;
                }
            }

            return attributeSet;
        }

        public void PopulateRowBufferWithAttributes(ref RowBuffer rowBuffer)
        {
            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            {

                if (!string.IsNullOrEmpty(DisplayAttributes.ExtendedFunctionCode))
                {
                    rowBuffer["extendedfunctioncode"] = DisplayAttributes.ExtendedFunctionCode;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Identity))
                {
                    rowBuffer["affiliation"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator))
                {
                    rowBuffer["hqtffd"] = DisplayAttributes.Indicator;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon))
                {
                    rowBuffer["echelonmobility"] = DisplayAttributes.Echelon;
                }

            }
            else
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.Identity))
                {
                    rowBuffer["identity"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolSet))
                {
                    rowBuffer["symbolset"] = Convert.ToInt32(DisplayAttributes.SymbolSet);
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolEntity))
                {
                    rowBuffer["symbolentity"] = Convert.ToInt32(DisplayAttributes.SymbolEntity);
                }

                //Indicator / HQTFFD /

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator))
                {
                    rowBuffer["indicator"] = DisplayAttributes.Indicator;
                }

                //Echelon or Mobility

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon))
                {
                    rowBuffer["echelon"] = DisplayAttributes.Echelon;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Mobility))
                {
                    rowBuffer["mobility"] = DisplayAttributes.Mobility;
                }

                //Statuses or Operation

                if (!string.IsNullOrEmpty(DisplayAttributes.OperationalCondition))
                {
                    rowBuffer["operationalcondition"] = DisplayAttributes.OperationalCondition;
                }

                //Delta attributes
                if (!string.IsNullOrEmpty(DisplayAttributes.Context))
                {
                    rowBuffer["context"] = DisplayAttributes.Context;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier1))
                {
                    rowBuffer["modifier1"] = DisplayAttributes.Modifier1;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier2))
                {
                    rowBuffer["modifier2"] = DisplayAttributes.Modifier2;
                }
            }

            if (!string.IsNullOrEmpty(DisplayAttributes.Status))
            {
                rowBuffer["status"] = DisplayAttributes.Status;
            }

            //LABELS
            if (LabelAttributes.DateTimeValid != null)
            {
                rowBuffer["datetimevalid"] = LabelAttributes.DateTimeValid.ToString();
            }

            if (LabelAttributes.DateTimeExpired != null)
            {
                rowBuffer["datetimeexpired"] = LabelAttributes.DateTimeExpired.ToString();
            }
            
            if (!string.IsNullOrEmpty(LabelAttributes.UniqueDesignation))
            {
                rowBuffer["uniquedesignation"] = LabelAttributes.UniqueDesignation;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.StaffComments))
            {
                rowBuffer["staffcomment"] = LabelAttributes.StaffComments;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.AdditionalInformation))
            {
                rowBuffer["additionalinformation"] = LabelAttributes.AdditionalInformation;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.Type))
            {
                rowBuffer["type"] = LabelAttributes.Type;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.CommonIdentifier))
            {
                rowBuffer["commonidentifier"] = LabelAttributes.CommonIdentifier;
            }

            if (LabelAttributes.Speed != null)
            {
                //Short
                rowBuffer["speed"] = LabelAttributes.Speed;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.HigherFormation))
            {
                rowBuffer["higherFormation"] = LabelAttributes.HigherFormation;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.Reinforced))
            {
                rowBuffer["reinforced"] = LabelAttributes.Reinforced;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.Credibility))
            {
                rowBuffer["credibility"] = LabelAttributes.Credibility;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.Reliability))
            {
                rowBuffer["reliability"] = LabelAttributes.Reliability;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.CountryCode))
            {
                rowBuffer["countrycode"] = LabelAttributes.CountryCode;
            }
        }

        public void PopulateFeatureWithAttributes(ref Feature feature)
        {
            if (feature == null)
            {
                // not normally possible with ref parameter, but check just in case
                System.Diagnostics.Debug.WriteLine("Null Feature passed to PopulateFeatureWithAttributes");
                return;
            }

            // Implementation Note: feature fields with domains attached can not be set to null
            // so these fields with domains must also be checked for IsNullOrEmpty
            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            { 
                if (!string.IsNullOrEmpty(DisplayAttributes.ExtendedFunctionCode) && 
                    (feature.FindField("extendedfunctioncode") >= 0))
                    feature["extendedfunctioncode"] = DisplayAttributes.ExtendedFunctionCode;

                if (!string.IsNullOrEmpty(DisplayAttributes.Identity) && 
                    (feature.FindField("affiliation") >= 0))
                    feature["affiliation"] = DisplayAttributes.Identity;

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator) && 
                    (feature.FindField("hqtffd") >= 0))
                    feature["hqtffd"] = DisplayAttributes.Indicator;

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon) && 
                    (feature.FindField("echelonmobility") >= 0))
                    feature["echelonmobility"] = DisplayAttributes.Echelon;
            }
            else // 2525D/Delta attributes
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.Identity) && 
                    (feature.FindField("identity") >= 0))
                    feature["identity"] = DisplayAttributes.Identity;

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolSet) && 
                    (feature.FindField("symbolset") >= 0))
                    feature["symbolset"] = Convert.ToInt32(DisplayAttributes.SymbolSet);

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolEntity) && 
                    (feature.FindField("symbolentity") >= 0))
                    feature["symbolentity"] = Convert.ToInt32(DisplayAttributes.SymbolEntity);

                //Indicator / HQTFFD
                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator) && 
                    (feature.FindField("indicator") >= 0))
                    feature["indicator"] = DisplayAttributes.Indicator;

                //Echelon or Mobility
                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon) && 
                    (feature.FindField("echelon") >= 0))
                    feature["echelon"] = DisplayAttributes.Echelon;

                if (!string.IsNullOrEmpty(DisplayAttributes.Mobility) && 
                    (feature.FindField("mobility") >= 0))
                    feature["mobility"] = DisplayAttributes.Mobility;

                //Statuses or Operation
                if (!string.IsNullOrEmpty(DisplayAttributes.OperationalCondition) && 
                    (feature.FindField("operationalcondition") >= 0))
                    feature["operationalcondition"] = DisplayAttributes.OperationalCondition;

                if (!string.IsNullOrEmpty(DisplayAttributes.Context) && 
                    (feature.FindField("context") >= 0))
                    feature["context"] = DisplayAttributes.Context;

                //Modifiers
                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier1) && 
                    (feature.FindField("modifier1") >= 0))
                    feature["modifier1"] = DisplayAttributes.Modifier1;

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier2) && 
                    (feature.FindField("modifier2") >= 0))
                    feature["modifier2"] = DisplayAttributes.Modifier2;
            }

            // Apply to all versions of the standard
            if (!string.IsNullOrEmpty(DisplayAttributes.Status) && 
               (feature.FindField("status") >= 0))
                feature["status"] = DisplayAttributes.Status;

            //LABELS
            if ((LabelAttributes.DateTimeValid != null) && 
                (feature.FindField("datetimevalid") >= 0))
            {
                feature["datetimevalid"] = LabelAttributes.DateTimeValid.ToString();
            }

            if ((LabelAttributes.DateTimeExpired != null) && 
                (feature.FindField("datetimeexpired") >= 0))
            {
                feature["datetimeexpired"] = LabelAttributes.DateTimeExpired.ToString();
            }

            if (feature.FindField("uniquedesignation") >= 0)
                feature["uniquedesignation"] = LabelAttributes.UniqueDesignation;

            if (feature.FindField("staffcomment") >= 0)
                feature["staffcomment"] = LabelAttributes.StaffComments;

            if (feature.FindField("additionalinformation") >= 0)
                feature["additionalinformation"] = LabelAttributes.AdditionalInformation;

            if (feature.FindField("type") >= 0)
                feature["type"] = LabelAttributes.Type;

            if (feature.FindField("commonidentifier") >= 0)
                feature["commonidentifier"] = LabelAttributes.CommonIdentifier;

            if (feature.FindField("speed") >= 0)
                feature["speed"] = LabelAttributes.Speed;

            if (feature.FindField("higherFormation") >= 0)
                feature["higherFormation"] = LabelAttributes.HigherFormation;

            if (!string.IsNullOrEmpty(LabelAttributes.Reinforced) && 
                (feature.FindField("reinforced") >= 0))
                feature["reinforced"] = LabelAttributes.Reinforced;

            if (!string.IsNullOrEmpty(LabelAttributes.Credibility) && 
                (feature.FindField("credibility") >= 0))
                feature["credibility"] = LabelAttributes.Credibility;

            if (!string.IsNullOrEmpty(LabelAttributes.Reliability) &&
                (feature.FindField("reliability") >= 0))
                feature["reliability"] = LabelAttributes.Reliability;

            if (!string.IsNullOrEmpty(LabelAttributes.CountryCode) && 
                (feature.FindField("countrycode") >= 0))
                feature["countrycode"] = LabelAttributes.CountryCode;
        }

        private void SetPropertiesFromFieldAttributes(Dictionary<string, string> fieldValues)
        {
            if (fieldValues == null)
                return;

            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            {
                if (fieldValues.ContainsKey("extendedfunctioncode"))
                {
                    DisplayAttributes.ExtendedFunctionCode = fieldValues["extendedfunctioncode"];
                }
                if (fieldValues.ContainsKey("affiliation"))
                {
                    DisplayAttributes.Identity = fieldValues["affiliation"];
                }
                if (fieldValues.ContainsKey("hqtffd"))
                {
                    DisplayAttributes.Indicator = fieldValues["hqtffd"];
                }
                if (fieldValues.ContainsKey("echelonmobility"))
                {
                    DisplayAttributes.Echelon = fieldValues["echelonmobility"];
                }
            }
            else
            {
                if (fieldValues.ContainsKey("identity"))
                {
                    DisplayAttributes.Identity = fieldValues["identity"];
                }
                if (fieldValues.ContainsKey("symbolset"))
                {
                    // Tricky symbolset string expected to be len 2 - fixes bug with "01" "02" "05" symbol sets
                    string symbolSetValue = fieldValues["symbolset"];
                    if (!string.IsNullOrEmpty(symbolSetValue))
                    {
                        string paddedSymbolSet = symbolSetValue.PadLeft(2, '0');
                        DisplayAttributes.SymbolSet = paddedSymbolSet;
                    }
                }

                if (fieldValues.ContainsKey("symbolentity"))
                {
                    DisplayAttributes.SymbolEntity = fieldValues["symbolentity"];
                }

                if (fieldValues.ContainsKey("indicator"))
                {
                    DisplayAttributes.Indicator = fieldValues["indicator"];
                }

                if (fieldValues.ContainsKey("echelon"))
                {
                    DisplayAttributes.Echelon = fieldValues["echelon"];
                }

                if (fieldValues.ContainsKey("mobility"))
                {
                    DisplayAttributes.Mobility = fieldValues["mobility"];
                }

                if (fieldValues.ContainsKey("operationalcondition"))
                {
                    DisplayAttributes.OperationalCondition = fieldValues["operationalcondition"];
                }
                if (fieldValues.ContainsKey("context"))
                {
                    DisplayAttributes.Context = fieldValues["context"];
                }

                if (fieldValues.ContainsKey("modifier1"))
                {
                    DisplayAttributes.Modifier1 = fieldValues["modifier1"];
                }

                if (fieldValues.ContainsKey("modifier2"))
                {
                    DisplayAttributes.Modifier2 = fieldValues["modifier2"];
                }
            }

            if (fieldValues.ContainsKey("status"))
            {
                DisplayAttributes.Status = fieldValues["status"];
            }

            //LABELS
            if (fieldValues.ContainsKey("datetimevalid"))
            {
                // TODO: add tryparse
                LabelAttributes.DateTimeValid = DateTime.Parse(fieldValues["datetimevalid"]);
            }

            if (fieldValues.ContainsKey("datetimeexpired"))
            {
                // TODO: add tryparse
                LabelAttributes.DateTimeExpired = DateTime.Parse(fieldValues["datetimeexpired"]);
            }

            if (fieldValues.ContainsKey("uniquedesignation"))
            {
                LabelAttributes.UniqueDesignation = fieldValues["uniquedesignation"];
            }

            if (fieldValues.ContainsKey("staffcomment"))
            {
                LabelAttributes.StaffComments = fieldValues["staffcomment"];
            }

            if (fieldValues.ContainsKey("additionalinformation"))
            {
                LabelAttributes.AdditionalInformation = fieldValues["additionalinformation"];
            }

            if (fieldValues.ContainsKey("type"))
            {
                LabelAttributes.Type = fieldValues["type"];
            }

            if (fieldValues.ContainsKey("commonidentifier"))
            {
                LabelAttributes.CommonIdentifier = fieldValues["commonidentifier"];
            }

            if (fieldValues.ContainsKey("speed"))
            {
                LabelAttributes.Speed = short.Parse(fieldValues["speed"]);
            }

            if (fieldValues.ContainsKey("higherFormation"))
            {
                LabelAttributes.HigherFormation = fieldValues["higherFormation"];
            }

            if (fieldValues.ContainsKey("reinforced"))
            {
                LabelAttributes.Reinforced = fieldValues["reinforced"];
            }

            if (fieldValues.ContainsKey("credibility"))
            {
                LabelAttributes.Credibility = fieldValues["credibility"];
            }

            if (fieldValues.ContainsKey("reliability"))
            {
                LabelAttributes.Reliability = fieldValues["reliability"];
            }

            if (fieldValues.ContainsKey("countrycode"))
            {
                LabelAttributes.CountryCode = fieldValues["countrycode"];
            }
        }

        public void ResetAttributes()
        {
            //Reset attributes
            _symbolImage = null;

            DisplayAttributes.SymbolSet = "";
            DisplayAttributes.SymbolEntity = "";
            DisplayAttributes.ExtendedFunctionCode = "";
            DisplayAttributes.Echelon = "";
            DisplayAttributes.Identity = "";
            DisplayAttributes.OperationalCondition = "";
            DisplayAttributes.Status = "";
            DisplayAttributes.Mobility = "";
            DisplayAttributes.Indicator = "";
            DisplayAttributes.Context = "";
            DisplayAttributes.Modifier1 = "";
            DisplayAttributes.Modifier2 = "";

            //Reset label text attributes
            LabelAttributes.DateTimeValid = null;
            LabelAttributes.DateTimeExpired = null;
            LabelAttributes.UniqueDesignation = "";
            LabelAttributes.StaffComments = "";
            LabelAttributes.AdditionalInformation = "";
            LabelAttributes.Type = "";
            LabelAttributes.CommonIdentifier = "";
            LabelAttributes.Speed = null;
            LabelAttributes.HigherFormation = "";
            LabelAttributes.Reinforced = "";
            LabelAttributes.Credibility = null;
            LabelAttributes.Reliability = "";
            LabelAttributes.CountryCode = "";

            SymbolTags = "";

            StandardVersion = ProSymbolUtilities.StandardString;
        }

        private void Attributes_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GeneratePreviewSymbol();

            // Tell the XCTK grid to get the updated label for this 
            NotifyPropertyChanged(() => Name);
            NotifyPropertyChanged(() => AttributesDictionary);
        }

        private void LabelAttributes_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Tell the XCTK grid to get the updated label for this 
            NotifyPropertyChanged(() => Name);
            NotifyPropertyChanged(() => AttributesDictionary);
        }
    }
}
