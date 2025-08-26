using Gauniv.Client.ViewModels;

namespace Gauniv.Client.Pages
{
    public partial class MyGames : ContentPage
    {
        public MyGames(MyGamesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}