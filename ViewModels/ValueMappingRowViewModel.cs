using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CMMT.ViewModels
{
 public partial class ValueMappingRowViewModel : ObservableObject
 {
        [ObservableProperty]
        private string? sourceValue; //csv value

        [ObservableProperty]
        private string? targetValue; //db value user maos


    }
}
