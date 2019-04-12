### Tools for creating Pro Layer Packages (lpkx)

These steps/tools are used to create a Schema-Only layer package for Military Overlay. 

A layer package is a file that includes the layer properties (ex. drawing settings, renderer, etc.) and data. A schema-only layer package has an empty geodatabase (no data entries).

To create the layer packages:

1. Get the latest layer package from this repo.
2. Make any desired changes
3. **Spatial Index Layer Packaging Workaround**     
  a. There is currently a workaround to address an error "Spatial Index Invalid" (See: https://github.com/Esri/military-features-data/issues/287 )                                   
  b. Run the following tools from the LayerPackageUtilities toolbox on each geodatabase being packaged. These tools/models should be run immediately before packaging the (schema only) Military Overlay Layer Packages in ArcGIS Pro:
    1. Delete All Features From Workspace        
     2. SpatialIndexWorkaround  - This is a model that will add a blank spatial index to a layer package
4. Export as a **Schema-Only** layer package
