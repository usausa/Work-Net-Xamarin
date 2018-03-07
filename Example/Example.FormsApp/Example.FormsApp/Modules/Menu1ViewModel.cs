﻿namespace Example.FormsApp.Modules
{
    using Smart.Forms.Input;
    using Smart.Navigation;

    public class Menu1ViewModel : AppViewModelBase
    {
        public AsyncCommand<ViewId> ForwardCommand { get; }

        public Menu1ViewModel(ApplicationState applicationState)
            : base(applicationState)
        {
            ForwardCommand = MakeAsyncCommand<ViewId>(x => Navigator.ForwardAsync(x));
        }
    }
}
