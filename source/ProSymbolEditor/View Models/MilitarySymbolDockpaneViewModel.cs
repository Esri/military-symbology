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

namespace ProSymbolEditor
{
    internal class MilitarySymbolDockpaneViewModel : DockPane
    {
        //Member Variables
        private const string _dockPaneID = "ProSymbolEditor_MilitarySymbolDockpane";
        private const string _menuID = "ProSymbolEditor_MilitarySymbolDockpane_Menu";
        private const string _mil2525dStyleFilePath = @"C:\Program Files\ArcGIS\Pro\Resources\Dictionaries\mil2525d\mil2525d.stylx";
        private string _currentFeatureClassName = "";
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

        //Binded Variables - Text Boxes
        private string _searchString = "";

        //Binded Variables - List Boxes
        private IList<SymbolStyleItem> _styleItems = new List<SymbolStyleItem>();

        //Binded Variables - Combo Boxes
        ObservableCollection<DomainCodedValuePair> _identityDomainValues = new ObservableCollection<DomainCodedValuePair>();
        ObservableCollection<DomainCodedValuePair> _echolonDomainValues = new ObservableCollection<DomainCodedValuePair>();
        ObservableCollection<DomainCodedValuePair> _statusesDomainValues = new ObservableCollection<DomainCodedValuePair>();
        ObservableCollection<DomainCodedValuePair> _operationalConditionAmplifierDomainValues = new ObservableCollection<DomainCodedValuePair>();
        ObservableCollection<DomainCodedValuePair> _mobilityDomainValues = new ObservableCollection<DomainCodedValuePair>();
        ObservableCollection<DomainCodedValuePair> _tfFdHqDomainValues = new ObservableCollection<DomainCodedValuePair>();
        ObservableCollection<DomainCodedValuePair> _contextDomainValues = new ObservableCollection<DomainCodedValuePair>();
        ObservableCollection<DomainCodedValuePair> _modifier1DomainValues = new ObservableCollection<DomainCodedValuePair>();
        ObservableCollection<DomainCodedValuePair> _modifier2DomainValues = new ObservableCollection<DomainCodedValuePair>();

        //Binded Variables - Other
        private SymbolAttributeSet _symbolAttributeSet = new SymbolAttributeSet();
        private int _selectedTabIndex = 0;

        //Commands
        private ICommand _searchResultCommand = null;
        private ICommand _goToModifyTabCommand = null;

        protected MilitarySymbolDockpaneViewModel()
        {
            ArcGIS.Desktop.Core.Events.ProjectOpenedEvent.Subscribe(async (args) =>
            {
                //Add military style to project
                Task<StyleProjectItem> getMilitaryStyle = GetMilitaryStyleAsync();
                _militaryStyleItem = await getMilitaryStyle;
            });

            //Create locks for variables that are updated in worker threads
            BindingOperations.EnableCollectionSynchronization(_identityDomainValues, _identityLock);
            BindingOperations.EnableCollectionSynchronization(_echolonDomainValues, _echelonLock);
            BindingOperations.EnableCollectionSynchronization(_statusesDomainValues, _statusesLock);
            BindingOperations.EnableCollectionSynchronization(_operationalConditionAmplifierDomainValues, _operationalConditionAmplifierLock);
            BindingOperations.EnableCollectionSynchronization(_mobilityDomainValues, _mobilityLock);
            BindingOperations.EnableCollectionSynchronization(_tfFdHqDomainValues, _tfFdHqLock);
            BindingOperations.EnableCollectionSynchronization(_contextDomainValues, _contextLock);
            BindingOperations.EnableCollectionSynchronization(_modifier1DomainValues, _modifier1Lock);
            BindingOperations.EnableCollectionSynchronization(_modifier2DomainValues, _modifier2Lock);
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

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "My DockPane";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }

        #region Properties for user inputs

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
                    string[] symbolIdCode = ParseKeyForSymbolIdCode(_selectedStyleItem.Tags);
                    _symbolAttributeSet.SymbolSet = symbolIdCode[0];
                    _symbolAttributeSet.SymbolEntity = symbolIdCode[1];

                    _symbolAttributeSet.Echelon = "";
                    _symbolAttributeSet.Identity = "";
                    _symbolAttributeSet.OperationalCondition = "";
                    _symbolAttributeSet.Statuses = "";
                    _symbolAttributeSet.Mobility = "";
                    _symbolAttributeSet.Indicator = "";
                    _symbolAttributeSet.Context = "";
                    _symbolAttributeSet.Modifier1 = "";
                    _symbolAttributeSet.Modifier2 = "";

                    //Get feature class name to generate domains
                    _currentFeatureClassName = _symbolSetMappings.SymbolsDictionaryMapping[_symbolAttributeSet.SymbolSet];
                    if (_currentFeatureClassName != null)
                    {
                        //Generate domains
                        GetMilitaryDomains();
                    }
                }

                NotifyPropertyChanged(() => SelectedStyleItem);
            }
        }

        public ObservableCollection<DomainCodedValuePair> IdentityDomainValues
        {
            get
            {
                return _identityDomainValues;
            }
            set
            {
                _identityDomainValues = value;
            }
        }

        public ObservableCollection<DomainCodedValuePair> EcholonsDomainValues
        {
            get
            {
                return _echolonDomainValues;
            }
            set
            {
                _echolonDomainValues = value;
            }
        }

        public ObservableCollection<DomainCodedValuePair> StatusesDomainValues
        {
            get
            {
                return _statusesDomainValues;
            }
            set
            {
                _statusesDomainValues = value;
            }
        }

        public ObservableCollection<DomainCodedValuePair> OperationalConditionAmplifierDomainValues
        {
            get
            {
                return _operationalConditionAmplifierDomainValues;
            }
            set
            {
                _operationalConditionAmplifierDomainValues = value;
            }
        }

        public ObservableCollection<DomainCodedValuePair> IndicatorDomainValues
        {
            get
            {
                return _tfFdHqDomainValues;
            }
            set
            {
                _tfFdHqDomainValues = value;
            }
        }

        public ObservableCollection<DomainCodedValuePair> MobilityDomainValues
        {
            get
            {
                return _mobilityDomainValues;
            }
            set
            {
                _mobilityDomainValues = value;
            }
        }

        public ObservableCollection<DomainCodedValuePair> ContextDomainValues
        {
            get
            {
                return _contextDomainValues;
            }
            set
            {
                _contextDomainValues = value;
            }
        }

        public ObservableCollection<DomainCodedValuePair> Modifier1DomainValues
        {
            get
            {
                return _modifier1DomainValues;
            }
            set
            {
                _modifier1DomainValues = value;
            }
        }

        public ObservableCollection<DomainCodedValuePair> Modifier2DomainValues
        {
            get
            {
                return _modifier2DomainValues;
            }
            set
            {
                _modifier2DomainValues = value;
            }
        }

        #endregion

        #region Commands

        public ICommand SearchResultCommand
        {
            get
            {
                if (_searchResultCommand == null)
                {
                    _searchResultCommand = new RelayCommand(SearchStyles, param => true);
                }
                return _searchResultCommand;
            }
        }

        public ICommand GoToModifyTabCommand
        {
            get
            {
                if (_goToModifyTabCommand == null)
                {
                    _goToModifyTabCommand = new RelayCommand(GoToTab, param => true);
                }

                return _goToModifyTabCommand;
            }
        }

        #endregion

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
            
            //TODO: uncomment for lines and polygons
            //(combinedSymbols as List<SymbolStyleItem>).AddRange(lineSymbols);
            //(combinedSymbols as List<SymbolStyleItem>).AddRange(polygonSymbols);

            _styleItems = combinedSymbols;
            // _styleItems.AddRange(lineSymbols);
            //_styleItems.AddRange(polygonSymbols);

            NotifyPropertyChanged(() => StyleItems);
        }

        private void ParseKeyValues()
        {

        }

        private void GoToTab(object parameter)
        {
            SelectedTabIndex = Convert.ToInt32(parameter);
        }

        public async Task<StyleProjectItem> GetMilitaryStyleAsync()
        {
            if (Project.Current != null)
            {
                //_militaryStyleItem = await QueuedTask.Run<StyleProjectItem>(() =>
                //{
                //    Project.Current.AddStyleAsync(_mil2525dStyleFilePath);

                //    //Get all styles in the project
                //    var styles = Project.Current.GetItems<StyleProjectItem>();

                //    //Get a specific style in the project
                //    return styles.First(x => x.Name == "mil2525d");
                //});
                await Project.Current.AddStyleAsync(_mil2525dStyleFilePath);

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
                            using (ArcGIS.Core.Data.FeatureClass airFeatureClass = geodatabase.OpenDataset<FeatureClass>(_currentFeatureClassName))
                            {
                                ArcGIS.Core.Data.FeatureClassDefinition facilitySiteDefinition = airFeatureClass.GetDefinition();
                                IReadOnlyList<ArcGIS.Core.Data.Field> fields = facilitySiteDefinition.GetFields();

                                GetDomainAndPopulateList(fields, "identity", _identityDomainValues);
                                GetDomainAndPopulateList(fields, "echelon", _echolonDomainValues);
                                GetDomainAndPopulateList(fields, "status", _statusesDomainValues);
                                GetDomainAndPopulateList(fields, "operationalcondition", _operationalConditionAmplifierDomainValues);
                                GetDomainAndPopulateList(fields, "mobility", _mobilityDomainValues);
                                GetDomainAndPopulateList(fields, "indicator", _tfFdHqDomainValues);
                                GetDomainAndPopulateList(fields, "context", _contextDomainValues);
                                GetDomainAndPopulateList(fields, "modifier1", _modifier1DomainValues);
                                GetDomainAndPopulateList(fields, "modifier2", _modifier2DomainValues);
                            }
                        }

                    }
                }
            });
        }

        private void GetDomainAndPopulateList(IReadOnlyList<Field> fields, string fieldName, ObservableCollection<DomainCodedValuePair> memberCodedValueDomains)//SortedList<object, string> memberCodedValueDomains)
        {
            ArcGIS.Core.Data.Field foundField = fields.FirstOrDefault(field => field.Name == fieldName);

            //Clear out any old values
            memberCodedValueDomains.Clear();

            if (foundField != null)
            {
                Domain domain = foundField.GetDomain();
                var codedValueDomain = domain as CodedValueDomain;
                SortedList<object, string> codedValuePairs = codedValueDomain.GetCodedValuePairs();

                foreach (KeyValuePair<object, string> pair in codedValuePairs)
                {
                    DomainCodedValuePair domainObjectPair = new DomainCodedValuePair(pair.Key, pair.Value);
                    memberCodedValueDomains.Add(domainObjectPair);//pair.Value);
                }
            }
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
