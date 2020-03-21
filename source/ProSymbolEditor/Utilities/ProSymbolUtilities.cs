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
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using CoordinateConversionLibrary.Models;
using ArcGIS.Desktop.Mapping;
using System.Threading.Tasks;
using System.Collections.Generic; 
using System.Linq;

namespace ProSymbolEditor
{
    public class ProSymbolUtilities
    {
        public enum SupportedStandardsType : int
        {
            [CoordinateConversionLibrary.LocalizableDescription(@"Enummil2525d", typeof(Properties.Resources))]
            mil2525d = 1,

            [CoordinateConversionLibrary.LocalizableDescription(@"Enummil2525c", typeof(Properties.Resources))]
            mil2525c = 2,

            [CoordinateConversionLibrary.LocalizableDescription(@"Enummil2525b", typeof(Properties.Resources))]
            mil2525b = 3,

            [CoordinateConversionLibrary.LocalizableDescription(@"Enumapp6d", typeof(Properties.Resources))]
            app6d = 4,

            [CoordinateConversionLibrary.LocalizableDescription(@"Enumapp6b", typeof(Properties.Resources))]
            app6b = 5,

        };

        public static SupportedStandardsType Standard
        {
            get;
            set;
        }

        public static string StandardString
        {
            get
            {
                return GetStandardString(Standard);
            }
        }

        public static string StandardLabel
        {
            get
            {
                return GetShortStandardLabel(Standard);
            }
        }

        public static string GetStandardLabel(SupportedStandardsType standardIn)
        {
            string standardLabel = Properties.Resources.PSymmil2525d; // default

            switch (standardIn)
            {
                case SupportedStandardsType.app6b: standardLabel = Properties.Resources.PSymapp6b; break;
                case SupportedStandardsType.app6d: standardLabel = Properties.Resources.PSymapp6d; break;
                case SupportedStandardsType.mil2525c: standardLabel = Properties.Resources.PSymmil2525c; break;
                case SupportedStandardsType.mil2525b: standardLabel = Properties.Resources.PSymmil2525b; break;
                default: break;
            }

            return standardLabel;
        }

        public static string GetDictionaryString()
        {
            return GetDictionaryString(ProSymbolUtilities.Standard);
        }

        public static string GetDictionaryString(SupportedStandardsType standardIn)
        {
            string dictionaryString = Properties.Resources.PSymDicDef; // default

            string dictionaryStringMil2525b = Properties.Resources.PSymDicOth;
            string dictionaryStringMil2525c = Properties.Resources.PSymDicOth;

            if ((ProSymbolUtilities.ProMajorVersion >= 2) && (ProSymbolUtilities.ProMinorVersion >= 4))
            {
                // These were split into separate styles after 2.4
                dictionaryStringMil2525b = Properties.Resources.PSymDic2525b;
                dictionaryStringMil2525c = Properties.Resources.PSymDic2525c;
            }

            switch (standardIn)
            {
                case SupportedStandardsType.app6b: dictionaryString = Properties.Resources.PSymDicapp6b; break;
                case SupportedStandardsType.app6d: dictionaryString = Properties.Resources.PSymDicapp6d; break;
                case SupportedStandardsType.mil2525c: dictionaryString = dictionaryStringMil2525c; break;
                case SupportedStandardsType.mil2525b: dictionaryString = dictionaryStringMil2525b; break;
                default: break;
            }

            return dictionaryString;
        }

        public static SupportedStandardsType GetStandardFromLabel(string standardString)
        {
            if (standardString == GetStandardLabel(SupportedStandardsType.mil2525d))
                return SupportedStandardsType.mil2525d;
            else
                if (standardString == GetStandardLabel(SupportedStandardsType.app6d))
                return SupportedStandardsType.app6d;
            else
                if (standardString == GetStandardLabel(SupportedStandardsType.mil2525c))
                return SupportedStandardsType.mil2525c;
            else
                if (standardString == GetStandardLabel(SupportedStandardsType.mil2525b))
                return SupportedStandardsType.mil2525b;
            else
                if (standardString == GetStandardLabel(SupportedStandardsType.app6b))
                return SupportedStandardsType.app6b;
            else
            {
                System.Diagnostics.Trace.WriteLine(Properties.Resources.PSymStFromLblMsg + standardString);
                return SupportedStandardsType.mil2525d;
            }
        }

        public static string GetShortStandardLabel(SupportedStandardsType standardIn)
        {
            string standardLabel = Properties.Resources.PSymStLblDef;

            switch (standardIn)
            {
                case SupportedStandardsType.app6b: standardLabel = Properties.Resources.PSymStLblapp6b; break;
                case SupportedStandardsType.app6d: standardLabel = Properties.Resources.PSymStLblapp6d; break;
                case SupportedStandardsType.mil2525c: standardLabel = Properties.Resources.PSymStLblmil2525c; break;
                case SupportedStandardsType.mil2525b: standardLabel = Properties.Resources.PSymStLblmil2525b; break;
                default: break;
            }

            return standardLabel;
        }

        public static string GetDatasetName(SupportedStandardsType standardIn)
        {
            string datasetName = Properties.Resources.PSymDtsNameDef;

            switch (standardIn)
            {
                case SupportedStandardsType.app6b: datasetName = Properties.Resources.PSymDtsNameapp6b; break;
                case SupportedStandardsType.app6d: datasetName = Properties.Resources.PSymDtsNameapp6d; break;
                case SupportedStandardsType.mil2525c: datasetName = Properties.Resources.PSymDtsNamemil2525c; break;
                case SupportedStandardsType.mil2525b: datasetName = Properties.Resources.PSymDtsNamemil2525b; break;
                default: break;
            }

            return datasetName;
        }

        public static string GetStandardString(SupportedStandardsType standardIn)
        {
            string standardName = Properties.Resources.PSymStNameDef;

            switch (standardIn)
            {
                case SupportedStandardsType.app6b: standardName = Properties.Resources.PSymStNameapp6b; break;
                case SupportedStandardsType.app6d: standardName = Properties.Resources.PSymStNameapp6d; break;
                case SupportedStandardsType.mil2525c: standardName = Properties.Resources.PSymStNamemil2525c; break;
                case SupportedStandardsType.mil2525b: standardName = Properties.Resources.PSymStNamemil2525b; break;
                default: break;
            }

            return standardName;
        }

        public static bool IsLegacyStandard()
        {
            return IsLegacyStandard(Standard);
        }

        public static bool IsLegacyStandard(SupportedStandardsType standardIn)
        {
            if ((standardIn == SupportedStandardsType.mil2525b) ||
                (standardIn == SupportedStandardsType.mil2525c) ||
                (standardIn == SupportedStandardsType.app6b))
                return true;
            else
                return false;
        }

        public static string GetLayerFileFromCurrentStandard()
        {
            string layerFileStandard = "2525BChange2"; // this name format is slightly different
            if (ProSymbolUtilities.Standard != SupportedStandardsType.mil2525b)
                layerFileStandard = ProSymbolUtilities.StandardString;

            string layerFileName = "MilitaryOverlay-" + layerFileStandard + ".lpkx";

            string layerFilePath = System.IO.Path.Combine(ProSymbolUtilities.AddinAssemblyLocation(),
                "LayerFiles", layerFileName);

            return layerFilePath;
        }

        public static string NameSeparator
        {
            get { return " : "; }
        }

        public static string ProVersion
        {
            get
            {
                if (string.IsNullOrEmpty(proVersion))
                    proVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

                return proVersion;
            }
        }
        private static string proVersion = string.Empty;

        public static int ProMajorVersion
        {
            get
            {
                if (proMajorVersion < 0)
                    proMajorVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Major;

                return proMajorVersion;
            }
        }
        private static int proMajorVersion = -1;

        public static int ProMinorVersion
        {
            get
            {
                if (proMinorVersion < 0)
                    proMinorVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Minor;

                return proMinorVersion;
            }
        }
        private static int proMinorVersion = -1;

        public static string UserSettingsLocation()
        {
            // Use local user settings Pro folder 
            string settingsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESRI", "ArcGISPro");

            // This should not happen, but use MyDocuments as backup just in case there is some odd roaming case 
            if (!System.IO.Directory.Exists(settingsPath))
                settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            return settingsPath;
        }

        public static string AddinAssemblyLocation()
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            return System.IO.Path.GetDirectoryName(
                              Uri.UnescapeDataString(
                                      new Uri(asm.CodeBase).LocalPath));
        }

        public static bool IsNewStyleFormat
        {
            get
            {
                bool newStyleFormat = ((ProSymbolUtilities.ProMajorVersion >= 2) && (ProSymbolUtilities.ProMinorVersion >= 3));

                return newStyleFormat;
            }
        }

        public static string NullFieldValueFlag
        {
            get { return "<null>"; }
        }

        public static CoordinateType GetCoordinateType(string input, out MapPoint point)
        {
            point = null;

            // DD
            CoordinateDD dd;
            if (CoordinateDD.TryParse(input, out dd))
            {
                point = QueuedTask.Run(() =>
                {
                    ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                    return MapPointBuilder.CreateMapPoint(dd.Lon, dd.Lat, 0, sptlRef);
                }).Result;
                return CoordinateType.DD;
            }

            // DDM
            CoordinateDDM ddm;
            if (CoordinateDDM.TryParse(input, out ddm))
            {
                dd = new CoordinateDD(ddm);
                point = QueuedTask.Run(() =>
                {
                    ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                    return MapPointBuilder.CreateMapPoint(dd.Lon, dd.Lat, 0, sptlRef);
                }).Result;
                return CoordinateType.DDM;
            }
            // DMS
            CoordinateDMS dms;
            if (CoordinateDMS.TryParse(input, out dms))
            {
                dd = new CoordinateDD(dms);
                point = QueuedTask.Run(() =>
                {
                    ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                    return MapPointBuilder.CreateMapPoint(dd.Lon, dd.Lat, 0, sptlRef);
                }).Result;
                return CoordinateType.DMS;
            }

            CoordinateGARS gars;
            if (CoordinateGARS.TryParse(input, out gars))
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                        var tmp = MapPointBuilder.FromGeoCoordinateString(gars.ToString("", new CoordinateGARSFormatter()), sptlRef, GeoCoordinateType.GARS, FromGeoCoordinateMode.Default);
                        tmp = MapPointBuilder.CreateMapPoint(tmp.X, tmp.Y, 0, sptlRef);
                        return tmp;
                    }).Result;

                    return CoordinateType.GARS;
                }
                catch { }
            }

            CoordinateMGRS mgrs;
            if (CoordinateMGRS.TryParse(input, out mgrs))
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                        var tmp = MapPointBuilder.FromGeoCoordinateString(mgrs.ToString("", new CoordinateMGRSFormatter()), sptlRef, GeoCoordinateType.MGRS, FromGeoCoordinateMode.Default);
                        tmp = MapPointBuilder.CreateMapPoint(tmp.X, tmp.Y, 0, sptlRef);
                        return tmp;
                    }).Result;

                    return CoordinateType.MGRS;
                }
                catch { }
            }

            CoordinateUSNG usng;
            if (CoordinateUSNG.TryParse(input, out usng))
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                        var tmp = MapPointBuilder.FromGeoCoordinateString(usng.ToString("", new CoordinateMGRSFormatter()), sptlRef, GeoCoordinateType.USNG, FromGeoCoordinateMode.Default);
                        tmp = MapPointBuilder.CreateMapPoint(tmp.X, tmp.Y, 0, sptlRef);
                        return tmp;
                    }).Result;

                    return CoordinateType.USNG;
                }
                catch { }
            }

            CoordinateUTM utm;
            if (CoordinateUTM.TryParse(input, out utm))
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                        var tmp = MapPointBuilder.FromGeoCoordinateString(utm.ToString("", new CoordinateUTMFormatter()), sptlRef, GeoCoordinateType.UTM, FromGeoCoordinateMode.Default);
                        tmp = MapPointBuilder.CreateMapPoint(tmp.X, tmp.Y, 0, sptlRef);
                        return tmp;
                    }).Result;

                    return CoordinateType.UTM;
                }
                catch { }
            }

            return CoordinateType.Unknown;
        }

        public static string GeometryTypeToGeometryTagString(GeometryType gt)
        {
            if (gt == GeometryType.Polygon)
                return "AREA";
            else if ((gt == GeometryType.Polyline) || (gt == GeometryType.Multipoint))
                return "LINE";
            else
                return "POINT";
        }

        public static string GetPathFromGeodatabase(ArcGIS.Core.Data.Geodatabase gdb)
        {
            string gdbPath = string.Empty;

            if (gdb == null)
                return gdbPath; // Empty

            ArcGIS.Core.Data.FileGeodatabaseConnectionPath fgdbcp = gdb.GetConnector() as
                ArcGIS.Core.Data.FileGeodatabaseConnectionPath;

            if (fgdbcp != null)
            {
                gdbPath = fgdbcp.Path.LocalPath;
            }

            return gdbPath;
        }

        public static bool ExecuteBuiltinCommand(string commandId)
        {
            bool success = false;

            // Important/Note: Must be called on UI Thread (i.e. from a button or tool)
            ArcGIS.Desktop.Framework.FrameworkApplication.Current.Dispatcher.Invoke(() =>
            {
                // Use the built-in Pro button/command
                ArcGIS.Desktop.Framework.IPlugInWrapper wrapper =
                    ArcGIS.Desktop.Framework.FrameworkApplication.GetPlugInWrapper(commandId);
                var command = wrapper as System.Windows.Input.ICommand;
                if ((command != null) && command.CanExecute(null))
                {
                    command.Execute(null);
                    success = true;
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine(Properties.Resources.PSymExcCmdWar + commandId);
                }
            });

            return success;
        }

        public static void SaveProject()
        {
            // Note: Must be called on Main/UI Thread
            ArcGIS.Desktop.Framework.FrameworkApplication.Current.Dispatcher.Invoke(async () =>
            {
                bool success = await ArcGIS.Desktop.Core.Project.Current.SaveAsync();
            });
        }

        public static bool ClearMapSelection()
        {
            // Clear the feature selection using the built-in Pro button/command
            return ExecuteBuiltinCommand("esri_mapping_clearSelectionButton");
        }

        public static string TagsToSymbolName(string tags)
        {
            string symbolName = "Unknown Symbol";

            if (string.IsNullOrEmpty(tags))
                return symbolName; // Unknown 

            // Get the geometry type off a tag on the symbol 
            // TRICKY: geometry will be tags[-3] in the tags list 
            List<string> reverseTags = tags.Split(';').ToList();
            reverseTags.Reverse();

            if (reverseTags.Count > 1)
                symbolName = reverseTags[1];

            return symbolName;
        }

        public static GeometryType TagsToGeometryType(string tags)
        {
            // Default to point 
            GeometryType geometryType = GeometryType.Point;

            if (string.IsNullOrEmpty(tags))
                return geometryType; // default

            // Get the geometry type off a tag on the symbol 
            // TRICKY: geometry will be tags[-3] in the tags list 
            List<string> reverseTags = tags.Split(';').ToList();
            reverseTags.Reverse();

            if (reverseTags.Count < 3)
                return geometryType; // Point 

            string geometryTypeTag = reverseTags[2].ToUpper();

            if (geometryTypeTag == "LINE")
            {
                geometryType = GeometryType.Polyline;
            }
            else if (geometryTypeTag == "AREA")
            {
                geometryType = GeometryType.Polygon;
            }
            else
            {
                // point, set by default above 
            }

            return geometryType;
        }

        private static Dictionary<string, string> fieldMap2525D =
            new Dictionary<string, string>()
        {
                {"identity", "identity"},
                {"symbolset", "symbolset"},
                {"symbolentity", "symbolentity"},
                {"modifier1", "modifier1"},
                {"modifier2", "modifier2"},
                {"echelon", "echelon"},
                {"indicator", "indicator"},
                {"context", "context"},
                {"mobility", "mobility"},
                {"operationalcondition", "operationalcondition"},
                {"uniquedesignation", "uniquedesignation"},
                {"higherformation", "higherformation"},
                {"additionalinformation", "additionalinformation"}
        };

        private static Dictionary<string, string> fieldMap2525C_B2 =
            new Dictionary<string, string>()
        {
                {"affiliation", "affiliation"},
                {"extendedfunctioncode", "extendedfunctioncode"},
                {"echelonmobility", "echelonmobility"},
                {"status", "status"},
                {"hqtffd", "hqtffd"},
                {"uniquedesignation", "uniquedesignation"},
                {"higherformation", "higherformation"},
                {"additionalinformation", "additionalinformation"}
        };

        public static ArcGIS.Core.CIM.CIMDictionaryRenderer CreateDictionaryRenderer()
        {
            Dictionary<string, string> fieldMapDictionary;

            if (IsLegacyStandard())
                fieldMapDictionary = fieldMap2525C_B2;
            else
                fieldMapDictionary = fieldMap2525D;

            ArcGIS.Core.CIM.CIMStringMap[] stringMap = new ArcGIS.Core.CIM.CIMStringMap[fieldMapDictionary.Count()];

            int count = 0;
            foreach (KeyValuePair<string, string> pair in fieldMapDictionary)
            {
                stringMap[count] = new ArcGIS.Core.CIM.CIMStringMap();
                stringMap[count].Key = pair.Key;
                stringMap[count].Value = pair.Value;
                count++;
            }

            ArcGIS.Core.CIM.CIMDictionaryRenderer dictionaryRenderer =
                new ArcGIS.Core.CIM.CIMDictionaryRenderer();

            dictionaryRenderer.DictionaryName = GetDictionaryString();
            dictionaryRenderer.FieldMap = stringMap;

            return dictionaryRenderer;
        }

        public static string BrowseItem(string itemFilter, string initialPath = "")
        {
            string itemPath = "";

            if (string.IsNullOrEmpty(initialPath))
                initialPath = ArcGIS.Desktop.Core.Project.Current.HomeFolderPath;

            ArcGIS.Desktop.Catalog.OpenItemDialog pathDialog =
                new ArcGIS.Desktop.Catalog.OpenItemDialog()
                {
                    Title = Properties.Resources.PSymBwItemGDB,
                    InitialLocation = initialPath,
                    MultiSelect = false,
                    Filter = itemFilter,
                };

            bool? ok = pathDialog.ShowDialog();
            if ((ok == true) && (pathDialog.Items.Count() > 0))
            {
                IEnumerable<ArcGIS.Desktop.Core.Item> selectedItems = pathDialog.Items;
                itemPath = selectedItems.First().Path;
            }

            return itemPath;
        }

        public static bool IsSIDC(string checkString)
        {
            if (string.IsNullOrEmpty(checkString))
                return false;

            bool isSIDC = false;
            bool isLegacy = IsLegacyStandard();

            if (isLegacy && checkString.Length == 15)
            {
                string checkStringUpper = checkString.ToUpper();
                const string validSIDCExpression = @"^[SGWIOE][PUAFNSHGWMDLJKO\-][PAGSUFXTMOEVLIRNZ\-][APCDXF\-][A-Z0-9\-\*]{6}[A-Z\-\*]{2}[A-Z\-\*]{2}[AECGNSX\-\*]$";
                if (System.Text.RegularExpressions.Regex.IsMatch(checkStringUpper, validSIDCExpression))
                    isSIDC = true;
            }
            else if (checkString.Length == 20)
            {
                // is it a 20 digit number? Then assume 2525D or later SIDC
                UInt64 result;
                if (UInt64.TryParse(checkString, out result))
                    isSIDC = true;
            }

            return isSIDC;
        }

        public static string GetSearchStringFromSIDC(string sidc)
        {
            string searchString = string.Empty;

            // validate length
            if (!IsSIDC(sidc))
                return searchString;

            string sidcUpper = sidc.ToUpper();

            if (IsLegacyStandard())
                searchString = sidcUpper.Substring(4, 6); // Function Code
            else
                searchString = sidcUpper.Substring(4, 2) + sidcUpper.Substring(10, 6); // Symbol Set + Entity 

            return searchString;
        }

        public static string ZeroPadLeft(string original, int requiredLength)
        {
            if (string.IsNullOrEmpty(original))
                return string.Empty.PadLeft(requiredLength, '0');

            string zeroPaddedString;
            if (original.Length < requiredLength)
                zeroPaddedString = original.PadLeft(requiredLength, '0');
            else
                zeroPaddedString = original;

            return zeroPaddedString;
        }

    }
}
