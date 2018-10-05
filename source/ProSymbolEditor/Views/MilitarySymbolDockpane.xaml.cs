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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProSymbolEditor
{
    /// <summary>
    /// Interaction logic for MilitarySymbolDockpaneView.xaml
    /// </summary>
    public partial class MilitarySymbolDockpaneView : UserControl
    {
        public MilitarySymbolDockpaneView()
        {
            InitializeComponent();
        }

        private void DockPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;

            MilitarySymbolDockpaneViewModel vm = this.DataContext as MilitarySymbolDockpaneViewModel;

            vm.DockPanel_MouseDown(e);
        }
    }
}
