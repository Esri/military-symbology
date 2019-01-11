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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProSymbolEditor
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : ArcGIS.Desktop.Framework.Controls.ProWindow, INotifyPropertyChanged
    {
        public bool IsSettingsReadOnly
        {
            get; set;
        }

        public bool IsSettingsNotReadOnly
        {
            get { return !IsSettingsReadOnly; }
        }

        public ProSymbolUtilities.SupportedStandardsType Standard
        {
            get; set;
        }

        public ObservableCollection<string> SymbologyStandards { get; set; }

        public string SelectedSymbologyStandard
        {
            get
            {
                return ProSymbolUtilities.GetStandardLabel(Standard);
            }
            set
            {
                string standardString = value;

                Standard = ProSymbolUtilities.GetStandardFromLabel(standardString);
            }
        }


        public bool IsSelectDBEnabled
        {
            get; set;
        }

        public string DefaultDatabase
        {
            get
            {
                if (string.IsNullOrEmpty(_defaultDatabase))
                    return "{default}";
                else
                    return _defaultDatabase;
            }
            set
            {
                _defaultDatabase = value;
            }
        }
        private string _defaultDatabase;

        public bool DefaultDatabaseChanged
        {
            get;
            set;
        }

        public SettingsWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            IsSettingsReadOnly = false;
            DefaultDatabaseChanged = false;
            IsSelectDBEnabled = false;

            SymbologyStandards = new ObservableCollection<string>();
            SymbologyStandards.Add(ProSymbolUtilities.GetStandardLabel(ProSymbolUtilities.SupportedStandardsType.mil2525c_b2));
            SymbologyStandards.Add(ProSymbolUtilities.GetStandardLabel(ProSymbolUtilities.SupportedStandardsType.mil2525d));

            // APP6D only available after 2.2
            if ((ProSymbolUtilities.ProMajorVersion >= 2) && (ProSymbolUtilities.ProMinorVersion >= 2))
                SymbologyStandards.Add(ProSymbolUtilities.GetStandardLabel(ProSymbolUtilities.SupportedStandardsType.app6d));
        }

        public void ShowDialog(Window owner)
        {
            this.Owner = owner;
            this.ShowDialog();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // or give user the option of selecting workspace:
            string selectedGDB = ProSymbolUtilities.BrowseItem(ArcGIS.Desktop.Catalog.ItemFilters.geodatabases);

            if (string.IsNullOrEmpty(selectedGDB))
                return;

            if (DefaultDatabase != selectedGDB)
            {
                DefaultDatabaseChanged = true;
                DefaultDatabase = selectedGDB;

                // See if the selected database already contains a standard, 
                // if so set the standard, and disable the control

                var selectedGDBasItem = ArcGIS.Desktop.Core.ItemFactory.
                    Instance.Create(selectedGDB);

                bool hasStandard = false;
                ProSymbolUtilities.SupportedStandardsType standardFound = 
                    ProSymbolUtilities.SupportedStandardsType.mil2525d;

                foreach (ProSymbolUtilities.SupportedStandardsType standard in
                    Enum.GetValues(typeof(ProSymbolUtilities.SupportedStandardsType)))
                {
                    bool containsStandard = 
                        await ProSymbolEditorModule.Current.MilitaryOverlaySchema.
                            GDBContainsMilitaryOverlay(
                            selectedGDBasItem as ArcGIS.Desktop.Catalog.GDBProjectItem, 
                            standard);

                    if (containsStandard)
                    {
                        hasStandard = true;
                        standardFound = standard;
                        break;
                    }
                }

                if (hasStandard)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        "Database: " + selectedGDB + "\n" +
                        "contains a schema for standard: \n" +
                        ProSymbolUtilities.GetStandardLabel(standardFound) + ".\n" +
                        "Setting standard to this value."
                        , "Database Contains Schema",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    Standard = standardFound;
                    RaisePropertyChanged("SelectedSymbologyStandard");
                }

                // Disable/enable the standard button if GDB had schema 
                IsSettingsReadOnly = hasStandard;
                RaisePropertyChanged("IsSettingsNotReadOnly");

                RaisePropertyChanged("DefaultDatabase");
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
