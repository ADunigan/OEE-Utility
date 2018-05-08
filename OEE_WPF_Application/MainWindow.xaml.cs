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

namespace OEE_WPF_Application
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Package> packages = new List<Package>();
        List<Unit_Op> unit_ops = new List<Unit_Op>();
        Package selectedPackage;

        public MainWindow()
        {
            InitializeComponent();

            EstablishPresets();
        }

        private void EstablishPresets()
        {
            lv_Units.ItemsSource = packages;
        }

        private void btn_click_AddUnit(object sender, RoutedEventArgs e)
        {          
            if (!(packages.Count > 0))         
            {
                AddUnit au = new AddUnit(true);
                au.ShowDialog();

                if(!String.IsNullOrEmpty(au.Unit.Name))
                {
                    packages.Add(au.Unit);
                    lv_Units.Items.Refresh();
                }
            }
            else
            {
                AddUnit au = new AddUnit(false);
                au.ShowDialog();

                if (!String.IsNullOrEmpty(au.Unit.Name))
                {
                    packages.Add(au.Unit);
                    lv_Units.Items.Refresh();
                }
            }
        }

        private void btn_click_DeleteUnit(object sender, RoutedEventArgs e)
        {
            packages.Remove(selectedPackage);
            lv_Units.Items.Refresh();
        }

        private void btn_click_Simulate(object sender, RoutedEventArgs e)
        {

        }

        private void btn_click_MoveUp(object sender, RoutedEventArgs e)
        {

        }

        private void btn_click_MoveDown(object sender, RoutedEventArgs e)
        {

        }

        private void btn_click_Configure(object sender, RoutedEventArgs e)
        {

        }

        private void lv_selectionchanged_Units(object sender, SelectionChangedEventArgs e)
        {
            if(lv_Units.SelectedItem != null)
            {
                selectedPackage = (Package)lv_Units.SelectedItem;
                btn_DeleteUnit.IsEnabled = true;
            }
            else
            {
                btn_DeleteUnit.IsEnabled = false;
            }
        }

        private void btn_click_AddOp(object sender, RoutedEventArgs e)
        {

        }

        private void btn_click_DeleteOp(object sender, RoutedEventArgs e)
        {

        }
    }
}
