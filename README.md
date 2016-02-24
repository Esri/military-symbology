# military-symbol-editor-addin-wpf
A user-focused addin for searching, creating, and editing military symbols in ArcGIS Pro 1.2.

![Image of Military Symbol Editor Addin](screenshot.png)

## Features

* Addin for ArcGIS Pro 1.2
* Quickly and easily search, modify attributes, and preview multilayer military symbols using ArcGIS Pro

## Sections

* [Requirements](#requirements)
* [Instructions](#instructions)
* [Resources](#resources)
* [Issues](#issues)
* [Contributing](#contributing)
* [Licensing](#licensing)

## Requirements

### Developers

* Visual Studio 2015
* ArcGIS Pro 1.2
* ArcGIS Pro 1.2 SDK

### Users

* ArcGIS Pro 1.2

## Instructions

### General Help

* [New to Github? Get started here.](http://htmlpreview.github.com/?https://github.com/Esri/esri.github.com/blob/master/help/esri-getting-to-know-github.html)

### Getting Started with the Military Symbol Editor

## Developers

* Building
	* To Build Using Visual Studio
		* Open and build solution file
	* To use MSBuild to build the solution
		* Open a Visual Studio Command Prompt: Start Menu | Visual Studio 2015 | Visual Studio Tools | Developer Command Prompt for VS2015
		* ``` cd military-symbol-editor-addin-wpf\source ```
		* ``` msbuild ProSymbolEditor.sln /property:Configuration=Release ```
	* Note : Assembly references are based on a default install of the SDK, you may have to update the references if you chose an alternate install option
	
## Users

* Running
	* To run from a stand-alone deployment in ArcGIS Pro
		* Install the add-in from the application folder by double clicking it
		* The ADD-IN appears under the "ADD-IN" tab in Pro	
		* Click the "Military Symbol Designer" button and the tool will appear
		* The Search tab is the first tab:
			* Type a search term into the bar, and click Search (or hit enter)
			* The tool will return matches to that term in the Military style file
			* Selecting a search result will show a low resolution preview next to it
			* Click the modify button or the modify tab when you have selected a style symbol-editor-addin-wpf
		* The Modify tab is the third tab:
			* The application will display all attributes associated with the chosen style, with combo boxes for selecting values
			* As you select values, the symbol will update to incorporate those changes
					

## Resources

* [ArcGIS Pro 1.2 Help](http://resources.arcgis.com/en/help/)
* [ArcGIS Blog](http://blogs.esri.com/esri/arcgis/)
* ![Twitter](https://g.twimg.com/twitter-bird-16x16.png)[@EsriDefense](http://twitter.com/EsriDefense)
* [ArcGIS Solutions Website](http://solutions.arcgis.com/military/)

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an [issue](https://github.com/ArcGIS/military-symbol-editor-addin-wpf/issues).

## Contributing

Anyone and everyone is welcome to contribute. Please see our [guidelines for contributing](https://github.com/esri/contributing).

### Repository Points of Contact

#### Repository Owner: [Travis](https://github.com/tlauver)

* Merge Pull Requests
* Creates Releases and Tags
* Manages Milestones
* Manages and Assigns Issues

#### Secondary: TBD

* Backup when the Owner is away

## Licensing

Copyright 2016 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A copy of the license is available in the repository's [license.txt](license.txt) file.

Note: This addin uses MIL_STD_2525D_Symbols that is governed by the Apache License.  For more details see https://github.com/ArcGIS/military-symbol-editor-addin-wpf/blob/master/source/ProSymbolEditor/Images/MIL_STD_2525D_Symbols/license.txt.

[](Esri Tags: Military Analyst Defense ArcGIS ArcObjects .NET WPF ArcGISSolutions ArcMap ArcPro Add-In Symbol Editor)
[](Esri Language: C#) 