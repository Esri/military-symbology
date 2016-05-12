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
        public bool SchemaExists { get; set; }
        public string DatabaseName { get; set; }
        public List<string> FieldsToCheck { get; set; }
        public Dictionary<string, bool> FeatureClassExists { get; set; }

        public SchemaDataModel()
        {
            //Set up Feature Class Schema
            FeatureClassExists = new Dictionary<string, bool>();
            FeatureClassExists.Add("Activities", false);
            FeatureClassExists.Add("Air", false);
            FeatureClassExists.Add("AirMissile", false);
            FeatureClassExists.Add("Civilian", false);
            FeatureClassExists.Add("ControlMeasuresAreas", false);
            FeatureClassExists.Add("ControlMeasuresLines", false);
            FeatureClassExists.Add("ControlMeasuresPoints", false);
            FeatureClassExists.Add("Cyberspace", false);
            FeatureClassExists.Add("Installations", false);
            FeatureClassExists.Add("LandEquipment", false);
            FeatureClassExists.Add("METOCAreasAtmospheric", false);
            FeatureClassExists.Add("METOCAreasOceanographic", false);
            FeatureClassExists.Add("METOCLinesAtmospheric", false);
            FeatureClassExists.Add("METOCLinesOceanographic", false);
            FeatureClassExists.Add("METOCPointsAtmospheric", false);
            FeatureClassExists.Add("METOCPointsOceanographic", false);
            FeatureClassExists.Add("MineWarfare", false);
            FeatureClassExists.Add("SeaSubsurface", false);
            FeatureClassExists.Add("SeaSurface", false);
            FeatureClassExists.Add("SIGINT", false);
            FeatureClassExists.Add("Space", false);
            FeatureClassExists.Add("SpaceMissile", false);
            FeatureClassExists.Add("Units", false);

            //Set up Fields to check
            FieldsToCheck = new List<string>();
            FieldsToCheck.Add("symbolset");
            FieldsToCheck.Add("symbolentity");

            SchemaExists = false;
        }

        public bool IsSchemaComplete()
        {
            foreach(KeyValuePair<string,bool> pair in FeatureClassExists)
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
            SchemaExists = false;

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
                            FeatureClassExists = FeatureClassExists.ToDictionary(kvp => kvp.Key, kvp => false);

                            IReadOnlyList<FeatureClassDefinition> featureClassDefinitions = geodatabase.GetDefinitions<FeatureClassDefinition>();

                            foreach(FeatureClassDefinition featureClassDefinition in featureClassDefinitions)
                            {
                                string featureClassName = featureClassDefinition.GetName();

                                if (FeatureClassExists.ContainsKey(featureClassName))
                                {
                                    //Feature Class Exists!  Check for fields
                                    bool fieldsExist = true;
                                    foreach(string fieldName in FieldsToCheck)
                                    {
                                        IEnumerable<Field> foundFields = featureClassDefinition.GetFields().Where(x => x.Name == fieldName);

                                        if (foundFields.Count() < 1)
                                        {
                                            fieldsExist = false;
                                        }
                                    }

                                    FeatureClassExists[featureClassName] = fieldsExist;
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
                                DatabaseName = geodatabase.GetPath();
                                SchemaExists = true;

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
