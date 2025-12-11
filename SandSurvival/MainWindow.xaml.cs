using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SandSurvival
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // --- NAVIGATION MENU ---

        private void Button_Jouer_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Lancement du jeu !");
        }

        private void Button_Parametre_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Paramètres...");
        }

        private void Button_Quitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // --- GESTION RÈGLES ---

        private void Button_Regles_Click(object sender, RoutedEventArgs e)
        {
            GridRegles.Visibility = Visibility.Visible;
        }

        private void Button_FermerRegles_Click(object sender, RoutedEventArgs e)
        {
            GridRegles.Visibility = Visibility.Collapsed;
        }

        // --- GESTION CRÉDITS (ANIMATION) ---

        private void Button_Credit_Click(object sender, RoutedEventArgs e)
        {
            // 1. Afficher l'écran noir
            GridCredit.Visibility = Visibility.Visible;

            // 2. Configurer l'animation
            DoubleAnimation animation = new DoubleAnimation();
            // Point de départ : La hauteur de la fenêtre (le texte est caché tout en bas)
            animation.From = this.ActualHeight;
            // Point d'arrivée : -500 (le texte remonte jusqu'à sortir par le haut)
            animation.To = -500;
            // Durée : 10 secondes pour tout lire
            animation.Duration = new Duration(TimeSpan.FromSeconds(10));
            // Répéter l'animation à l'infini
            animation.RepeatBehavior = RepeatBehavior.Forever;

            // 3. Lancer l'animation sur la propriété "Canvas.Top" du StackPanel
            StackPanelCredit.BeginAnimation(Canvas.TopProperty, animation);
        }

        private void Button_FermerCredit_Click(object sender, RoutedEventArgs e)
        {
            // Cacher l'écran et arrêter l'animation
            GridCredit.Visibility = Visibility.Collapsed;
            StackPanelCredit.BeginAnimation(Canvas.TopProperty, null);
        }
    }
}