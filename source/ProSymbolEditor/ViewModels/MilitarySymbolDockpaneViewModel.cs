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
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Editing;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using CoordinateConversionLibrary.Models;
using Microsoft.Win32;
using System.Web.Script.Serialization;
using System.Windows.Threading;
using ArcGIS.Core.CIM;

namespace ProSymbolEditor
{
    internal class MilitarySymbolDockpaneViewModel : DockPane, IDataErrorInfo
    {
        public Views.SearchView SearchViewTab { get; set; }

        public Views.ModifyView ModifyViewTab { get; set; }

        public Views.FavoritesView FavoritesViewTab { get; set; }

        public Views.SymbolView SymbolViewTab { get; set; }

        public Views.LabelView LabelViewTab { get; set; }

        public Views.CoordinateView CoordinateViewTab { get; set; }
        
        //Member Variables
        private const string _dockPaneID = "ProSymbolEditor_MilitarySymbolDockpane";
        private const string _menuID = "ProSymbolEditor_MilitarySymbolDockpane_Menu";

        public bool IsAddinEnabled
        {
            get
            {
                return isAddinEnabled;
            }
            set
            {
                isAddinEnabled = value;

                if (!isAddinEnabled)
                {
                    IsCoordinateTabEnabled = false;
                    IsStyleItemSelected = false;
                    SelectedTabIndex = 0;

                    _searchString = "Please click to enable addin...";
                }
                else
                {
                    _searchString = "";
                }

                NotifyPropertyChanged(() => SearchString);
                NotifyPropertyChanged(() => IsAddinEnabled);
            }
        }
        private bool isAddinEnabled = false;

        public string StatusMessage
        {
            get
            {
                return _statusMessage + 
                    " (" + ProSymbolUtilities.StandardLabel + ")";
            }
            set
            {
                _statusMessage = value;

                NotifyPropertyChanged(() => StatusMessage);
            }
        }
        private string _statusMessage;

        private static string MilitaryStyleName
        {
            get
            {
                return ProSymbolUtilities.GetDictionaryString();
            }
        }

        private string Mil2525RelativePath
        {
            get
            {
                return "Resources" + Path.DirectorySeparatorChar +
                    "Dictionaries" + Path.DirectorySeparatorChar +
                    MilitaryStyleName + Path.DirectorySeparatorChar + 
                    MilitaryStyleName + ".stylx";
            }
        }

        private string ProInstallPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_proInstallPath))
                    return _proInstallPath;

                //Get Military Symbol Style Install Path
                _proInstallPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\ESRI\ArcGISPro\", "InstallDir", null);

                if (string.IsNullOrEmpty(_proInstallPath))
                {
                    //Try to get the install path from current user instead of local machine
                    _proInstallPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\ESRI\ArcGISPro\", "InstallDir", null);
                }
                return _proInstallPath;
            }
        }
        private string _proInstallPath = string.Empty;

        private string Mil2525StyleFullFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(ProInstallPath))
                {
                    return Path.Combine(ProInstallPath, Mil2525RelativePath);
                }

                return "";
            }
        }

        private string _currentFeatureClassName = "";
        private string _favoritesFilePath = "";
        private FeatureClass _currentFeatureClass = null;
        private StyleProjectItem _militaryStyleItem = null;
        private SymbolStyleItem _selectedStyleItem = null;
        private SymbolStyleItem _savedStyleItem = null;
        private SelectedFeature _selectedSelectedFeature = null;
        private SymbolAttributeSet _selectedFavoriteSymbol = null;
        private SymbolAttributeSet _savedSelectedFavoriteSymbol = null;
        private SymbolAttributeSet _editSelectedFeatureSymbol = null;
        private SymbolSetMappings _symbolSetMappings = new SymbolSetMappings();

        //Lock objects for ObservableCollections
        private static object _lock = new object();

        //Binded Variables - Text Boxes
        private string _searchString = "";
        private string _mapCoordinatesString = "";
        private string _resultCount = "";

        //Binded Variables - List Boxes
        private IList<SymbolStyleItem> _styleItems = new List<SymbolStyleItem>();

        //Binded Variables - Other
        private SymbolAttributeSet _symbolAttributeSet = new SymbolAttributeSet();
        private MilitaryFieldsInspectorModel _militaryFieldsInspectorModel = new MilitaryFieldsInspectorModel();
        private int _selectedTabIndex = 0;
        private ArcGIS.Core.Geometry.Geometry _mapCoordinates;
        public bool _coordinateValid = false;
        private bool _isStyleItemSelected = false;
        private bool _isCoordinateTabEnabled = false;
        private bool _isFavoriteItemSelected = false;
        private bool _addToMapToolEnabled = false;
        private bool _selectToolEnabled = false;
        private Visibility _pointCoordinateVisibility;
        private Visibility _polyCoordinateVisibility;
        private ProgressDialog _progressDialogLoad;
        private ProgressDialog _progressDialogSearch;
        private ICollectionView _favoritesView;
        private string _favoritesSearchFilter = "";
        private bool _isEditing = false;
        private bool _isAddingNew = false;

        private void resetViewModelState()
        {
            //Reset things
            ClearSearch();

            this.IsFavoriteItemSelected = false;
            this.SelectedTabIndex = 0;
            this.SearchString = "";
            _symbolAttributeSet.ResetAttributes();
            SelectedStyleTags.Clear();
            SelectedFeaturesCollection.Clear();
            SelectedSelectedFeature = null;

            // reset this so standard change will force new Style lookup:
            _militaryStyleItem = null;

            // re-load the favorites
            foreach (SymbolAttributeSet set in Favorites)
            {
                set.GeneratePreviewSymbol();
            }

            _favoritesView.Refresh();

            SymbolAttributeSet.StandardVersion = ProSymbolUtilities.StandardString;
        }

        private void setStandardFromSettings()
        {
            if (Properties.Settings.Default.DefaultStandard ==
                    ProSymbolUtilities.GetStandardString(ProSymbolUtilities.SupportedStandardsType.mil2525c_b2))
                ProSymbolUtilities.Standard = ProSymbolUtilities.SupportedStandardsType.mil2525c_b2;
            else
                ProSymbolUtilities.Standard = ProSymbolUtilities.SupportedStandardsType.mil2525d;
        }

        private async Task Initialize()
        {
            // Not enabled until Schema found/set
            IsAddinEnabled = false;

            ProSymbolEditorModule.Current.MilitaryOverlaySchema.Reset();

            StatusMessage = "Addin Not Enabled";

            // Somewhat tricky, see if the project has a GDB with an existing standard, if so just set to that
            bool isEnabled2525C_B2 = await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync(ProSymbolUtilities.SupportedStandardsType.mil2525c_b2);
            bool isEnabled2525D = await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync(ProSymbolUtilities.SupportedStandardsType.mil2525d);

            if (!isEnabled2525D && !isEnabled2525C_B2)
            {
                // NOTE: this has been moved to DockPane_OnMouseClick
                // If neither standard found in the project, prompt the user to:
                // Add the Layer package for the desired standard and/or select an existing GDB 
                // CheckAddinEnabled();
                resetViewModelState();
                IsAddinEnabled = false;

                return;
            }
            else
            {
                // Note: this special case where both standards/databases exist only 
                // occurs in the Military Overlay Template
                if (isEnabled2525D && isEnabled2525C_B2)
                {
                    // However, if both standards are found in GDBs in the project, 
                    // let the user pick the one to use
                    var result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            "Multiple databases containing the Military Overlay datamodel were found in this project. \n" +
                            "Would you like to select the default database to use for edits?", "Multiple Military Overlay Databases",
                            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Asterisk);

                    if (Convert.ToString(result) == "Yes")
                    {
                        bool success = await ShowSettingsWindowAsync(true);

                        if (!success)
                            return;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (isEnabled2525D)
                        ProSymbolUtilities.Standard = ProSymbolUtilities.SupportedStandardsType.mil2525d;
                    else
                        ProSymbolUtilities.Standard = ProSymbolUtilities.SupportedStandardsType.mil2525c_b2;

                    StatusMessage = "Initialized";
                }
            }

            IsAddinEnabled = true;

            //Add military style to project
            Task<StyleProjectItem> getMilitaryStyle = GetMilitaryStyleAsync();
            _militaryStyleItem = await getMilitaryStyle;

            //Reset things
            resetViewModelState();
        }

        protected MilitarySymbolDockpaneViewModel()
        {
            SearchViewTab = new Views.SearchView();
            SearchViewTab.DataContext = this;

            ModifyViewTab = new Views.ModifyView();
            ModifyViewTab.DataContext = this;

            FavoritesViewTab = new Views.FavoritesView();
            FavoritesViewTab.DataContext = this;

            SymbolViewTab = new Views.SymbolView();
            SymbolViewTab.DataContext = this;

            LabelViewTab = new Views.LabelView();
            LabelViewTab.DataContext = this;

            CoordinateViewTab = new Views.CoordinateView();
            CoordinateViewTab.DataContext = this;

            ArcGIS.Desktop.Core.Events.ProjectOpenedEvent.Subscribe(async (args) =>
            {
                await Initialize();
            });

            ArcGIS.Desktop.Framework.Events.ActiveToolChangedEvent.Subscribe(OnActiveToolChanged);
            ArcGIS.Desktop.Mapping.Events.MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
            ArcGIS.Desktop.Mapping.Events.LayersRemovedEvent.Subscribe(OnLayersRemoved);
            ArcGIS.Desktop.Mapping.Events.LayersRemovingEvent.Subscribe(OnLayersRemoving);

            //Create locks for variables that are updated in worker threads
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.IdentityDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.EchelonDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.StatusDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.OperationalConditionAmplifierDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.MobilityDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.TfFdHqDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.ContextDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.Modifier1DomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.Modifier2DomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.ReinforcedDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.ReliabilityDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.CredibilityDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.CountryCodeDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.ExtendedFunctionCodeValues, _lock);

            //Set up Commands
            SearchResultCommand = new RelayCommand(SearchStylesAsync, param => true);
            GoToTabCommand = new RelayCommand(GoToTab, param => true);
            //ActivateMapToolCommand = new RelayCommand(ActivateCoordinateMapTool, param => true);
            AddCoordinateToMapCommand = new RelayCommand(CreateNewFeatureAsync, CanCreatePolyFeatureFromCoordinates);
            MarkCoordinateOnMapCommand = new RelayCommand(MarkCoordinateOnMap, CanCreatePolyFeatureFromCoordinates);

            ActivateAddToMapToolCommand = new RelayCommand(ActivateDrawFeatureSketchTool, param => true);
            SaveEditsCommand = new RelayCommand(SaveEdits, param => true);
            CopyImageToClipboardCommand = new RelayCommand(CopyImageToClipboard, param => true);
            SaveImageToCommand = new RelayCommand(SaveImageAs, param => true);
            SaveSymbolFileCommand = new RelayCommand(SaveSymbolAsFavorite, param => true);
            DeleteFavoriteSymbolCommand = new RelayCommand(DeleteFavoriteSymbol, param => true);
            CreateTemplateFromFavoriteCommand = new RelayCommand(CreateTemplateFromFavorite, param => true);
            SaveFavoritesFileAsCommand = new RelayCommand(SaveFavoritesAsToFile, param => true);
            ImportFavoritesFileCommand = new RelayCommand(ImportFavoritesFile, param => true);
            SelectToolCommand = new RelayCommand(ActivateSelectTool, param => true);
            ShowAboutWindowCommand = new RelayCommand(ShowAboutWindow, param => true);
            ShowSettingsWindowCommand = new RelayCommand(ShowSettingsWindow, param => true);
            ClearSearchTextCommand = new RelayCommand(ClearSearchText, param => true);

            _symbolAttributeSet.LabelAttributes.DateTimeValid = null;
            _symbolAttributeSet.LabelAttributes.DateTimeExpired = null;
            IsStyleItemSelected = false;

            PolyCoordinates = new ObservableCollection<CoordinateObject>();
            Favorites = new ObservableCollection<SymbolAttributeSet>();
            SelectedStyleTags = new ObservableCollection<string>();
            SelectedFavoriteStyleTags = new ObservableCollection<string>();
            SelectedFeaturesCollection = new ObservableCollection<SelectedFeature>();
            BindingOperations.EnableCollectionSynchronization(SelectedFeaturesCollection, _lock);

            _progressDialogLoad = new ProgressDialog("Loading Layer Package...");
            _progressDialogSearch = new ProgressDialog("Searching...");

            //Load saved favorites
            _favoritesFilePath = System.IO.Path.Combine(ProSymbolUtilities.AddinAssemblyLocation(), "SymbolFavorites.json");
            LoadAllFavoritesFromFile();

            // If the Addin has been opened while there is already a Military Overlay loaded, set the state/standard
            if (MapView.Active != null)
                ArcGIS.Desktop.Framework.FrameworkApplication.Current.Dispatcher.Invoke(async () => {
                    await Initialize();
                });           
        }

        #region General Add-In Getters/Setters

        public int SelectedTabIndex
        {
            get
            {
                return _selectedTabIndex;
            }
            set
            {
                // if switching from the Coordinate Tab, clear any coordinate marker
                if (_selectedTabIndex == 5)
                    RemoveCoordinateMarker();

                _selectedTabIndex = value;

                NotifyPropertyChanged(() => SelectedTabIndex);
            }
        }

        public ObservableCollection<SymbolAttributeSet> Favorites { get; set; }

        #endregion

        #region Commands Get/Sets

        public ICommand SearchResultCommand { get; set; }

        public ICommand GoToTabCommand { get; set; }

        //public ICommand ActivateMapToolCommand { get; set; }

        public ICommand AddCoordinateToMapCommand { get; set; }

        public ICommand MarkCoordinateOnMapCommand { get; set; }

        public ICommand ActivateAddToMapToolCommand { get; set; }

        public ICommand SaveEditsCommand { get; set; }

        public ICommand SaveImageToCommand { get; set; }

        public ICommand CopyImageToClipboardCommand { get; set; }

        public ICommand SaveSymbolFileCommand { get; set; }

        public ICommand DeleteFavoriteSymbolCommand { get; set; }

        public ICommand CreateTemplateFromFavoriteCommand { get; set; }

        public ICommand ImportFavoritesFileCommand { get; set; }

        public ICommand SaveFavoritesFileAsCommand { get; set; }

        public ICommand SelectToolCommand { get; set; }

        public ICommand ShowAboutWindowCommand { get; set; }

        public ICommand ShowSettingsWindowCommand { get; set; }

        public ICommand ClearSearchTextCommand { get; set; }
        #endregion

        #region Style Getters/Setters

        public string SearchString
        {
            get
            {
                return _searchString;
            }
            set
            {
                if (_searchString == value)
                    return;

                _searchString = value;

                NotifyPropertyChanged(() => SearchString);

                if (_searchString.Length > 0)
                {
                    SearchStylesAsync(null);
                }
                else
                {
                    // clear item list if search term cleared
                    if (StyleItems.Count > 0)
                    {
                        StyleItems.Clear();
                        NotifyPropertyChanged(() => StyleItems);
                    }
                }
            }
        }

        public string ResultCount
        {
            get
            {
                return _resultCount;
            }
            set
            {
                _resultCount = value;

                NotifyPropertyChanged(() => ResultCount);
            }

        }

        public string FavoritesSearchFilter
        {
            get
            {
                return _favoritesSearchFilter;
            }
            set
            {
                if (value != _favoritesSearchFilter)
                {
                    _favoritesSearchFilter = value;
                    _favoritesView.Refresh();
                    NotifyPropertyChanged(() => FavoritesSearchFilter);
                }
            }
        }

        public bool IsStyleItemSelected
        {
            get
            {
                return _isStyleItemSelected;
            }
            set
            {
                _isStyleItemSelected = value;

                if (IsEditing)
                    IsCoordinateTabEnabled = false;
                else
                    IsCoordinateTabEnabled = value;

                NotifyPropertyChanged(() => IsStyleItemSelected);
            }
        }

        public bool IsCoordinateTabEnabled
        {
            get
            {
                return _isCoordinateTabEnabled;
            }
            set
            {
                _isCoordinateTabEnabled = value;

                NotifyPropertyChanged(() => IsCoordinateTabEnabled);
            }
        }

        public bool IsFavoriteItemSelected
        {
            get
            {
                return _isFavoriteItemSelected;
            }
            set
            {
                _isFavoriteItemSelected = value;
                NotifyPropertyChanged(() => IsFavoriteItemSelected);
            }
        }

        public bool IsEditing
        {
            get
            {
                return _isEditing;
            }
            set
            {
                _isEditing = value;

                if (_isEditing == false)
                {
                    IsAddingNew = true;
                }
                else
                {
                    IsAddingNew = false;
                }

                NotifyPropertyChanged(() => IsEditing);
            }
        }

        public bool IsAddingNew
        {
            get
            {
                return _isAddingNew;
            }
            set
            {
                _isAddingNew = value;
                NotifyPropertyChanged(() => IsAddingNew);
            }
        }

        public ICollectionView FavoritesView
        {
            get
            {
                return _favoritesView;
            }
        }

        public IList<SymbolStyleItem> StyleItems
        {
            get
            {
                return _styleItems;
            }
        }

        public SymbolStyleItem SelectedStyleItem
        {
            get
            {
                return _selectedStyleItem;
            }
            set
            {
                if (_selectedStyleItem == value)
                    return;

                _selectedStyleItem = value;

                if (!ProSymbolEditorModule.Current.MilitaryOverlaySchema.SchemaExists && value != null)
                {
                    _savedStyleItem = _selectedStyleItem;
                    ShowAddInNotEnabledMessageBox();
                    return;
                }

                //Clear old attributes
                _symbolAttributeSet.ResetAttributes();

                if (_selectedStyleItem != null)
                {
                    //Tokenize tags
                    _symbolAttributeSet.SymbolTags = _selectedStyleItem.Tags;
                    SelectedStyleTags.Clear();
                    foreach(string tag in _selectedStyleItem.Tags.Split(';').ToList())
                    {
                        SelectedStyleTags.Add(tag);
                    }

                    //Get the geometry type off a tag on the symbol
                    List<string> reverseTags = _selectedStyleItem.Tags.Split(';').ToList();
                    reverseTags.Reverse();
                    string geometryTypeTag = reverseTags[2];

                    if (geometryTypeTag.ToUpper() == "POINT")
                    {
                        GeometryType = GeometryType.Point;
                        PointCoordinateVisibility = Visibility.Visible;
                        PolyCoordinateVisibility = Visibility.Collapsed;
                    }
                    else if (geometryTypeTag.ToUpper() == "LINE")
                    {
                        GeometryType = GeometryType.Polyline;
                        PointCoordinateVisibility = Visibility.Collapsed;
                        PolyCoordinateVisibility = Visibility.Visible;
                    }
                    else if (geometryTypeTag.ToUpper() == "AREA")
                    {
                        GeometryType = GeometryType.Polygon;
                        PointCoordinateVisibility = Visibility.Collapsed;
                        PolyCoordinateVisibility = Visibility.Visible;
                    }
                    else
                    {
                        //No tag found for geometry type, so use the geometry type off the symbol itself
                        if (_selectedStyleItem.ItemType == StyleItemType.PointSymbol)
                        {
                            GeometryType = GeometryType.Point;
                            PointCoordinateVisibility = Visibility.Visible;
                            PolyCoordinateVisibility = Visibility.Collapsed;
                        }
                        else if (_selectedStyleItem.ItemType == StyleItemType.PolygonSymbol)
                        {
                            GeometryType = GeometryType.Polygon;
                            PointCoordinateVisibility = Visibility.Collapsed;
                            PolyCoordinateVisibility = Visibility.Visible;
                        }
                        else if (_selectedStyleItem.ItemType == StyleItemType.LineSymbol)
                        {
                            GeometryType = GeometryType.Polyline;
                            PointCoordinateVisibility = Visibility.Collapsed;
                            PolyCoordinateVisibility = Visibility.Visible;
                        }
                    }

                    //Parse key for symbol id codes
                    string[] symbolIdCode = ParseKeyForSymbolIdCode(_selectedStyleItem.Tags);
                    _symbolAttributeSet.DisplayAttributes.SymbolSet = symbolIdCode[0];
                    _symbolAttributeSet.DisplayAttributes.SymbolEntity = symbolIdCode[1];

                    SymbolAttributeSet loadSet = new SymbolAttributeSet();

                    // Set 2525C_B2 SIDC/attribute if applicable
                    if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
                    {
                        string functionCode = symbolIdCode[2];
                        _symbolAttributeSet.DisplayAttributes.ExtendedFunctionCode = functionCode;

                        loadSet.DisplayAttributes.ExtendedFunctionCode = functionCode;
                    }

                    //Get feature class name to generate domains
                    _currentFeatureClassName = _symbolSetMappings.GetFeatureClassFromMapping(
                        _symbolAttributeSet.DisplayAttributes, GeometryType);

                    if (!string.IsNullOrEmpty(_currentFeatureClassName))
                    {
                        //Generate domains
                        GetMilitaryDomainsAsync(loadSet);
                    }
                    else
                    {
                        // LogError - notify user
                    }

                    IsEditing = false;
                    IsStyleItemSelected = true;
                }
                else
                {
                    IsStyleItemSelected = false;
                }
            }
        }

        public SelectedFeature SelectedSelectedFeature
        {
            get
            {
                return _selectedSelectedFeature;
            }
            set
            {
                if (_selectedSelectedFeature == value)
                    return;

                if (SelectedTabIndex == 0)
                {
                    // Don't allow selection from the Search Tab
                    return;
                }
                else
                {
                    // for other tabs - clear the search selection (so user has to reselect)
                    ClearSearchSelection();
                }

                _selectedSelectedFeature = value;

                if (_selectedSelectedFeature != null)
                {
                    try
                    {
                        // TODO: there is an exception here when:
                        // 1: Multiple Maps are open
                        // 2: Trying to flash a feature that is selected on another map, that is not the active map
                        MapView.Active.FlashFeature(_selectedSelectedFeature.FeatureLayer, _selectedSelectedFeature.ObjectId);
                        ArcGIS.Desktop.Framework.FrameworkApplication.Current.Dispatcher.Invoke(async () => {
                            await CreateSymbolSetFromFieldValuesAsync();
                        });
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Trace.WriteLine("Exception in SelectedSelectedFeature: " + exception.Message);
                    }
                }
                else
                {
                    EditSelectedFeatureSymbol = null;
                    IsStyleItemSelected = false;

                    if (SelectedTabIndex > 2)
                    {
                        //Reset tab to modify if the user is in symbol/text/coordinates (since they'll be disabled)
                        SelectedTabIndex = 1;
                    }
                }

                NotifyPropertyChanged(() => SelectedSelectedFeature);
            }
        }

        public SymbolAttributeSet SelectedFavoriteSymbol
        {
            get
            {
                return _selectedFavoriteSymbol;
            }
            set
            {
                if (_selectedFavoriteSymbol == value)
                    return;

                _selectedFavoriteSymbol = value;
                SelectedFavoriteStyleTags.Clear();

                //Tokenize tags
                if (_selectedFavoriteSymbol != null)
                {
                    foreach (string tag in _selectedFavoriteSymbol.SymbolTags.Split(';').ToList())
                    {
                        SelectedFavoriteStyleTags.Add(tag);
                    }

                    IsFavoriteItemSelected = true;

                    _savedSelectedFavoriteSymbol = _selectedFavoriteSymbol;

                    ClearSearchSelection();
                }
                else
                {
                    IsFavoriteItemSelected = false;
                }

                //Load Symbol
                LoadSymbolIntoWorkflow(false);

                NotifyPropertyChanged(() => SelectedFavoriteSymbol);
            }
        }

        public SymbolAttributeSet EditSelectedFeatureSymbol
        {
            get
            {
                return _editSelectedFeatureSymbol;
            }
            set
            {
                if (_editSelectedFeatureSymbol == value)
                    return;

                _editSelectedFeatureSymbol = value;

                if (_editSelectedFeatureSymbol != null)
                    _editSelectedFeatureSymbol.StandardVersion = ProSymbolUtilities.StandardString;

                //Load into editing???

                NotifyPropertyChanged(() => EditSelectedFeatureSymbol);
            }
        }

        #endregion

        #region Feature Data and Map Getters/Setters

        public GeometryType GeometryType { get; set; }

        public ObservableCollection<CoordinateObject> PolyCoordinates { get; set; }
        public ObservableCollection<string> SelectedStyleTags { get; set; }
        public ObservableCollection<string> SelectedFavoriteStyleTags { get; set; }
        public ObservableCollection<SelectedFeature> SelectedFeaturesCollection { get; set; }

        public bool PointCoordinateValid
        {
            get
            {
                return _coordinateValid;
            }
            set
            {
                _coordinateValid = value;
                NotifyPropertyChanged(() => PointCoordinateValid);
            }
        }

        public ArcGIS.Core.Geometry.Geometry MapGeometry
        {
            get
            {
                return _mapCoordinates;
            }
            set
            {
                _mapCoordinates = value;
                NotifyPropertyChanged(() => MapGeometry);
            }
        }

        public string MapPointCoordinatesString
        {
            get
            {
                return _mapCoordinatesString;
            }
            set
            {
                _mapCoordinatesString = value;

                MapPoint point;
                var coordType = ProSymbolUtilities.GetCoordinateType(_mapCoordinatesString, out point);

                if (coordType == CoordinateType.Unknown)
                {
                    //Error
                    MapGeometry = null;
                    PointCoordinateValid = false;
                }
                else
                {
                    MapGeometry = point;
                    PointCoordinateValid = true;
                }

                NotifyPropertyChanged(() => MapPointCoordinatesString);
            }
        }

        public SymbolAttributeSet SymbolAttributeSet
        {
            get
            {
                _symbolAttributeSet.StandardVersion = ProSymbolUtilities.StandardString;

                return _symbolAttributeSet;
            }
        }

        public MilitaryFieldsInspectorModel MilitaryFieldsInspectorModel
        {
            get
            {
                return _militaryFieldsInspectorModel;
            }
        }

        public Visibility PointCoordinateVisibility
        {
            get
            {
                return _pointCoordinateVisibility;
            }
            set
            {
                _pointCoordinateVisibility = value;
                NotifyPropertyChanged(() => PointCoordinateVisibility);
            }
        }

        public Visibility PolyCoordinateVisibility
        {
            get
            {
                return _polyCoordinateVisibility;
            }
            set
            {
                _polyCoordinateVisibility = value;
                NotifyPropertyChanged(() => PolyCoordinateVisibility);
            }
        }

        public bool AddToMapToolEnabled
        {
            get
            {
                return _addToMapToolEnabled;
            }
            set
            {
                _addToMapToolEnabled = value;
                NotifyPropertyChanged(() => AddToMapToolEnabled);
            }
        }

        public bool SelectToolEnabled
        {
            get
            {
                return _selectToolEnabled;
            }
            set
            {
                _selectToolEnabled = value;
                NotifyPropertyChanged(() => SelectToolEnabled);
            }
        }

        public void ClearFeatureSelection()
        {
            SelectedFeaturesCollection.Clear();

            IsEditing = false;
        }
        #endregion

        #region Command Methods

        private System.IDisposable _overlayObject = null;

        private void RemoveCoordinateMarker()
        {
            if (_overlayObject == null)
                return;

            _overlayObject.Dispose();
            _overlayObject = null;
        }

        private async void MarkCoordinateOnMap(object parameter)
        {
            if (MapView.Active == null) // should not happen
                return;

            MapPoint markerPoint = null;

            if (MapGeometry is MapPoint)
            {
                markerPoint = MapGeometry as MapPoint;
            }
            else if ((PolyCoordinates != null) && (PolyCoordinates.Count > 1))
            {
                // Use the last point in the collection to mark/zoom to
                CoordinateObject coordObject = PolyCoordinates.Last();

                markerPoint = coordObject.MapPoint;
            }

            if (markerPoint == null)  // should not happen, but last check 
                return;

            await QueuedTask.Run(() =>
            {
                RemoveCoordinateMarker();

                var coordinateMarker = ArcGIS.Desktop.Mapping.SymbolFactory.Instance.ConstructMarker(ArcGIS.Desktop.Mapping.ColorFactory.Instance.RedRGB, 12,
                    ArcGIS.Desktop.Mapping.SimpleMarkerStyle.Circle);

                ArcGIS.Core.CIM.CIMPointSymbol _pointCoordSymbol =
                    ArcGIS.Desktop.Mapping.SymbolFactory.Instance.ConstructPointSymbol(coordinateMarker);

                _overlayObject = MapView.Active.AddOverlay(markerPoint, _pointCoordSymbol.MakeSymbolReference());

                MapView.Active.ZoomTo(markerPoint);
            });
        }

        private void ActivateDrawFeatureSketchTool(object parameter)
        {
            FrameworkApplication.SetCurrentToolAsync("ProSymbolEditor_DrawFeatureSketchTool");
            AddToMapToolEnabled = true;
        }

        private void ActivateSelectTool(object parameter)
        {
            if (FrameworkApplication.CurrentTool == "ProSymbolEditor_SelectionMapTool")
            {
                // Clear the map selection
                ProSymbolUtilities.ClearMapSelection();

                ClearFeatureSelection();

                SelectToolEnabled = false;

                FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
            }
            else
            {
                SelectToolEnabled = true;

                // If selection tool already active, turn this tool off, by switching to default map tool
                FrameworkApplication.SetCurrentToolAsync("ProSymbolEditor_SelectionMapTool");
            }
        }

        private void ShowAboutWindow(object parameter)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog(FrameworkApplication.Current.MainWindow);
        }

        // Async version called when schema is not found 
        private async Task<bool> ShowSettingsWindowAsync(bool initialConfiguration)
        {
            bool success = false;

            // ensure called on UI thread 
            await Application.Current.Dispatcher.Invoke(async() =>
            {
                success = await ShowSettingsWindow(initialConfiguration);
            });

            IsAddinEnabled = success;

            return success;
        }

        // Relay Command version called on button click
        private async void ShowSettingsWindow(object parameter)
        {
            // If this is shown on start, when the addin is first enabled
            // enable some extra features (setting the database, adding the layer package, etc.)
            bool initialConfiguration = false;
            if (parameter != null)
                initialConfiguration = (bool)parameter;

            bool success = false;

            await Application.Current.Dispatcher.Invoke(async () =>
            {
                success = await ShowSettingsWindow(initialConfiguration);
            });

            IsAddinEnabled = success;
        }

        private async Task<bool> ShowSettingsWindow(bool initialConfiguration)
        { 
            string previousDefaultDatabase = ProSymbolEditorModule.Current.
            MilitaryOverlaySchema.DatabaseName;
            bool enabledWithPreviousStandard = false;
            if (initialConfiguration || !string.IsNullOrEmpty(previousDefaultDatabase))
                enabledWithPreviousStandard = await ProSymbolEditorModule.Current.
                    MilitaryOverlaySchema.ShouldAddInBeEnabledAsync();

            ProSymbolUtilities.SupportedStandardsType previousSettingStandard =
                ProSymbolUtilities.Standard;

            bool isSettingsReadOnly = false;
            if (enabledWithPreviousStandard && !initialConfiguration)
            {
                string message = "This project already contains a database with schema for standard: " + 
                    ProSymbolUtilities.GetStandardLabel(previousSettingStandard) + ".\n" +
                    "Please create a new project to change these settings.";
                MessageBoxResult result = 
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, 
                    "Application Settings Read-Only", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                isSettingsReadOnly = true;
            }

            if (!isSettingsReadOnly)
            {
                // set this status in case user cancels any of this setup at start
                StatusMessage = "Addin Not Enabled";
            }

            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.IsSettingsReadOnly = isSettingsReadOnly;
            settingsWindow.Standard = previousSettingStandard;
            settingsWindow.DefaultDatabase = previousDefaultDatabase;
            settingsWindow.IsSelectDBEnabled = !isSettingsReadOnly;

            settingsWindow.ShowDialog(FrameworkApplication.Current.MainWindow);

            bool enabledWithNewStandard = false;

            // If Settings Dialog cancelled - or read-only - return
            if (isSettingsReadOnly)
                return true;
            if (settingsWindow.DialogResult != true)
                return false; 

            ProSymbolUtilities.SupportedStandardsType newSettingStandard =
                settingsWindow.Standard;

            string newDatabase = settingsWindow.DefaultDatabase;

            if (settingsWindow.DefaultDatabaseChanged)
            {
                var currentItem = ItemFactory.Instance.Create(newDatabase);

                // Check item is GDB
                if (!(currentItem is GDBProjectItem))
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        "Could not open Database: " + newDatabase,
                        "Could Not Open Database",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }

                // Ensure database is added to the project if not present
                GDBProjectItem existingGdbProjectItem =
                    Project.Current.GetItems<GDBProjectItem>().FirstOrDefault(
                    item => item.Path.Equals(newDatabase, StringComparison.CurrentCultureIgnoreCase));

                if (existingGdbProjectItem == null)
                {
                    // If not currently in the project - add it
                    await QueuedTask.Run(() => Project.Current.AddItem(currentItem as IProjectItem));

                    // Then save the project with the new GDB package added
                    ProSymbolUtilities.SaveProject();
                }

                enabledWithNewStandard =
                    await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync(newDatabase, newSettingStandard);

                if (enabledWithNewStandard)
                {
                    StatusMessage = "Database Added";
                }
                else
                {
                    // Need to do an additional check if this database already contains a 
                    // military overlay 
                    bool dbAlreadyContainsMilitaryOverlay =
                        await ProSymbolEditorModule.Current.MilitaryOverlaySchema.
                            GDBContainsMilitaryOverlay(currentItem as GDBProjectItem);

                    if (dbAlreadyContainsMilitaryOverlay)
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                            "Database: " + newDatabase + "\n" +
                            "already contains a schema for Military Overlay." + "\n" +
                            "Please select a different database from Addin Settings.",
                            "Unable to Select Database", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                        // Remove the item added above
                        await QueuedTask.Run(() => Project.Current.RemoveItem(currentItem as IProjectItem));

                        // Then save the project with the new GDB package added
                        ProSymbolUtilities.SaveProject();

                        return false;
                    }

                    // We need to make this the project default database so layer package unpacks here:
                    await QueuedTask.Run(() => Project.Current.SetDefaultGeoDatabasePath(newDatabase) );
                }
            } // if DefaultDatabaseChanged
            else
            {
                enabledWithNewStandard = await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync(newSettingStandard);
            }

            ProSymbolUtilities.Standard = newSettingStandard;

            if (!enabledWithNewStandard)
            {
                var result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    "A military overlay schema matching standard: " + "\n" +
                    ProSymbolUtilities.GetStandardLabel(newSettingStandard) + "\n" +
                    "will be added to database: " + 
                    newDatabase + "\n" +
                    "Note: this may take several minutes."
                    , "Adding Schema to Database",
                    MessageBoxButton.OKCancel, MessageBoxImage.Information);

                if (Convert.ToString(result) == "Cancel")
                {
                    return false;
                }

                // Unpack layer package to the default GDB and add layers
                bool success = await AddLayerPackageToMapAsync();
                if (!success)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        "Unable to add layer package to map.\n" +
                        "Please try again from Addin Settings when map available.",
                        "Unable to Add Layer Package to Map", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return false;
                }

                // Save the project with the layer package added
                ProSymbolUtilities.SaveProject();
                enabledWithNewStandard = true;

                StatusMessage = "Military Layers Added";
            }

            if (!enabledWithNewStandard)
            {
                StatusMessage = "Addin Not Enabled";
                return false;
            }

            // Reset everything when standard changed
            resetViewModelState();

            // Save settings (or TODO: or do this in close/unload):
            Properties.Settings.Default.DefaultStandard =
                ProSymbolUtilities.GetStandardString(ProSymbolUtilities.Standard);
            Properties.Settings.Default.Save();

            return true;
        }

        private async void SaveEdits(object parameter)
        {
            string message = String.Empty;
            bool modificationResult = false;
            ArcGIS.Core.Geometry.Geometry savedGeometry = null;

            IEnumerable<GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();
            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                try
                {
                    foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                    {
                        using (Datastore datastore = gdbProjectItem.GetDatastore())
                        {
                            //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                            if (datastore is UnknownDatastore)
                                continue;

                            Geodatabase geodatabase = datastore as Geodatabase;
                            if (geodatabase == null)
                                continue;

                            //Find the correct gdb for the one with the complete schema
                            string geodatabasePath = gdbProjectItem.Path; 
                            if (geodatabasePath == ProSymbolEditorModule.Current.MilitaryOverlaySchema.DatabaseName)
                            {
                                EditOperation editOperation = new EditOperation();
                                editOperation.Callback(context =>
                                {
                                    string oidFieldName = _selectedSelectedFeature.FeatureLayer.GetTable().GetDefinition().GetObjectIDField();
                                    QueryFilter queryFilter = new QueryFilter();
                                    queryFilter.WhereClause = string.Format("{0} = {1}", oidFieldName, _selectedSelectedFeature.ObjectId);

                                    using (RowCursor cursor = _selectedSelectedFeature.FeatureLayer.GetTable().Search(queryFilter, false))
                                    {
                                        while (cursor.MoveNext())
                                        {
                                            Feature feature = (Feature)cursor.Current;

                                            // In order to update the Map and/or the attribute table.
                                            // Has to be called before any changes are made to the row
                                            context.Invalidate(feature);

                                            _symbolAttributeSet.PopulateFeatureWithAttributes(ref feature);

                                            feature.Store();

                                            savedGeometry = feature.GetShape();

                                            // Has to be called after the store too
                                            context.Invalidate(feature);

                                        }
                                    }
                                }, _selectedSelectedFeature.FeatureLayer.GetTable());

                                var task = editOperation.ExecuteAsync();
                                modificationResult = task.Result;
                                if (!modificationResult)
                                    message = editOperation.ErrorMessage;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    message = exception.ToString();
                    System.Diagnostics.Debug.WriteLine(message);
                }
            });

            if (modificationResult)
            {
                // Reselect the saved feature (so UI is updated and feature flashed)
                if (savedGeometry != null)
                {
                    await QueuedTask.Run(() =>
                    {
                        MapView.Active.SelectFeatures(savedGeometry, SelectionCombinationMethod.New);
                    });
                }
            }
            else
            {
                // Something went wrong, alert user           
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message);
            }
        }
            
        private async void SearchStylesAsync(object parameter)
        {
            //Make sure that military style is in project
            if (!IsStyleInProject() || _militaryStyleItem == null)
            {
                if (!File.Exists(Mil2525StyleFullFilePath))
                {
                    ShowMilitaryStyleNotFoundMessageBox();
                    return;
                }
                else
                {
                    //Add military style to project
                    Task<StyleProjectItem> getMilitaryStyle = GetMilitaryStyleAsync();
                    _militaryStyleItem = await getMilitaryStyle;
                }
            }

            //Clear for new search
            if (_styleItems.Count != 0)
                _styleItems.Clear();

            ResultCount = "---";

            _progressDialogSearch.Show();
            await SearchSymbols();

            StatusMessage = "Search Complete";

            NotifyPropertyChanged(() => StyleItems);

            if (_styleItems.Count > 0)
            {
                // Select the first item returned
                SelectedStyleItem = _styleItems[0];
                NotifyPropertyChanged(() => SelectedStyleItem);
            }
        }

        private void GoToTab(object parameter)
        {
            SelectedTabIndex = Convert.ToInt32(parameter);
        }

        public async void CreateNewFeatureAsync(object parameter)
        {
            RemoveCoordinateMarker();

            string message = String.Empty;
            bool creationResult = false;

            // TODO: may need to enable this 
            // Check again for schema just in case user said "No" at Add Schema Form
            // ShowAddInNotEnabledMessageBox();

            // WARNING HERE IF: the feature class is in the Project BUT *NOT* in Active Map/View
            bool isLayerInActiveView = await 
                ProSymbolEditorModule.Current.MilitaryOverlaySchema.IsGDBAndFeatureClassInActiveView(
                    _currentFeatureClassName);

            if (!isLayerInActiveView)
            {
                string requiredLayerName = _currentFeatureClassName;
                if (string.IsNullOrEmpty(requiredLayerName))
                    requiredLayerName = "{Layer Not Found}";

                // Could not find layer in map - ask to re-add it
                string warningMessage = "The required layer is not in the Active Map. \n" +
                    "Required layer: " + requiredLayerName + ".\n" +
                    "Add this military overlay layer to the map?";
                Debug.WriteLine(warningMessage);
                MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(warningMessage, 
                    "Add Layer to Map?", MessageBoxButton.YesNoCancel, 
                    MessageBoxImage.Exclamation);

                bool continueWithAdd = false;
                if (result.ToString() == "Yes")
                {
                    continueWithAdd = await ProSymbolEditorModule.Current.MilitaryOverlaySchema.AddFeatureClassToActiveView(_currentFeatureClassName);
                }

                // If user cancelled, or unable to add layer provide warning 
                if (!continueWithAdd)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        "Could not create map feature in layer: " + requiredLayerName,
                        "Could Not Create Map Feature", MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);

                    return;
                }
            }

            // Generate geometry if polygon or polyline, if adding new feature is from using coordinates and not the map tool
            if (Convert.ToBoolean(parameter) == true)
            {
                if (GeometryType == GeometryType.Polyline || GeometryType == GeometryType.Polygon)
                {
                    GeneratePolyGeometry();
                }
            }

            IEnumerable<GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();
            await QueuedTask.Run(async () =>
            {
                foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                {
                    using (Datastore datastore = gdbProjectItem.GetDatastore())
                    {
                        //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                        if (datastore is UnknownDatastore)
                            continue;
                        Geodatabase geodatabase = datastore as Geodatabase;

                        // Should not happen, since only looping though gdb project items, but just in case
                        if (geodatabase == null)
                            continue;

                        //Find the correct gdb for the one with the complete schema
                        string geodatabasePath = gdbProjectItem.Path;
                        if (geodatabasePath == ProSymbolEditorModule.Current.MilitaryOverlaySchema.DatabaseName)
                        {
                            //Correct GDB, open the current selected feature class
                            FeatureClass featureClass = geodatabase.OpenDataset<FeatureClass>(_currentFeatureClassName);
                            using (featureClass)
                            using (FeatureClassDefinition facilitySiteDefinition = featureClass.GetDefinition())
                            {
                                EditOperation editOperation = new EditOperation();
                                editOperation.Name = "Military Symbol Insert";
                                editOperation.Callback(context =>
                                {
                                    try
                                    {
                                        RowBuffer rowBuffer = featureClass.CreateRowBuffer();
                                        _symbolAttributeSet.PopulateRowBufferWithAttributes(ref rowBuffer);
                                        rowBuffer["Shape"] = GeometryEngine.Instance.Project(MapGeometry, facilitySiteDefinition.GetSpatialReference());

                                        Feature feature = featureClass.CreateRow(rowBuffer);
                                        feature.Store();

                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature);
                                    }
                                    catch (GeodatabaseException geodatabaseException)
                                    {
                                        message = geodatabaseException.Message;
                                    }
                                }, featureClass);

                                creationResult = await editOperation.ExecuteAsync();

                                if (!creationResult)
                                {
                                    message = editOperation.ErrorMessage;
                                }

                                break;
                            }
                        }
                    }
                }
            });

            if (!creationResult)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message);
            }
        }

        private bool CanCreatePolyFeatureFromCoordinates()
        {
            if (GeometryType == GeometryType.Polyline)
            {
                if (PolyCoordinates.Count < 2)
                {
                    return false;
                }
            }

            if (GeometryType == GeometryType.Polygon)
            {
                if (PolyCoordinates.Count < 3)
                {
                    return false;
                }
            }

            if (GeometryType == GeometryType.Point)
            {
                return PointCoordinateValid;
            }

            foreach(CoordinateObject coordObject in PolyCoordinates)
            {
                if (!coordObject.IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private BitmapSource GetClipboardImage(BitmapSource sourceImage)
        {
            if (sourceImage == null)
                return null;

            //There's an issue copying the image directly to the clipboard, where transparency isn't retained, and will have a black background.
            //The code below will switch that to be a pseudo-transparency with a white background.
            Size size = new Size(sourceImage.Width, sourceImage.Height);

            // Create a white background render bitmap
            int dWidth = (int)size.Width;
            int dHeight = (int)size.Height;
            int dStride = dWidth * 4;
            byte[] pixels = new byte[dHeight * dStride];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = 0xFF;
            }
            BitmapSource bg = BitmapSource.Create(
                dWidth,
                dHeight,
                96,
                96,
                PixelFormats.Pbgra32,
                null,
                pixels,
                dStride
            );

            // Adding those two render bitmap to the same drawing visual
            DrawingVisual dv = new DrawingVisual();

            // Important/Workaround: The image is rotated 180, see: SymbolAttributeSet.GetBitmapImageAsync
            dv.Transform = new System.Windows.Media.RotateTransform(180.0, size.Width / 2, size.Height / 2);

            DrawingContext dc = dv.RenderOpen();
            dc.DrawImage(bg, new Rect(size));
            dc.DrawImage(sourceImage, new Rect(size));
            dc.Close();

            // Render the result
            RenderTargetBitmap resultBitmap =
                new RenderTargetBitmap(
                (int)size.Width,
                (int)size.Height,
                96d,
                96d,
                PixelFormats.Pbgra32
            );
            resultBitmap.Render(dv);

            return resultBitmap;
        }

        private void SaveImageAs(object parameter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "symbol";
            saveFileDialog.Filter = "Png Image|*.png";
            Nullable<bool> result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                BitmapSource bitmap = GetClipboardImage(SymbolAttributeSet.SymbolImage);

                if (bitmap == null)
                    return;

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var stream = saveFileDialog.OpenFile())
                {
                    encoder.Save(stream);
                }
            }
        }

        private void CopyImageToClipboard(object parameter)
        {
            try
            {
                BitmapSource bitmap = GetClipboardImage(SymbolAttributeSet.SymbolImage);

                // Copy to clipboard
                if (bitmap != null)
                    Clipboard.SetImage(bitmap);
            }
            catch(Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.ToString());
            }
        }

        /// <summary>
        /// Method that will load either a favorite symbol or a feature that's already been created into the add-in
        /// to allow users to edid the symbol through the workflow.
        /// </summary>
        /// <param name="isEditSymbol">If the symbol to load is a selected edit symbol.  If false, it will load a selected favorite.</param>
        private void LoadSymbolIntoWorkflow(bool isEditSymbol)
        {
            //Load the currently selected favorite
            SymbolAttributeSet loadSet;

            if (isEditSymbol)
            {
                loadSet = _editSelectedFeatureSymbol;
            }
            else
            {
                loadSet = _selectedFavoriteSymbol;
            }

            //Clear old attributes
            _symbolAttributeSet.ResetAttributes();
            SelectedStyleTags.Clear();

            if (loadSet != null)
            {
                //Tokenize tags (for favorites, edit symbols don't have any)
                if (!isEditSymbol)
                {
                    string geometryTypeTag = "POINT";

                    if (!string.IsNullOrEmpty(loadSet.SymbolTags))
                    {
                        foreach (string tag in loadSet.SymbolTags.Split(';').ToList())
                        {
                            SelectedStyleTags.Add(tag);
                        }

                        //Get the geometry type off a tag on the symbol
                        List<string> reverseTags = loadSet.SymbolTags.Split(';').ToList();
                        reverseTags.Reverse();

                        if (reverseTags.Count >= 2)
                            geometryTypeTag = reverseTags[2];
                    }

                    if (geometryTypeTag.ToUpper() == "LINE")
                    {
                        GeometryType = GeometryType.Polyline;
                        PointCoordinateVisibility = Visibility.Collapsed;
                        PolyCoordinateVisibility = Visibility.Visible;
                    }
                    else if (geometryTypeTag.ToUpper() == "AREA")
                    {
                        GeometryType = GeometryType.Polygon;
                        PointCoordinateVisibility = Visibility.Collapsed;
                        PolyCoordinateVisibility = Visibility.Visible;
                    }
                    else // "POINT"
                    {
                        GeometryType = GeometryType.Point;
                        PointCoordinateVisibility = Visibility.Visible;
                        PolyCoordinateVisibility = Visibility.Collapsed;
                    }

                    IsEditing = false;
                }
                else
                {
                    //Get geometry from selected selected layer
                    if (SelectedSelectedFeature.FeatureLayer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryPoint)
                    {
                        GeometryType = GeometryType.Point;
                        PointCoordinateVisibility = Visibility.Visible;
                        PolyCoordinateVisibility = Visibility.Collapsed;
                    }
                    else if ((SelectedSelectedFeature.FeatureLayer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryLine) ||
                        (SelectedSelectedFeature.FeatureLayer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryPolyline))
                    {
                        GeometryType = GeometryType.Polyline;
                        PointCoordinateVisibility = Visibility.Collapsed;
                        PolyCoordinateVisibility = Visibility.Visible;
                    }
                    else if (SelectedSelectedFeature.FeatureLayer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryPolygon)
                    {
                        GeometryType = GeometryType.Polygon;
                        PointCoordinateVisibility = Visibility.Collapsed;
                        PolyCoordinateVisibility = Visibility.Visible;
                    }
                    else
                    {
                        //Other geometry type, so use as a point
                        GeometryType = GeometryType.Point;
                        PointCoordinateVisibility = Visibility.Visible;
                        PolyCoordinateVisibility = Visibility.Collapsed;
                    }

                    IsEditing = true;
                }

                //Get feature class name to generate domains
                SymbolAttributeSet.DisplayAttributes.SymbolSet = loadSet.DisplayAttributes.SymbolSet;
                SymbolAttributeSet.DisplayAttributes.SymbolEntity = loadSet.DisplayAttributes.SymbolEntity;

                SymbolAttributeSet.DisplayAttributes.ExtendedFunctionCode = loadSet.DisplayAttributes.ExtendedFunctionCode;

                _currentFeatureClassName = 
                    _symbolSetMappings.GetFeatureClassFromMapping(
                    _symbolAttributeSet.DisplayAttributes, GeometryType);

                if (!string.IsNullOrEmpty(_currentFeatureClassName))
                {
                    //Generate domains and pass in set to update values initially
                    GetMilitaryDomainsAsync(loadSet);
                }

                IsStyleItemSelected = true;

                //Set label values (that are not combo boxes)
                SymbolAttributeSet.LabelAttributes.DateTimeValid = loadSet.LabelAttributes.DateTimeValid;
                SymbolAttributeSet.LabelAttributes.DateTimeExpired = loadSet.LabelAttributes.DateTimeExpired;
                SymbolAttributeSet.LabelAttributes.Type = loadSet.LabelAttributes.Type;
                SymbolAttributeSet.LabelAttributes.CommonIdentifier = loadSet.LabelAttributes.CommonIdentifier;
                SymbolAttributeSet.LabelAttributes.Speed = loadSet.LabelAttributes.Speed;
                SymbolAttributeSet.LabelAttributes.UniqueDesignation = loadSet.LabelAttributes.UniqueDesignation;
                SymbolAttributeSet.LabelAttributes.StaffComments = loadSet.LabelAttributes.StaffComments;
                SymbolAttributeSet.LabelAttributes.AdditionalInformation = loadSet.LabelAttributes.AdditionalInformation;
                SymbolAttributeSet.LabelAttributes.HigherFormation = loadSet.LabelAttributes.HigherFormation;
                SymbolAttributeSet.SymbolTags = loadSet.SymbolTags;

                SymbolAttributeSet.StandardVersion = ProSymbolUtilities.StandardString;
            }
        }

        private void SaveSymbolAsFavorite(object parameter)
        {
            try
            {
                // If it is not a valid or exportable symbol error+return
                if ((SymbolAttributeSet == null) || !SymbolAttributeSet.IsValid)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("The current favorite is not valid.", 
                    "Invalid Favorite", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (Favorites.Contains(SymbolAttributeSet))
                {
                    throw new Exception("Favorite already exists.");
                }

                SymbolAttributeSet.FavoriteId = Guid.NewGuid().ToString();
                //Create copy by serializing/deserializing
                var json = new JavaScriptSerializer().Serialize(SymbolAttributeSet);
                SymbolAttributeSet favoriteSet = new JavaScriptSerializer().Deserialize<SymbolAttributeSet>(json);

                //Add to favorites
                if (favoriteSet == null) // should not happen
                    throw new Exception("Could not create Favorite.");

                favoriteSet.GeneratePreviewSymbol();
                Favorites.Add(favoriteSet);

                //Serialize Favorites and save to file
                var favoritesJson = new JavaScriptSerializer().Serialize(Favorites);
                File.WriteAllText(_favoritesFilePath, favoritesJson);
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Unable to add the current favorite. " + ex.Message, "Error Adding Favorite", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void DeleteFavoriteSymbol(object parameter)
        {
            if (SelectedFavoriteSymbol != null)
            {
                Favorites.Remove(SelectedFavoriteSymbol);

                //Serialize Favorites and save to file
                var favoritesJson = new JavaScriptSerializer().Serialize(Favorites);
                File.WriteAllText(_favoritesFilePath, favoritesJson);
            }
        }

        private async void CreateTemplateFromFavorite(object parameter)
        {
            bool success = true;

            if (SelectedFavoriteSymbol == null)
            {
                success = false; // error
            }

            GeometryType geometryType = ProSymbolUtilities.TagsToGeometryType(SelectedFavoriteSymbol.SymbolTags);

            //Get required layer name
            string requiredFeatureClassName = _symbolSetMappings.GetFeatureClassFromMapping(
                SelectedFavoriteSymbol.DisplayAttributes, geometryType);

            if (string.IsNullOrEmpty(requiredFeatureClassName))
            {
                success = false; // error
            }

            await QueuedTask.Run(() =>
            {
                ////////////////////////////////
                // Move to Utility
                IEnumerable<FeatureLayer> mapLayers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>(); ;

                FeatureLayer targetLayer = null;
                foreach (var layer in mapLayers)
                {
                    string fcName = layer.GetFeatureClass().GetName();

                    if (fcName == requiredFeatureClassName)
                    {
                        targetLayer = layer;
                        break;
                    }
                }
                ////////////////////////////////

                if (targetLayer == null)
                {
                    success = false; // error
                    return;
                }

                // Now add the new template:

                string templateName = SelectedFavoriteSymbol.Name;

                // Check if a template with this name already exists (if so modify?)
                var checkTemplate = targetLayer.GetTemplate(templateName);

                // Get CIM layer definition
                var layerDef = targetLayer.GetDefinition() as CIMFeatureLayer;

                if (layerDef == null)
                {
                    success = false; // error
                    return;
                }

                // Get all templates on this layer
                var layerTemplates = layerDef.FeatureTemplates.ToList();
               
                // Create a new template
                var newTemplate = new CIMFeatureTemplate();

                //Set template values
                newTemplate.Name = templateName;
                newTemplate.Description = templateName;
                newTemplate.WriteTags(SelectedFavoriteSymbol.SymbolTags.Split(';').ToList());
                newTemplate.DefaultValues = SelectedFavoriteSymbol.GenerateAttributeSetDictionary();

                // Add the new template to the layer template list
                layerTemplates.Add(newTemplate);

                // Set the layer definition templates from the list
                layerDef.FeatureTemplates = layerTemplates.ToArray();
                // Finally set the layer definition
                targetLayer.SetDefinition(layerDef);
            });

            if (!success)
            {
                MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Could Not Create Template", 
                    "Could Not Create Template", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }

        private void SaveFavoritesAsToFile(object parameter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "favorites";
            saveFileDialog.Filter = "JSON|*.json";
            Nullable<bool> result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                var favoritesJson = new JavaScriptSerializer().Serialize(Favorites);
                File.WriteAllText(saveFileDialog.FileName, favoritesJson);
            }
        }

        private void ImportFavoritesFile(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json";
            if (openFileDialog.ShowDialog() == true)
            {
                if (Path.GetExtension(openFileDialog.FileName).ToUpper() == ".JSON")
                {
                    string json = File.ReadAllText(openFileDialog.FileName);

                    ObservableCollection<SymbolAttributeSet> importedFavorites = new JavaScriptSerializer().Deserialize<ObservableCollection<SymbolAttributeSet>>(json);

                    //Go through favorites, find if uid is already in favorites - if so, replace that favorite
                    //If not found, add favorite
                    foreach (SymbolAttributeSet set in importedFavorites)
                    {
                        foreach (SymbolAttributeSet favSet in Favorites)
                        {
                            if (favSet.FavoriteId == set.FavoriteId)
                            {
                                //Match found, remove found
                                Favorites.Remove(favSet);
                                break;
                            }
                        }

                        set.GeneratePreviewSymbol();
                        Favorites.Add(set);
                    }

                    //Re-serialize to save the imported favorites
                    var favoritesJson = new JavaScriptSerializer().Serialize(Favorites);
                    File.WriteAllText(_favoritesFilePath, favoritesJson);
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("The import file you selected is invalid - please choose a valid JSON file.");
                }
            }
        }

        private void ClearSearchText(object parameter)
        {
            SearchString = string.Empty;

            ClearSearch();
        }

        private void ClearSearch()
        {
            SelectedStyleTags.Clear();
            SelectedStyleItem = null;
            IsStyleItemSelected = false;
            StyleItems.Clear();

            ResultCount = "---";
            // HACK:
            // NotifyPropertyChanged(() => StyleItems);
            // StyleItems list update was not updating the view, 
            // not sure why this bound property is not updating the UI
            // TODO: test switching this to an ObservableCollection
            // Force the tab to be redrawn to workaround the issue
            SelectedTabIndex = 1;
            SelectedTabIndex = 0;
            // END HACK
        }

        #endregion

        #region Event Listeners

        private void OnActiveToolChanged(ArcGIS.Desktop.Framework.Events.ToolEventArgs args)
        {
            if (args.CurrentID == "ProSymbolEditor_DrawFeatureSketchTool")
            {
                //Toggle all down
                AddToMapToolEnabled = true;
                SelectToolEnabled = false;

                // Just in case this tool has been activated from the favorites tab
                // in a new project, check for the required data model
                ShowAddInNotEnabledMessageBox();
            }
            else if (args.CurrentID == "ProSymbolEditor_SelectionMapTool")
            {
                SelectToolEnabled = true;
                AddToMapToolEnabled = false;
            }
            else
            {
                //Disable all toggles
                AddToMapToolEnabled = false;
                SelectToolEnabled = false;
            }
        }

        private Task<ArcGIS.Desktop.Mapping.Events.LayersRemovingEventArgs> 
            OnLayersRemoving(ArcGIS.Desktop.Mapping.Events.LayersRemovingEventArgs args)
        {
            foreach (var layer in args.Layers)
            {
                if (layer == null) continue;

                if (!string.IsNullOrEmpty(layer.Name) && layer.Name.StartsWith("Military Overlay"))
                {
                    string warningMessage = "Removing the required Military Overlay layers will reset the Military Symbol Editor.\n" + 
                        "Continue?";
                    Debug.WriteLine(warningMessage);
                    MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(warningMessage, 
                        "Remove Military Overlay?", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                    if (result.ToString() == "Yes")
                    {
                        // Workaround: this event is being called on layer remove
                        ArcGIS.Desktop.Mapping.Events.MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);

                        resetViewModelState();
                    }
                    else
                    {
                        args.Cancel = true;
                        return Task.FromResult<ArcGIS.Desktop.Mapping.Events.LayersRemovingEventArgs>(args);
                    }
                }
            }

            args.Cancel = false;
            return Task.FromResult<ArcGIS.Desktop.Mapping.Events.LayersRemovingEventArgs>(args);
        }

        private void OnLayersRemoved(ArcGIS.Desktop.Mapping.Events.LayerEventsArgs args)
        {
            foreach (var layer in args.Layers)
            {
                if (layer == null) continue;

                if (!string.IsNullOrEmpty(layer.Name) && layer.Name.StartsWith("Military Overlay"))
                {
                    string warningMessage = "The required Military Overlay layers have been removed from the active map.\n" +
                    "The Military Symbol Editor has been reset.";
                    Debug.WriteLine(warningMessage);
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(warningMessage, "Military Overlay Removed", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    // Workaround: re-subscribe to this event
                    ArcGIS.Desktop.Mapping.Events.MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
                }
            }
        }

        private async void OnMapSelectionChanged(ArcGIS.Desktop.Mapping.Events.MapSelectionChangedEventArgs args)
        {
            // Only allow selection event if addin enabled
            if (!IsAddinEnabled)
                return;

            // And data model present
            bool isSchemaPresent = await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync();
            if (!isSchemaPresent)
                return;

            //Get the selected features from the map and filter out the standalone table selection.
            var selectedFeatures = args.Selection
              .Where(kvp => kvp.Key is BasicFeatureLayer)
              .ToDictionary(kvp => (BasicFeatureLayer)kvp.Key, kvp => kvp.Value);

            if (selectedFeatures.Count < 1)
            {
                ClearFeatureSelection();
                return;
            }

            SelectedFeaturesCollection.Clear();

            string symbolSetFieldName = "symbolset";
            string symbolEntityFieldName = "symbolentity";

            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            {
                symbolSetFieldName = "extendedfunctioncode";
                symbolEntityFieldName = ""; // not used
            }

            foreach (KeyValuePair<BasicFeatureLayer, List<long>> kvp in selectedFeatures)
            {
                await QueuedTask.Run(() =>
                {
                    ArcGIS.Core.Data.Field symbolSetField = kvp.Key.GetTable().GetDefinition().GetFields().FirstOrDefault(x => x.Name == symbolSetFieldName);
                    if (symbolSetField == null) 
                    {
                        // then feature does not have required field, skip
                        // Note: we used to issue a warning, but it was requested to remove
                        return;
                    }

                    CodedValueDomain symbolSetDomain = symbolSetField.GetDomain() as CodedValueDomain;
                    if (symbolSetDomain == null) // then field does not have domain
                        return;

                    SortedList<object, string> symbolSetDomainSortedList = symbolSetDomain.GetCodedValuePairs();

                    ArcGIS.Core.Data.Field symbolEntityField = null;
                    SortedList<object, string> symbolEntityDomainSortedList = null;

                    if (!string.IsNullOrEmpty(symbolEntityFieldName))
                    {
                        symbolEntityField = kvp.Key.GetTable().GetDefinition().GetFields().FirstOrDefault(x => x.Name == symbolEntityFieldName);
                        if (symbolEntityField == null) // then does not have required field
                            return;

                        CodedValueDomain symbolEntityDomain = symbolEntityField.GetDomain() as CodedValueDomain;
                        if (symbolEntityDomain != null)
                            symbolEntityDomainSortedList = symbolEntityDomain.GetCodedValuePairs();
                    }

                    foreach (long id in kvp.Value)
                    {
                        //Query for field values

                        string oidFieldName = kvp.Key.GetTable().GetDefinition().GetObjectIDField();
                        QueryFilter queryFilter = new QueryFilter();
                        queryFilter.WhereClause = string.Format("{0} = {1}", oidFieldName, id);
                        RowCursor cursor = kvp.Key.Search(queryFilter);
                        Row row = null;

                        if (cursor.MoveNext())
                        {
                            row = cursor.Current;
                        }

                        if (row != null)
                        {
                            SelectedFeature newSelectedFeature = new SelectedFeature(kvp.Key, id);

                            if ((row.FindField(symbolSetFieldName) >=0) &&
                                ( row[symbolSetFieldName] != null))
                            {
                                string symbolSetString = row[symbolSetFieldName].ToString();
                                foreach (KeyValuePair<object, string> symbolSetKeyValuePair in symbolSetDomainSortedList)
                                {
                                    if (symbolSetKeyValuePair.Key.ToString() == symbolSetString)
                                    {
                                        newSelectedFeature.SymbolSetName = symbolSetKeyValuePair.Value;
                                        break;
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(symbolEntityFieldName) && 
                                (row.FindField(symbolEntityFieldName) >= 0) &&
                                (row[symbolEntityFieldName] != null) &&                                  
                                (symbolEntityDomainSortedList !=null))
                            {
                                string symbolEntityString = row[symbolEntityFieldName].ToString();

                                foreach (KeyValuePair<object, string> symbolEntityKeyValuePair in symbolEntityDomainSortedList)
                                {
                                    if (symbolEntityKeyValuePair.Key.ToString() == symbolEntityString)
                                    {
                                        newSelectedFeature.EntityName = symbolEntityKeyValuePair.Value;
                                        break;
                                    }
                                }
                            }

                            SelectedFeaturesCollection.Add(newSelectedFeature);
                        }
                    } // for each id
                });
            }

            SelectedSelectedFeature = SelectedFeaturesCollection.FirstOrDefault();
        }

        private async Task CheckAddinEnabled()
        {
            if (!IsAddinEnabled)
            {
                await Task.FromResult<bool>(true);

                var result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    "The Military Symbol Editor requires the Military Overlay data model.\n" +
                    "Would you like to add the data model \n" +
                    "(database schema and layers to the TOC) to the project?. \n",
                    "Add-in Disabled",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Asterisk);

                if (Convert.ToString(result) == "Yes")
                {
                    bool success = await ShowSettingsWindowAsync(true);
                }
            }
        }

        public async void DockPanel_MouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            await CheckAddinEnabled();
        }
        #endregion

        private void ClearSearchSelection()
        {
            _selectedStyleItem = null;
            NotifyPropertyChanged(() => SelectedStyleItem);
        }

        #region Private Methods

        private async Task<StyleProjectItem> GetMilitaryStyleAsync()
        {
            StyleProjectItem style = null;

            if (!File.Exists(Mil2525StyleFullFilePath))
            {
                ShowMilitaryStyleNotFoundMessageBox();
            }
            else
            {
                if (Project.Current != null)
                {
                    await QueuedTask.Run(() =>
                    {
                        //Get all styles in the project
                        var styles = Project.Current.GetItems<StyleProjectItem>();

                        //Get the named military style in the project
                        style = styles.FirstOrDefault(x => x.Name == MilitaryStyleName);

                        if (style == null)
                        {
                            // add it, if it wasn't found
                            Project.Current.AddStyle(Mil2525StyleFullFilePath);

                            // then check again for style (just in case)
                            styles = Project.Current.GetItems<StyleProjectItem>();
                            style = styles.FirstOrDefault(x => x.Name == MilitaryStyleName);
                        }
                    });
                }
            }

            return style;
        }

        private bool IsStyleInProject()
        {
            if (Project.Current != null)
            {
                IEnumerable<StyleProjectItem> projectStyles = Project.Current.GetItems<StyleProjectItem>();

                foreach(StyleProjectItem projectStyle in projectStyles)
                {
                    if (projectStyle.Path == Mil2525StyleFullFilePath)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private async Task SetIdentityFromTags()
        {
            // TRICKY:
            // Set the Identity Combo Box based on the Style tag/name is it is implied by the tag/name

            //Check for affiliation tag or style item name to suss out the affiliation
            if (_selectedStyleItem == null)
                return;

            // These differ between 2525D & C_B2 schemas              
            string affiliationField = "identity";
            string hostileValue = "Hostile/Faker";
            string friendValue = "Friend";

            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
            {
                affiliationField = "affiliation";
                hostileValue = "Hostile";
                friendValue = "Friendly";
            }

            string identityCode = "";
            if (_selectedStyleItem.Tags.ToUpper().Contains("FRIEND") ||
                _selectedStyleItem.Name.ToUpper().Contains(": FRIEND"))
            {
                identityCode = await GetDomainValueAsync(affiliationField, friendValue);
            }
            else if (_selectedStyleItem.Tags.ToUpper().Contains("HOSTILE") ||
                _selectedStyleItem.Name.ToUpper().Contains(": HOSTILE"))
            {
                identityCode = await GetDomainValueAsync(affiliationField, hostileValue);
            }
            else if (_selectedStyleItem.Tags.ToUpper().Contains("NEUTRAL") ||
                _selectedStyleItem.Name.ToUpper().Contains(": NEUTRAL"))
            {
                identityCode = await GetDomainValueAsync(affiliationField, "Neutral");
            }
            else if (_selectedStyleItem.Tags.ToUpper().Contains("UNKNOWN") ||
                _selectedStyleItem.Name.ToUpper().Contains(": UNKNOWN"))
            {
                identityCode = await GetDomainValueAsync(affiliationField, "Unknown");
            }

            if (identityCode != "")
            {
                foreach (DomainCodedValuePair dcvp in MilitaryFieldsInspectorModel.IdentityDomainValues)
                {
                    if (dcvp.Code.ToString() == identityCode)
                    {
                        SymbolAttributeSet.DisplayAttributes.SelectedIdentityDomainPair = dcvp;
                        break;
                    }
                }
            }
        }

        private async void GetMilitaryDomainsAsync(SymbolAttributeSet loadSet = null)
        {
            try
            {
                IEnumerable<GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                    {
                        using (Datastore datastore = gdbProjectItem.GetDatastore())
                        {
                            //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                            if (datastore is UnknownDatastore)
                                    continue;
                            Geodatabase geodatabase = datastore as Geodatabase;

                            // Should not happen, since only looping though gdb project items, but just in case
                            if (geodatabase == null)
                                continue;

                            string geodatabasePath = gdbProjectItem.Path;
                            if (geodatabasePath == ProSymbolEditorModule.Current.MilitaryOverlaySchema.DatabaseName)
                            {
                                GeodatabaseType gdbType = geodatabase.GetGeodatabaseType();
                                if (gdbType == GeodatabaseType.RemoteDatabase)
                                {
                                    // if an SDE/EGDB, then feature class name format will differ:
                                    // Database. + User. + Feature Class Name 
                                    DatabaseConnectionProperties dbcps = geodatabase.GetConnector() as DatabaseConnectionProperties;
                                    if (dbcps != null)
                                    {
                                        _currentFeatureClassName = dbcps.Database + "." + dbcps.User + "." + _currentFeatureClassName;
                                    }
                                }

                                //Correct GDB, open the current selected feature class
                                _currentFeatureClass = geodatabase.OpenDataset<FeatureClass>(_currentFeatureClassName);
                                using (_currentFeatureClass)
                                {
                                    ArcGIS.Core.Data.FeatureClassDefinition facilitySiteDefinition = _currentFeatureClass.GetDefinition();
                                    IReadOnlyList<ArcGIS.Core.Data.Field> fields = facilitySiteDefinition.GetFields();

                                    MilitaryFieldsInspectorModel.PopulateDomains(fields);
                                    MilitaryFieldsInspectorModel.CheckLabelFieldsExistence(fields);
                                }

                                break;
                            }
                        }
                    }
                });

                await SetIdentityFromTags();

                //Load any passed in values to selected values for the domain combo boxes
                if (loadSet != null)
                {
                    // Only set the non-null properties of the loadset 
                    if (loadSet.DisplayAttributes.ExtendedFunctionCode != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedExtendedFunctionCodeDomainPair 
                            = MilitaryFieldsInspectorModel.ExtendedFunctionCodeValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.ExtendedFunctionCode);

                    if (loadSet.DisplayAttributes.Identity != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedIdentityDomainPair 
                            = MilitaryFieldsInspectorModel.IdentityDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Identity);

                    if (loadSet.DisplayAttributes.Echelon != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedEchelonDomainPair 
                            = MilitaryFieldsInspectorModel.EchelonDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Echelon);

                    if (loadSet.DisplayAttributes.Mobility != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedMobilityDomainPair 
                            = MilitaryFieldsInspectorModel.MobilityDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Mobility);

                    if (loadSet.DisplayAttributes.OperationalCondition != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedOperationalConditionDomainPair 
                            = MilitaryFieldsInspectorModel.OperationalConditionAmplifierDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.OperationalCondition);

                    if (loadSet.DisplayAttributes.Indicator != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedIndicatorDomainPair 
                            = MilitaryFieldsInspectorModel.TfFdHqDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Indicator);

                    if (loadSet.DisplayAttributes.Status != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedStatusDomainPair 
                            = MilitaryFieldsInspectorModel.StatusDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Status);

                    if (loadSet.DisplayAttributes.Context != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedContextDomainPair 
                            = MilitaryFieldsInspectorModel.ContextDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Context);

                    if (loadSet.DisplayAttributes.Modifier1 != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedModifier1DomainPair = 
                            MilitaryFieldsInspectorModel.Modifier1DomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Modifier1);

                    if (loadSet.DisplayAttributes.Modifier2 != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedModifier2DomainPair = 
                            MilitaryFieldsInspectorModel.Modifier2DomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Modifier2);

                    if (loadSet.LabelAttributes.Credibility != null)
                        SymbolAttributeSet.LabelAttributes.SelectedCredibilityDomainPair = 
                            MilitaryFieldsInspectorModel.CredibilityDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.Credibility);

                    if (loadSet.LabelAttributes.Reinforced != null)
                        SymbolAttributeSet.LabelAttributes.SelectedReinforcedDomainPair = 
                            MilitaryFieldsInspectorModel.ReinforcedDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.Reinforced);

                    if (loadSet.LabelAttributes.Reliability != null)
                        SymbolAttributeSet.LabelAttributes.SelectedReliabilityDomainPair = 
                            MilitaryFieldsInspectorModel.ReliabilityDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.Reliability);

                    if (loadSet.LabelAttributes.SelectedCountryCodeDomainPair != null)
                        SymbolAttributeSet.LabelAttributes.SelectedCountryCodeDomainPair = 
                            MilitaryFieldsInspectorModel.CountryCodeDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.CountryCode);
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.ToString());
            }
        }

        private async Task<string> GetDomainValueAsync(string fieldName, string key)
        {
            try
            {
                IEnumerable<GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();
                return await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                    {
                        using (Datastore datastore = gdbProjectItem.GetDatastore())
                        {
                            //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                            if (datastore is UnknownDatastore)
                                continue;
                            Geodatabase geodatabase = datastore as Geodatabase;

                            // Should not happen, since only looping though gdb project items, but just in case
                            if (geodatabase == null)
                                continue;

                            string geodatabasePath = gdbProjectItem.Path;
                            if (geodatabasePath == ProSymbolEditorModule.Current.MilitaryOverlaySchema.DatabaseName)
                            {
                                //Correct GDB, open the current selected feature class
                                _currentFeatureClass = geodatabase.OpenDataset<FeatureClass>(_currentFeatureClassName);
                                using (_currentFeatureClass)
                                {
                                    ArcGIS.Core.Data.FeatureClassDefinition facilitySiteDefinition = _currentFeatureClass.GetDefinition();
                                    IReadOnlyList<ArcGIS.Core.Data.Field> fields = facilitySiteDefinition.GetFields();

                                    ArcGIS.Core.Data.Field foundField = fields.FirstOrDefault(field => field.Name == fieldName);

                                    if (foundField != null)
                                    {
                                        CodedValueDomain domain = foundField.GetDomain() as CodedValueDomain;
                                        return domain.GetCodedValue(key).ToString();         
                                    }
                                }

                                break;
                            }
                        }
                    }

                    return "";
                });
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.ToString());
            }

            return null;
        }

        private async Task CreateSymbolSetFromFieldValuesAsync()
        {
            try
            {
                Dictionary<string, string> fieldValues = new Dictionary<string, string>();
                GeometryType geoType = GeometryType.Point;
                await QueuedTask.Run(() =>
                {
                    string oidFieldName = _selectedSelectedFeature.FeatureLayer.GetTable().GetDefinition().GetObjectIDField();
                    QueryFilter queryFilter = new QueryFilter();
                    queryFilter.WhereClause = string.Format("{0} = {1}", oidFieldName, _selectedSelectedFeature.ObjectId);
                    RowCursor cursor = _selectedSelectedFeature.FeatureLayer.Search(queryFilter);
                    Row row = null;

                    if (cursor.MoveNext())
                    {
                        row = cursor.Current;
                    }

                    if (row == null)
                    {
                        return;
                    }

                    IReadOnlyList<Field> fields = row.GetFields();
                    lock (_lock)
                    {
                        foreach (Field field in fields)
                        {
                            if (field.FieldType == FieldType.Geometry)
                            {
                                ArcGIS.Core.Geometry.Geometry geo = row[field.Name] as ArcGIS.Core.Geometry.Geometry;
                                if (geo != null)
                                    geoType = geo.GeometryType;
                                continue;
                            }

                            var fieldValue = row[field.Name];

                            if (fieldValue != null)
                            {
                                fieldValues[field.Name] = fieldValue.ToString();
                            }
                        }
                    }

                });

                //Transfer field values into SymbolAttributes
                SymbolAttributeSet set = new SymbolAttributeSet(fieldValues);
                set.SymbolTags = _selectedSelectedFeature.ToString().Replace(ProSymbolUtilities.NameSeparator,";");
                set.SymbolTags += ";" + ProSymbolUtilities.GeometryTypeToGeometryTagString(geoType) + ";MAP_SELECTION;" + set.Name;
                GeometryType = geoType;
                EditSelectedFeatureSymbol = set;
                LoadSymbolIntoWorkflow(true);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.ToString());
            }

            return;
        }

        private string[] ParseKeyForSymbolIdCode(string tags)
        {
            string[] symbolId = new string[3];

            //TODO: check if symbolid is in key

            int lastSemicolon = tags.LastIndexOf(';');
            string symbolIdCode = tags.Substring(lastSemicolon + 1, tags.Length - lastSemicolon - 1);
            symbolId[0] = string.Format("{0}{1}", symbolIdCode[0], symbolIdCode[1]);
            symbolId[1] = string.Format("{0}{1}{2}{3}{4}{5}", symbolIdCode[2], symbolIdCode[3], symbolIdCode[4], symbolIdCode[5], symbolIdCode[6], symbolIdCode[7]);

            if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525d)
            {
                symbolId[2] = String.Empty;
            }
            else // mil2525c_b2
            {
                string[] tagArray = tags.Split(';');
                int tagCount = tagArray.Count();
                if (tagCount > 5)
                {
                    // Tricky - Legacy SIDC always Tags[-5]
                    string legacySidc = tagArray[tagCount - 5];

                    if (legacySidc.Count() >= 10)
                    {
                        symbolId[2] = string.Format("{0}-{1}-{2}", legacySidc[0], legacySidc[2], legacySidc.Substring(4, 6));
                    }

                }
            }

            return symbolId;
        }

        private void GeneratePolyGeometry()
        {
            //PolyCoordinates.ToList()
            List<MapPoint> points = new List<MapPoint>();
            foreach (CoordinateObject coordObject in PolyCoordinates)
            {
                points.Add(coordObject.MapPoint);
            }

            ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                if (GeometryType == GeometryType.Polyline)
                {
                    PolylineBuilder polylineBuilder = new PolylineBuilder(points);
                    polylineBuilder.HasZ = true;
                    MapGeometry = polylineBuilder.ToGeometry();
                }
                else if (GeometryType == GeometryType.Polygon)
                {
                    PolygonBuilder polygonBuilder = new PolygonBuilder(points);
                    polygonBuilder.HasZ = true;
                    MapGeometry = polygonBuilder.ToGeometry();
                }
            });
        }

        private async Task SearchSymbols()
        {
            // TODO: research how to speed this up - takes about 1 sec / 200 style items returned
            await QueuedTask.Run(() =>
            {
                var list = new List<StyleItemType>() { StyleItemType.PointSymbol, StyleItemType.LineSymbol, StyleItemType.PolygonSymbol };

                IEnumerable<IList<SymbolStyleItem>> symbolQuery = 
                    from type in list
                        select _militaryStyleItem.SearchSymbols(type, _searchString);

                var combinedSymbols = new List<SymbolStyleItem>();
                int outParse;

                if (symbolQuery == null)
                    return;

                foreach (var symbolType in symbolQuery)
                {
                    // Change style query based on current standard
                    if (ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
                    {
                        // TODO: also include 2525C keys in search                                         
                        combinedSymbols.AddRange(symbolType.Where(x =>
                          (((x.Key.Length == 8) && int.TryParse(x.Key, out outParse)) ||
                           ((x.Key.Length == 10) && (x.Key[8] == '_') && int.TryParse(x.Key[9].ToString(), out outParse)))
                        // TODO: Find less ugly way of filtering out 2525D symbols when in 2525C_B2 mode:
                        && (!x.Tags.Contains("NEW_AT_2525D"))
                        ));
                    }
                    else // 2525D
                    {
                        combinedSymbols.AddRange(symbolType.Where(x => (x.Key.Length == 8 && int.TryParse(x.Key, out outParse)) ||
                            (x.Key.Length == 10 && x.Key[8] == '_' && int.TryParse(x.Key[9].ToString(), out outParse))
                            ));
                    }
                }

                combinedSymbols.Sort((a, b) => (a.Name.CompareTo(b.Name)));

                _styleItems = combinedSymbols;

                _progressDialogSearch.Hide();
                ResultCount = combinedSymbols.Count.ToString();
            });
        }

        private void LoadAllFavoritesFromFile()
        {
            if (File.Exists(_favoritesFilePath))
            {
                string json = File.ReadAllText(_favoritesFilePath);
                Favorites = new JavaScriptSerializer().Deserialize<ObservableCollection<SymbolAttributeSet>>(json);
            }

            //Go through favorites, generate symbol image
            foreach (SymbolAttributeSet set in Favorites)
            {
                set.GeneratePreviewSymbol();
            }

            //Set up filter
            _favoritesView = CollectionViewSource.GetDefaultView(Favorites);
            _favoritesView.Filter = FavoritesFilter;
        }

        private bool FavoritesFilter(object item)
        {
            SymbolAttributeSet set = item as SymbolAttributeSet;

            // filter out those who standard version doesn't match
            if ((set == null) || 
                (set.StandardVersion != ProSymbolUtilities.StandardString))
            {
                return false;
            }

            //Do case insensitive filter
            bool idContains = set.FavoriteId.IndexOf(_favoritesSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            bool tagsContains = set.SymbolTags.IndexOf(_favoritesSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            if (idContains || tagsContains)
            {
                return true;
            }

            return false;
        }

        private void ShowMilitaryFeatureNotFoundMessageBox()
        {
            string message = "The Selected Feature does not seem to be a Military Feature or " +
                "does not match the Military Standard in use (" + ProSymbolUtilities.StandardLabel +
                ").";

            MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void ShowMilitaryStyleNotFoundMessageBox()
        {
            string message = "The Required Military Style (" + ProSymbolUtilities.StandardString +
                ") is not detected in Pro Install.";

            MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "Add-In Disabled", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private async void ShowAddInNotEnabledMessageBox()
        {
            // First check if is already enabled
            bool isEnabled = await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync();
            if (isEnabled)
                return; 

            // Clear the selected entries so they can be reselected after the schema adds
            // (they will not fully initialize without a valid schema)
            if (SelectedTabIndex == 0)
                SelectedStyleItem = null;
            else if (SelectedTabIndex == 2)
                SelectedFavoriteSymbol = null;

            // If not enabled see if schema should be added
            // Run on UI Thread
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(async () =>
            {
                string message = "The " + ProSymbolUtilities.StandardLabel +
                    " Military Overlay schema is not detected in any database in your project. \n" +
                    " so the Military Symbol Editor cannot continue." +
                    " Would you like to add the Military Overlay Layer Package to your project?";

                MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "Add-In Disabled", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result.ToString() == "Yes")
                {
                    if (MapView.Active != null)
                    {
                        await ShowSettingsWindowAsync(true);

                        // Save the project with the layer package added
                        ProSymbolUtilities.SaveProject();
                        // Reselect this style item onced the layer package is added
                        if (SelectedTabIndex == 0)
                            SelectedStyleItem = _savedStyleItem;
                        else if (SelectedTabIndex == 2)
                            SelectedFavoriteSymbol = _savedSelectedFavoriteSymbol;
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Your project does not contain any active map.  Create one and try again.", "Please Add Map to Your Project", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        SearchString = "ADD MAP TO PROJECT";
                    }
                }
                else
                {
                    // Not sure why this didn't work:
                    // Clear the search list
                    // StyleItems.Clear();
                    // NotifyPropertyChanged(() => StyleItems);
                    // WORKAROUND:
                    SearchString = "ADDIN NOT ENABLED";
                }

            }));
        }

        private async Task<bool> AddLayerPackageToMapAsync()
        {
            try
            {
                // Check the active map is available+ready
                if ((MapView.Active == null) || (MapView.Active.Map == null))
                {
                    var result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        "The project map is not currently available.\n" +
                        "Would you like to try again?\n" +
                        "Note: wait for map to be visible and ready.", "Retry Adding Military Overlay Datamodel?",
                        System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Asterisk);
                    if (Convert.ToString(result) != "Yes")
                        return false;
                }

                // Check again
                if ((MapView.Active == null) || (MapView.Active.Map == null))
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        "Unable to add layer package to map.\n" +
                        "Please ensure the project contains a map and the map is visible.",
                        "Unable to Add Layer Package to Map", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }

                _progressDialogLoad.Show();

                bool enabled = false;

                await QueuedTask.Run(async () =>
                {
                    // "MilitaryOverlay-{standard}.lpkx"
                    string layerFileName = "MilitaryOverlay-" + ProSymbolUtilities.StandardString.ToLower() + ".lpkx";
                    LayerFactory.Instance.CreateLayer(new Uri(System.IO.Path.Combine(ProSymbolUtilities.AddinAssemblyLocation(), "LayerFiles", layerFileName)), MapView.Active.Map);
                    enabled = await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync();

                    if (enabled)
                        StatusMessage = "Military Layers Added";
                    else
                        StatusMessage = "Addin Not Enabled";
                });

            }
            catch (Exception exception)
            {
                // Catch any exception found and display a message box.
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception caught: " + exception.Message);
                return false;
            }

            _progressDialogLoad.Hide();
            return true;
        }

        #endregion

        #region IDataErrorInfo Interface

        public string Error { get; set; }

        public string this[string columnName]
        {
            get
            {
                Error = null;

                switch (columnName)
                {
                    case "MapPointCoordinatesString":
                        if (!PointCoordinateValid)
                        {
                            Error = "The coordinates are invalid";
                        }
                        break;
                }

                return Error;
            }
        }

        #endregion

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

    } // end class

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class MilitarySymbolDockpane_ShowButton : Button
    {
        protected override void OnClick()
        {
            MilitarySymbolDockpaneViewModel.Show();
        }
    }
}
