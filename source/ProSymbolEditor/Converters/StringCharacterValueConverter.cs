using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ProSymbolEditor
{
    public class StringCharacterValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return value;
            }

            string s = value.ToString();
            int characterPosition;
            if (!int.TryParse(parameter.ToString(), out characterPosition) ||
                s.Length <= characterPosition)
            {
                return s;
            }
            return s[characterPosition];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
