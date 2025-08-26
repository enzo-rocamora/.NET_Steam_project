using Gauniv.Client.ViewModel;
using Gauniv.Network.ServerApi;

namespace Gauniv.Client.Pages;

public partial class Index : ContentPage
{
    private readonly ServerApi _serverApi;
    private readonly IndexViewModel _viewModel;
    public Index(IndexViewModel viewModel, ServerApi serverApi)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serverApi = serverApi;
        BindingContext = _viewModel;
    }
}