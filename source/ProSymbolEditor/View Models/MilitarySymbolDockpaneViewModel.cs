using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Catalog;
using MilitarySymbols;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows;
using Microsoft.Win32;
using ArcGIS.Desktop.Editing;
using ArcGIS.Core.Geometry;
using CoordinateToolLibrary.Models;
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
        private FeatureClass _currentFeatureClass = null;
        private StyleProjectItem _militaryStyleItem = null;
        private SymbolStyleItem _selectedStyleItem = null;
        private SymbolSetMappings _symbolSetMappings = new SymbolSetMappings();
        
        //Lock objects for ObservableCollections
        private static object _identityLock = new object();
        private static object _echelonLock = new object();
        private static object _statusesLock = new object();
        private static object _operationalConditionAmplifierLock = new object();
        private static object _mobilityLock = new object();
        private static object _tfFdHqLock = new object();
        private static object _contextLock = new object();
        private static object _modifier1Lock = new object();
        private static object _modifier2Lock = new object();
        private static object _reinforcedLock = new object();
        private static object _credibilityLock = new object();
        private static object _reliabilityLock = new object();

        //Binded Variables - Text Boxes
        private string _searchString = "";
        private string _selectedStyleTags = "";
        private string _mapCoordinatesString = "";

        //Binded Variables - List Boxes
        private IList<SymbolStyleItem> _styleItems = new List<SymbolStyleItem>();

        //Binded Variables - Other
        private SymbolAttributeSet _symbolAttributeSet = new SymbolAttributeSet();
        private MilitaryFieldsInspectorModel _militaryFieldsInspectorModel = new MilitaryFieldsInspectorModel();
        private int _selectedTabIndex = 0;
        private MapPoint _mapCoordinates;
        public bool _coordinateValid = false;
        private bool _isEnabled = false;

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
            });

            //Create locks for variables that are updated in worker threads
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.IdentityDomainValues, _identityLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.EcholonDomainValues, _echelonLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.StatusesDomainValues, _statusesLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.OperationalConditionAmplifierDomainValues, _operationalConditionAmplifierLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.MobilityDomainValues, _mobilityLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.TfFdHqDomainValues, _tfFdHqLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.ContextDomainValues, _contextLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.Modifier1DomainValues, _modifier1Lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.Modifier2DomainValues, _modifier2Lock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.ReinforcedDomainValues, _reinforcedLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.ReliabilityDomainValues, _reliabilityLock);
            BindingOperations.EnableCollectionSynchronization(MilitaryFieldsInspectorModel.CredibilityDomainValues, _credibilityLock);

            //Set up Commands
            SearchResultCommand = new RelayCommand(SearchStyles, param => true);
            GoToTabCommand = new RelayCommand(GoToTab, param => true);
            ActivateMapToolCommand = new RelayCommand(ActivateCoordinateMapTool, param => true);
            AddToMapCommand = new RelayCommand(AddStyleToMap, param => true);

            _symbolAttributeSet.DateTimeValid = DateTime.Now;
            _symbolAttributeSet.DateTimeExpired = DateTime.Now;
        }

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

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
                NotifyPropertyChanged(() => IsEnabled);

                if (!_isEnabled && IsVisible)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("The Pro Symbol Editor is disabled until the Military Overlay project is opened.");
                    }));
                }
            }
        }

        #region Style Search Bindings

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
            }
        }

        public string SelectedStyleTags
        {
            get
            {
                return _selectedStyleTags;
            }
            set
            {
                _selectedStyleTags = value;

                NotifyPropertyChanged(() => SelectedStyleTags);
            }
        }

        public IList<SymbolStyleItem> StyleItems
        {
            get
            {
                return _styleItems;
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

                if (_selectedStyleItem != null)
                {
                    //Parse key for symbol id codes
                    //TODO: Change to just use the key instead of parsing the tags?
                    SelectedStyleTags = _selectedStyleItem.Tags;

                    _symbolAttributeSet.ResetAttributes();

                    string[] symbolIdCode = ParseKeyForSymbolIdCode(_selectedStyleItem.Tags);
                    _symbolAttributeSet.SymbolSet = symbolIdCode[0];
                    _symbolAttributeSet.SymbolEntity = symbolIdCode[1];

                    //Get feature class name to generate domains
                    _currentFeatureClassName = _symbolSetMappings.GetFeatureClassFromMapping(_symbolAttributeSet.SymbolSet, _selectedStyleItem.ItemType);
                    if (_currentFeatureClassName != null && _currentFeatureClassName != "")
                    {
                        //Generate domains
                        GetMilitaryDomains();
                    }
                }

                NotifyPropertyChanged(() => SelectedStyleItem);
            }
        }

        #endregion

        #region Map Tab Bindings

        public bool CoordinateValid
        {
            get
            {
                return _coordinateValid;
            }
            set
            {
                _coordinateValid = value;
                NotifyPropertyChanged(() => CoordinateValid);
            }
        }

        public MapPoint MapCoordinates
        {
            get
            {
                return _mapCoordinates;
            }
            set
            {
                _mapCoordinates = value;


                CoordinateValid = true;

                NotifyPropertyChanged(() => MapCoordinates);
            }
        }

        public string MapCoordinatesString
        {
            get
            {
                return _mapCoordinatesString;
            }
            set
            {
                _mapCoordinatesString = value;

                MapPoint point;
                var coordType = GetCoordinateType(_mapCoordinatesString, out point);

                if (coordType == CoordinateType.Unknown)
                {
                    //Error
                    CoordinateValid = false;
                }
                else
                {
                    MapCoordinates = point;
                }

                NotifyPropertyChanged(() => MapCoordinatesString);
            }
        }

        #endregion

        #region Commands Get/Sets

        public ICommand SearchResultCommand { get; set; }

        public ICommand GoToTabCommand { get; set; }

        public ICommand ActivateMapToolCommand { get; set; }

        public ICommand AddToMapCommand { get; set; }

        #endregion

        #region Command Methods

        private void ActivateCoordinateMapTool(object parameter)
        {
            FrameworkApplication.SetCurrentToolAsync("ProSymbolEditor_CoordinateMapTool");
        }

        private async void SearchStyles(object parameter)
        {
            //Make sure we have the military style file
            if (_militaryStyleItem == null)
            {
                //Add military style to project
                Task<StyleProjectItem> getMilitaryStyle = GetMilitaryStyleAsync();
                _militaryStyleItem = await getMilitaryStyle;
            }

            //Clear for new search
            if (_styleItems.Count != 0)
                _styleItems.Clear();

            //Get results and populate symbol gallery
            IList<SymbolStyleItem> pointSymbols = await _militaryStyleItem.SearchSymbolsAsync(StyleItemType.PointSymbol, _searchString);
            IList<SymbolStyleItem> lineSymbols = await _militaryStyleItem.SearchSymbolsAsync(StyleItemType.LineSymbol, _searchString);
            IList<SymbolStyleItem> polygonSymbols = await _militaryStyleItem.SearchSymbolsAsync(StyleItemType.PolygonSymbol, _searchString);

            IList<SymbolStyleItem> combinedSymbols = new List<SymbolStyleItem>();
            (combinedSymbols as List<SymbolStyleItem>).AddRange(pointSymbols);
            (combinedSymbols as List<SymbolStyleItem>).AddRange(lineSymbols);
            (combinedSymbols as List<SymbolStyleItem>).AddRange(polygonSymbols);

            int outParse;
            _styleItems = combinedSymbols.Where( x => x.Key.Length == 8).Where(x => int.TryParse(x.Key, out outParse)).ToList();
            //_styleItems.AddRange(lineSymbols);
            //_styleItems.AddRange(polygonSymbols);

            NotifyPropertyChanged(() => StyleItems);
        }

        private void GoToTab(object parameter)
        {
            SelectedTabIndex = Convert.ToInt32(parameter);
        }

        private async void AddStyleToMap(object parameter)
        {
            string message = String.Empty;
            bool creationResult = false;

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
                        // Use the geodatabase.

                        string geodatabasePath = geodatabase.GetPath();
                        if (geodatabasePath.Contains("militaryoverlay.gdb"))
                        {
                            //Correct GDB, open the current selected feature class
                            FeatureClass testfc = geodatabase.OpenDataset<FeatureClass>(_currentFeatureClassName);
                            using (testfc)
                            using (FeatureClassDefinition facilitySiteDefinition = testfc.GetDefinition())
                            {
                                EditOperation editOperation = new EditOperation();
                                editOperation.Name = "Military Symbol Insert";
                                editOperation.Callback(context =>
                                {
                                    try
                                    {
                                        RowBuffer rowBuffer = testfc.CreateRowBuffer();
                                        _symbolAttributeSet.PopulateRowBufferWithAttributes(ref rowBuffer);
                                        rowBuffer["Shape"] = GeometryEngine.Project(MapCoordinates, facilitySiteDefinition.GetSpatialReference());// as ArcGIS.Core.Geometry.Geometry;

                                        Feature feature = testfc.CreateRow(rowBuffer);
                                        feature.Store();

                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature);
                                    }
                                    catch (GeodatabaseException geodatabaseException)
                                    {
                                        message = geodatabaseException.Message;
                                    }
                                }, testfc);

                                var task = editOperation.ExecuteAsync();
                                creationResult = task.Result;
                                if (!creationResult)
                                {
                                    message = editOperation.ErrorMessage;
                                }

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

        private void Edits(EditOperation.IEditContext context)
        {
            ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                Dictionary<string, object> atts = new Dictionary<string, object>();
                //atts.Add("identity", _symbolAttributeSet.Identity);
                atts.Add("Shape", MapCoordinates);   // I know the shape field is called Shape - but dont assume

                var editOperation = new EditOperation();
                editOperation.Name = "Add Military Symbol";
                //var disLayer = ArcGIS.Desktop.Mapping.MapView.Active.Map.FindLayers("Air").FirstOrDefault() as BasicFeatureLayer;

                editOperation.Create(_currentFeatureClass, atts);
                //editOperation.EditOperationType = EditOperationType.Long;

                //bool success = editOperation.Execute();
            });
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
                    case "MapCoordinatesString":
                        if (!_coordinateValid)
                        {
                            Error = "The coordinates are invalid";
                        }
                        break;
                }

                return Error;
            }
        }

        #endregion

        private CoordinateType GetCoordinateType(string input, out MapPoint point)
        {
            point = null;

            // DD
            CoordinateDD dd;
            if (CoordinateDD.TryParse(input, out dd))
            {
                point = QueuedTask.Run(() =>
                {
                    ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                    return MapPointBuilder.CreateMapPoint(dd.Lon, dd.Lat, 0, sptlRef);
                }).Result;
                return CoordinateType.DD;
            }

            // DDM
            CoordinateDDM ddm;
            if (CoordinateDDM.TryParse(input, out ddm))
            {
                dd = new CoordinateDD(ddm);
                point = QueuedTask.Run(() =>
                {
                    ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                    return MapPointBuilder.CreateMapPoint(dd.Lon, dd.Lat, 0, sptlRef);
                }).Result;
                return CoordinateType.DDM;
            }
            // DMS
            CoordinateDMS dms;
            if (CoordinateDMS.TryParse(input, out dms))
            {
                dd = new CoordinateDD(dms);
                point = QueuedTask.Run(() =>
                {
                    ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                    return MapPointBuilder.CreateMapPoint(dd.Lon, dd.Lat, 0, sptlRef);
                }).Result;
                return CoordinateType.DMS;
            }

            CoordinateGARS gars;
            if (CoordinateGARS.TryParse(input, out gars))
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                        var tmp = MapPointBuilder.FromGeoCoordinateString(gars.ToString("", new CoordinateGARSFormatter()), sptlRef, GeoCoordinateType.GARS, FromGeoCoordinateMode.Default);
                        tmp = MapPointBuilder.CreateMapPoint(tmp.X, tmp.Y, 0, sptlRef);
                        return tmp;
                    }).Result;

                    return CoordinateType.GARS;
                }
                catch { }
            }

            CoordinateMGRS mgrs;
            if (CoordinateMGRS.TryParse(input, out mgrs))
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                        var tmp = MapPointBuilder.FromGeoCoordinateString(mgrs.ToString("", new CoordinateMGRSFormatter()), sptlRef, GeoCoordinateType.MGRS, FromGeoCoordinateMode.Default);
                        tmp = MapPointBuilder.CreateMapPoint(tmp.X, tmp.Y, 0, sptlRef);
                        return tmp;
                    }).Result;

                    return CoordinateType.MGRS;
                }
                catch { }
            }

            CoordinateUSNG usng;
            if (CoordinateUSNG.TryParse(input, out usng))
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                        var tmp = MapPointBuilder.FromGeoCoordinateString(usng.ToString("", new CoordinateMGRSFormatter()), sptlRef, GeoCoordinateType.USNG, FromGeoCoordinateMode.Default);
                        tmp = MapPointBuilder.CreateMapPoint(tmp.X, tmp.Y, 0, sptlRef);
                        return tmp;
                    }).Result;

                    return CoordinateType.USNG;
                }
                catch { }
            }

            CoordinateUTM utm;
            if (CoordinateUTM.TryParse(input, out utm))
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        ArcGIS.Core.Geometry.SpatialReference sptlRef = SpatialReferenceBuilder.CreateSpatialReference(4326);
                        var tmp = MapPointBuilder.FromGeoCoordinateString(utm.ToString("", new CoordinateUTMFormatter()), sptlRef, GeoCoordinateType.UTM, FromGeoCoordinateMode.Default);
                        tmp = MapPointBuilder.CreateMapPoint(tmp.X, tmp.Y, 0, sptlRef);
                        return tmp;
                    }).Result;

                    return CoordinateType.UTM;
                }
                catch { }
            }


            return CoordinateType.Unknown;
        }


        public async Task<StyleProjectItem> GetMilitaryStyleAsync()
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

        private async void GetMilitaryDomains()
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
                        // Use the geodatabase.

                        string geodatabasePath = geodatabase.GetPath();
                        if (geodatabasePath.Contains("militaryoverlay.gdb"))
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
                        }

                    }
                }
            });
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

        private async Task<bool> ShouldAddInBeEnabled()
        {
            //If we can get both the style and database, then enable the add-in
            if (Project.Current == null)
            {
                //No open project
                return false;
            }

            //Get style
            //Task<StyleProjectItem> getMilitaryStyle = GetMilitaryStyleAsync();
            //StyleProjectItem militaryStyleProjectItem = await getMilitaryStyle;

            //if (militaryStyleProjectItem == null)
            //{
            //    return false;
            //}

            //Get database
            IEnumerable<GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();
            Geodatabase militaryGeodatabase = await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
            {
                foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                {
                    using (Datastore datastore = gdbProjectItem.GetDatastore())
                    {
                        //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                        if (datastore is UnknownDatastore)
                            continue;
                        Geodatabase geodatabase = datastore as Geodatabase;
                        // Use the geodatabase.

                        string geodatabasePath = geodatabase.GetPath();
                        if (geodatabasePath.Contains("militaryoverlay.gdb"))
                        {
                            return geodatabase;
                        }

                    }
                }

                return null;
            });

            if (militaryGeodatabase == null)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class MilitarySymbolDockpane_ShowButton : Button
    {
        protected override void OnClick()
        {
            if (!ProSymbolEditorModule._isEnabled)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("The Pro Symbol Editor is disabled until the Military Overlay project is opened.");
                }));
            }

            MilitarySymbolDockpaneViewModel.Show();
        }
    }
}
