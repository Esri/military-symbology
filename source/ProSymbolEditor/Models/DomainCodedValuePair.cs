using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSymbolEditor
{
    public class DomainCodedValuePair
    {
        private object _code;
        private string _name;

        public DomainCodedValuePair(object value, string name)
        {
            _code = value;
            _name = name;
        }

        public object Code
        {
            get
            {
                return _code;
            }
            set
            {
                _code = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

    }
}
