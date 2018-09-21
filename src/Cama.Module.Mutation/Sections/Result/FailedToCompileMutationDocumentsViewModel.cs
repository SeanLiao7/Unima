﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Cama.Core.Models.Mutation;
using Cama.Core.Report.Cama;
using Prism.Mvvm;

namespace Cama.Module.Mutation.Sections.Result
{
    public class FailedToCompileMutationDocumentsViewModel : BindableBase, INotifyPropertyChanged
    {
        public FailedToCompileMutationDocumentsViewModel()
        {
            MutantsFailedToCompile = new ObservableCollection<CamaReportMutationItem>();
        }

        public ObservableCollection<CamaReportMutationItem> MutantsFailedToCompile { get; set; }

        public void InitializeMutants(IList<CamaReportMutationItem> mutantsFailedToCompile)
        {
            MutantsFailedToCompile = new ObservableCollection<CamaReportMutationItem>(mutantsFailedToCompile);
        }
    }
}
