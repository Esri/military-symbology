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
                return GetStandardLabel(Standard);
            }
        }

        public static string GetStandardLabel(SupportedStandardsType standardIn)
        {           
            if (standardIn == SupportedStandardsType.mil2525d)
                return "2525D";
            else
                return "2525B";
        }

        public static string GetStandardString(SupportedStandardsType standardIn)
        {
            if (standardIn == SupportedStandardsType.mil2525c_b2)
                return "2525C_B2";
            else
                return "2525D";
        }

        public static string NameSeparator
        {
            get { return " : "; }
        }

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

        public static bool ClearMapSelection()
        {
            bool success = false;

            // Note: Must be called on UI Thread
            ArcGIS.Desktop.Framework.FrameworkApplication.Current.Dispatcher.Invoke(() =>
            {
                // Clear the feature selection using the built-in Pro button/command
                ArcGIS.Desktop.Framework.IPlugInWrapper wrapper = 
                    ArcGIS.Desktop.Framework.FrameworkApplication.GetPlugInWrapper("esri_mapping_clearSelectionButton");
                var command = wrapper as System.Windows.Input.ICommand;
                if ((command != null) && command.CanExecute(null))
                {
                    command.Execute(null);
                    success = true;
                }
            });

            return success;
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

    }
}
