using System.Windows;
using QuanLyTaiChinhCaNhan_Nhom06.ViewModels;

namespace QuanLyTaiChinhCaNhan_Nhom06
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

