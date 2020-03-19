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

                    _searchString = Properties.Resources.MSDocVMSS;
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
                if (set.StandardVersion == ProSymbolUtilities.StandardString)
                    set.GeneratePreviewSymbol();
            }

            _favoritesView.Refresh();

            NotifyPropertyChanged(() => EchelonLabel);

            SymbolAttributeSet.StandardVersion = ProSymbolUtilities.StandardString;
        }

        private void setStandardFromSettings()
        {
// TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (Properties.Settings.Default.DefaultStandard ==
                    ProSymbolUtilities.GetStandardString(ProSymbolUtilities.SupportedStandardsType.mil2525b))
                ProSymbolUtilities.Standard = ProSymbolUtilities.SupportedStandardsType.mil2525b;
            else
                ProSymbolUtilities.Standard = ProSymbolUtilities.SupportedStandardsType.mil2525d;
        }

        private async Task Initialize()
        {
            // Not enabled until Schema found/set
            IsAddinEnabled = false;

            ProSymbolEditorModule.Current.MilitaryOverlaySchema.Reset();

            bool isExistingStandardSchemaFound = false;
            ProSymbolUtilities.SupportedStandardsType foundStandard = ProSymbolUtilities.SupportedStandardsType.mil2525d;

            // Loop through the supported standards to see if a datamodel already exists for one of these
            Array standards = Enum.GetValues(typeof(ProSymbolUtilities.SupportedStandardsType));
            foreach (ProSymbolUtilities.SupportedStandardsType standard in standards)
            {
                bool isEnabledWithStandard =
                    await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync(standard);

                if (isEnabledWithStandard)
                {
                    isExistingStandardSchemaFound = true;
                    foundStandard = standard;
                    break;
                }
            }

            if (!isExistingStandardSchemaFound ||
                !ProSymbolEditorModule.Current.MilitaryOverlaySchema.SchemaExists) // extra check
            {
                resetViewModelState();
                IsAddinEnabled = false;
                StatusMessage = Properties.Resources.MSDocVMMsg1;

                return;
            }

            ProSymbolUtilities.Standard = foundStandard;
            StatusMessage = Properties.Resources.MSDocVMMsg2;
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
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.SignatureEquipmentDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.CountryCodeDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.ExtendedFunctionCodeValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.EntityCodeValues, _lock);

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

            _progressDialogLoad = new ProgressDialog(Properties.Resources.MSDocVMPg1);
            _progressDialogSearch = new ProgressDialog(Properties.Resources.MSDocVMPg2);

            //Load saved favorites
            _favoritesFilePath = System.IO.Path.Combine(ProSymbolUtilities.UserSettingsLocation(), "SymbolFavorites.json");
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

                if (!string.IsNullOrEmpty(_searchString))
                {
                    _searchString = _searchString.Trim();
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

                if (!_isStyleItemSelected && (SelectedTabIndex > 1))
                {
                    //Reset tab
                    SelectedTabIndex = 0;
                }

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

                    _symbolAttributeSet.DisplayAttributes.SymbolGeometry = GeometryType;

                    //Parse key for symbol id codes
                    string[] symbolIdCode = GetSymbolIdCodeFromStyle(_selectedStyleItem);

                    if (symbolIdCode.Length >= 2)
                    {
                        _symbolAttributeSet.DisplayAttributes.SymbolSet = symbolIdCode[0];
                        _symbolAttributeSet.DisplayAttributes.SymbolEntity = symbolIdCode[1];
                    }

                    SymbolAttributeSet loadSet = new SymbolAttributeSet();

                    // Set 2525C_B2 SIDC/attribute if applicable
                    if ((ProSymbolUtilities.IsLegacyStandard()) 
                        && (symbolIdCode.Length >= 3))
                    {
                        string functionCode = symbolIdCode[2];
                        _symbolAttributeSet.DisplayAttributes.ExtendedFunctionCode = functionCode;

                        loadSet.DisplayAttributes.ExtendedFunctionCode = functionCode;
                    }
                    else
                    {
                        loadSet.DisplayAttributes.SymbolSet = _symbolAttributeSet.DisplayAttributes.SymbolSet;
                        loadSet.DisplayAttributes.SymbolEntity = _symbolAttributeSet.DisplayAttributes.SymbolEntity;
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

                _selectedSelectedFeature = value;

                if ((_selectedSelectedFeature == null) ||
                    !IsAddinEnabled || (MapView.Active == null))
                {
                    EditSelectedFeatureSymbol = null;
                    IsStyleItemSelected = false;

                    return;
                }

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
                    System.Diagnostics.Trace.WriteLine(Properties.Resources.MSDocVMEx1 + exception.Message);
                }

                // if not on Symbol or Label Tab, set to Symbol Tab
                if (!((SelectedTabIndex == 2) || (SelectedTabIndex == 3)))
                    SelectedTabIndex = 2;  // Symbol Tab

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

        public string EchelonLabel
        {
            get
            {
                if (ProSymbolUtilities.IsLegacyStandard())
                    return Properties.Resources.MSDocVMEchLabel1;
                else
                    return Properties.Resources.MSDocVMEchLabel2;
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
                string message = Properties.Resources.MSDocVMMsg3 + 
                    ProSymbolUtilities.GetStandardLabel(previousSettingStandard) + Properties.Resources.MSDocVMMsg8 + System.Environment.NewLine + Properties.Resources.MSDocVMMsg4;
                MessageBoxResult result = 
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message,
                    Properties.Resources.MSDocVMCpt1, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                isSettingsReadOnly = true;
            }

            if (!isSettingsReadOnly)
            {
                // set this status in case user cancels any of this setup at start
                StatusMessage = Properties.Resources.MSDocVMMsg1;
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
                        Properties.Resources.MSDocVMMsg5 + newDatabase,
                        Properties.Resources.MSDocVMCpt2,
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
                    StatusMessage = Properties.Resources.MSDocVMMsg6;
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
                            Properties.Resources.MSDocVMMsg9 + newDatabase + System.Environment.NewLine +
                            Properties.Resources.MSDocVMMsg10 + System.Environment.NewLine +
                            Properties.Resources.MSDocVMMsg11,
                            Properties.Resources.MSDocVMCpt3, MessageBoxButton.OK, MessageBoxImage.Exclamation);

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
                    Properties.Resources.MSDocVMMsg12 + System.Environment.NewLine +
                    ProSymbolUtilities.GetStandardLabel(newSettingStandard) + System.Environment.NewLine +
                    Properties.Resources.MSDocVMMsg13 + 
                    newDatabase + System.Environment.NewLine +
                    Properties.Resources.MSDocVMMsg14
                    , Properties.Resources.MSDocVMCpt4,
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
                        Properties.Resources.MSDocVMMsg15 + System.Environment.NewLine +
                        Properties.Resources.MSDocVMMsg16,
                        Properties.Resources.MSDocVMCpt5, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return false;
                }

                // Save the project with the layer package added
                ProSymbolUtilities.SaveProject();
                enabledWithNewStandard = true;

                StatusMessage = Properties.Resources.MSDocVMMsg17;
            }

            if (!enabledWithNewStandard)
            {
                StatusMessage = Properties.Resources.MSDocVMMsg1;
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
            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
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

                                modificationResult = await editOperation.ExecuteAsync();

                                if (!modificationResult)
                                {
                                    message = editOperation.ErrorMessage;
                                    await Project.Current.DiscardEditsAsync();
                                }
                                else
                                {
                                    await Project.Current.SaveEditsAsync();
                                }
                            } // ifcorrect geodatabase
                        } // using
                    } // for each 
                } // try
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
                        MapView.Active.SelectFeatures(savedGeometry, SelectionCombinationMethod.And);
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

            if ((_styleItems.Count == 0) && ProSymbolUtilities.IsSIDC(_searchString))
            {
                // If nothing found, but search text matches SIDC format, then do the search again  
                // with the search string set to the subset of the SIDC that each style item contains
                _searchString = ProSymbolUtilities.GetSearchStringFromSIDC(_searchString);
                await SearchSymbols();
            }


            StatusMessage = Properties.Resources.MSDocVMMsg18;

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
                    requiredLayerName = Properties.Resources.MSDocVMMsg19;

                // Could not find layer in map - ask to re-add it
                string warningMessage = Properties.Resources.MSDocVMMsg20 + System.Environment.NewLine +
                    Properties.Resources.MSDocVMMsg21 + requiredLayerName + "." + System.Environment.NewLine +
                    Properties.Resources.MSDocVMMsg22;
                Debug.WriteLine(warningMessage);
                MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(warningMessage,
                    Properties.Resources.MSDocVMCpt6, MessageBoxButton.YesNoCancel, 
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
                        Properties.Resources.MSDocVMMsg23 + requiredLayerName,
                        Properties.Resources.MSDocVMCpt7, MessageBoxButton.OK,
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
                                editOperation.Name = Properties.Resources.MSDocVMEd1;
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
                                    catch (Exception ex)
                                    {
                                        // Other exception
                                        message = ex.Message;
                                    }
                                }, featureClass);

                                creationResult = await editOperation.ExecuteAsync();

                                if (!creationResult && !string.IsNullOrEmpty(message))
                                {
                                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, Properties.Resources.MSDocVMCpt8);
                                    message = string.Empty; // set to empty so 2nd error message is not displayed below
                                    await Project.Current.DiscardEditsAsync();
                                }
                                else
                                {
                                    await Project.Current.SaveEditsAsync();
                                }

                                break;
                            }
                        }
                    }
                }
            });

            if (!creationResult && !string.IsNullOrEmpty(message))
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, Properties.Resources.MSDocVMCpt8);
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
            saveFileDialog.FileName = Properties.Resources.MSDocVMSaveFln;
            saveFileDialog.Filter = Properties.Resources.MSDocVMSaveFlt;
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
                SymbolAttributeSet.DisplayAttributes.SymbolGeometry = GeometryType;

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
                LabelAttributes.MaxLengthValidationOn = false;
                SymbolAttributeSet.LabelAttributes.DateTimeValid = loadSet.LabelAttributes.DateTimeValid;
                SymbolAttributeSet.LabelAttributes.DateTimeExpired = loadSet.LabelAttributes.DateTimeExpired;
                SymbolAttributeSet.LabelAttributes.Type = loadSet.LabelAttributes.Type;
                SymbolAttributeSet.LabelAttributes.CommonIdentifier = loadSet.LabelAttributes.CommonIdentifier;
                SymbolAttributeSet.LabelAttributes.Speed = loadSet.LabelAttributes.Speed;
                SymbolAttributeSet.LabelAttributes.Direction = loadSet.LabelAttributes.Direction;
                SymbolAttributeSet.LabelAttributes.UniqueDesignation = loadSet.LabelAttributes.UniqueDesignation;
                SymbolAttributeSet.LabelAttributes.StaffComments = loadSet.LabelAttributes.StaffComments;
                SymbolAttributeSet.LabelAttributes.AdditionalInformation = loadSet.LabelAttributes.AdditionalInformation;
                SymbolAttributeSet.LabelAttributes.HigherFormation = loadSet.LabelAttributes.HigherFormation;
                SymbolAttributeSet.SymbolTags = loadSet.SymbolTags;
                LabelAttributes.MaxLengthValidationOn = true;

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
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg24,
                    Properties.Resources.MSDocVMCpt9, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (Favorites.Contains(SymbolAttributeSet))
                {
                    throw new Exception(Properties.Resources.MSDocVMEx1);
                }

                SymbolAttributeSet.FavoriteId = Guid.NewGuid().ToString();
                LabelAttributes.MaxLengthValidationOn = false;

                //Create copy by serializing/deserializing
                SymbolAttributeSet favoriteSet = null;

                try
                {
                    var json = new JavaScriptSerializer().Serialize(SymbolAttributeSet);
                    favoriteSet = new JavaScriptSerializer().Deserialize<SymbolAttributeSet>(json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(Properties.Resources.MSDocVMMsg25 + ex.Message);
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg26 +
                        _favoritesFilePath + System.Environment.NewLine + Properties.Resources.MSDocVMMsg27 + System.Environment.NewLine +
                        Properties.Resources.MSDocVMMsg28,
                        Properties.Resources.MSDocVMCpt10, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                LabelAttributes.MaxLengthValidationOn = true;

                //Add to favorites
                if (favoriteSet == null) // should not happen
                    throw new Exception(Properties.Resources.MSDocVMMsg29);

                favoriteSet.GeneratePreviewSymbol();
                Favorites.Add(favoriteSet);

                //Serialize Favorites and save to file
                var favoritesJson = new JavaScriptSerializer().Serialize(Favorites);
                File.WriteAllText(_favoritesFilePath, favoritesJson);

                // Switch to Favorites Tab to provide feedback favorite was added
                SelectedTabIndex = 1;

                SelectedFavoriteSymbol = favoriteSet;
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg30 + ex.Message, Properties.Resources.MSDocVMCpt11, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

            SelectedFavoriteSymbol = null;
            IsStyleItemSelected = false;
            if (AddToMapToolEnabled)
                FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");  // select another tool to disable add to map tool 

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
                MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg31,
                    Properties.Resources.MSDocVMCpt12, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }

        private void SaveFavoritesAsToFile(object parameter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = Properties.Resources.MSDocVMSaveFln1;
            saveFileDialog.Filter = Properties.Resources.MSDocVMSaveFlt1;
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
            openFileDialog.Filter = Properties.Resources.MSDocVMImpFlt;
            if (openFileDialog.ShowDialog() == true)
            {
                if (Path.GetExtension(openFileDialog.FileName).ToUpper() == Properties.Resources.MSDocVMImpExt)
                {

                    ObservableCollection<SymbolAttributeSet> importedFavorites = null;

                    LabelAttributes.MaxLengthValidationOn = false;
                    try
                    {
                        string json = File.ReadAllText(openFileDialog.FileName);

                        importedFavorites = new JavaScriptSerializer().Deserialize<ObservableCollection<SymbolAttributeSet>>(json);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(Properties.Resources.MSDocVMMsg25 + ex.Message);
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg26 +
                            _favoritesFilePath + System.Environment.NewLine + Properties.Resources.MSDocVMMsg27 + System.Environment.NewLine +
                            Properties.Resources.MSDocVMMsg28,
                            Properties.Resources.MSDocVMCpt10, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    LabelAttributes.MaxLengthValidationOn = true;

                    if (importedFavorites == null)
                        return;

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
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg32);
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
                    string warningMessage = Properties.Resources.MSDocVMMsg33 + System.Environment.NewLine + Properties.Resources.MSDocVMMsg34;
                  
                    Debug.WriteLine(warningMessage);
                    MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(warningMessage,
                        Properties.Resources.MSDocVMCpt13, MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

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
                    string warningMessage = Properties.Resources.MSDocVMMsg35 + System.Environment.NewLine +
                    Properties.Resources.MSDocVMMsg36;
                    Debug.WriteLine(warningMessage);
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(warningMessage, Properties.Resources.MSDocVMCpt14, MessageBoxButton.OK, MessageBoxImage.Exclamation);

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

            string symbolSetFieldName = Properties.Resources.MSDocVMStr1;
            string symbolEntityFieldName = Properties.Resources.MSDocVMStr2;

            if (ProSymbolUtilities.IsLegacyStandard())
            {
                symbolSetFieldName = Properties.Resources.MSDocVMStr3;
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

                            if ((row.FindField(symbolSetFieldName) >= 0) &&
                                (row[symbolSetFieldName] != null))
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
                                (symbolEntityDomainSortedList != null))
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

            if (SelectedFeaturesCollection.Count > 1)
            {
                // TRICKY: if > 1 feature selected, we want to select the last one, since that will be drawn on top/visible
                SelectedSelectedFeature = SelectedFeaturesCollection.Last();
            }
            else
            {
                SelectedSelectedFeature = SelectedFeaturesCollection.FirstOrDefault();
            }
        }

        private async Task CheckAddinEnabled()
        {
            if (!IsAddinEnabled)
            {
                await Task.FromResult<bool>(true);

                var result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    Properties.Resources.MSDocVMMsg37 + System.Environment.NewLine +
                    Properties.Resources.MSDocVMMsg38,
                    Properties.Resources.MSDocVMCpt15,
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
            string affiliationField = Properties.Resources.MSDocVMaff;
            string hostileValue = Properties.Resources.MSDocVMhos;
            string friendValue = Properties.Resources.MSDocVMfri;
            string neutralValue = Properties.Resources.MSDocVMNeu;

            // These have different values for 2525C/B2
            if (ProSymbolUtilities.IsLegacyStandard())
            {
                affiliationField = Properties.Resources.MSDocVMaff1;
                hostileValue = Properties.Resources.MSDocVMhos1;
                if (ProSymbolUtilities.Standard != ProSymbolUtilities.SupportedStandardsType.app6b) // app6b is different for some reason 
                    friendValue = Properties.Resources.MSDocVMfri1;
            }

            string upperTagsName = _selectedStyleItem.Tags.ToUpper();
            string upperItemName = _selectedStyleItem.Name.ToUpper();

            string identityCode = "";
            if (upperTagsName.Contains(Properties.Resources.MSDocVMfriUp) || upperItemName.Contains(Properties.Resources.MSDocVMfri1Up))
            {
                identityCode = await GetDomainValueAsync(affiliationField, friendValue);
            }
            else if (upperTagsName.Contains(Properties.Resources.MSDocVMhosUp) || upperItemName.Contains(Properties.Resources.MSDocVMhos1Up))
            {
                identityCode = await GetDomainValueAsync(affiliationField, hostileValue);
            }
            else if (upperTagsName.Contains(Properties.Resources.MSDocVMNeuUp) || upperItemName.Contains(Properties.Resources.MSDocVMNeu1Up))
            {
                identityCode = await GetDomainValueAsync(affiliationField, neutralValue);
            }
            else
            // IMPORTANT: Default to the "Unknown" value - as of 2.3 affiliation is now 
            // a required attribute
            // else if (upperTagsName.Contains("UNKNOWN") || upperItemName.Contains(": UNKNOWN"))
            {
                identityCode = await GetDomainValueAsync(affiliationField, Properties.Resources.MSDocVMUnk);
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

                    if (loadSet.DisplayAttributes.SymbolEntity != null)
                        SymbolAttributeSet.DisplayAttributes.SelectedEntityCodeDomainPair
                            = MilitaryFieldsInspectorModel.EntityCodeValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.SymbolEntity);

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

                    if (loadSet.LabelAttributes.SignatureEquipment != null)
                        SymbolAttributeSet.LabelAttributes.SelectedSignatureEquipmentDomainPair =
                            MilitaryFieldsInspectorModel.SignatureEquipmentDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.SignatureEquipment);

                    if (loadSet.LabelAttributes.CountryCode != null)
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
                set.SymbolTags += ";" + ProSymbolUtilities.GeometryTypeToGeometryTagString(geoType);
                set.SymbolTags += ";" + set.Name;
                set.SymbolTags += ";" + ProSymbolUtilities.StandardLabel;

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

        /// <summary>
        /// Gets the Symbol ID Code from a Dictionary Style Item
        /// </summary>
        /// <param name="styleItem">Dictionary Style Item</param>
        /// <returns>
        /// symbolId[0] = symbol set
        /// symbolId[1] = entity code
        /// symbolId[2] = 2525B/C SIDC if applicable
        /// </returns>
        private string[] GetSymbolIdCodeFromStyle(SymbolStyleItem styleItem)
        {
            string key = styleItem.Key;
            string tags = styleItem.Tags;

            string[] symbolId = new string[3];

            int lastSemicolon = tags.LastIndexOf(';');
            string symbolIdCode = tags.Substring(lastSemicolon + 1, tags.Length - lastSemicolon - 1);

            if (ProSymbolUtilities.IsLegacyStandard())
            {
                symbolId[0] = String.Empty;
                symbolId[1] = String.Empty;
            }
            else // 2525d/app6d or later
            {
                symbolId[0] = string.Format("{0}{1}", symbolIdCode[0], symbolIdCode[1]);
                symbolId[1] = string.Format("{0}{1}{2}{3}{4}{5}", symbolIdCode[2], symbolIdCode[3], symbolIdCode[4], symbolIdCode[5], symbolIdCode[6], symbolIdCode[7]);
                symbolId[2] = String.Empty;
            }

            if (ProSymbolUtilities.IsLegacyStandard())
            {
                // Set symbolId[2] with legacy SIDC

                if (ProSymbolUtilities.IsNewStyleFormat)
                {
                    if (key.Length >= 10)
                        symbolId[2] = key.Substring(0, 10);
                }
                else
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
                    if (ProSymbolUtilities.IsLegacyStandard())
                    {
                        // Keys changed format at 2.3
                        if (ProSymbolUtilities.IsNewStyleFormat)
                        {
                            combinedSymbols.AddRange(symbolType.Where(x =>
                             ((x.Key.Length == 10) || (x.Key.Length == 12) || (x.Key.Length == 13))
                              ));
                        }
                        else
                        {
                            // TODO: also include 2525C keys in search                                         
                            combinedSymbols.AddRange(symbolType.Where(x =>
                              (((x.Key.Length == 8) && int.TryParse(x.Key, out outParse)) ||
                               ((x.Key.Length == 10) && (x.Key[8] == '_') && int.TryParse(x.Key[9].ToString(), out outParse)))
                               // Filter out 2525D-only symbols when in 2525C_B2 mode:
                               && (!x.Tags.Contains(Properties.Resources.MSDocVMStr4))
                               ));
                        }
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
            LabelAttributes.MaxLengthValidationOn = false;

            if (File.Exists(_favoritesFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_favoritesFilePath);
                    Favorites = new JavaScriptSerializer().Deserialize<ObservableCollection<SymbolAttributeSet>>(json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(Properties.Resources.MSDocVMMsg25 + ex.Message);
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg26 + 
                        _favoritesFilePath + System.Environment.NewLine + Properties.Resources.MSDocVMMsg27 + System.Environment.NewLine +
                        Properties.Resources.MSDocVMMsg28,
                        Properties.Resources.MSDocVMCpt10, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }

            LabelAttributes.MaxLengthValidationOn = true;

            //Go through favorites, generate symbol image
            foreach (SymbolAttributeSet set in Favorites)
            {
                if (set.StandardVersion == ProSymbolUtilities.StandardString)
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
            string message = Properties.Resources.MSDocVMMsg39 + ProSymbolUtilities.StandardLabel +
                Properties.Resources.MSDocVMMsg40;

            MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, Properties.Resources.MSDocVMCpt16, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void ShowMilitaryStyleNotFoundMessageBox()
        {
            string message = Properties.Resources.MSDocVMMsg41 + ProSymbolUtilities.StandardString +
                Properties.Resources.MSDocVMMsg42;

            MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, Properties.Resources.MSDocVMCpt17, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
            else if (SelectedTabIndex == 1)
                SelectedFavoriteSymbol = null;

            // If not enabled see if schema should be added
            // Run on UI Thread
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(async () =>
            {
                string message = Properties.Resources.MSDocVMMsg43 + ProSymbolUtilities.StandardLabel +
                    Properties.Resources.MSDocVMMsg44 + System.Environment.NewLine +
                    Properties.Resources.MSDocVMMsg45;

                MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, Properties.Resources.MSDocVMCpt17, MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
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
                        else if (SelectedTabIndex == 1)
                            SelectedFavoriteSymbol = _savedSelectedFavoriteSymbol;
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg46, Properties.Resources.MSDocVMCpt18, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        SearchString = Properties.Resources.MSDocVMStr5;
                    }
                }
                else
                {
                    // Not sure why this didn't work:
                    // Clear the search list
                    // StyleItems.Clear();
                    // NotifyPropertyChanged(() => StyleItems);
                    // WORKAROUND:
                    SearchString = Properties.Resources.MSDocVMStr6;
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
                    var result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg47 + System.Environment.NewLine
                        + Properties.Resources.MSDocVMMsg48 + System.Environment.NewLine + Properties.Resources.MSDocVMMsg49, Properties.Resources.MSDocVMCpt19,
                        System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Asterisk);
                    if (Convert.ToString(result) != "Yes")
                        return false;
                }

                // Check again
                if ((MapView.Active == null) || (MapView.Active.Map == null))
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMMsg50 + System.Environment.NewLine
                        + Properties.Resources.MSDocVMMsg51, Properties.Resources.MSDocVMCpt20, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return false;
                }

                _progressDialogLoad.Show();

                bool enabled = false;

                await QueuedTask.Run(async () =>
                {
                    // Load layer file: "MilitaryOverlay-{standard}.lpkx"
                    LayerFactory.Instance.CreateLayer(new Uri(ProSymbolUtilities.GetLayerFileFromCurrentStandard()), MapView.Active.Map);
                    enabled = await ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync();

                    if (enabled)
                        StatusMessage = Properties.Resources.MSDocVMStr7;
                    else
                        StatusMessage = Properties.Resources.MSDocVMStr8;
                });

            }
            catch (Exception exception)
            {
                // Catch any exception found and display a message box.
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(Properties.Resources.MSDocVMExt1 + exception.Message);
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
                            Error = Properties.Resources.MSDocVMExt2;
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
