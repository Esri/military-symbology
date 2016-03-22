using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        //protected override Task OnToolActivateAsync(bool hasMapViewChanged)
        //{
        //    //Get view, determine what type of geometry we're editing (polygon or line)
        //    var symbolDockPaneViewModel = FrameworkApplication.DockPaneManager.Find("ProSymbolEditor_MilitarySymbolDockpane") as MilitarySymbolDockpaneViewModel;
        //    if (symbolDockPaneViewModel.GeometryType == GeometryType.Point)
        //    {
        //        SketchType = SketchGeometryType.Point;
        //    }
        //    else if (symbolDockPaneViewModel.GeometryType == GeometryType.Polyline)
        //    {
        //        SketchType = SketchGeometryType.Line;
        //    }
        //    else if (symbolDockPaneViewModel.GeometryType == GeometryType.Polygon)
        //    {
        //        SketchType = SketchGeometryType.Polygon;
        //    }

        //    return base.OnToolActivateAsync(hasMapViewChanged);
        //}

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
            //if (symbolDockPaneViewModel == null)
            //    return Task.FromResult(0);

            //Get the map coordinates from the click point and set the property on the ViewModel.
            return QueuedTask.Run(() =>
            {
                try
                {
                    // for now we will always project to WGS84
                    Geometry projectedGeometry = GeometryEngine.Project(geometry, SpatialReferences.WGS84);
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
