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

using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ProSymbolEditor
{
    public class DisplayAttributes : PropertyChangedBase
    {
        //Base attributes
        private string _identity;
        private DomainCodedValuePair _selectedIdentityDomainPair;
        private string _status;
        private DomainCodedValuePair _selectedStatusDomainPair;
        private string _operationalCondition;
        private DomainCodedValuePair _selectedOperationalConditionDomainPair;
        private string _echelon;
        private DomainCodedValuePair _selectedEchelonDomainPair;
        private string _indicator;
        private DomainCodedValuePair _selectedIndicatorDomainPair;
        private string _mobility;
        private DomainCodedValuePair _selectedMobilityDomainPair;
        private string _context;
        private DomainCodedValuePair _selectedContextDomainPair;
        private string _modifier1;
        private DomainCodedValuePair _selectedModifier1DomainPair;
        private string _modifier2;
        private DomainCodedValuePair _selectedModifier2DomainPair;

        public DisplayAttributes()  {    }

        #region Getters/Setters

        public string Identity
        {
            get
            {
                return _identity;
            }
            set
            {
                _identity = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore,Browsable(false)]
        public DomainCodedValuePair SelectedIdentityDomainPair
        {
            get
            {
                return _selectedIdentityDomainPair;
            }
            set
            {
                _selectedIdentityDomainPair = value;
                if (_selectedIdentityDomainPair != null)
                {
                    Identity = _selectedIdentityDomainPair.Code.ToString();
                }
                else
                {
                    Identity = "";
                }
                NotifyPropertyChanged(() => SelectedIdentityDomainPair);
            }
        }

        public string Echelon
        {
            get
            {
                return _echelon;
            }
            set
            {
                _echelon = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedEchelonDomainPair
        {
            get
            {
                return _selectedEchelonDomainPair;
            }
            set
            {
                _selectedEchelonDomainPair = value;
                if (_selectedEchelonDomainPair != null)
                {
                    Echelon = _selectedEchelonDomainPair.Code.ToString();
                }
                else
                {
                    Echelon = "";
                }
                NotifyPropertyChanged(() => SelectedEchelonDomainPair);
            }
        }

        public string OperationalCondition
        {
            get
            {
                return _operationalCondition;
            }
            set
            {
                _operationalCondition = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedOperationalConditionDomainPair
        {
            get
            {
                return _selectedOperationalConditionDomainPair;
            }
            set
            {
                _selectedOperationalConditionDomainPair = value;
                if (_selectedOperationalConditionDomainPair != null)
                {
                    OperationalCondition = _selectedOperationalConditionDomainPair.Code.ToString();
                }
                else
                {
                    OperationalCondition = "";
                }
                NotifyPropertyChanged(() => SelectedOperationalConditionDomainPair);
            }
        }

        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedStatusDomainPair
        {
            get
            {
                return _selectedStatusDomainPair;
            }
            set
            {
                _selectedStatusDomainPair = value;
                if (_selectedStatusDomainPair != null)
                {
                    Status = _selectedStatusDomainPair.Code.ToString();
                }
                else
                {
                    Status = "";
                }
                NotifyPropertyChanged(() => SelectedStatusDomainPair);
            }
        }

        public string Indicator
        {
            get
            {
                return _indicator;
            }
            set
            {
                _indicator = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedIndicatorDomainPair
        {
            get
            {
                return _selectedIndicatorDomainPair;
            }
            set
            {
                _selectedIndicatorDomainPair = value;
                if (_selectedIndicatorDomainPair != null)
                {
                    Indicator = _selectedIndicatorDomainPair.Code.ToString();
                }
                else
                {
                    Indicator = "";
                }
                NotifyPropertyChanged(() => SelectedIndicatorDomainPair);
            }
        }

        public string Mobility
        {
            get
            {
                return _mobility;
            }
            set
            {
                _mobility = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedMobilityDomainPair
        {
            get
            {
                return _selectedMobilityDomainPair;
            }
            set
            {
                _selectedMobilityDomainPair = value;
                if (_selectedMobilityDomainPair != null)
                {
                    Mobility = _selectedMobilityDomainPair.Code.ToString();
                }
                else
                {
                    Mobility = "";
                }
                NotifyPropertyChanged(() => SelectedMobilityDomainPair);
            }
        }

        public string Context
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedContextDomainPair
        {
            get
            {
                return _selectedContextDomainPair;
            }
            set
            {
                _selectedContextDomainPair = value;
                if (_selectedContextDomainPair != null)
                {
                    Context = _selectedContextDomainPair.Code.ToString();
                }
                else
                {
                    Context = "";
                }
                NotifyPropertyChanged(() => SelectedContextDomainPair);
            }
        }

        public string Modifier1
        {
            get
            {
                return _modifier1;
            }
            set
            {
                _modifier1 = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedModifier1DomainPair
        {
            get
            {
                return _selectedModifier1DomainPair;
            }
            set
            {
                _selectedModifier1DomainPair = value;
                if (_selectedModifier1DomainPair != null)
                {
                    Modifier1 = _selectedModifier1DomainPair.Code.ToString();
                }
                else
                {
                    Modifier1 = "";
                }
                NotifyPropertyChanged(() => SelectedModifier1DomainPair);
            }
        }

        public string Modifier2
        {
            get
            {
                return _modifier2;
            }
            set
            {
                _modifier2 = value;
                //GenerateSelectedSymbolTagsString();
                //GeneratePreviewSymbol();
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedModifier2DomainPair
        {
            get
            {
                return _selectedModifier2DomainPair;
            }
            set
            {
                _selectedModifier2DomainPair = value;
                if (_selectedModifier2DomainPair != null)
                {
                    Modifier2 = _selectedModifier2DomainPair.Code.ToString();
                }
                else
                {
                    Modifier2 = "";
                }
                NotifyPropertyChanged(() => SelectedModifier2DomainPair);
            }
        }

        #endregion
    }
}
