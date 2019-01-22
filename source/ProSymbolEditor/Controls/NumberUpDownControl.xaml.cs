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

        public static readonly DependencyProperty ValueStartProperty = DependencyProperty.Register("ValueStart", typeof(short), typeof(NumberUpDownControl), new PropertyMetadata((short)0, ValueStartCallBack));

        private static void ValueStartCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;

            var control = d as NumberUpDownControl;

            if (control == null)
                return;

            control.ValueStart = (short)e.NewValue;

            control.CurrentValue = (short?)e.NewValue;
        }

        public short ValueStart
        {
            get { return (short)GetValue(ValueStartProperty); }
            set { SetValue(ValueStartProperty, value); }
        }

        public static readonly DependencyProperty CurrentValueProperty = DependencyProperty.Register("CurrentValue", typeof(object), typeof(NumberUpDownControl), new PropertyMetadata(null));

        public short? CurrentValue
        {
            get { return (short?)GetValue(CurrentValueProperty); }
            set { SetValue(CurrentValueProperty, value); }
        }

        public static readonly DependencyProperty SmallIncrementProperty = DependencyProperty.Register("SmallIncrement", typeof(short), typeof(NumberUpDownControl), new PropertyMetadata((short)1));

        public short SmallIncrement
        {
            get { return (short)GetValue(SmallIncrementProperty); }
            set { SetValue(SmallIncrementProperty, value); }
        }

        public static readonly DependencyProperty LargeIncrementProperty = DependencyProperty.Register("LargeIncrement", typeof(short), typeof(NumberUpDownControl), new PropertyMetadata((short)5));

        public short LargeIncrement
        {
            get { return (short)GetValue(LargeIncrementProperty); }
            set { SetValue(LargeIncrementProperty, value); }
        }

        public static readonly DependencyProperty ValueMinProperty = DependencyProperty.Register("ValueMin", typeof(short), typeof(NumberUpDownControl), new PropertyMetadata(short.MinValue));

        public short ValueMin
        {
            get { return (short)GetValue(ValueMinProperty); }
            set { SetValue(ValueMinProperty, value); }
        }
        public static readonly DependencyProperty ValueMaxProperty = DependencyProperty.Register("ValueMax", typeof(short), typeof(NumberUpDownControl), new PropertyMetadata(short.MaxValue));

        public short ValueMax
        {
            get { return (short)GetValue(ValueMaxProperty); }
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

        private bool _ignoreTextUpdate = false;
        private void NumberUpDownControl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignoreTextUpdate)
                return;

            short number = 0;
            if (!string.IsNullOrWhiteSpace(NumberUpDownControlTextBox.Text))
                if (!short.TryParse(NumberUpDownControlTextBox.Text, out number)) NumberUpDownControlTextBox.Text = ValueStart.ToString();
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

        private void IncrementCurrentValue(short amount)
        {
            if (IsDefault())
                return;

            var newValue = (short?)(CurrentValue + amount);
            CurrentValue = newValue > ValueMax ? ValueMax : newValue;
            UpdateControlText();
        }

        private void DecrementCurrentValue(short amount)
        {
            if (IsDefault())
                return;

            var newValue = (short?)(CurrentValue - amount);
            CurrentValue = newValue < ValueMin ? ValueMin : newValue;
            UpdateControlText();
        }

        private bool IsDefault()
        {
            if (!string.IsNullOrWhiteSpace(NumberUpDownControlTextBox.Text) || CurrentValue.HasValue)
                return false;

            CurrentValue = ValueStart;
            UpdateControlText();

            return true;
        }

        private void UpdateControlText()
        {
            _ignoreTextUpdate = true;
            NumberUpDownControlTextBox.Text = CurrentValue.ToString();
            _ignoreTextUpdate = false;
        }
    }
}
