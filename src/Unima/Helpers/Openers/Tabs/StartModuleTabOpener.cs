﻿using NewProjectDialog = Unima.Sections.NewProject.NewProjectDialog;

namespace Unima.Helpers.Openers.Tabs
{
    public class StartModuleTabOpener : IStartModuleTabOpener
    {
        private readonly IMainTabContainer _mainTabContainer;

        public StartModuleTabOpener(IMainTabContainer mainTabContainer)
        {
            _mainTabContainer = mainTabContainer;
        }

        public void OpenNewProjectTab()
        {
            _mainTabContainer.AddTab(new NewProjectDialog());
        }
    }
}
