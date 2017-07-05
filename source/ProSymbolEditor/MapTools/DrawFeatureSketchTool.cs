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

using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace ProSymbolEditor
{
    class DrawFeatureSketchTool : MapTool
    {
        public DrawFeatureSketchTool()
        {
            IsSketchTool = true;
            SketchOutputMode = SketchOutputMode.Map;
        }

        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            //Get view, determine what type of geometry we're editing (polygon or line)
            var symbolDockPaneViewModel = FrameworkApplication.DockPaneManager.Find("ProSymbolEditor_MilitarySymbolDockpane") as MilitarySymbolDockpaneViewModel;
            if (symbolDockPaneViewModel.GeometryType == GeometryType.Point)
            {
                SketchType = SketchGeometryType.Point;
            }
            else if (symbolDockPaneViewModel.GeometryType == GeometryType.Polyline)
            {
                SketchType = SketchGeometryType.Line;
            }
            else if (symbolDockPaneViewModel.GeometryType == GeometryType.Polygon)
            {
                SketchType = SketchGeometryType.Polygon;
            }

            base.OnToolMouseDown(e);
        }

        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            //Get the instance of the ViewModel
            var symbolDockPaneViewModel = FrameworkApplication.DockPaneManager.Find("ProSymbolEditor_MilitarySymbolDockpane") as MilitarySymbolDockpaneViewModel;

            //Get the map coordinates from the click point and set the property on the ViewModel.
            return QueuedTask.Run(() =>
            {
                try
                {
                    // for now we will always project to WGS84
                    Geometry projectedGeometry = GeometryEngine.Instance.Project(geometry, SpatialReferences.WGS84);
                    symbolDockPaneViewModel.MapGeometry = projectedGeometry;
                    symbolDockPaneViewModel.CreateNewFeatureAsync(null);
                }
                catch
                {
                    //TODO: Add exception handler
                }

                return true;
            });
        }
    }
}
