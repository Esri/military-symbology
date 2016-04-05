using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProSymbolEditor;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Geometry;

namespace SymbolEditorUnitTests
{
    [TestClass]
    public class SymbolEditorTests
    {
        [TestMethod]
        public void CheckLayerForSymbolSetTest()
        {
            SymbolSetMappings symbolSetMappings = new SymbolSetMappings();
            string featureClassName = symbolSetMappings.GetFeatureClassFromMapping("01", StyleItemType.PointSymbol);
            Assert.IsTrue(featureClassName == "Air", "Feature Class from mapping is incorrect");

            featureClassName = symbolSetMappings.GetFeatureClassFromMapping("40", StyleItemType.PointSymbol);
            Assert.IsTrue(featureClassName == "Activities", "Feature Class from mapping is incorrect");

            featureClassName = symbolSetMappings.GetFeatureClassFromMapping("60", StyleItemType.PointSymbol);
            Assert.IsTrue(featureClassName == "Cyberspace", "Feature Class from mapping is incorrect");
        }

        [TestMethod]
        public void CoordinateTypeTest()
        {
            MapPoint mapPoint;
            var coordType = ProSymbolUtilities.GetCoordinateType("10SFF", out mapPoint);
            Assert.IsTrue(mapPoint != null, "MGRS coordinate is invalid, when it should be valid");

            coordType = ProSymbolUtilities.GetCoordinateType("blahblah", out mapPoint);
            Assert.IsTrue(mapPoint == null, "MGRS coordinate is valid, when it should be invalid");
        }
    }
}
