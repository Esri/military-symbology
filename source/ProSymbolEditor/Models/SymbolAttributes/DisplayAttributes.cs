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
        public override bool Equals(object obj)
        {
            if ((obj == null) || (GetType() != obj.GetType()))
                return false;

            bool equals = GetHashCode() == obj.GetHashCode();

            return equals;
        }

        public override int GetHashCode()
        {
            int hashcode;
            const int PRIME = 263;
            unchecked
            {
                hashcode = PRIME * (_symbolSet != null ? _symbolSet.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_symbolEntity != null ? _symbolEntity.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_identity != null ? _identity.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_status != null ? _status.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_operationalCondition != null ? _operationalCondition.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_echelon != null ? _echelon.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_indicator != null ? _indicator.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_mobility != null ? _mobility.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_context != null ? _context.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_modifier1 != null ? _context.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_modifier2 != null ? _context.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_extendedFunctionCode != null ? _extendedFunctionCode.GetHashCode() : 0);
            }

            return hashcode;
        }

        //Base attributes
        private string _symbolSet;
        private string _symbolEntity;
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
        private string _extendedFunctionCode;
        private DomainCodedValuePair _selectedExtendedFunctionCodeDomainPair;

        public string LegacySymbolIdCode
        {
            get
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
                string extendedFunctionCode = ExtendedFunctionCode;
                if ((ProSymbolUtilities.Standard == ProSymbolUtilities.SupportedStandardsType.mil2525c_b2)
                    && (!String.IsNullOrEmpty(extendedFunctionCode)) && (extendedFunctionCode.Length >= 10))
                {
                    sb.Append(extendedFunctionCode[0]);
                    // TODO: Check for Alpha
                    if (String.IsNullOrEmpty(Identity))
                        sb.Append('U');
                    else
                        sb.Append(Identity);
                    sb.Append(extendedFunctionCode[2]);

                    // TODO: Check for Alpha
                    if (String.IsNullOrEmpty(Status))
                        sb.Append('P');
                    else
                        sb.Append(Status);
                    sb.Append(extendedFunctionCode.Substring(4, 6));
                    // TODO: Check for Alpha
                    if (String.IsNullOrEmpty(Indicator))
                        sb.Append('-');
                    else
                        sb.Append(Indicator);
                    if (String.IsNullOrEmpty(Echelon))
                        sb.Append('-');
                    else
                        sb.Append(Echelon);
                    sb.Append("---");
                }

                return sb.ToString();
            }
                
        }

        public string ExtendedFunctionCode
        {
            get
            {
                return _extendedFunctionCode;
            }
            set
            {
                _extendedFunctionCode = value;
                NotifyPropertyChanged(() => LegacySymbolIdCode);
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedExtendedFunctionCodeDomainPair
        {
            get
            {
                return _selectedExtendedFunctionCodeDomainPair;
            }
            set
            {
                _selectedExtendedFunctionCodeDomainPair = value;
                if (_selectedExtendedFunctionCodeDomainPair != null)
                {
                    _extendedFunctionCode = _selectedExtendedFunctionCodeDomainPair.Code.ToString();
                    NotifyPropertyChanged(() => SelectedExtendedFunctionCodeDomainPair);
                }
                else
                {
                    _extendedFunctionCode = "";
                }
            }
        }

        public DisplayAttributes()  {    }

        #region Getters/Setters

        public string SymbolSet
        {
            get
            {
                return _symbolSet;
            }
            set
            {
                _symbolSet = value;
                NotifyPropertyChanged(() => SymbolSet);
            }
        }

        public string SymbolEntity
        {
            get
            {
                return _symbolEntity;
            }
            set
            {
                _symbolEntity = value;
                NotifyPropertyChanged(() => SymbolEntity);
            }
        }

        public string Identity
        {
            get
            {
                return _identity;
            }
            set
            {
                _identity = value;
                NotifyPropertyChanged(() => Identity);
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
                    NotifyPropertyChanged(() => SelectedIdentityDomainPair);
                }
                else
                {
                    Identity = "";
                }
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
                NotifyPropertyChanged(() => Echelon);
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
                    NotifyPropertyChanged(() => SelectedEchelonDomainPair);
                }
                else
                {
                    Echelon = "";
                }
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
                NotifyPropertyChanged(() => OperationalCondition);
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
                    NotifyPropertyChanged(() => SelectedOperationalConditionDomainPair);
                }
                else
                {
                    OperationalCondition = "";
                }
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
                NotifyPropertyChanged(() => Status);
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
                    NotifyPropertyChanged(() => SelectedStatusDomainPair);
                }
                else
                {
                    Status = "";
                }
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
                NotifyPropertyChanged(() => Indicator);
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
                    NotifyPropertyChanged(() => SelectedIndicatorDomainPair);
                }
                else
                {
                    Indicator = "";
                }
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
                NotifyPropertyChanged(() => Mobility);
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
                    NotifyPropertyChanged(() => SelectedMobilityDomainPair);
                }
                else
                {
                    Mobility = "";
                }
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
                NotifyPropertyChanged(() => Context);
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
                NotifyPropertyChanged(() => Modifier1);
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
                NotifyPropertyChanged(() => Modifier2);
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
