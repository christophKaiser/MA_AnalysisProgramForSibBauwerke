using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MA_ETL_process
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlClient? sqlClient = null;

        public MainWindow()
        {
            InitializeComponent();
            Utilities.SetTxtConsoleReference = mainWindow.txtConsole;
            Utilities.ConsoleLog("Click a button");
        }

        private void btn_SqlConnection_Click(object sender, RoutedEventArgs e)
        {
            // create and open a new connection to the SQL Server
            sqlClient = new SqlClient();
        }

        private void btn_SqlTest_Click(object sender, RoutedEventArgs e)
        {
            if (sqlClient == null)
            {
                Utilities.ConsoleLog("no SQL connection");
                return;
            }

            Utilities.ConsoleLog("\nBauwerk:");
            sqlClient.SelectRows(
                @"SELECT [BWNR], [BWNAME], [ORT], [ANZ_TEILBW], [LAENGE_BR]
                FROM [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW]
                WHERE [SIB_BAUWERKE_19_20230427].[dbo].[GES_BW].[BWNR]=5527701");

            Utilities.ConsoleLog("\nTeilbauwerke:");
            sqlClient.SelectRows(
                @"SELECT [BWNR], [TEIL_BWNR], [TW_NAME], [KONSTRUKT], [ID_NR]
                FROM [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW]
                WHERE [SIB_BAUWERKE_19_20230427].[dbo].[TEIL_BW].[BWNR]=5527701");

            Utilities.ConsoleLog("\nDurchgeführte Prüfungen:");
            sqlClient.SelectRows(
                @"SELECT [BWNR], [TEIL_BWNR], [ID_NR], [PRUF_DATUM], [PRUF_ZYKL], [ZUSTANDSN], [PRUF_NR], [IDENT]
                FROM [SIB_BAUWERKE_19_20230427].[dbo].[PRUF_DGF]
                WHERE [SIB_BAUWERKE_19_20230427].[dbo].[PRUF_DGF].[BWNR]=5527701");

            Utilities.ConsoleLog("'Test SQL Connection' finished");
        }

        private void btn_Neo4jConnection_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_Neo4jTest_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}