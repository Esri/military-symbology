using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProSymbolEditor.Views
{
    /// <summary>
    /// Interaction logic for SymbolView.xaml
    /// </summary>
    public partial class SymbolView : UserControl
    {
        public SymbolView()
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

            symbolTabImage.RenderTransformOrigin = transformPoint;
            symbolTabImage.RenderTransform = flipTransform;
            // END WORKAROUND
            ///////////////////////////////////////////////////////
        }
    }
}
