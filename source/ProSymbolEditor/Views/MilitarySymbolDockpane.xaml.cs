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
using System.Windows.Controls.Primitives;

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

        private void SearchUniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as UniformGrid;
            if (grid.ActualWidth > 1000)
            {
                grid.Columns = 2;
                grid.Rows = 1;
            }
            else
            {
                grid.Columns = 1;
                grid.Rows = 2;
            }
        }
    }
}
