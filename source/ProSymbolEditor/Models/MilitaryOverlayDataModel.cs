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

namespace ProSymbolEditor
{
    public class MilitaryOverlayDataModel
    {
        private bool _schemaExists;
        private string _databaseName;

        private string _egdbConnectionString;

        public MilitaryOverlayDataModel()
        {
            _schemaExists = false;
        }

        Dictionary<string, bool> GetFeatureClassExistsMap(ProSymbolUtilities.SupportedStandardsType standard,
            Geodatabase gdb = null)
        {

            string prefixName = string.Empty;
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
                        prefixName = dbcps.Database + "." + dbcps.User + ".";

                        // Also save this connection string to identify this EGDB later 
                        _egdbConnectionString = (gdb as Datastore).GetConnectionString();
                    }
                }
            }

            Dictionary<string, bool> featureClassExists = new Dictionary<string, bool>();

            if (standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            {
                // 2525c_b2
                featureClassExists.Add(prefixName + "Activities", false);
                featureClassExists.Add(prefixName + "Air", false);
                featureClassExists.Add(prefixName + "ControlMeasuresAreas", false);
                featureClassExists.Add(prefixName + "ControlMeasuresLines", false);
                featureClassExists.Add(prefixName + "ControlMeasuresPoints", false);
                featureClassExists.Add(prefixName + "Installations", false);
                featureClassExists.Add(prefixName + "LandEquipment", false);
                featureClassExists.Add(prefixName + "METOCAreas", false);
                featureClassExists.Add(prefixName + "METOCLines", false);
                featureClassExists.Add(prefixName + "METOCPoints", false);
                featureClassExists.Add(prefixName + "SeaSubsurface", false);
                featureClassExists.Add(prefixName + "SeaSurface", false);
                featureClassExists.Add(prefixName + "SIGINT", false);
                featureClassExists.Add(prefixName + "Space", false);
                featureClassExists.Add(prefixName + "Units", false);
            }
            else
            {
                // 2525d
                featureClassExists.Add(prefixName + "Activities", false);
                featureClassExists.Add(prefixName + "Air", false);
                featureClassExists.Add(prefixName + "AirMissile", false);
                featureClassExists.Add(prefixName + "Civilian", false);
                featureClassExists.Add(prefixName + "ControlMeasuresAreas", false);
                featureClassExists.Add(prefixName + "ControlMeasuresLines", false);
                featureClassExists.Add(prefixName + "ControlMeasuresPoints", false);
                featureClassExists.Add(prefixName + "Cyberspace", false);
                featureClassExists.Add(prefixName + "Installations", false);
                featureClassExists.Add(prefixName + "LandEquipment", false);
                featureClassExists.Add(prefixName + "METOCAreasAtmospheric", false);
                featureClassExists.Add(prefixName + "METOCAreasOceanographic", false);
                featureClassExists.Add(prefixName + "METOCLinesAtmospheric", false);
                featureClassExists.Add(prefixName + "METOCLinesOceanographic", false);
                featureClassExists.Add(prefixName + "METOCPointsAtmospheric", false);
                featureClassExists.Add(prefixName + "METOCPointsOceanographic", false);
                featureClassExists.Add(prefixName + "MineWarfare", false);
                featureClassExists.Add(prefixName + "SeaSubsurface", false);
                featureClassExists.Add(prefixName + "SeaSurface", false);
                featureClassExists.Add(prefixName + "SIGINT", false);
                featureClassExists.Add(prefixName + "Space", false);
                featureClassExists.Add(prefixName + "SpaceMissile", false);
                featureClassExists.Add(prefixName + "Units", false);
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

        public async Task<bool> IsGDBAndFeatureClassInActiveView(string featureClassName)
        {
            string activeGdbPath = DatabaseName;

            IEnumerable<FeatureLayer> mapLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>(); ;

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

                                if (cs == _egdbConnectionString)
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

        public async Task<bool> ShouldAddInBeEnabledAsync()
        {
            return await ShouldAddInBeEnabledAsync(ProSymbolUtilities.Standard);
        }

        public async Task<bool> ShouldAddInBeEnabledAsync(ProSymbolUtilities.SupportedStandardsType standard)
        {
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
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                    {
                        if (gdbProjectItem.Name == "Map") // ignore the project Map GDB
                            continue;

                        using (Datastore datastore = gdbProjectItem.GetDatastore())
                        {
                            //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                            if (datastore is UnknownDatastore)
                                continue;

                            Geodatabase geodatabase = datastore as Geodatabase;
                            if (geodatabase == null)
                                continue;

                            //Set up Fields to check
                            List<string> fieldsToCheck = new List<string>();

                            if (standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
                            {
                                fieldsToCheck.Add("extendedfunctioncode");
                            }
                            else
                            {   // 2525d
                                fieldsToCheck.Add("symbolset");
                                fieldsToCheck.Add("symbolentity");
                            }

                            // Reset schema data model exists to false for each feature class
                            Dictionary<string, bool> featureClassExists = GetFeatureClassExistsMap(standard, geodatabase);

                            IReadOnlyList<FeatureClassDefinition> featureClassDefinitions = geodatabase.GetDefinitions<FeatureClassDefinition>();

                            bool stopLooking = false;
                            foreach(FeatureClassDefinition featureClassDefinition in featureClassDefinitions)
                            {
                                // stop looking after the first feature class not found
                                if (stopLooking)
                                    break;

                                string featureClassName = featureClassDefinition.GetName();

                                if (featureClassExists.ContainsKey(featureClassName))
                                {
                                    //Feature Class Exists!  Check for fields
                                    bool fieldsExist = true;
                                    foreach(string fieldName in fieldsToCheck)
                                    {
                                        IEnumerable<Field> foundFields = featureClassDefinition.GetFields().Where(x => x.Name == fieldName);

                                        if (foundFields.Count() < 1)
                                        {
                                            fieldsExist = false;
                                            stopLooking = true;
                                            break;
                                        }
                                    }

                                    featureClassExists[featureClassName] = fieldsExist;
                                }
                                else
                                {
                                    //Key doesn't exist, so ignore
                                }
                            }

                            bool isSchemaComplete = true;
       
                            foreach (KeyValuePair<string, bool> pair in featureClassExists)
                            {
                                if (pair.Value == false)
                                {
                                    isSchemaComplete =  false;
                                    break;
                                }
                            }

                            //Check if schema is all there
                            if (isSchemaComplete)
                            {
                                //Save geodatabase path to use as the selected database
                                _databaseName = gdbProjectItem.Path;
                                _schemaExists = true;

                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLine(exception.Message);
            }

            return SchemaExists;
        }
    }
}
