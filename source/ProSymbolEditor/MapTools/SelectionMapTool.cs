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

using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Windows.Input;

namespace ProSymbolEditor
{
    internal class SelectionMapTool : MapTool
    {

        public SelectionMapTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Point; 
            SketchOutputMode = SketchOutputMode.Screen;
        }

        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }

        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            base.OnToolMouseDown(e);
        }

        /// <summary>
        /// Called when the sketch is finished.
        /// </summary>
        protected override async Task<bool> OnSketchCompleteAsync(ArcGIS.Core.Geometry.Geometry geometry)
        {
            // Clear any previous selection using the built-in Pro button/command
            // TRICKY: Must be called here before the QueuedTask so runs on Main UI Thread
            ProSymbolUtilities.ClearMapSelection();

            return await QueuedTask.Run(() =>
            {
                //Return all the features that intersect the sketch geometry
                var result = MapView.Active.GetFeatures(geometry);

                if ((result != null) && (result.Count > 0))
                {
                    MapView.Active.SelectFeatures(geometry, SelectionCombinationMethod.New);
                }

                return true;
            });
        }
    }
}
