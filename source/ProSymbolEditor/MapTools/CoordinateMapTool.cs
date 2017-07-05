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
using System;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;

namespace ProSymbolEditor
{
    /// <summary>
    /// Tool is not currently used by the addin, but keeping in case we do.  It will grab the coordinates if a user clicks on the map.
    /// </summary>
    internal class CoordinateMapTool : MapTool
    {
        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }

        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            //On mouse down check if the mouse button pressed is the left mouse button. If it is handle the event.
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                e.Handled = true;

        }

        /// <summary>
        /// Called when the OnToolMouseDown event is handled. Allows the opportunity to perform asynchronous operations corresponding to the event.
        /// </summary>
        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
        {
            //Get the instance of the ViewModel
            var symbolDockPaneViewModel = FrameworkApplication.DockPaneManager.Find("ProSymbolEditor_MilitarySymbolDockpane") as MilitarySymbolDockpaneViewModel;
            if (symbolDockPaneViewModel == null)
                return Task.FromResult(0);

            //Get the map coordinates from the click point and set the property on the ViewModel.
            return QueuedTask.Run(() =>
            {
                var tempMapPoint = MapView.Active.ClientToMap(e.ClientPoint);
                try
                {
                    // for now we will always project to WGS84
                    MapPoint projectedMapPoint = GeometryEngine.Instance.Project(tempMapPoint, SpatialReferences.WGS84) as MapPoint;

                    //Create a point with a z value, since the mil spec feature classes have z enabled (TODO: other way to do this?)
                    symbolDockPaneViewModel.MapGeometry = MapPointBuilder.CreateMapPoint(projectedMapPoint.X, projectedMapPoint.Y, 0, projectedMapPoint.SpatialReference);
                    symbolDockPaneViewModel.MapPointCoordinatesString = string.Format("{0:0.0####} {1:0.0####}", tempMapPoint.Y, tempMapPoint.X);
                }
                catch(Exception exception)
                {
                    System.Console.WriteLine(exception.Message);
                }
            });
        }
    }
}
