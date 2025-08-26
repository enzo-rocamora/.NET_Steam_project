using Gauniv.Client.ViewModel;

namespace Gauniv.Client.Pages;

public partial class GameDetails : ContentPage
{
    private readonly GameDetailsViewModel _viewModel;

    public GameDetails(GameDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}