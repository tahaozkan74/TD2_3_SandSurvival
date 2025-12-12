using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Nécessaire pour le son (MediaPlayer)
using System.Windows.Media.Animation; // Nécessaire pour l'animation des crédits

namespace SandSurvival
{
    public partial class MainWindow : Window
    {
        // On crée le lecteur de musique
        private MediaPlayer player = new MediaPlayer();

        public MainWindow()
        {
            InitializeComponent();

            // On lance la musique dès le démarrage
            LancerMusique();
        }

        private void LancerMusique()
        {
            try
            {
                // Charge le fichier musique
                // Assure-toi que "Audio/desert_theme.mp3" existe bien et est en "Contenu" / "Copier si plus récent"
                player.Open(new Uri("P:\\SAE - 1.01\\SandSurvival-11-12-25\\SandSurvival\\Audio\\BLACK OPS 2 ZOMBIES OFFICIAL Theme Song.mp3", UriKind.Relative));

                // Volume initial (correspond à la valeur par défaut du Slider)
                player.Volume = 0.5;

                player.Play();

                // Boucle infinie pour la musique (quand ça finit, ça recommence)
                player.MediaEnded += (sender, e) =>
                {
                    player.Position = TimeSpan.Zero;
                    player.Play();
                };
            }
            catch (Exception ex)
            {
                // Si le fichier n'est pas trouvé, on affiche une erreur sans faire planter le jeu
                MessageBox.Show("Erreur Audio : " + ex.Message);
            }
        }

        // ==========================================
        // 1. BOUTONS PRINCIPAUX (Menu)
        // ==========================================

        private void Button_Jouer_Click(object sender, RoutedEventArgs e)
        {
            // 1. Créer la fenêtre du jeu
            Jeu fenetreJeu = new Jeu();
            // 2. L'afficher
            fenetreJeu.Show();
            // 3. Fermer le menu actuel
            this.Close();
        }

        private void Button_Quitter_Click(object sender, RoutedEventArgs e)
        {
            // Ferme complètement l'application
            Application.Current.Shutdown();
        }

        private void Button_Parametre_Click(object sender, RoutedEventArgs e)
        {
            GridParametres.Visibility = Visibility.Visible;
        }

        // ==========================================
        // 2. GESTION DES RÈGLES
        // ==========================================

        private void Button_Regles_Click(object sender, RoutedEventArgs e)
        {
            GridRegles.Visibility = Visibility.Visible;
        }

        private void Button_FermerRegles_Click(object sender, RoutedEventArgs e)
        {
            GridRegles.Visibility = Visibility.Collapsed;
        }

        // ==========================================
        // 3. GESTION DES CRÉDITS (Responsive)
        // ==========================================

        private void Button_Credit_Click(object sender, RoutedEventArgs e)
        {
            // 1. Afficher l'écran
            GridCredit.Visibility = Visibility.Visible;

            // 2. FORCER LE CALCUL DES TAILLES (UpdateLayout)
            // C'est crucial pour le responsive : on oblige WPF à calculer la taille du texte maintenant
            GridCredit.UpdateLayout();

            // 3. Calculer les distances dynamiquement
            double hauteurTexte = StackPanelCredit.ActualHeight;
            double hauteurEcran = this.ActualHeight;

            // 4. Configurer l'animation
            DoubleAnimation animation = new DoubleAnimation();

            // Départ : Tout en bas de l'écran
            animation.From = hauteurEcran;

            // Arrivée : On remonte de toute la hauteur du texte (pour qu'il sorte totalement de l'écran)
            animation.To = -hauteurTexte;

            // Durée : 15 secondes (vitesse de lecture confortable)
            animation.Duration = new Duration(TimeSpan.FromSeconds(15));

            // Répétition infinie
            animation.RepeatBehavior = RepeatBehavior.Forever;

            // 5. Lancer l'animation sur la position verticale (Canvas.Top)
            StackPanelCredit.BeginAnimation(Canvas.TopProperty, animation);
        }

        private void Button_FermerCredit_Click(object sender, RoutedEventArgs e)
        {
            // Cacher l'écran
            GridCredit.Visibility = Visibility.Collapsed;

            // Arrêter l'animation proprement (sinon elle continue de tourner en fond)
            StackPanelCredit.BeginAnimation(Canvas.TopProperty, null);
        }

        // ==========================================
        // 4. GESTION DU VOLUME (Paramètres)
        // ==========================================

        private void Button_FermerParametre_Click(object sender, RoutedEventArgs e)
        {
            GridParametres.Visibility = Visibility.Collapsed;
        }

        // Se déclenche quand on bouge le curseur de volume
        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Met à jour le volume du lecteur de musique en temps réel
            player.Volume = e.NewValue;
        }
    }
}