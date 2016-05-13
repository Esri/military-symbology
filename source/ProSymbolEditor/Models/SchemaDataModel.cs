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
    public class SchemaDataModel
    {
        private bool _schemaExists;
        private string _databaseName;
        private List<string> _fieldsToCheck;
        private Dictionary<string, bool> _featureClassExists;

        public SchemaDataModel()
        {
            //Set up Feature Class Schema
            _featureClassExists = new Dictionary<string, bool>();
            _featureClassExists.Add("Activities", false);
            _featureClassExists.Add("Air", false);
            _featureClassExists.Add("AirMissile", false);
            _featureClassExists.Add("Civilian", false);
            _featureClassExists.Add("ControlMeasuresAreas", false);
            _featureClassExists.Add("ControlMeasuresLines", false);
            _featureClassExists.Add("ControlMeasuresPoints", false);
            _featureClassExists.Add("Cyberspace", false);
            _featureClassExists.Add("Installations", false);
            _featureClassExists.Add("LandEquipment", false);
            _featureClassExists.Add("METOCAreasAtmospheric", false);
            _featureClassExists.Add("METOCAreasOceanographic", false);
            _featureClassExists.Add("METOCLinesAtmospheric", false);
            _featureClassExists.Add("METOCLinesOceanographic", false);
            _featureClassExists.Add("METOCPointsAtmospheric", false);
            _featureClassExists.Add("METOCPointsOceanographic", false);
            _featureClassExists.Add("MineWarfare", false);
            _featureClassExists.Add("SeaSubsurface", false);
            _featureClassExists.Add("SeaSurface", false);
            _featureClassExists.Add("SIGINT", false);
            _featureClassExists.Add("Space", false);
            _featureClassExists.Add("SpaceMissile", false);
            _featureClassExists.Add("Units", false);

            //Set up Fields to check
            _fieldsToCheck = new List<string>();
            _fieldsToCheck.Add("symbolset");
            _fieldsToCheck.Add("symbolentity");

            _schemaExists = false;
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

        public bool IsSchemaComplete()
        {
            foreach(KeyValuePair<string,bool> pair in _featureClassExists)
            {
                if (pair.Value == false)
                {
                    return false;
                }
            }

            return true;
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
                        using (Datastore datastore = gdbProjectItem.GetDatastore())
                        {
                            //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                            if (datastore is UnknownDatastore)
                                continue;
                            Geodatabase geodatabase = datastore as Geodatabase;

                            //Reset schema data model to false
                            _featureClassExists = _featureClassExists.ToDictionary(kvp => kvp.Key, kvp => false);

                            IReadOnlyList<FeatureClassDefinition> featureClassDefinitions = geodatabase.GetDefinitions<FeatureClassDefinition>();

                            foreach(FeatureClassDefinition featureClassDefinition in featureClassDefinitions)
                            {
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
                                        }
                                    }

                                    _featureClassExists[featureClassName] = fieldsExist;
                                }
                                else
                                {
                                    //Key doesn't exist, so ignore
                                }
                            }

                            //Check if schema is all there
                            if (IsSchemaComplete())
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
