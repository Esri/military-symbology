using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSymbolEditor
{
    public class DomainCodedValuePair
    {
        public object Code { get; set; }
        public string Name { get; set; }

        public DomainCodedValuePair(object value, string name)
        {
            Code = value;
            Name = name;
        }
    }
}
