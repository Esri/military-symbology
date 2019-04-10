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
 
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;

namespace ProSymbolEditor
{
    public class MilitaryFieldsInspectorModel : PropertyChangedBase
    {
        //Boolean visibilities for string fields
        private Visibility _dateTimeValidFieldExists = Visibility.Collapsed;
        private Visibility _dateTimeExpiredFieldExists = Visibility.Collapsed;
        private Visibility _uniqueDesignation = Visibility.Collapsed;
        private Visibility _typeFieldExists = Visibility.Collapsed;
        private Visibility _commonIdentifierFieldExists = Visibility.Collapsed;
        private Visibility _speedFieldExists = Visibility.Collapsed; //short
        private Visibility _staffCommentsFieldExists = Visibility.Collapsed;
        private Visibility _additionalInformationFieldExists = Visibility.Collapsed;
        private Visibility _higherFormationFieldExists = Visibility.Collapsed;

        #region Mutators

        //Domains for combo boxes for text labels
        public ObservableCollection<DomainCodedValuePair> ReinforcedDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> CredibilityDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> ReliabilityDomainValues { get; set; }

        //Domains for attribute combo boxes

        public ObservableCollection<DomainCodedValuePair> IdentityDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> EchelonDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> StatusDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> OperationalConditionAmplifierDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> MobilityDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> TfFdHqDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> ContextDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> Modifier1DomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> Modifier2DomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> CountryCodeDomainValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> ExtendedFunctionCodeValues { get; set; }
        public ObservableCollection<DomainCodedValuePair> EntityCodeValues { get; set; }

        public Visibility DateTimeValidFieldExists
        {
            get
            {
                return _dateTimeValidFieldExists;
            }
            set
            {
                _dateTimeValidFieldExists = value;
                NotifyPropertyChanged(() => DateTimeValidFieldExists);
            }
        }

        public Visibility DateTimeExpiredFieldExists
        {
            get
            {
                return _dateTimeExpiredFieldExists;
            }
            set
            {
                _dateTimeExpiredFieldExists = value;
                NotifyPropertyChanged(() => DateTimeExpiredFieldExists);
            }
        }

        public Visibility UniqueDesignationFieldExists
        {
            get
            {
                return _uniqueDesignation;
            }
            set
            {
                _uniqueDesignation = value;
                NotifyPropertyChanged(() => UniqueDesignationFieldExists);
            }
        }

        public Visibility TypeFieldExists
        {
            get
            {
                return _typeFieldExists;
            }
            set
            {
                _typeFieldExists = value;
                NotifyPropertyChanged(() => TypeFieldExists);
            }
        }

        public Visibility CommonIdentifierFieldExists
        {
            get
            {
                return _commonIdentifierFieldExists;
            }
            set
            {
                _commonIdentifierFieldExists = value;
                NotifyPropertyChanged(() => CommonIdentifierFieldExists);
            }
        }

        public Visibility SpeedFieldExists
        {
            get
            {
                return _speedFieldExists;
            }
            set
            {
                _speedFieldExists = value;
                NotifyPropertyChanged(() => SpeedFieldExists);
            }
        }

        public Visibility StaffCommentsFieldExists
        {
            get
            {
                return _staffCommentsFieldExists;
            }
            set
            {
                _staffCommentsFieldExists = value;
                NotifyPropertyChanged(() => StaffCommentsFieldExists);
            }
        }

        public Visibility AdditionalInformationFieldExists
        {
            get
            {
                return _additionalInformationFieldExists;
            }
            set
            {
                _additionalInformationFieldExists = value;
                NotifyPropertyChanged(() => AdditionalInformationFieldExists);
            }
        }

        public Visibility HigherFormationFieldExists
        {
            get
            {
                return _higherFormationFieldExists;
            }
            set
            {
                _higherFormationFieldExists = value;
                NotifyPropertyChanged(() => HigherFormationFieldExists);
            }
        }

        #endregion

        public MilitaryFieldsInspectorModel()
        {
            ReinforcedDomainValues = new ObservableCollection<DomainCodedValuePair>();
            CredibilityDomainValues = new ObservableCollection<DomainCodedValuePair>();
            ReliabilityDomainValues = new ObservableCollection<DomainCodedValuePair>();
            ExtendedFunctionCodeValues = new ObservableCollection<DomainCodedValuePair>();
            IdentityDomainValues = new ObservableCollection<DomainCodedValuePair>();
            EchelonDomainValues = new ObservableCollection<DomainCodedValuePair>();
            StatusDomainValues = new ObservableCollection<DomainCodedValuePair>();
            OperationalConditionAmplifierDomainValues = new ObservableCollection<DomainCodedValuePair>();
            MobilityDomainValues = new ObservableCollection<DomainCodedValuePair>();
            TfFdHqDomainValues = new ObservableCollection<DomainCodedValuePair>();
            ContextDomainValues = new ObservableCollection<DomainCodedValuePair>();
            EntityCodeValues = new ObservableCollection<DomainCodedValuePair>();
            Modifier1DomainValues = new ObservableCollection<DomainCodedValuePair>();
            Modifier2DomainValues = new ObservableCollection<DomainCodedValuePair>();
            CountryCodeDomainValues = new ObservableCollection<DomainCodedValuePair>();
        }

        public void CheckLabelFieldsExistence(IReadOnlyList<ArcGIS.Core.Data.Field> fields)
        {
            DateTimeValidFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "datetimevalid") != null)
            {
                DateTimeValidFieldExists = Visibility.Visible;
            }

            DateTimeExpiredFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "datetimeexpired") != null)
            {
                DateTimeExpiredFieldExists = Visibility.Visible;
            }

            UniqueDesignationFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "uniquedesignation") != null)
            {
                UniqueDesignationFieldExists = Visibility.Visible;
            }

            TypeFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "type") != null)
            {
                TypeFieldExists = Visibility.Visible;
            }

            CommonIdentifierFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "commonidentifier") != null)
            {
                CommonIdentifierFieldExists = Visibility.Visible;
            }

            SpeedFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "speed") != null)
            {
                SpeedFieldExists = Visibility.Visible;
            }

            StaffCommentsFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "staffcomment") != null)
            {
                StaffCommentsFieldExists = Visibility.Visible;
            }

            AdditionalInformationFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "additionalinformation") != null)
            {
                AdditionalInformationFieldExists = Visibility.Visible;
            }

            HigherFormationFieldExists = Visibility.Collapsed;
            if (fields.FirstOrDefault(field => field.Name == "higherformation") != null)
            {
                HigherFormationFieldExists = Visibility.Visible;
            }
        }

        public void PopulateDomains(IReadOnlyList<ArcGIS.Core.Data.Field> fields)
        {
            //Get domains for text labels
            GetDomainAndPopulateList(fields, "reinforced", ReinforcedDomainValues);
            GetDomainAndPopulateList(fields, "credibility", CredibilityDomainValues);
            GetDomainAndPopulateList(fields, "reliability", ReliabilityDomainValues);

            //Get domains for attributes

            if (ProSymbolUtilities.IsLegacyStandard())
            {
                EntityCodeValues.Clear();
                GetDomainAndPopulateList(fields, "affiliation", IdentityDomainValues);
                GetDomainAndPopulateList(fields, "echelonmobility", EchelonDomainValues);
                GetDomainAndPopulateList(fields, "extendedfunctioncode", ExtendedFunctionCodeValues);
                GetDomainAndPopulateList(fields, "hqtffd", TfFdHqDomainValues);
            }
            else // 2525D
            {
                ExtendedFunctionCodeValues.Clear();
                GetDomainAndPopulateList(fields, "identity", IdentityDomainValues);
                GetDomainAndPopulateList(fields, "echelon", EchelonDomainValues);
                GetDomainAndPopulateList(fields, "indicator", TfFdHqDomainValues);
            }

            GetDomainAndPopulateList(fields, "status", StatusDomainValues);
            GetDomainAndPopulateList(fields, "operationalcondition", OperationalConditionAmplifierDomainValues);
            GetDomainAndPopulateList(fields, "mobility", MobilityDomainValues);
            GetDomainAndPopulateList(fields, "context", ContextDomainValues);
            GetDomainAndPopulateList(fields, "symbolentity", EntityCodeValues);
            GetDomainAndPopulateList(fields, "modifier1", Modifier1DomainValues);
            GetDomainAndPopulateList(fields, "modifier2", Modifier2DomainValues);
            GetDomainAndPopulateList(fields, "countrycode", CountryCodeDomainValues, true);
        }

        private void GetDomainAndPopulateList(IReadOnlyList<Field> fields, string fieldName, ObservableCollection<DomainCodedValuePair> memberCodedValueDomains, bool orderbyName = false)
        {
            ArcGIS.Core.Data.Field foundField = fields.FirstOrDefault(field => field.Name == fieldName);

            //Clear out any old values
            memberCodedValueDomains.Clear();

            if (foundField != null)
            {
                Domain domain = foundField.GetDomain();

                var codedValueDomain = domain as CodedValueDomain;
                if (codedValueDomain != null)
                {
                    SortedList<object, string> codedValuePairs = codedValueDomain.GetCodedValuePairs();

                    foreach (KeyValuePair<object, string> pair in codedValuePairs)
                    {
                        DomainCodedValuePair domainObjectPair = new DomainCodedValuePair(pair.Key, pair.Value);
                        memberCodedValueDomains.Add(domainObjectPair);
                    }

                    if (orderbyName)
                    {
                        //Order the collection alphabetically by the names, rather than the default by the code
                        memberCodedValueDomains.Sort();
                    }

                    // Minor hack to make USA appear first in the list
                    if (fieldName == "countrycode")
                    {
                        DomainCodedValuePair domainObjectPair = new DomainCodedValuePair("USA", "United States");
                        memberCodedValueDomains.Insert(0, domainObjectPair);
                    }

                    // Add a "<null>" value to the domain list - so this field can be cleared with this flag
                    // but skip ones that will cause problems with the addin
                    if (!((fieldName == "identity") || (fieldName == "affiliation") || 
                        (fieldName == "extendedfunctioncode") || (fieldName == "symbolentity")))
                    {
                        memberCodedValueDomains.Add(new DomainCodedValuePair(ProSymbolUtilities.NullFieldValueFlag,
                        ProSymbolUtilities.NullFieldValueFlag));
                    }
                }
            }
        }
    }
}
