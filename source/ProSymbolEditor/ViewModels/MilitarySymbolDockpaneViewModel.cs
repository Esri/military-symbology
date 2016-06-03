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
using CoordinateToolLibrary.Models;
using Microsoft.Win32;
using System.Web.Script.Serialization;
using System.Windows.Threading;

namespace ProSymbolEditor
{
    internal class MilitarySymbolDockpaneViewModel : DockPane, IDataErrorInfo
    {
        //Member Variables
        private const string _dockPaneID = "ProSymbolEditor_MilitarySymbolDockpane";
        private const string _menuID = "ProSymbolEditor_MilitarySymbolDockpane_Menu";
        private const string _mil2525dRelativePath = @"Resources\Dictionaries\mil2525d\mil2525d.stylx";
        private string _mil2525dStyleFullFilePath;
        private string _currentFeatureClassName = "";
        private string _favoritesFilePath = "";
        private FeatureClass _currentFeatureClass = null;
        private StyleProjectItem _militaryStyleItem = null;
        private SymbolStyleItem _selectedStyleItem = null;
        private SelectedFeature _selectedSelectedFeature = null;
        private SymbolAttributeSet _selectedFavoriteSymbol = null;
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
        private bool _isFavoriteItemSelected = false;
        private bool _addToMapToolEnabled = false;
        private Visibility _pointCoordinateVisibility;
        private Visibility _polyCoordinateVisibility;
        private ProgressDialog _progressDialog;
        private ICollectionView _favoritesView;
        private string _favoritesSearchFilter = "";
        private bool _isEditing = false;
        private bool _isAddingNew = false;

        protected MilitarySymbolDockpaneViewModel()
        {
            //Get Military Symbol Style Install Path
            string installPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\ESRI\ArcGISPro\", "InstallDir", null);

            if (installPath == null || installPath == "")
            {
                //Try to get the install path from current user instead of local machine
                installPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\ESRI\ArcGISPro\", "InstallDir", null);
            }

            if (installPath != null)
            {
                _mil2525dStyleFullFilePath = Path.Combine(installPath, _mil2525dRelativePath);
            }

            ArcGIS.Desktop.Core.Events.ProjectOpenedEvent.Subscribe(async (args) =>
            {
                //Add military style to project
                Task<StyleProjectItem> getMilitaryStyle = GetMilitaryStyleAsync();
                _militaryStyleItem = await getMilitaryStyle;

                //Reset things
                this.SelectedStyleItem = null;
                this.IsStyleItemSelected = false;
                this.IsFavoriteItemSelected = false;
                this.StyleItems.Clear();
                this.SelectedTabIndex = 0;
                this.ResultCount = "---";
                this.SearchString = "";
                _symbolAttributeSet.ResetAttributes();
                SelectedStyleTags.Clear();

            });

            ArcGIS.Desktop.Framework.Events.ActiveToolChangedEvent.Subscribe(OnActiveToolChanged);
            ArcGIS.Desktop.Mapping.Events.MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);

            //Create locks for variables that are updated in worker threads
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.IdentityDomainValues, _lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.EcholonDomainValues, _lock);
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

            //Set up Commands
            SearchResultCommand = new RelayCommand(SearchStylesAsync, param => true);
            GoToTabCommand = new RelayCommand(GoToTab, param => true);
            //ActivateMapToolCommand = new RelayCommand(ActivateCoordinateMapTool, param => true);
            AddCoordinateToMapCommand = new RelayCommand(CreateNewFeatureAsync, CanCreatePolyFeatureFromCoordinates);
            ActivateAddToMapToolCommand = new RelayCommand(ActivateDrawFeatureSketchTool, param => true);
            SaveEditsCommand = new RelayCommand(SaveEdits, param => true);
            CopyImageToClipboardCommand = new RelayCommand(CopyImageToClipboard, param => true);
            SaveImageToCommand = new RelayCommand(SaveImageAs, param => true);
            SaveSymbolFileCommand = new RelayCommand(SaveSymbolAsFavorite, param => true);
            DeleteFavoriteSymbolCommand = new RelayCommand(DeleteFavoriteSymbol, param => true);
            SaveFavoritesFileAsCommand = new RelayCommand(SaveFavoritesAsToFile, param => true);
            ImportFavoritesFileCommand = new RelayCommand(ImportFavoritesFile, param => true);

            _symbolAttributeSet.LabelAttributes.DateTimeValid = null;
            _symbolAttributeSet.LabelAttributes.DateTimeExpired = null;
            IsStyleItemSelected = false;

            PolyCoordinates = new ObservableCollection<CoordinateObject>();
            Favorites = new ObservableCollection<SymbolAttributeSet>();
            SelectedStyleTags = new ObservableCollection<string>();
            SelectedFavoriteStyleTags = new ObservableCollection<string>();
            SelectedFeaturesCollection = new ObservableCollection<SelectedFeature>();

            _progressDialog = new ProgressDialog("Loading...");
            _symbolAttributeSet.StandardVersion = "2525D";

            //Load saved favorites
            _favoritesFilePath = System.IO.Path.Combine(ProSymbolUtilities.AddinAssemblyLocation(), "SymbolFavorites.json");
            LoadAllFavoritesFromFile();
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

        public ICommand ActivateAddToMapToolCommand { get; set; }

        public ICommand SaveEditsCommand { get; set; }

        public ICommand SaveImageToCommand { get; set; }

        public ICommand CopyImageToClipboardCommand { get; set; }

        public ICommand SaveSymbolFileCommand { get; set; }

        public ICommand DeleteFavoriteSymbolCommand { get; set; }

        public ICommand ImportFavoritesFileCommand { get; set; }

        public ICommand SaveFavoritesFileAsCommand { get; set; }

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
                //SetProperty(ref _searchString, value, () => SearchString);
                _searchString = value;

                NotifyPropertyChanged(() => SearchString);

                if (_searchString.Length > 0)
                {
                    SearchStylesAsync(null);
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
                NotifyPropertyChanged(() => IsStyleItemSelected);
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

                if (!ProSymbolEditorModule.Current.MilitaryOverlaySchema.SchemaExists && value != null)
                {
                    ShowAddInNotEnabledMessageBox();
                    _selectedStyleItem = null;
                    return;
                }

                _selectedStyleItem = value;

                if (_selectedStyleItem != null)
                {
                    //Clear old attributes
                    _symbolAttributeSet.ResetAttributes();

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

                    //Get feature class name to generate domains
                    _currentFeatureClassName = _symbolSetMappings.GetFeatureClassFromMapping(_symbolAttributeSet.DisplayAttributes.SymbolSet, GeometryType);
                    if (_currentFeatureClassName != null && _currentFeatureClassName != "")
                    {
                        //Generate domains
                        GetMilitaryDomainsAsync();
                    }

                    IsStyleItemSelected = true;
                    IsEditing = false;
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

                if (_selectedSelectedFeature != null)
                {
                    CreateSymbolSetFromFieldValuesAsync();
                }
                else
                {
                    EditSelectedFeatureSymbol = null;
                }
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

        #endregion

        #region Command Methods

        private void ActivateDrawFeatureSketchTool(object parameter)
        {
            FrameworkApplication.SetCurrentToolAsync("ProSymbolEditor_DrawFeatureSketchTool");
            AddToMapToolEnabled = true;
        }

        private async void SaveEdits(object parameter)
        {
            string message = String.Empty;
            bool modificationResult = false;

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

                            //Find the correct gdb for the one with the complete schema
                            string geodatabasePath = geodatabase.GetPath();
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
                    System.Console.WriteLine(exception.Message);
                }
            });

            if (!modificationResult)
            {
                MessageBox.Show(message);
            }
        }
            

        private async void SearchStylesAsync(object parameter)
        {
            //Make sure that military style is in project
            if (!IsStyleInProject() || _militaryStyleItem == null)
            {
                //Add military style to project
                Task<StyleProjectItem> getMilitaryStyle = GetMilitaryStyleAsync();
                _militaryStyleItem = await getMilitaryStyle;
            }

            //Clear for new search
            if (_styleItems.Count != 0)
                _styleItems.Clear();

            ResultCount = "---";

            _progressDialog.Show();
            await SearchSymbols();

            //Check for Schema again
            Task<bool> isEnabledMethod = ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync();
            bool enabled = await isEnabledMethod;

            NotifyPropertyChanged(() => StyleItems);
        }

        private void GoToTab(object parameter)
        {
            SelectedTabIndex = Convert.ToInt32(parameter);
        }

        public async void CreateNewFeatureAsync(object parameter)
        {
            string message = String.Empty;
            bool creationResult = false;

            //Generate geometry if polygon or polyline, if adding new feature is from using coordinates and not the map tool
            if (Convert.ToBoolean(parameter) == true)
            {
                if (GeometryType == GeometryType.Polyline || GeometryType == GeometryType.Polygon)
                {
                    GeneratePolyGeometry();
                }
            }

            IEnumerable<GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();
            await QueuedTask.Run(() =>
            {
                foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                {
                    using (Datastore datastore = gdbProjectItem.GetDatastore())
                    {
                        //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                        if (datastore is UnknownDatastore)
                            continue;
                        Geodatabase geodatabase = datastore as Geodatabase;
                        
                        //Find the correct gdb for the one with the complete schema
                        string geodatabasePath = geodatabase.GetPath();
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
                                        rowBuffer["Shape"] = GeometryEngine.Project(MapGeometry, facilitySiteDefinition.GetSpatialReference());

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

                                var task = editOperation.ExecuteAsync();
                                creationResult = task.Result;
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
                MessageBox.Show(message);
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

        private void SaveImageAs(object parameter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "symbol";
            saveFileDialog.Filter = "Png Image|*.png";
            Nullable<bool> result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(SymbolAttributeSet.SymbolImage));
                using (var stream = saveFileDialog.OpenFile())
                {
                    encoder.Save(stream);
                }
            }
        }

        private void CopyImageToClipboard(object parameter)
        {
            //There's an issue copying the image directly to the clipboard, where transparency isn't retained, and will have a black background.
            //The code below will switch that to be a pseudo-transparency with a white background.
            Size size = new Size(SymbolAttributeSet.SymbolImage.Width, SymbolAttributeSet.SymbolImage.Height);

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
            DrawingContext dc = dv.RenderOpen();
            dc.DrawImage(bg, new Rect(size));
            dc.DrawImage(SymbolAttributeSet.SymbolImage, new Rect(size));
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

            // Copy it to clipboard
            try
            {
                Clipboard.SetImage(resultBitmap);
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception.Message);
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
                    foreach (string tag in loadSet.SymbolTags.Split(';').ToList())
                    {
                        SelectedStyleTags.Add(tag);
                    }

                    //Get the geometry type off a tag on the symbol
                    List<string> reverseTags = loadSet.SymbolTags.Split(';').ToList();
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
                        //No tag found for geometry type, so use it's a point
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
                    else if (SelectedSelectedFeature.FeatureLayer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryLine)
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
                _currentFeatureClassName = _symbolSetMappings.GetFeatureClassFromMapping(_symbolAttributeSet.DisplayAttributes.SymbolSet, GeometryType);
                if (_currentFeatureClassName != null && _currentFeatureClassName != "")
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
            }
        }

        private void SaveSymbolAsFavorite(object parameter)
        {
            //Create copy by serializing/deserializing
            SymbolAttributeSet.FavoriteId = Guid.NewGuid().ToString();
            var json = new JavaScriptSerializer().Serialize(SymbolAttributeSet);
            SymbolAttributeSet favoriteSet = new JavaScriptSerializer().Deserialize<SymbolAttributeSet>(json);

            //Add to favorites
            favoriteSet.GeneratePreviewSymbol();
            Favorites.Add(favoriteSet);

            //Serialize Favorites and save to file
            var favoritesJson = new JavaScriptSerializer().Serialize(Favorites);
            File.WriteAllText(_favoritesFilePath, favoritesJson);
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
            if (openFileDialog.ShowDialog() == true)
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
        }

        #endregion

        #region Event Listeners

        private void OnActiveToolChanged(ArcGIS.Desktop.Framework.Events.ToolEventArgs args)
        {
            if (args.CurrentID == "ProSymbolEditor_DrawFeatureSketchTool")
            {
                //Toggle all down
                AddToMapToolEnabled = true;
            }
            else
            {
                //Disable all toggles
                AddToMapToolEnabled = false;
            }
        }

        private void OnMapSelectionChanged(ArcGIS.Desktop.Mapping.Events.MapSelectionChangedEventArgs args)
        {
            //Dictionary<MapMember, List<long>> selection = args.Selection;

            //Get the selected features from the map and filter out the standalone table selection.
            var selectedFeatures = args.Selection
              .Where(kvp => kvp.Key is BasicFeatureLayer)
              .ToDictionary(kvp => (BasicFeatureLayer)kvp.Key, kvp => kvp.Value);


            //TODO:  Further filter features so it only contains ones that are in layers that are in the military schema
            SelectedFeaturesCollection.Clear();
            foreach (KeyValuePair<BasicFeatureLayer, List<long>> kvp in selectedFeatures)
            {
                foreach(long id in kvp.Value)
                {
                    SelectedFeature newSelectedFeature = new SelectedFeature(kvp.Key, id);
                    SelectedFeaturesCollection.Add(newSelectedFeature);
                }
            }
            
        }

        #endregion

        private int _searchUniformGridRows = 2;
        public int SearchUniformGridRows
        {
            get
            {
                return _searchUniformGridRows;
            }
            set
            {
                _searchUniformGridRows = value;

                NotifyPropertyChanged(() => SearchUniformGridRows);
            }
        }

        private int _searchUniformGridColumns = 1;
        public int SearchUniformGridColumns
        {
            get
            {
                return _searchUniformGridColumns;
            }
            set
            {
                _searchUniformGridColumns = value;

                NotifyPropertyChanged(() => SearchUniformGridColumns);
            }
        }

        private int _searchUniformGridWith;
        public int SearchUniformGridWidth
        {
            get
            {
                return _searchUniformGridWith;
            }
            set
            {
                _searchUniformGridWith = value;

                if (_searchUniformGridColumns < 600)
                {
                    SearchUniformGridColumns = 1;
                    SearchUniformGridRows = 2;
                }
                else
                {
                    SearchUniformGridColumns = 2;
                    SearchUniformGridRows = 1;
                }
            }
        }

        #region Private Methods

        private async Task<StyleProjectItem> GetMilitaryStyleAsync()
        {
            if (Project.Current != null)
            {
                await Project.Current.AddStyleAsync(_mil2525dStyleFullFilePath);

                //Get all styles in the project
                var styles = Project.Current.GetItems<StyleProjectItem>();

                //Get a specific style in the project
                return styles.First(x => x.Name == "mil2525d");
            }

            return null;
        }

        private bool IsStyleInProject()
        {
            if (Project.Current != null)
            {
                IEnumerable<StyleProjectItem> projectStyles = Project.Current.GetItems<StyleProjectItem>();

                foreach(StyleProjectItem projectStyle in projectStyles)
                {
                    if (projectStyle.Path == _mil2525dStyleFullFilePath)
                    {
                        return true;
                    }
                }
            }

            return false;
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

                            string geodatabasePath = geodatabase.GetPath();
                            if (geodatabasePath == ProSymbolEditorModule.Current.MilitaryOverlaySchema.DatabaseName)
                            {
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

                //Check for affiliation tag
                if (_selectedStyleItem != null)
                {
                    string identityCode = "";
                    if (_selectedStyleItem.Tags.ToUpper().Contains("FRIEND"))
                    {
                        identityCode = await GetDomainValueAsync("identity", "Friend");
                    }
                    else if (_selectedStyleItem.Tags.ToUpper().Contains("HOSTILE"))
                    {
                        identityCode = await GetDomainValueAsync("identity", "Hostile/Faker");
                    }
                    else if (_selectedStyleItem.Tags.ToUpper().Contains("NEUTRAL"))
                    {
                        identityCode = await GetDomainValueAsync("identity", "Neutral");
                    }
                    else if (_selectedStyleItem.Tags.ToUpper().Contains("UNKNOWN"))
                    {
                        identityCode = await GetDomainValueAsync("identity", "Unknown");
                    }

                    //Check name of style last to see if it has an affiliation, but no associated tag
                    //But only do this if no tag existed
                    if (identityCode == "")
                    {
                        if (_selectedStyleItem.Name.ToUpper().Contains(": FRIEND"))
                        {
                            identityCode = await GetDomainValueAsync("identity", "Friend");
                        }
                        else if (_selectedStyleItem.Name.ToUpper().Contains(": HOSTILE"))
                        {
                            identityCode = await GetDomainValueAsync("identity", "Hostile/Faker");
                        }
                        else if (_selectedStyleItem.Name.ToUpper().Contains(": NEUTRAL"))
                        {
                            identityCode = await GetDomainValueAsync("identity", "Neutral");
                        }
                        else if (_selectedStyleItem.Name.ToUpper().Contains(": UNKNOWN"))
                        {
                            identityCode = await GetDomainValueAsync("identity", "Unknown");
                        }
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

                //Load any passed in values to selected values for the domain combo boxes
                if (loadSet != null)
                {
                    SymbolAttributeSet.DisplayAttributes.SelectedIdentityDomainPair = MilitaryFieldsInspectorModel.IdentityDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Identity);
                    SymbolAttributeSet.DisplayAttributes.SelectedEchelonDomainPair = MilitaryFieldsInspectorModel.EcholonDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Echelon);
                    SymbolAttributeSet.DisplayAttributes.SelectedMobilityDomainPair = MilitaryFieldsInspectorModel.MobilityDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Mobility);
                    SymbolAttributeSet.DisplayAttributes.SelectedOperationalConditionDomainPair = MilitaryFieldsInspectorModel.OperationalConditionAmplifierDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.OperationalCondition);
                    SymbolAttributeSet.DisplayAttributes.SelectedIndicatorDomainPair = MilitaryFieldsInspectorModel.TfFdHqDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Indicator);
                    SymbolAttributeSet.DisplayAttributes.SelectedStatusDomainPair = MilitaryFieldsInspectorModel.StatusDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Status);
                    SymbolAttributeSet.DisplayAttributes.SelectedContextDomainPair = MilitaryFieldsInspectorModel.ContextDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Context);
                    SymbolAttributeSet.DisplayAttributes.SelectedModifier1DomainPair = MilitaryFieldsInspectorModel.Modifier1DomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Modifier1);
                    SymbolAttributeSet.DisplayAttributes.SelectedModifier2DomainPair = MilitaryFieldsInspectorModel.Modifier2DomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.DisplayAttributes.Modifier2);

                    SymbolAttributeSet.LabelAttributes.SelectedCredibilityDomainPair = MilitaryFieldsInspectorModel.CredibilityDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.Credibility);
                    SymbolAttributeSet.LabelAttributes.SelectedReinforcedDomainPair = MilitaryFieldsInspectorModel.ReinforcedDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.Reinforced);
                    SymbolAttributeSet.LabelAttributes.SelectedReliabilityDomainPair = MilitaryFieldsInspectorModel.ReliabilityDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.Reliability);
                    SymbolAttributeSet.LabelAttributes.SelectedCountryCodeDomainPair = MilitaryFieldsInspectorModel.CountryCodeDomainValues.FirstOrDefault(pair => pair.Code.ToString() == loadSet.LabelAttributes.CountryCode);
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

                            string geodatabasePath = geodatabase.GetPath();
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

                    //Dictionary<string, string> fieldValuesThread = new Dictionary<string, string>();
                    IReadOnlyList<Field> fields = row.GetFields();
                    lock (_lock)
                    {
                        foreach (Field field in fields)
                        {
                            if (field.FieldType == FieldType.Geometry)
                            {
                                continue;
                            }

                            var fieldValue = row[field.Name];

                            if (fieldValue != null)
                            {
                                fieldValues[field.Name] = fieldValue.ToString();
                            }
                        }
                    }

                    //return fieldVa
                });

                //Transfer field values into SymbolAttributes
                SymbolAttributeSet set = new SymbolAttributeSet(fieldValues);
                set.SymbolTags = "";
                EditSelectedFeatureSymbol = set;
                LoadSymbolIntoWorkflow(true);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.ToString());
            }

            return;
        }

        private string[] ParseKeyForSymbolIdCode(string key)
        {
            string[] symbolId = new string[2];

            //todo check if symbolid is in key

            int lastSemicolon = key.LastIndexOf(';');
            string symbolIdCode = key.Substring(lastSemicolon + 1, key.Length - lastSemicolon - 1);
            symbolId[0] = string.Format("{0}{1}", symbolIdCode[0], symbolIdCode[1]);
            symbolId[1] = string.Format("{0}{1}{2}{3}{4}{5}", symbolIdCode[2], symbolIdCode[3], symbolIdCode[4], symbolIdCode[5], symbolIdCode[6], symbolIdCode[7]);

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

        private Task SearchSymbols()
        {
            return QueuedTask.Run(async () =>
            {
                var list = new List<StyleItemType>() { StyleItemType.PointSymbol, StyleItemType.LineSymbol, StyleItemType.PolygonSymbol };

                IEnumerable<Task<IList<SymbolStyleItem>>> symbolQuery = from type in list select _militaryStyleItem.SearchSymbolsAsync(type, _searchString);

                var combinedSymbols = new List<SymbolStyleItem>();
                int outParse;

                // start the query
                var searchTasks = symbolQuery.ToList();

                while (searchTasks.Count > 0)
                {
                    var nextTask = await Task.WhenAny(searchTasks);
                    var results = await nextTask;
                    searchTasks.Remove(nextTask);
                    combinedSymbols.AddRange(results.Where(x => (x.Key.Length == 8 && int.TryParse(x.Key, out outParse)) ||
                                                         (x.Key.Length == 10 && x.Key[8] == '_' && int.TryParse(x.Key[9].ToString(), out outParse))));
                }

                _styleItems = combinedSymbols;

                _progressDialog.Hide();
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

            //Do case insensitive filter
            bool idContains = set.FavoriteId.IndexOf(_favoritesSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            bool tagsContains = set.SymbolTags.IndexOf(_favoritesSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            if (idContains || tagsContains)
            {
                return true;
            }

            return false;
        }

        private void ShowAddInNotEnabledMessageBox()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                string message = "The Military Overlay schema as not detected in any database in your project, so the Pro Symbol Editor cannot continue.  " +
                                 "Would you like to add the Military Overlay Layer Package to add the schema to your project?";

                
                MessageBoxResult result = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(message, "Add-In Disabled", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result.ToString() == "Yes")
                { 
                    if (MapView.Active != null)
                    {
                        AddLayerPackageToMapAsync();
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Your project does not contain any active map.  Create one and try again.");
                    }
                }
            }));
        }

        private async Task AddLayerPackageToMapAsync()
        {
            try
            {
                _progressDialog.Show();

                await QueuedTask.Run(async () =>
                {
                    LayerFactory.CreateLayer(new Uri(System.IO.Path.Combine(ProSymbolUtilities.AddinAssemblyLocation(), "Files", "MilitaryOverlay.lpkx")), MapView.Active.Map);
                    Task<bool> isEnabledMethod = ProSymbolEditorModule.Current.MilitaryOverlaySchema.ShouldAddInBeEnabledAsync();
                    bool enabled = await isEnabledMethod;
                    _progressDialog.Hide();
                });
            }
            catch (Exception exception)
            {
                // Catch any exception found and display a message box.
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception caught: " + exception.Message);
                return;
            }
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
    }

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
