using System.Windows;
using FastFoodSimulator.ViewModels;

namespace FastFoodSimulator;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(Width, Math.Max(MinWidth, workArea.Width - 32));
        Height = Math.Min(Height, Math.Max(MinHeight, workArea.Height - 32));

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        Closed += (_, _) => _viewModel.Dispose();
    }
}
