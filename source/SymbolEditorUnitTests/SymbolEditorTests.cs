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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProSymbolEditor;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Hosting;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace SymbolEditorUnitTests
{
    [TestClass]
    public class SymbolEditorTests
    {
        [ClassInitialize()]
        [TestCategory("ProAddin")]
        public static void ClassInit(TestContext testContext)
        {
            // This call is needed to run Pro SDK Code Outside of Pro
            Host.Initialize();
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            // TODO: Figure out how to unload Pro SDK
            // System.AppDomainUnloadedException: Attempted to access an unloaded AppDomain. 
            // This can happen if the test(s) started a thread but did not stop it. 
            // Make sure that all the threads started by the test(s)are stopped before completion.
        }

        // Method commented out until we figure out if we can open Pro Projects outside of Pro
        // [TestMethod]
        public async Task Test_Task()
        {
            // NOT WORKING:
            // We thought we might be able to test SDK code with Pro Project dependencies
            // with this code, but the call throws exceptions in other threads and never returns
            await QueuedTask.Run(async () =>
            {
                string aprx = @"C:\Projects\MySymbolEditorTest\MySymbolEditorTest.aprx";

                string version;
                ArcGIS.Desktop.Core.Project.CanOpen(aprx, out version);

                await ArcGIS.Desktop.Core.Project.OpenAsync(aprx);
            });

            // Never gets here:

            // Now project is loaded, we can test VM objects with Pro dependencies
            MilitaryOverlayDataModel modm = new MilitaryOverlayDataModel();
            Task<bool> isEnabledMethod = modm.ShouldAddInBeEnabledAsync();
            bool enabled = await isEnabledMethod;

            Assert.IsTrue(enabled);
        }

        [TestMethod]
        public void CheckLayerForSymbolSetTest()
        {

            SymbolSetMappings symbolSetMappings = new SymbolSetMappings();
            string featureClassName = symbolSetMappings.GetFeatureClassFromMapping("01", GeometryType.Point);
            Assert.IsTrue(featureClassName == "Air", "Feature Class from mapping is incorrect");

            featureClassName = symbolSetMappings.GetFeatureClassFromMapping("40", GeometryType.Point);
            Assert.IsTrue(featureClassName == "Activities", "Feature Class from mapping is incorrect");

            featureClassName = symbolSetMappings.GetFeatureClassFromMapping("60", GeometryType.Point);
            Assert.IsTrue(featureClassName == "Cyberspace", "Feature Class from mapping is incorrect");

            featureClassName = symbolSetMappings.GetFeatureClassFromMapping("BOGUS", GeometryType.Point);
            Assert.IsTrue(string.IsNullOrEmpty(featureClassName), "Feature Class from mapping is incorrect");

        }

        [TestMethod, STAThread]
        public void CoordinateTypeTest()
        {        
            MapPoint mapPoint;
            var coordType = ProSymbolUtilities.GetCoordinateType("10SFF", out mapPoint);
            Assert.IsTrue(mapPoint != null, "MGRS coordinate is invalid, when it should be valid");

            coordType = ProSymbolUtilities.GetCoordinateType("invalidpoint", out mapPoint);
            Assert.IsTrue(mapPoint == null, "MGRS coordinate is valid, when it should be invalid");
        }
    }
}
