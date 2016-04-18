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
using System.Drawing;
using System.Windows.Media.Imaging;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using MilitarySymbols;
using System.Web.Script.Serialization;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace ProSymbolEditor
{
    public class SymbolAttributeSet : PropertyChangedBase
    {
        private string _symbolSet;
        private string _symbolEntity;
        private BitmapImage _symbolImage = null;

        public SymbolAttributeSet()
        {
            DisplayAttributes = new DisplayAttributes();
            DisplayAttributes.PropertyChanged += Attributes_PropertyChanged;

            LabelAttributes = new LabelAttributes();
        }

        #region Getters/Setters

        [ExpandableObject]
        public DisplayAttributes DisplayAttributes { get; set; }

        [ExpandableObject]
        public LabelAttributes LabelAttributes { get; set; }

        public string FavoriteId { get; set; }

        public string StandardVersion { get; set; }

        public string SymbolTags { get; set; }

        public string SymbolSet
        {
            get
            {
                return _symbolSet;
            }
            set
            {
                _symbolSet = value;
                //GenerateSelectedSymbolTagsString();
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
                //GenerateSelectedSymbolTagsString();
                GeneratePreviewSymbol();
                NotifyPropertyChanged(() => SymbolEntity);
            }
        }

        [ScriptIgnore]
        public BitmapImage SymbolImage
        {
            get
            {
                return _symbolImage;
            }
        }

        #endregion

        public void GeneratePreviewSymbol()
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

            if (!string.IsNullOrEmpty(DisplayAttributes.Identity))
            {
                attributeSet["identity"] = DisplayAttributes.Identity;
            }

            if (!string.IsNullOrEmpty(_symbolSet))
            {
                attributeSet["symbolset"] = _symbolSet;
            }

            if (!string.IsNullOrEmpty(_symbolEntity))
            {
                attributeSet["symbolentity"] = _symbolEntity;
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

            return attributeSet;
        }

        public void PopulateRowBufferWithAttributes(ref RowBuffer rowBuffer)
        {
            if (!string.IsNullOrEmpty(DisplayAttributes.Identity))
            {
                rowBuffer["identity"] = DisplayAttributes.Identity;
            }

            if (!string.IsNullOrEmpty(_symbolSet))
            {
                rowBuffer["symbolset"] = Convert.ToInt32(_symbolSet);
            }

            if (!string.IsNullOrEmpty(_symbolEntity))
            {
                rowBuffer["symbolentity"] = Convert.ToInt32(_symbolEntity);
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

            if (!string.IsNullOrEmpty(DisplayAttributes.Status))
            {
                rowBuffer["status"] = DisplayAttributes.Status;
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
        }
        
        public void ResetAttributes()
        {
            //Reset attributes
            SymbolSet = "";
            SymbolEntity = "";
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
            LabelAttributes.DateTimeValid = DateTime.Now;
            LabelAttributes.DateTimeExpired = DateTime.Now;
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

            SymbolTags = "";
        }

        private void Attributes_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GeneratePreviewSymbol();
        }
    }
}
