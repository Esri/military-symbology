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
using System.Web.Script.Serialization;
using System.ComponentModel;

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;

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

            resettingAttributes = true;
            SetPropertiesFromFieldAttributes(fieldValues);
            resettingAttributes = false;
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

            if ((compareObj == null) || (DisplayAttributes == null) || (LabelAttributes == null))
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
                    if (string.IsNullOrEmpty(valueAsString))
                        continue;

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
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("Max"))
                        continue;

                    object value = prop.GetValue(LabelAttributes, null);
                    if (value == null)
                        continue;

                    string valueAsString = value.ToString();
                    if (string.IsNullOrEmpty(valueAsString))
                        continue;

                    // Skip non-value types
                    if (valueAsString.StartsWith("ProSymbolEditor"))
                        continue;

                    // If debug needed:
                    // System.Diagnostics.Trace.WriteLine(string.Format("{0}={1}", key, valueAsString));

                    if (string.IsNullOrEmpty(valueAsString) || dict.ContainsKey(key))
                        continue;

                    dict.Add(key, valueAsString);
                }

                if (!string.IsNullOrEmpty(StandardVersion))
                    dict.Add("Standard", StandardVersion);

                return dict;
            }
        }

        #region Getters/Setters

        public DisplayAttributes DisplayAttributes { get; set; }

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

        // disable some operations when resetting 
        private bool resettingAttributes = false;

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
            if ((attributes == null) || (attributes.Count == 0))
                return null;

            return ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() => {
                try
                {
                    string standard = ProSymbolUtilities.GetDictionaryString();
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
                    System.Diagnostics.Trace.WriteLine("Exception in GetBitmapImageAsync: " + ex.Message);
                    return null;
                }
             });
        }

        public async void GeneratePreviewSymbol()
        {
            // Step 1: Create a dictionary/map of well known attribute names to values
            Dictionary<string, object> attributeSet = GenerateAttributeSetDictionary();

            // Don't create preview until we have the minimum number of attributes in
            // order to minimize flicker - minimum attributes:
            // 2525D: { symbolset, entity, affiliation }
            // 2525B: { extendedfunctioncode, affiliation }
            int minimumAttributeCount = 4;
            if (ProSymbolUtilities.IsLegacyStandard())
            {
                minimumAttributeCount = 3;
            }

            if (attributeSet.ContainsKey("IsMETOC") && (bool)attributeSet["IsMETOC"])
            {
                //////////////////////////
                // WORKAROUND: Pro 2.3 broke exporting METOC by attribute "extendedfunctioncode"
                // so have to set "sidc" attribute instead
                if (ProSymbolUtilities.IsNewStyleFormat && attributeSet.ContainsKey("extendedfunctioncode"))
                    attributeSet.Add("sidc", DisplayAttributes.LegacySymbolIdCode);

                // METOC do not have identity/affiliation so have 1 less attribute
                minimumAttributeCount--;
            }

            // Validate that image is ready to be created
            if ((attributeSet == null) || (attributeSet.Count < minimumAttributeCount))
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

            bool isMETOC = false;
            if (ProSymbolUtilities.IsLegacyStandard())
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.ExtendedFunctionCode) &&
                    (DisplayAttributes.ExtendedFunctionCode != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["extendedfunctioncode"] = DisplayAttributes.ExtendedFunctionCode;

                    if (DisplayAttributes.ExtendedFunctionCode[0] == 'W')
                        isMETOC = true;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Identity) &&
                    (DisplayAttributes.Identity != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["affiliation"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Status) &&
                    (DisplayAttributes.Status != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["status"] = DisplayAttributes.Status;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator) &&
                    (DisplayAttributes.Indicator != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["hqtffd"] = DisplayAttributes.Indicator;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon) && 
                    (DisplayAttributes.Echelon != ProSymbolUtilities.NullFieldValueFlag))
                { 
                    attributeSet["echelonmobility"] = DisplayAttributes.Echelon;
                }
            }
            else // 2525D
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.Identity) &&
                    (DisplayAttributes.Identity != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["identity"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolSet) &&
                    (DisplayAttributes.SymbolSet != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["symbolset"] = DisplayAttributes.SymbolSet;

                    if ((DisplayAttributes.SymbolSet == "45") ||
                        (DisplayAttributes.SymbolSet == "46"))
                        isMETOC = true;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolEntity) &&
                    (DisplayAttributes.SymbolEntity != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["symbolentity"] = DisplayAttributes.SymbolEntity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator) &&
                    (DisplayAttributes.Indicator != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["indicator"] = DisplayAttributes.Indicator;
                }

                //Echelon or Mobility
                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon) && 
                    (DisplayAttributes.Echelon != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["echelon"] = DisplayAttributes.Echelon;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Mobility) &&
                    (DisplayAttributes.Mobility != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["echelon"] = DisplayAttributes.Mobility;
                }

                //Statuses or Operation
                if (!string.IsNullOrEmpty(DisplayAttributes.OperationalCondition) &&
                    (DisplayAttributes.OperationalCondition != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["operationalcondition"] = DisplayAttributes.OperationalCondition;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Status) &&
                    (DisplayAttributes.Status != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["operationalcondition"] = DisplayAttributes.Status;
                }

                // 2525D attributes
                if (!string.IsNullOrEmpty(DisplayAttributes.Context) &&
                    (DisplayAttributes.Context != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["context"] = DisplayAttributes.Context;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier1) &&
                    (DisplayAttributes.Modifier1 != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["modifier1"] = DisplayAttributes.Modifier1;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier2) &&
                    (DisplayAttributes.Modifier2 != ProSymbolUtilities.NullFieldValueFlag))
                {
                    attributeSet["modifier2"] = DisplayAttributes.Modifier2;
                }
            }

            attributeSet.Add("IsMETOC", isMETOC);

            return attributeSet;
        }

        public void PopulateRowBufferWithAttributes(ref RowBuffer rowBuffer)
        {
            if (rowBuffer == null)
            {
                // not normally possible with ref parameter, but check just in case
                System.Diagnostics.Debug.WriteLine("Null RowBuffer passed to PopulateRowBufferWithAttributes");
                return;
            }

            if (ProSymbolUtilities.IsLegacyStandard())
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.ExtendedFunctionCode) &&
                    (DisplayAttributes.ExtendedFunctionCode != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["extendedfunctioncode"] = DisplayAttributes.ExtendedFunctionCode;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Identity) &&
                    (DisplayAttributes.Identity != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["affiliation"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator) &&
                    (DisplayAttributes.Indicator != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["hqtffd"] = DisplayAttributes.Indicator;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon) &&
                    (DisplayAttributes.Echelon != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["echelonmobility"] = DisplayAttributes.Echelon;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.Identity) &&
                    (DisplayAttributes.Identity != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["identity"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolSet) &&
                    (DisplayAttributes.SymbolSet != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["symbolset"] = Convert.ToInt32(DisplayAttributes.SymbolSet);
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolEntity) &&
                    (DisplayAttributes.SymbolEntity != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["symbolentity"] = Convert.ToInt32(DisplayAttributes.SymbolEntity);
                }

                //Indicator / HQTFFD /

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator) &&
                    (DisplayAttributes.Indicator != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["indicator"] = DisplayAttributes.Indicator;
                }

                //Echelon or Mobility

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon) &&
                    (DisplayAttributes.Echelon != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["echelon"] = DisplayAttributes.Echelon;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Mobility) &&
                    (DisplayAttributes.Mobility != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["mobility"] = DisplayAttributes.Mobility;
                }

                //Statuses or Operation

                if (!string.IsNullOrEmpty(DisplayAttributes.OperationalCondition) &&
                    (DisplayAttributes.OperationalCondition != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["operationalcondition"] = DisplayAttributes.OperationalCondition;
                }

                // 2525D attributes
                if (!string.IsNullOrEmpty(DisplayAttributes.Context) &&
                    (DisplayAttributes.Context != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["context"] = DisplayAttributes.Context;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier1) &&
                    (DisplayAttributes.Modifier1 != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["modifier1"] = DisplayAttributes.Modifier1;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier2) &&
                    (DisplayAttributes.Modifier2 != ProSymbolUtilities.NullFieldValueFlag))
                {
                    rowBuffer["modifier2"] = DisplayAttributes.Modifier2;
                }
            }

            if (!string.IsNullOrEmpty(DisplayAttributes.Status) &&
                (DisplayAttributes.Status != ProSymbolUtilities.NullFieldValueFlag))
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

            if (!string.IsNullOrEmpty(LabelAttributes.Reinforced) &&
                (LabelAttributes.Reinforced != ProSymbolUtilities.NullFieldValueFlag))
            {
                rowBuffer["reinforced"] = LabelAttributes.Reinforced;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.Credibility) &&
                (LabelAttributes.Credibility != ProSymbolUtilities.NullFieldValueFlag))
            {
                rowBuffer["credibility"] = LabelAttributes.Credibility;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.Reliability) &&
                (LabelAttributes.Reliability != ProSymbolUtilities.NullFieldValueFlag))
            {
                rowBuffer["reliability"] = LabelAttributes.Reliability;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.CountryCode) &&
                (LabelAttributes.CountryCode != ProSymbolUtilities.NullFieldValueFlag))
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

            // Implementation Note: only force a feature field value with domains attached 
            // to null if NullFieldValueFlag("<null>) is set on that field

            // 2525C/B attributes:
            if (ProSymbolUtilities.IsLegacyStandard())
            { 
                if (!string.IsNullOrEmpty(DisplayAttributes.ExtendedFunctionCode) && 
                    (feature.FindField("extendedfunctioncode") >= 0))
                {
                    if (DisplayAttributes.ExtendedFunctionCode == ProSymbolUtilities.NullFieldValueFlag)
                        feature["extendedfunctioncode"] = null;
                    else
                        feature["extendedfunctioncode"] = DisplayAttributes.ExtendedFunctionCode;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Identity) && 
                    (feature.FindField("affiliation") >= 0))
                {
                    if (DisplayAttributes.Identity == ProSymbolUtilities.NullFieldValueFlag)
                        feature["affiliation"] = null;
                    else
                        feature["affiliation"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator) &&
                    (feature.FindField("hqtffd") >= 0))
                {
                    if (DisplayAttributes.Indicator == ProSymbolUtilities.NullFieldValueFlag)
                        feature["hqtffd"] = null;
                    else
                        feature["hqtffd"] = DisplayAttributes.Indicator;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon) &&
                    (feature.FindField("echelonmobility") >= 0))
                {
                    if (DisplayAttributes.Echelon == ProSymbolUtilities.NullFieldValueFlag)
                        feature["echelonmobility"] = null;
                    else
                        feature["echelonmobility"] = DisplayAttributes.Echelon;
                }
            }
            else // 2525D attributes:
            {
                if (!string.IsNullOrEmpty(DisplayAttributes.Identity) && 
                    (feature.FindField("identity") >= 0))
                {
                    if (DisplayAttributes.Identity == ProSymbolUtilities.NullFieldValueFlag)
                        feature["identity"] = null;
                    else
                        feature["identity"] = DisplayAttributes.Identity;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolSet) &&
                    (feature.FindField("symbolset") >= 0))
                {
                    if (DisplayAttributes.SymbolSet == ProSymbolUtilities.NullFieldValueFlag)
                        feature["symbolset"] = null;
                    else
                        feature["symbolset"] = Convert.ToInt32(DisplayAttributes.SymbolSet);
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.SymbolEntity) &&
                    (feature.FindField("symbolentity") >= 0))
                {
                    if (DisplayAttributes.SymbolEntity == ProSymbolUtilities.NullFieldValueFlag)
                        feature["symbolentity"] = null;
                    else
                        feature["symbolentity"] = Convert.ToInt32(DisplayAttributes.SymbolEntity);
                }

                //Indicator / HQTFFD
                if (!string.IsNullOrEmpty(DisplayAttributes.Indicator) &&
                    (feature.FindField("indicator") >= 0))
                {
                    if (DisplayAttributes.Indicator == ProSymbolUtilities.NullFieldValueFlag)
                        feature["indicator"] = null;
                    else
                        feature["indicator"] = DisplayAttributes.Indicator;
                }

                //Echelon or Mobility
                if (!string.IsNullOrEmpty(DisplayAttributes.Echelon) &&
                    (feature.FindField("echelon") >= 0))
                {
                    if (DisplayAttributes.Echelon == ProSymbolUtilities.NullFieldValueFlag)
                        feature["echelon"] = null;
                    else
                        feature["echelon"] = DisplayAttributes.Echelon;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Mobility) &&
                    (feature.FindField("mobility") >= 0))
                {
                    if (DisplayAttributes.Mobility == ProSymbolUtilities.NullFieldValueFlag)
                        feature["mobility"] = null;
                    else
                        feature["mobility"] = DisplayAttributes.Mobility;
                }

                //Statuses or Operation
                if (!string.IsNullOrEmpty(DisplayAttributes.OperationalCondition) &&
                    (feature.FindField("operationalcondition") >= 0))
                {
                    if (DisplayAttributes.OperationalCondition == ProSymbolUtilities.NullFieldValueFlag)
                        feature["operationalcondition"] = null;
                    else
                        feature["operationalcondition"] = DisplayAttributes.OperationalCondition;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Context) &&
                    (feature.FindField("context") >= 0))
                {
                    if (DisplayAttributes.Context == ProSymbolUtilities.NullFieldValueFlag)
                        feature["context"] = null;
                    else
                        feature["context"] = DisplayAttributes.Context;
                }

                //Modifiers
                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier1) &&
                    (feature.FindField("modifier1") >= 0))
                {
                    if (DisplayAttributes.Modifier1 == ProSymbolUtilities.NullFieldValueFlag)
                        feature["modifier1"] = null;
                    else
                        feature["modifier1"] = DisplayAttributes.Modifier1;
                }

                if (!string.IsNullOrEmpty(DisplayAttributes.Modifier2) && 
                    (feature.FindField("modifier2") >= 0))
                {
                    if (DisplayAttributes.Modifier2 == ProSymbolUtilities.NullFieldValueFlag)
                        feature["modifier2"] = null;
                    else
                        feature["modifier2"] = DisplayAttributes.Modifier2;
                }
            }

            // Applies to all versions of the standard
            if (!string.IsNullOrEmpty(DisplayAttributes.Status) &&
               (feature.FindField("status") >= 0))
            {
                if (DisplayAttributes.Status == ProSymbolUtilities.NullFieldValueFlag)
                    feature["status"] = null;
                else
                    feature["status"] = DisplayAttributes.Status;
            }

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
            {
                if (LabelAttributes.Reinforced == ProSymbolUtilities.NullFieldValueFlag)
                    feature["reinforced"] = null;
                else
                    feature["reinforced"] = LabelAttributes.Reinforced;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.Credibility) &&
                (feature.FindField("credibility") >= 0))
            {
                if (LabelAttributes.Credibility == ProSymbolUtilities.NullFieldValueFlag)
                    feature["credibility"] = null;
                else
                    feature["credibility"] = LabelAttributes.Credibility;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.Reliability) &&
                (feature.FindField("reliability") >= 0))
            {
                if (LabelAttributes.Reliability == ProSymbolUtilities.NullFieldValueFlag)
                    feature["reliability"] = null;
                else
                    feature["reliability"] = LabelAttributes.Reliability;
            }

            if (!string.IsNullOrEmpty(LabelAttributes.CountryCode) &&
                (feature.FindField("countrycode") >= 0))
            {
                if (LabelAttributes.CountryCode == ProSymbolUtilities.NullFieldValueFlag)
                    feature["countrycode"] = null;
                else
                    feature["countrycode"] = LabelAttributes.CountryCode;
            }
        }

        private void SetPropertiesFromFieldAttributes(Dictionary<string, string> fieldValues)
        {
            if (fieldValues == null)
                return;

            if (ProSymbolUtilities.IsLegacyStandard())
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

            // TRICKY: turn off the UI validation for max length, so this exception is not thrown
            LabelAttributes.MaxLengthValidationOn = false;

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

            // TRICKY: turn UI validation back on
            LabelAttributes.MaxLengthValidationOn = true;
        }

        public void ResetAttributes()
        {
            //Reset attributes
            _symbolImage = null;

            resettingAttributes = true;

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

            resettingAttributes = false;

            NotifyPropertyChanged(() => IsValid);
        }

        private void Attributes_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // disable this when everything being reset
            if (resettingAttributes)
                return;

            GeneratePreviewSymbol();

            // Tell the proprties datagrids to get the updated info
            NotifyPropertyChanged(() => Name);
            NotifyPropertyChanged(() => AttributesDictionary);
        }

        private void LabelAttributes_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Tell the proprties datagrids to get the updated info
            NotifyPropertyChanged(() => Name);
            NotifyPropertyChanged(() => AttributesDictionary);
        }
    }
}
