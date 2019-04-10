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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Geometry;

namespace ProSymbolEditor
{
    public class MilitaryOverlayDataModel
    {
        private bool _schemaExists;
        private string _databaseName;
        private string _egdbConnectionString;
        private ProSymbolUtilities.SupportedStandardsType _standard;

        public MilitaryOverlayDataModel()
        {
            Reset();

            initializeSymbolSetToFeatureClassMapping();
        }

        public void Reset()
        {
            _schemaExists = false;
            _databaseName = string.Empty;
            _egdbConnectionString = string.Empty;
            EGDBPrefixName = string.Empty;
        }

        public string EGDBPrefixName
        {
            get; set;
        }

        Dictionary<string, bool> GetFeatureClassExistsMap(ProSymbolUtilities.SupportedStandardsType standard,
            Geodatabase gdb = null)
        {
            EGDBPrefixName = string.Empty;
            _egdbConnectionString = string.Empty;

            if (gdb != null)
            {
                GeodatabaseType gdbType = gdb.GetGeodatabaseType();
                if (gdbType == GeodatabaseType.RemoteDatabase)
                {
                    // if an SDE/EGDB, then feature class name format will differ:
                    // Database + User + Feature Class Name 
                    DatabaseConnectionProperties dbcps = gdb.GetConnector() as DatabaseConnectionProperties;

                    if (dbcps != null)
                    {
                        EGDBPrefixName = dbcps.Database + "." + dbcps.User + ".";

                        // Also save this connection string to identify this EGDB later 
                        _egdbConnectionString = ((Datastore)gdb).GetConnectionString();
                    }
                }
            }

            Dictionary<string, bool> featureClassExists = new Dictionary<string, bool>();

            List<SymbolSetMapping> symbolSetMapping = null;

            switch (standard)
            {
                case ProSymbolUtilities.SupportedStandardsType.app6d: symbolSetMapping = _symbolSetMappingAPP6D; break;
                case ProSymbolUtilities.SupportedStandardsType.mil2525c: symbolSetMapping = _symbolSetMapping2525C; break;
                case ProSymbolUtilities.SupportedStandardsType.mil2525b: symbolSetMapping = _symbolSetMapping2525B; break;
                default:
                    symbolSetMapping = _symbolSetMapping2525D;
                    break;
            }

            foreach (SymbolSetMapping mapping in symbolSetMapping)
            {
                string featureClassName = EGDBPrefixName + mapping.FeatureClassName;

                if (!featureClassExists.ContainsKey(featureClassName))
                    featureClassExists.Add(featureClassName, false);
            }

            return featureClassExists;
        }

        public bool SchemaExists
        {
            get
            {
                return _schemaExists;
            }
        }

        public string DatabaseName
        {
            get
            {
                return _databaseName;
            }
        }

        private async Task<Geodatabase> GetGDBFromLayer(BasicFeatureLayer layer)
        {
            if (layer == null)
                return null;

            Geodatabase geodatabase = null;
            await QueuedTask.Run(() => geodatabase = (layer.GetTable().GetDatastore() as Geodatabase));
            return geodatabase;
        }

        public string GetDatasetName()
        {
            return EGDBPrefixName + ProSymbolUtilities.GetDatasetName(ProSymbolUtilities.Standard);
        }

        public async Task<bool> IsGDBAndFeatureClassInActiveView(string featureClassName)
        {
            string activeGdbPath = DatabaseName;

            IEnumerable<FeatureLayer> mapLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>();

            bool isFeatureClassInActiveView = false;

            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
            {
                foreach (var layer in mapLayers)
                {
                    string fcName = layer.GetFeatureClass().GetName();

                    isFeatureClassInActiveView = false;

                    // GDB and View feature class name match?
                    if (fcName == featureClassName)
                    {
                        Geodatabase geodatabase = await GetGDBFromLayer(layer);
                        if (geodatabase == null)
                        {
                            isFeatureClassInActiveView = false;
                        }
                        else
                        {
                            string gdbPath = ProSymbolUtilities.GetPathFromGeodatabase(geodatabase);

                            if (gdbPath == activeGdbPath)
                            {
                                isFeatureClassInActiveView = true;
                                break;
                            }

                            // last check if it is EGDB
                            if (string.IsNullOrEmpty(gdbPath) && !string.IsNullOrEmpty(_egdbConnectionString)
                                && (geodatabase != null))
                            {
                                string cs = ((Datastore)geodatabase).GetConnectionString();

                                // TODO: find a full-proof method to see if this layer 
                                // is the same data source 
                                // Note: different threads may have different connection strings 
                                // so this comparison is not a full-proof test
                                // if (cs == _egdbConnectionString)
                                //    isFeatureClassInActiveView = true;
                                // For now just assume the EGDB is the correct one 
                                isFeatureClassInActiveView = true;
                            }
                        }
                        break;
                    }
                }

                return isFeatureClassInActiveView;
            });

            return isFeatureClassInActiveView;
        }

        public async Task<bool> AddFeatureClassToActiveView(string featureClassName)
        {
            if (string.IsNullOrEmpty(featureClassName) || string.IsNullOrEmpty(DatabaseName))
                return await Task.FromResult<bool>(false);

            bool layerAdded = false;

            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                string uri = $@"{DatabaseName}\{GetDatasetName()}\{featureClassName}";
                Item currentItem = ItemFactory.Instance.Create(uri);
                if (LayerFactory.Instance.CanCreateLayerFrom(currentItem))
                {
                    FeatureLayer fl = LayerFactory.Instance.CreateLayer(currentItem, 
                        MapView.Active.Map) as FeatureLayer;

                    if (fl != null)
                    {
                        ArcGIS.Core.CIM.CIMDictionaryRenderer dictionaryRenderer =
                            ProSymbolUtilities.CreateDictionaryRenderer();
                        fl.SetRenderer(dictionaryRenderer);

                        layerAdded = true;
                    }
                }
            });

            return await Task.FromResult<bool>(layerAdded);
        }

        public bool IsMilitaryOverlayInActiveMap()
        {
            // See if Active Map
            if (MapView.Active == null)
                return false; //No active map

            // See if Military Overlay in Active Map
            const string militaryOverlayName = "Military Overlay";
            IEnumerable<GroupLayer> mapLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<GroupLayer>().Where(l => l.Name.StartsWith(militaryOverlayName));
            if ((mapLayers == null) || (mapLayers.Count() == 0))
            {
                return false;
            }

            return true;
        }

        public async Task<bool> IsMapActiveAndAddInEnabledAsync()
        {
            if (!IsMilitaryOverlayInActiveMap())
                return await Task.FromResult<bool>(false);

            return await ShouldAddInBeEnabledAsync(ProSymbolUtilities.Standard);
        }

        public async Task<bool> ShouldAddInBeEnabledAsync()
        {
            return await ShouldAddInBeEnabledAsync(ProSymbolUtilities.Standard);
        }

        public async Task<bool> GDBContainsMilitaryOverlay(GDBProjectItem gdbProjectItem, 
            ProSymbolUtilities.SupportedStandardsType standard)
        {
            if (gdbProjectItem == null)
                return false;

            string militaryOverlayFeatureDatasetName = 
                ProSymbolUtilities.GetDatasetName(standard);

            bool gdbContainsMilitaryOverlay = await
                ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run<bool>(() =>
                {
                    using (Datastore datastore = gdbProjectItem.GetDatastore())
                    {
                        // Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                        if (datastore is UnknownDatastore)
                            return false;

                        Geodatabase geodatabase = datastore as Geodatabase;
                        if (geodatabase == null)
                            return false;

                        var defs = geodatabase.GetDefinitions<FeatureDatasetDefinition>().Where(fd => fd.GetName().Contains(militaryOverlayFeatureDatasetName)).ToList(); ;

                        return (defs.Count > 0);
                    }
                });

            return gdbContainsMilitaryOverlay;
        }

        public async Task<bool> GDBContainsMilitaryOverlay(GDBProjectItem gdbProjectItem)
        {
            if (gdbProjectItem == null)
                return false;

            string militaryOverlayFeatureDatasetName = "militaryoverlay";

            bool gdbContainsMilitaryOverlay = await
                ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run<bool>(() => 
            {
                using (Datastore datastore = gdbProjectItem.GetDatastore())
                {
                    // Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                    if (datastore is UnknownDatastore)
                        return false;

                    Geodatabase geodatabase = datastore as Geodatabase;
                    if (geodatabase == null)
                        return false;

                    var defs = geodatabase.GetDefinitions<FeatureDatasetDefinition>().Where(fd => fd.GetName().Contains(militaryOverlayFeatureDatasetName)).ToList(); ;

                    return (defs.Count > 0);
                }
            });

            return gdbContainsMilitaryOverlay;
        }

        // TODO: we may be able to deprecate this method (GDBContainsSchema) and use the method above (GDBContainsMilitaryOverlay)
        public async Task<bool> GDBContainsSchema(GDBProjectItem gdbProjectItem, 
            ProSymbolUtilities.SupportedStandardsType standard)
        {
            bool isSchemaComplete = await 
                ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run<bool>(() =>
            {
                if (gdbProjectItem == null)
                    return false;

                using (Datastore datastore = gdbProjectItem.GetDatastore())
                {
                    // Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                    if (datastore is UnknownDatastore)
                        return false; 

                    Geodatabase geodatabase = datastore as Geodatabase;
                    if (geodatabase == null)
                        return false;

                    // Set up Fields to check
                    List<string> fieldsToCheck = new List<string>();

                    if (ProSymbolUtilities.IsLegacyStandard(standard))
                    {
                        fieldsToCheck.Add("extendedfunctioncode");
                    }
                    else
                    {   // 2525d / app6d
                        fieldsToCheck.Add("symbolset");
                        fieldsToCheck.Add("symbolentity");
                    }

                    // Reset schema data model exists to false for each feature class
                    Dictionary<string, bool> featureClassExists = GetFeatureClassExistsMap(standard, geodatabase);

                    IReadOnlyList<FeatureClassDefinition> featureClassDefinitions = geodatabase.GetDefinitions<FeatureClassDefinition>();

                    bool stopLooking = false;
                    foreach (FeatureClassDefinition featureClassDefinition in featureClassDefinitions)
                    {
                        // Stop looking after the first feature class not found
                        if (stopLooking)
                        {
                            return false;
                        }

                        string featureClassName = featureClassDefinition.GetName();

                        if (featureClassExists.ContainsKey(featureClassName))
                        {
                            // Feature Class Exists. Check for fields
                            bool fieldsExist = true;

                            // Don't do this for remote databases (too slow)
                            if (geodatabase.GetGeodatabaseType() != GeodatabaseType.RemoteDatabase)
                            {
                                foreach (string fieldName in fieldsToCheck)
                                {
                                    IEnumerable<Field> foundFields = featureClassDefinition.GetFields().Where(x => x.Name == fieldName);

                                    if (foundFields.Count() < 1)
                                    {
                                        fieldsExist = false;
                                        return false;
                                    }
                                }
                            }

                            featureClassExists[featureClassName] = fieldsExist;
                        }
                        else
                        {
                            // Key doesn't exist, so ignore
                        }
                    }

                    foreach (KeyValuePair<string, bool> pair in featureClassExists)
                    {
                        if (pair.Value == false)
                        {
                            return false;
                        }
                    }

                    // If here, schema is complete
                    // Save geodatabase path to use as the selected database
                    _databaseName = gdbProjectItem.Path;
                    _schemaExists = true;
                    _standard = standard;

                    return true;
                }
            });

            return isSchemaComplete;
        }

        public async Task<bool> ShouldAddInBeEnabledAsync(string gdbPath, ProSymbolUtilities.SupportedStandardsType standard)
        {
            bool gdbEnabledWithStanadard = false;

            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
            {
                var currentItem = ItemFactory.Instance.Create(gdbPath);

                if (currentItem is GDBProjectItem)
                    gdbEnabledWithStanadard = await GDBContainsSchema(currentItem as GDBProjectItem, standard);
            });

            return gdbEnabledWithStanadard;
        }

        public async Task<bool> ShouldAddInBeEnabledAsync(ProSymbolUtilities.SupportedStandardsType standard)
        {
            if (_schemaExists && (_standard == standard))
                return true;

            _schemaExists = false;

            //If we can get the database, then enable the add-in
            if (Project.Current == null)
            {
                //No open project
                return false;
            }

            //Get database with correct schema
            try
            {
                IEnumerable<GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();

                foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                {
                    if (gdbProjectItem.Name == "map.gdb") // ignore the project Map GDB
                        continue;

                    bool isSchemaComplete = await GDBContainsMilitaryOverlay(gdbProjectItem, standard);

                    // if schema is there/complete then done
                    if (isSchemaComplete)
                    {
                        // If we get here, then schema is found for this standard
                        // Save geodatabase path to use as the selected database
                        _databaseName = gdbProjectItem.Path;
                        _schemaExists = true;
                        _standard = standard;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLine(exception.Message);
            }

            return SchemaExists;
        }

        public static List<SymbolSetMapping> SymbolSetToFeatureClassMapping2525D
        {
            get
            {
                if (_symbolSetMapping2525D == null)
                    initializeSymbolSetToFeatureClassMapping();

                return _symbolSetMapping2525D;
            }
        }
        private static List<SymbolSetMapping> _symbolSetMapping2525D = null;

        public static List<SymbolSetMapping> SymbolSetToFeatureClassMappingAPP6D
        {
            get
            {
                if (_symbolSetMappingAPP6D == null)
                    initializeSymbolSetToFeatureClassMapping();

                return _symbolSetMappingAPP6D;
            }
        }
        private static List<SymbolSetMapping> _symbolSetMappingAPP6D = null;

        public static List<SymbolSetMapping> SymbolSetToFeatureClassMapping2525C
        {
            get
            {
                if (_symbolSetMapping2525C == null)
                    initializeSymbolSetToFeatureClassMapping();

                return _symbolSetMapping2525C;
            }
        }
        private static List<SymbolSetMapping> _symbolSetMapping2525C = null;

        public static List<SymbolSetMapping> SymbolSetToFeatureClassMapping2525B
        {
            get
            {
                if (_symbolSetMapping2525B == null)
                    initializeSymbolSetToFeatureClassMapping();

                return _symbolSetMapping2525B;
            }
        }
        private static List<SymbolSetMapping> _symbolSetMapping2525B = null;

        private static void initializeSymbolSetToFeatureClassMapping()
        {
            // IMPORTANT: this is the single list of 
            // 1. What feature classes are part of the required schema
            // 2. What symbols map to those feature classes based on feature geometry and 
            //    other identifying attribute (symbol set for 2525D, SIDC for 2525C)

            // 2525D 
            _symbolSetMapping2525D = new List<SymbolSetMapping>();
            _symbolSetMapping2525D.Add(new SymbolSetMapping("Air", GeometryType.Point, "01"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("AirMissile", GeometryType.Point, "02"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("Space", GeometryType.Point, "05"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("SpaceMissile", GeometryType.Point, "06"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("Units", GeometryType.Point, "10"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("Civilian", GeometryType.Point, "11"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("LandEquipment", GeometryType.Point, "15"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("Installations", GeometryType.Point, "20"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("ControlMeasuresPoints", GeometryType.Point, "25"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("ControlMeasuresLines", GeometryType.Polyline, "25"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("ControlMeasuresAreas", GeometryType.Polygon, "25"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("SeaSurface", GeometryType.Point, "30"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("SeaSubsurface", GeometryType.Point, "35"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("MineWarfare", GeometryType.Point, "36"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("Activities", GeometryType.Point, "40"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("METOCPointsAtmospheric", GeometryType.Point, "45"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("METOCLinesAtmospheric", GeometryType.Polyline, "45"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("METOCAreasAtmospheric", GeometryType.Polygon, "45"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("METOCPointsOceanographic", GeometryType.Point, "46"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("METOCLinesOceanographic", GeometryType.Polyline, "46"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("METOCAreasOceanographic", GeometryType.Polygon, "46"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "50"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "51"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "52"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "53"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "54"));
            _symbolSetMapping2525D.Add(new SymbolSetMapping("Cyberspace", GeometryType.Point, "60"));

            // APP6D
            _symbolSetMappingAPP6D = new List<SymbolSetMapping>();
            // Adds this mapping
            _symbolSetMappingAPP6D.Add(new SymbolSetMapping("Dismounted", GeometryType.Point, "27"));
            // Copy the remaining mappings from 2525D
            foreach (SymbolSetMapping mapping in _symbolSetMapping2525D)
            {
                _symbolSetMappingAPP6D.Add(mapping);
            }

            // 2525B
            _symbolSetMapping2525B = new List<SymbolSetMapping>();
            _symbolSetMapping2525B.Add(new SymbolSetMapping("Air", GeometryType.Point, "^[S].[A].{7,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("Space", GeometryType.Point, "^[S].[P].{7,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("LandEquipment", GeometryType.Point, "^[S].[G].[E].{5,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("Installations", GeometryType.Point, "^[S].[G].[I].{5,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("Units", GeometryType.Point, "^[S].[GF].{7,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("ControlMeasuresPoints", GeometryType.Point, "^[G].{9,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("ControlMeasuresLines", GeometryType.Polyline, "^[G].{9,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("ControlMeasuresAreas", GeometryType.Polygon, "^[G].{9,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("SeaSurface", GeometryType.Point, "^[S].[S].{7,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("SeaSubsurface", GeometryType.Point, "^[S].[U].{7,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("Activities", GeometryType.Point, "^[OE].{9,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("METOCPoints", GeometryType.Point, "^[W].{9,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("METOCLines", GeometryType.Polyline, "^[W].{9,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("METOCAreas", GeometryType.Polygon, "^[W].{9,}"));
            _symbolSetMapping2525B.Add(new SymbolSetMapping("SIGINT", GeometryType.Point, "^[I].{9,}"));

            // 2525C
            _symbolSetMapping2525C = new List<SymbolSetMapping>();
            // Copy the mappings from 2525B
            foreach (SymbolSetMapping mapping in _symbolSetMapping2525B)
            {
                _symbolSetMapping2525C.Add(mapping);
            }
        }
    }
}
