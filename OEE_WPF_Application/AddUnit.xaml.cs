using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OEE_WPF_Application
{
    /// <summary>
    /// Interaction logic for AddUnit.xaml
    /// </summary>
    public partial class AddUnit : Window
    {
        private bool DefaultPrimary;
        public Package Unit = new Package();

        public AddUnit(bool defaultprimary)
        {
            InitializeComponent();

            DefaultPrimary = defaultprimary;
            EstablishPresets();            
        }

        private void EstablishPresets()
        {
            btn_DoneAddUnit.IsEnabled = false;

            if (DefaultPrimary)
            {
                chb_Primary.IsChecked = true;
                chb_Primary.IsEnabled = false;
            }
            else
            {
                chb_Primary.IsChecked = false;
                chb_Primary.IsEnabled = true;
            }
        }

        #region ButtonEvents
        private void btn_click_DoneAddUnit(object sender, RoutedEventArgs e)
        {
            Unit.Name = tb_UnitName.Text;
            Unit.PrimaryPackDensity = Convert.ToInt32(tb_PrimaryPacksPer.Text);
            Unit.PrimaryPack = (bool)chb_Primary.IsChecked;

            this.Close();
        }

        private void btn_click_CancelAddUnit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region TextBoxEvents
        private void tb_textchanged_UnitName(object sender, TextChangedEventArgs e)
        {
            if(!String.IsNullOrEmpty(tb_PrimaryPacksPer.Text) && !String.IsNullOrEmpty(tb_UnitName.Text))
            {
                btn_DoneAddUnit.IsEnabled = true;
            }
            else
            {
                btn_DoneAddUnit.IsEnabled = false;
            }
        }

        private void tb_textchanged_PrimaryPacksPer(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(tb_PrimaryPacksPer.Text) && !String.IsNullOrEmpty(tb_UnitName.Text))
            {
                btn_DoneAddUnit.IsEnabled = true;
            }
            else
            {
                btn_DoneAddUnit.IsEnabled = false;
            }
        }

        //Only allows numeric inputs into the Primary Packs Per textbox
        private void tb_previewkeydown_PrimaryPacksPer(object sender, KeyEventArgs e)
        {
            //Disallows any keyboard inputs if a shift key is pressed (prevents shift and numeric input)
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                e.Handled = true;
                return;
            }

            //Some keys are allowed to maintain editing functionality.  Regex expression is used to verify input is numeric.
            if (!(e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Enter || e.Key == Key.OemPeriod || e.Key == Key.Decimal || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.Up))
            {
                Regex intRegex = new Regex(@"[0123456789]");
                MatchCollection matches = intRegex.Matches(e.Key.ToString());
                if (matches.Count == 0)
                {
                    e.Handled = true;
                    return;
                }
            }
        }
        #endregion
    }
}
