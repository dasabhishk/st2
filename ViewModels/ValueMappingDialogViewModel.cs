using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CMMT.ViewModels
{

    public partial class ValueMappingDialogViewModel : ObservableObject
    {
        public ObservableCollection<ValueMappingRowViewModel> Mappings { get; } = new(); //list of mapping rows in the dialog

        public ObservableCollection<string> AvailableSources { get; } //valid source values for the mapping, e.g. csv values
        public ObservableCollection<string> AvailableTargets { get; } //valid target values for the mapping, e.g. database values

        [ObservableProperty]
        public string? defaultTargetValue; //default target value to use when no data in csv for the column

        [ObservableProperty]
        private string? validationMessage; //ui validation message

        [ObservableProperty]
        private bool hasCSVData;

        public ValueMappingDialogViewModel(IEnumerable<string> sources, IEnumerable<string> targets)
        {
            AvailableSources = new ObservableCollection<string>(sources);
            AvailableTargets = new ObservableCollection<string>(targets ?? Enumerable.Empty<string>());
            HasCSVData = sources.Any();
            // Initialize with one empty row
            AddRow();
        }

        [RelayCommand]
        public void AddRow() //command adding a new mapping row
        {
            Mappings.Add(new ValueMappingRowViewModel());

        }

        [RelayCommand]
        public void RemoveRow(ValueMappingRowViewModel row) //command removing a mapping row
        {
            if (row != null && Mappings.Contains(row))
            {
                Mappings.Remove(row);
            }
        }

        public bool Validate()
        {
            if(Mappings.Any(n=>string.IsNullOrWhiteSpace(n.SourceValue) || string.IsNullOrWhiteSpace(n.TargetValue)))
            {
                ValidationMessage = "All mapping rows must have both source and target values.";
                return false;
            }
            else if (Mappings.GroupBy(n => n.SourceValue).Any(g => g.Count() > 1))
            {
                ValidationMessage = "Source values must be unique.";
                return false;
            }
            else
            {
                ValidationMessage = null; // Clear validation message if everything is valid
                return true;
            }
        }
        public Dictionary<string, string> GetMappings() //returns the mappings as a dictionary
        {
            Validate(); //ensure validation is run before returning mappings
            if (ValidationMessage != null)
            {
                throw new InvalidOperationException("Cannot get mappings due to validation errors.");
            }
            return Mappings.ToDictionary(m => m.SourceValue!, m => m.TargetValue!);
        }
    }

}