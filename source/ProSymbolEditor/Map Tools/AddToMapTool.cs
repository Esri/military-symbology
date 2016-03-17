using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;

namespace ProSymbolEditor
{
    internal class AddToMapTool : MapTool
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
                    MapPoint projectedMapPoint = GeometryEngine.Project(tempMapPoint, SpatialReferences.WGS84) as MapPoint;

                    //Create a point with a z value, since the mil spec feature classes have z enabled (TODO: other way to do this?)
                    MapPoint zEnabledMapPoint = MapPointBuilder.CreateMapPoint(projectedMapPoint.X, projectedMapPoint.Y, 0, projectedMapPoint.SpatialReference);
                    symbolDockPaneViewModel.MapCoordinates = zEnabledMapPoint;

                    symbolDockPaneViewModel.AddStyleToMap(null);
                }
                catch
                {
                    //TODO: Add exception handler
                }
            });
        }
    }

}
