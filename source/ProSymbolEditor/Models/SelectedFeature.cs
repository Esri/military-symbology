using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSymbolEditor
{
    public class SelectedFeature : PropertyChangedBase
    {
        public BasicFeatureLayer FeatureLayer { get; set; }
        public string FeatureLayerName { get; set; }
        public long ObjectId { get; set; }

        public SelectedFeature() { }

        public SelectedFeature(BasicFeatureLayer featureLayer, long objectId)
        {
            FeatureLayer = featureLayer;
            FeatureLayerName = featureLayer.Name;
            ObjectId = objectId;
        }
    }
}
