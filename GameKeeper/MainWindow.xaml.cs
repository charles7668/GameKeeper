using System.ComponentModel;
using System.Windows;
using GameKeeper.ViewModels;

namespace GameKeeper;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && !viewModel.DetachAll())
        {
            e.Cancel = true;
        }

        base.OnClosing(e);
    }
}
