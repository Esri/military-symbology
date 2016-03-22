using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using CoordinateToolLibrary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSymbolEditor
{
    public class CoordinateObject : PropertyChangedBase, IDataErrorInfo
    {
        //private bool _valid;
        private string _coordinate;
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

        public bool IsValid { get; set; }

        public string Error { get; set; }

        public MapPoint MapPoint { get; set; }

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
                            Error = "The coordinates are invalid";
                        }
                        break;
                }

                return Error;
            }
        }

        #endregion
    }
}
