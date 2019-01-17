using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProSymbolEditor.Controls
{
    /// <summary>
    /// Interaction logic for NumberUpDownControl.xaml
    /// </summary>
    public partial class NumberUpDownControl : UserControl
    {
        public NumberUpDownControl()
        {
            InitializeComponent();
        }

        #region Properties

        public static readonly DependencyProperty ValueStartProperty = DependencyProperty.Register("ValueStart", typeof(int), typeof(NumberUpDownControl), new PropertyMetadata(0, ValueStartCallBack));

        private static void ValueStartCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;

            var control = d as NumberUpDownControl;

            if (control == null)
                return;

            control.ValueStart = (int)e.NewValue;

            control.CurrentValue = (int)e.NewValue;
        }

        public int ValueStart
        {
            get { return (int)GetValue(ValueStartProperty); }
            set { SetValue(ValueStartProperty, value); }
        }

        public static readonly DependencyProperty CurrentValueProperty = DependencyProperty.Register("CurrentValue", typeof(int), typeof(NumberUpDownControl), new PropertyMetadata(0));

        public int CurrentValue
        {
            get { return (int)GetValue(CurrentValueProperty); }
            set { SetValue(CurrentValueProperty, value); }
        }

        public static readonly DependencyProperty SmallIncrementProperty = DependencyProperty.Register("SmallIncrement", typeof(int), typeof(NumberUpDownControl), new PropertyMetadata(1));

        public int SmallIncrement
        {
            get { return (int)GetValue(SmallIncrementProperty); }
            set { SetValue(SmallIncrementProperty, value); }
        }

        public static readonly DependencyProperty LargeIncrementProperty = DependencyProperty.Register("LargeIncrement", typeof(int), typeof(NumberUpDownControl), new PropertyMetadata(5));

        public int LargeIncrement
        {
            get { return (int)GetValue(LargeIncrementProperty); }
            set { SetValue(LargeIncrementProperty, value); }
        }

        public static readonly DependencyProperty ValueMinProperty = DependencyProperty.Register("ValueMin", typeof(int), typeof(NumberUpDownControl), new PropertyMetadata(int.MinValue));

        public int ValueMin
        {
            get { return (int)GetValue(ValueMinProperty); }
            set { SetValue(ValueMinProperty, value); }
        }
        public static readonly DependencyProperty ValueMaxProperty = DependencyProperty.Register("ValueMax", typeof(int), typeof(NumberUpDownControl), new PropertyMetadata(int.MaxValue));

        public int ValueMax
        {
            get { return (int)GetValue(ValueMaxProperty); }
            set { SetValue(ValueMaxProperty, value); }
        }

        #endregion

        private void NumberUpDownControlButtonUP_Click(object sender, RoutedEventArgs e)
        {
            IncrementCurrentValue(SmallIncrement);
        }

        private void NumberUpDownControlButtonDown_Click(object sender, RoutedEventArgs e)
        {
            DecrementCurrentValue(SmallIncrement);
        }

        private void NumberUpDownControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Up)
                IncrementCurrentValue(SmallIncrement);
            else if (e.Key == Key.Down)
                DecrementCurrentValue(SmallIncrement);
            else if(e.Key == Key.PageUp)
                IncrementCurrentValue(LargeIncrement);
            else if(e.Key == Key.PageDown)
                DecrementCurrentValue(LargeIncrement);
        }

        private void NumberUpDownControl_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = 0;
            if (!string.IsNullOrWhiteSpace(NumberUpDownControlTextBox.Text))
                if (!int.TryParse(NumberUpDownControlTextBox.Text, out number)) NumberUpDownControlTextBox.Text = ValueStart.ToString();
            if (number > ValueMax) NumberUpDownControlTextBox.Text = ValueMax.ToString();
            if (number < ValueMin) NumberUpDownControlTextBox.Text = ValueMin.ToString();
            NumberUpDownControlTextBox.SelectionStart = NumberUpDownControlTextBox.Text.Length;
            CurrentValue = number;
        }

        private void NumberUpDownControl_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                IncrementCurrentValue(SmallIncrement);
            else if (e.Delta < 0)
                DecrementCurrentValue(SmallIncrement);
        }

        private void IncrementCurrentValue(int amount)
        {
            var newValue = CurrentValue + amount;
            CurrentValue = newValue > ValueMax ? ValueMax : newValue;
            UpdateControlText();
        }

        private void DecrementCurrentValue(int amount)
        {
            var newValue = CurrentValue - amount;
            CurrentValue = newValue < ValueMin ? ValueMin : newValue;
            UpdateControlText();
        }

        private void UpdateControlText()
        {
            NumberUpDownControlTextBox.Text = CurrentValue.ToString();
        }
    }
}
