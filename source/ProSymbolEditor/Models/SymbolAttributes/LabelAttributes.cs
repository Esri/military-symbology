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
    public class LabelAttributes : PropertyChangedBase
    {
        private DateTime? _dateTimeValid;
        private DateTime? _dateTimeExpired;
        private string _staffComments;
        private string _additionalInformation;
        private string _uniqueDesignation;
        private string _type;
        private string _commonidentifier;
        private short? _speed;
        private short? _direction;
        private string _higherFormation;
        private string _reinforced;
        private DomainCodedValuePair _selectedReinforcedDomainPair;
        private string _credibility;
        private DomainCodedValuePair _selectedCredibilityDomainPair;
        private string _reliability;
        private DomainCodedValuePair _selectedReliabilityDomainPair;
        private string _signatureEquipment;
        private DomainCodedValuePair _selectedSignatureEquipmentDomainPair;
        private string _countryCode;
        private DomainCodedValuePair _selectedCountryCodeDomainPair;
        private string lengthError = "The label entered is at the maximum allowable length for this feature field";


        [ScriptIgnore, Browsable(false)]
        public static bool MaxLengthValidationOn { get; set; } = true;

        [ScriptIgnore, Browsable(false)]
        public int MaxLen30 => 30;
        [ScriptIgnore, Browsable(false)]
        public int MaxLen24 => 24;
        [ScriptIgnore, Browsable(false)]
        public int MaxLen20 => 20;
        [ScriptIgnore, Browsable(false)]
        public int MaxLen21 => 21;
        [ScriptIgnore, Browsable(false)]
        public int MaxLen12 => 12;

        public LabelAttributes() {  }

        public override string ToString()
        {
            return "Label Attributes";
        }

        public string Name
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (!string.IsNullOrEmpty(UniqueDesignation))
                    sb.Append(UniqueDesignation + ProSymbolUtilities.NameSeparator);

                if (!string.IsNullOrEmpty(HigherFormation))
                    sb.Append(HigherFormation + ProSymbolUtilities.NameSeparator);

                if (!string.IsNullOrEmpty(AdditionalInformation))
                    sb.Append(AdditionalInformation + ProSymbolUtilities.NameSeparator);

                if (!string.IsNullOrEmpty(CountryCode))
                    sb.Append(CountryCode);

                if (sb.Length == 0)
                {
                    sb.Append("Label Attributes");
                }

                return sb.ToString();
            }
        }

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
                hashcode = PRIME * (_staffComments != null ? _staffComments.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_additionalInformation != null ? _additionalInformation.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_uniqueDesignation != null ? _uniqueDesignation.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_type != null ? _type.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_commonidentifier != null ? _commonidentifier.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_speed != null ? _speed.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_direction != null ? _direction.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_higherFormation != null ? _higherFormation.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_reinforced != null ? _reinforced.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_credibility != null ? _credibility.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_reliability != null ? _reliability.GetHashCode() : 0);
                hashcode = (hashcode * PRIME) ^ (_countryCode != null ? _countryCode.GetHashCode() : 0);
            }

            return hashcode;
        }

        #region Getters/Setters
        public DateTime? DateTimeValid
        {
            get
            {
                return _dateTimeValid;
            }
            set
            {
                _dateTimeValid = value;

                NotifyPropertyChanged(() => DateTimeValid);
            }
        }

        public DateTime? DateTimeExpired
        {
            get
            {
                return _dateTimeExpired;
            }
            set
            {
                _dateTimeExpired = value;

                NotifyPropertyChanged(() => DateTimeExpired);
            }
        }

        public string UniqueDesignation
        {
            get
            {
                return _uniqueDesignation;
            }
            set
            {
                _uniqueDesignation = value;

                if (MaxLengthValidationOn && 
                    !string.IsNullOrEmpty(_uniqueDesignation) && (_uniqueDesignation.Length >= MaxLen30))
                    throw new ArgumentException(lengthError);

                NotifyPropertyChanged(() => UniqueDesignation);
            }
        }

        public string StaffComments
        {
            get
            {
                return _staffComments;
            }
            set
            {
                _staffComments = value;

                if (MaxLengthValidationOn && 
                    !string.IsNullOrEmpty(_staffComments) && (_staffComments.Length >= MaxLen20))
                    throw new ArgumentException(lengthError);

                NotifyPropertyChanged(() => StaffComments);
            }
        }

        public string AdditionalInformation
        {
            get
            {
                return _additionalInformation;
            }
            set
            {
                _additionalInformation = value;

                if (MaxLengthValidationOn &&
                    !string.IsNullOrEmpty(_additionalInformation) && (_additionalInformation.Length >= MaxLen20))
                    throw new ArgumentException(lengthError);

                NotifyPropertyChanged(() => AdditionalInformation);
            }
        }

        public string Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;

                if (MaxLengthValidationOn &&
                    !string.IsNullOrEmpty(_type) && (_type.Length >= MaxLen24))
                    throw new ArgumentException(lengthError);

                NotifyPropertyChanged(() => Type);
            }
        }

        public string CommonIdentifier
        {
            get
            {
                return _commonidentifier;
            }
            set
            {
                _commonidentifier = value;

                if (MaxLengthValidationOn &&
                    !string.IsNullOrEmpty(_commonidentifier) && (_commonidentifier.Length >= MaxLen12))
                    throw new ArgumentException(lengthError);

                NotifyPropertyChanged(() => CommonIdentifier);
            }
        }

        public short? Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                _speed = value;
                NotifyPropertyChanged(() => Speed);
            }
        }

        public short? Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                _direction = value;
                NotifyPropertyChanged(() => Direction);
            }
        }

        public string HigherFormation
        {
            get
            {
                return _higherFormation;
            }
            set
            {
                _higherFormation = value;

                if (MaxLengthValidationOn &&
                    !string.IsNullOrEmpty(_higherFormation) && (_higherFormation.Length >= MaxLen21))
                    throw new ArgumentException(lengthError);

                NotifyPropertyChanged(() => HigherFormation);
            }
        }

        public string Reinforced
        {
            get
            {
                return _reinforced;
            }
            set
            {
                _reinforced = value;
                NotifyPropertyChanged(() => Reinforced);
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedReinforcedDomainPair
        {
            get
            {
                return _selectedReinforcedDomainPair;
            }
            set
            {
                _selectedReinforcedDomainPair = value;
                if (_selectedReinforcedDomainPair != null)
                {
                    Reinforced = _selectedReinforcedDomainPair.Code.ToString();
                }
                else
                {
                    Reinforced = "";
                }
                NotifyPropertyChanged(() => SelectedReinforcedDomainPair);
            }
        }

        public string Credibility
        {
            get
            {
                return _credibility;
            }
            set
            {
                _credibility = value;
                NotifyPropertyChanged(() => Credibility);
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedCredibilityDomainPair
        {
            get
            {
                return _selectedCredibilityDomainPair;
            }
            set
            {
                _selectedCredibilityDomainPair = value;
                if (_selectedCredibilityDomainPair != null)
                {
                    Credibility = _selectedCredibilityDomainPair.Code.ToString();
                }
                else
                {
                    Credibility = "";
                }
                NotifyPropertyChanged(() => SelectedCredibilityDomainPair);
            }
        }

        public string Reliability
        {
            get
            {
                return _reliability;
            }
            set
            {
                _reliability = value;
                NotifyPropertyChanged(() => Reliability);
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedReliabilityDomainPair
        {
            get
            {
                return _selectedReliabilityDomainPair;
            }
            set
            {
                _selectedReliabilityDomainPair = value;
                if (_selectedReliabilityDomainPair != null)
                {
                    Reliability = _selectedReliabilityDomainPair.Code.ToString();
                }
                else
                {
                    Reliability = "";
                }
                NotifyPropertyChanged(() => SelectedReliabilityDomainPair);
            }
        }

        public string SignatureEquipment
        {
            get
            {
                return _signatureEquipment;
            }
            set
            {
                _signatureEquipment = value;
                NotifyPropertyChanged(() => SignatureEquipment);
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedSignatureEquipmentDomainPair
        {
            get
            {
                return _selectedSignatureEquipmentDomainPair;
            }
            set
            {
                _selectedSignatureEquipmentDomainPair = value;
                if (_selectedSignatureEquipmentDomainPair != null)
                {
                    SignatureEquipment = _selectedSignatureEquipmentDomainPair.Code.ToString();
                }
                else
                {
                    SignatureEquipment = "";
                }
                NotifyPropertyChanged(() => SelectedSignatureEquipmentDomainPair);
            }
        }

        public string CountryLabel { get; set; }

        public string CountryCode
        {
            get
            {
                return _countryCode;
            }
            set
            {
                _countryCode = value;
                NotifyPropertyChanged(() => CountryCode);
            }
        }

        [ScriptIgnore, Browsable(false)]
        public DomainCodedValuePair SelectedCountryCodeDomainPair
        {
            get
            {
                return _selectedCountryCodeDomainPair;
            }
            set
            {
                _selectedCountryCodeDomainPair = value;
                if (_selectedCountryCodeDomainPair != null)
                {
                    CountryCode = _selectedCountryCodeDomainPair.Code.ToString();
                    CountryLabel = _selectedCountryCodeDomainPair.Name;
                }
                else
                {
                    CountryCode = "";
                    CountryLabel = "";
                }
                NotifyPropertyChanged(() => SelectedCountryCodeDomainPair);
            }
        }

        #endregion
    }
}
