using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuickTix.Desktop.Views.Pages
{
    /// <summary>
    /// Lógica de interacción para ClientsView.xaml
    /// </summary>
    public partial class ClientsView : INavigableView<ClientsViewModel>
    {
        public ClientsViewModel ViewModel { get; }
        public ClientsView(ClientsViewModel viewModel)
        {
            ViewModel = viewModel;
        
            InitializeComponent();

            DataContext = ViewModel;
        }
    }
}
