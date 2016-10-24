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

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSymbolEditor
{
    public class MilitaryOverlayDataModel
    {
        private bool _schemaExists;
        private string _databaseName;

        public MilitaryOverlayDataModel()
        {
            _schemaExists = false;
        }

        Dictionary<string, bool> GetFeatureClassExistsMap()
        {
            Dictionary<string, bool> featureClassExists = new Dictionary<string, bool>();

            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            {
                // 2525c_b2
                featureClassExists.Add("Activities", false);
                featureClassExists.Add("Air", false);
                featureClassExists.Add("ControlMeasuresAreas", false);
                featureClassExists.Add("ControlMeasuresLines", false);
                featureClassExists.Add("ControlMeasuresPoints", false);
                featureClassExists.Add("Installations", false);
                featureClassExists.Add("LandEquipment", false);
                featureClassExists.Add("METOCAreas", false);
                featureClassExists.Add("METOCLines", false);
                featureClassExists.Add("METOCPoints", false);
                featureClassExists.Add("SeaSubsurface", false);
                featureClassExists.Add("SeaSurface", false);
                featureClassExists.Add("SIGINT", false);
                featureClassExists.Add("Space", false);
                featureClassExists.Add("Units", false);
            }
            else
            {
                // 2525d
                featureClassExists.Add("Activities", false);
                featureClassExists.Add("Air", false);
                featureClassExists.Add("AirMissile", false);
                featureClassExists.Add("Civilian", false);
                featureClassExists.Add("ControlMeasuresAreas", false);
                featureClassExists.Add("ControlMeasuresLines", false);
                featureClassExists.Add("ControlMeasuresPoints", false);
                featureClassExists.Add("Cyberspace", false);
                featureClassExists.Add("Installations", false);
                featureClassExists.Add("LandEquipment", false);
                featureClassExists.Add("METOCAreasAtmospheric", false);
                featureClassExists.Add("METOCAreasOceanographic", false);
                featureClassExists.Add("METOCLinesAtmospheric", false);
                featureClassExists.Add("METOCLinesOceanographic", false);
                featureClassExists.Add("METOCPointsAtmospheric", false);
                featureClassExists.Add("METOCPointsOceanographic", false);
                featureClassExists.Add("MineWarfare", false);
                featureClassExists.Add("SeaSubsurface", false);
                featureClassExists.Add("SeaSurface", false);
                featureClassExists.Add("SIGINT", false);
                featureClassExists.Add("Space", false);
                featureClassExists.Add("SpaceMissile", false);
                featureClassExists.Add("Units", false);
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

        public async Task<bool> ShouldAddInBeEnabledAsync()
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

                            //Set up Fields to check
                            List<string> _fieldsToCheck = new List<string>();

                            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
                            {
                                _fieldsToCheck.Add("extendedfunctioncode");
                            }
                            else
                            {   // 2525d
                                _fieldsToCheck.Add("symbolset");
                                _fieldsToCheck.Add("symbolentity");
                            }

                            //Reset schema data model to false
                            // _featureClassExists = _featureClassExists.ToDictionary(kvp => kvp.Key, kvp => false);
                            Dictionary<string, bool> _featureClassExists = GetFeatureClassExistsMap();

                            IReadOnlyList<FeatureClassDefinition> featureClassDefinitions = geodatabase.GetDefinitions<FeatureClassDefinition>();

                            bool stopLooking = false;
                            foreach(FeatureClassDefinition featureClassDefinition in featureClassDefinitions)
                            {
                                // stop looking after the first feature class not found
                                if (stopLooking)
                                    break;

                                string featureClassName = featureClassDefinition.GetName();

                                if (_featureClassExists.ContainsKey(featureClassName))
                                {
                                    //Feature Class Exists!  Check for fields
                                    bool fieldsExist = true;
                                    foreach(string fieldName in _fieldsToCheck)
                                    {
                                        IEnumerable<Field> foundFields = featureClassDefinition.GetFields().Where(x => x.Name == fieldName);

                                        if (foundFields.Count() < 1)
                                        {
                                            fieldsExist = false;
                                            stopLooking = true;
                                            break;
                                        }
                                    }

                                    _featureClassExists[featureClassName] = fieldsExist;
                                }
                                else
                                {
                                    //Key doesn't exist, so ignore
                                }
                            }

                            bool isSchemaComplete = true;
       
                            foreach (KeyValuePair<string, bool> pair in _featureClassExists)
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
                                _databaseName = geodatabase.GetPath();
                                _schemaExists = true;

                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception exception)
            {
                System.Console.WriteLine(exception.Message);
            }

            return SchemaExists;
        }
    }
}
