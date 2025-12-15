using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading; // Nécessaire pour le Timer

namespace SandSurvival
{
    public partial class MainWindow : Window
    {
        public static double MusicVolume = 0.5;

        // Touches par défaut
        public static System.Windows.Input.Key InputUp = System.Windows.Input.Key.Z;
        public static System.Windows.Input.Key InputDown = System.Windows.Input.Key.S;
        public static System.Windows.Input.Key InputLeft = System.Windows.Input.Key.Q;
        public static System.Windows.Input.Key InputRight = System.Windows.Input.Key.D;

        private MediaPlayer musicPlayer = new MediaPlayer();

        // Timer pour les crédits
        private DispatcherTimer creditTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            LancerMusique();

            if (this.FindName("SliderVolume") is Slider slider) slider.Value = MusicVolume;

            // Configuration du Timer des crédits (60 FPS environ)
            creditTimer.Interval = TimeSpan.FromMilliseconds(16);
            creditTimer.Tick += CreditAnimation_Tick;
        }

        private void LancerMusique()
        {
            try
            {
                musicPlayer.Open(new Uri("Audio/BLACK OPS 2 ZOMBIES OFFICIAL Theme Song.mp3", UriKind.Relative));
                musicPlayer.MediaEnded += (s, e) => { musicPlayer.Position = TimeSpan.Zero; musicPlayer.Play(); };
                musicPlayer.Volume = MusicVolume;
                musicPlayer.Play();
            }
            catch { }
        }

        // --- ANIMATION CRÉDITS ---
        private void CreditAnimation_Tick(object sender, EventArgs e)
        {
            if (this.FindName("StackPanelCredit") is StackPanel sp)
            {
                double currentTop = Canvas.GetTop(sp);

                // Si pas défini, on commence en bas
                if (double.IsNaN(currentTop)) currentTop = 600;

                // On fait monter le texte
                currentTop -= 3.0;

                // Si le texte est trop haut, on le remet en bas pour boucler
                if (currentTop < -sp.ActualHeight) currentTop = this.ActualHeight;

                Canvas.SetTop(sp, currentTop);
            }
        }

        // --- BOUTONS ---

        private void Button_Jouer_Click(object sender, RoutedEventArgs e)
        {
            musicPlayer.Stop();
            Jeu j = new Jeu();
            j.Show();
            this.Close();
        }

        private void Button_Quitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Paramètres
        private void Button_Parametre_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("GridParametres") is Grid g) g.Visibility = Visibility.Visible;
        }

        private void Button_FermerParametre_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("GridParametres") is Grid g) g.Visibility = Visibility.Collapsed;
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MusicVolume = e.NewValue;
            musicPlayer.Volume = MusicVolume;
        }

        // Règles
        private void Button_Regles_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("GridRegles") is Grid g) g.Visibility = Visibility.Visible;
        }

        private void Button_FermerRegles_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("GridRegles") is Grid g) g.Visibility = Visibility.Collapsed;
        }

        // Crédits (Avec lancement du Timer)
        private void Button_Credit_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("GridCredit") is Grid g) g.Visibility = Visibility.Visible;

            // On reset la position du texte et on lance l'animation
            if (this.FindName("StackPanelCredit") is StackPanel sp)
            {
                Canvas.SetTop(sp, 600); // Commence en bas
                creditTimer.Start();
            }
        }

        private void Button_FermerCredit_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("GridCredit") is Grid g) g.Visibility = Visibility.Collapsed;
            creditTimer.Stop(); // On arrête l'animation pour économiser les ressources
        }
    }
}