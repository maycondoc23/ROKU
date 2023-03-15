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
using System.Windows.Shapes;

namespace SentinelaRoku
{
    /// <summary>
    /// Lógica interna para AlertMessage.xaml
    /// </summary>
    public partial class AlertMessage : Window
    {

        /*************************************************************************************************************************/
        /*--- Inicializa a janela ---*/
        public AlertMessage()
        {
            InitializeComponent();

        }

        /*************************************************************************************************************************/

        public void InsertMessage(string messageWebService)
        {
            tb_MessageWebService.Text = messageWebService;
        }

        /*************************************************************************************************************************/
        /*--- Fecha a janela ---*/
        private void bt_Fechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /*************************************************************************************************************************/
    }
}
