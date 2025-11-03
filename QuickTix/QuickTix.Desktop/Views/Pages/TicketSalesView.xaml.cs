using QuickTix.Desktop.ViewModels.Users;
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
    /// Lógica de interacción para SalesView.xaml
    /// </summary>
    public partial class TicketSalesView : INavigableView<TicketSalesViewModel>
    {
        public TicketSalesViewModel ViewModel { get; }
        public TicketSalesView(TicketSalesViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;

            InitializeComponent();

            //viewModel.OnPageLoaded();
        }
    }
}
