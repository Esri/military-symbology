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

            ///////////////////////////////////////////////////////
            // IMPORTANT + WORKAROUND + TRICKY:
            // See: SymbolAttributeSet.GetBitmapImageAsync for explanation of
            // this workaround, remove this flip transformation when workaround
            // no longer needed
            Point transformPoint = new Point(0.5, 0.5);
            ScaleTransform flipTransform = new ScaleTransform();
            flipTransform.ScaleX = -1;
            flipTransform.ScaleY = -1;

            searchTabImage.RenderTransformOrigin = transformPoint;
            searchTabImage.RenderTransform = flipTransform;

            modifyTabImage.RenderTransformOrigin = transformPoint;
            modifyTabImage.RenderTransform = flipTransform;

            symbolTabImage.RenderTransformOrigin = transformPoint;
            symbolTabImage.RenderTransform = flipTransform;

            favoritesTabImage.RenderTransformOrigin = transformPoint;
            favoritesTabImage.RenderTransform = flipTransform;

            textTabSymbolImage.RenderTransformOrigin = transformPoint;
            textTabSymbolImage.RenderTransform = flipTransform;

            coordTabSymbolImage.RenderTransformOrigin = transformPoint;
            coordTabSymbolImage.RenderTransform = flipTransform;
            // END WORKAROUND
            ///////////////////////////////////////////////////////
        }
    }
}
