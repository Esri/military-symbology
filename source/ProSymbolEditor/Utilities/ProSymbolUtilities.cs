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
        public enum SupportedStandardsType { mil2525d, mil2525c_b2 };

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
            if (standardIn == SupportedStandardsType.mil2525d)
                return "MIL-STD-2525D";
            else
                return "MIL-STD-2525B w/ Change 2";
        }

        public static SupportedStandardsType GetStandardFromLabel(string standardString)
        {
            if (standardString == GetStandardLabel(SupportedStandardsType.mil2525c_b2))
                return SupportedStandardsType.mil2525c_b2;
            else
                return SupportedStandardsType.mil2525d;
        }


        public static string GetShortStandardLabel(SupportedStandardsType standardIn)
        {           
            if (standardIn == SupportedStandardsType.mil2525d)
                return "2525D";
            else
                return "2525B";
        }

        public static string GetDatasetName(SupportedStandardsType standardIn)
        {
            if (standardIn == SupportedStandardsType.mil2525d)
                return "militaryoverlay2525d";
            else
                return "militaryoverlay2525b2";
        }

        public static string GetStandardString(SupportedStandardsType standardIn)
        {
            if (standardIn == SupportedStandardsType.mil2525c_b2)
                return "2525C_B2";
            else
                return "2525D";
        }

        public static string GetDictionaryString()
        {
            return "mil" + StandardString.ToLower();
        }

        public static string NameSeparator
        {
            get { return " : "; }
        }

        public static string ProVersion
        {
            get { return proVersion; }
        }
        private static string proVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

        public static int ProMajorVersion
        {
            get { return proMajorVersion; }
        }
        private static int proMajorVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Major;

        public static int ProMinorVersion
        {
            get { return proMinorVersion; }
        }
        private static int proMinorVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Minor;

        public static string AddinAssemblyLocation()
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            return System.IO.Path.GetDirectoryName(
                              Uri.UnescapeDataString(
                                      new Uri(asm.CodeBase).LocalPath));
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
                    System.Diagnostics.Trace.WriteLine("Warning - unable to execute command: " + commandId);
                }
            });

            return success;
        }

        public static void SaveProject()
        {
            // Note: Must be called on Main/UI Thread
            ArcGIS.Desktop.Framework.FrameworkApplication.Current.Dispatcher.Invoke(async() =>
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

            if (Standard == SupportedStandardsType.mil2525c_b2)
                fieldMapDictionary = fieldMap2525C_B2;
            else
                fieldMapDictionary = fieldMap2525D;

            ArcGIS.Core.CIM.CIMStringMap[] stringMap = new ArcGIS.Core.CIM.CIMStringMap[fieldMapDictionary.Count()];

            int count = 0;
            foreach (KeyValuePair<string, string> pair in fieldMapDictionary)
            {
                stringMap[count] = new ArcGIS.Core.CIM.CIMStringMap();
                stringMap[count].Key   = pair.Key;
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
                    Title = "Select Geodatabase",
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
       
    }
}
