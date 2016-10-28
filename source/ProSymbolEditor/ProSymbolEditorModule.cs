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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;

namespace ProSymbolEditor
{
    internal class ProSymbolEditorModule : Module
    {
        private static ProSymbolEditorModule _this = null;
        public MilitaryOverlayDataModel MilitaryOverlaySchema { get; set; }

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static ProSymbolEditorModule Current
        {
            get
            {
                return _this ?? (_this = (ProSymbolEditorModule)FrameworkApplication.FindModule("ProSymbolEditor_Module"));
            }
        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        protected override bool Initialize()
        {
            if (MilitaryOverlaySchema == null)
            {
                MilitaryOverlaySchema = new MilitaryOverlayDataModel();
            }

            return base.Initialize();
        }

        #endregion Overrides
    }
}
