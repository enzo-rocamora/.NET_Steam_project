using Gauniv.Client.ViewModel;
using Gauniv.Network.ServerApi;

namespace Gauniv.Client.Pages;

public partial class Store : ContentPage
{
    private readonly ServerApi _serverApi;
    private readonly StoreViewModel _viewModel;

    public Store(StoreViewModel viewModel, ServerApi serverApi)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serverApi = serverApi;
        BindingContext = _viewModel;
    }
}