using System.Windows;

namespace SandSurvival
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Action quand on clique sur le bouton JOUER
        private void Button_Jouer_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Lancement du jeu !");
        }

        // Action quand on clique sur le bouton PARAMÈTRES
        private void Button_Parametre_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ouverture des paramètres...");
        }

        // Action quand on clique sur le bouton CRÉDIT
        private void Button_Credit_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Affichage des crédits...");
        }
    }
}