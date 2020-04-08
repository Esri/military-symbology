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

using System.ComponentModel;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using ProAppCoordConversionModule.Models;

namespace ProSymbolEditor
{
    public class CoordinateObject : PropertyChangedBase, IDataErrorInfo
    {
        private string _coordinate;

        public MapPoint MapPoint { get; set; }
        public bool IsValid { get; set; }

        public string Coordinate
        {
            get
            {
                return _coordinate;
            }
            set
            {
                _coordinate = value;

                MapPoint point;
                var coordType = ProSymbolUtilities.GetCoordinateType(_coordinate, out point);

                if (coordType == CoordinateType.Unknown)
                {
                    //Error
                    IsValid = false;
                    MapPoint = null;
                }
                else
                {
                    IsValid = true;
                    MapPoint = point;
                }

                NotifyPropertyChanged(() => Coordinate);
            }
        }

        #region IDataErrorInfo Interface

        public string Error { get; set; }

        public string this[string columnName]
        {
            get
            {
                Error = null;

                switch (columnName)
                {
                    case "Coordinate":
                        if (!IsValid)
                        {
                            Error = Properties.Resources.CoordObjInvalidCoordinates;
                        }
                        break;
                }

                return Error;
            }
        }

        #endregion
    }
}
