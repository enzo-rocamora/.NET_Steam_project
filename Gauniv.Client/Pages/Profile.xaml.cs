using Gauniv.Client.ViewModel;

namespace Gauniv.Client.Pages
{
    public partial class Profile : ContentPage
    {
        public Profile(ProfileViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}