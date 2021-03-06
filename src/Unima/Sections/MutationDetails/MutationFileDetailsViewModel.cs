﻿using System.ComponentModel;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Unima.Application;
using Unima.Core;
using Unima.Core.Config;
using Unima.Helpers.Openers.Tabs;

namespace Unima.Sections.MutationDetails
{
    public class MutationFileDetailsViewModel : BindableBase, INotifyPropertyChanged
    {
        private readonly IMutationModuleTabOpener _tabOpener;
        private UnimaConfig _config;

        public MutationFileDetailsViewModel(IMutationModuleTabOpener tabOpener)
        {
            _tabOpener = tabOpener;
            ExecuteTestsCommand = new DelegateCommand(ExecuteTests);
            MutationSelectedCommand = new DelegateCommand<MutationDocument>(MutationSelected);
        }

        public string FileName { get; set; }

        public FileMutationsModel File { get; set; }

        public DelegateCommand ExecuteTestsCommand { get; set; }

        public DelegateCommand<MutationDocument> MutationSelectedCommand { get; set; }

        public void Initialize(FileMutationsModel file, UnimaConfig config)
        {
            _config = config;
            File = file;
            FileName = File.FileName;
        }

        private void ExecuteTests()
        {
            _tabOpener.OpenTestRunTab(File.MutationDocuments.ToList(), _config);
        }

        private void MutationSelected(MutationDocument obj)
        {
            _tabOpener.OpenDocumentDetailsTab(obj, _config);
        }
    }
}
