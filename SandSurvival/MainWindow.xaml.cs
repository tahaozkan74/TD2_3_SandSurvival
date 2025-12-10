using System.Windows;

namespace SandSurvival
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Action : JOUER
        private void Button_Jouer_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Lancement du jeu !");
        }

        // Action : PARAMÈTRES
        private void Button_Parametre_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ouverture des paramètres...");
        }

        // Action : QUITTER (Correction demandée)
        private void Button_Quitter_Click(object sender, RoutedEventArgs e)
        {
            // Cette commande ferme proprement l'application
            Application.Current.Shutdown();
        }
    }
}