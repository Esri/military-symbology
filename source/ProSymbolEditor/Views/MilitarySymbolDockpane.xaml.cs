using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


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

            //Loaded += delegate
            //{
            //    tokenizer.Focus();
            //};

            //this.tokenizer.TokenMatcher = text =>
            //{
            //    if (text.EndsWith(";"))
            //    {
            //        // Remove the ';'
            //        return text.Substring(0, text.Length - 1).Trim().ToUpper();
            //    }

            //    return null;
            //};
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

        //private void searchSymbolsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    //Update the binding to trigger an event handler in the view model
        //    var binding = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
        //    binding.UpdateSource();
        //}
    }
}
