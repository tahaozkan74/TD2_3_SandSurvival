using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SandSurvival
{
    public partial class MainWindow : Window
    {
        // =========================================================
        // VOLUME MUSIQUE (alias FR + alias ENG pour compatibilité)
        // =========================================================
        private static double volumeMusique = 0.5;

        // Nom FR (recommandé)
        public static double VolumeMusique
        {
            get { return volumeMusique; }
            set { volumeMusique = value; }
        }

        // Nom ENG (compatibilité avec ton ancien code)
        public static double MusicVolume
        {
            get { return volumeMusique; }
            set { volumeMusique = value; }
        }

        // =========================================================
        // TOUCHES (alias FR + alias ENG pour compatibilité)
        // =========================================================
        private static Key toucheHaut = Key.Z;
        private static Key toucheBas = Key.S;
        private static Key toucheGauche = Key.Q;
        private static Key toucheDroite = Key.D;

        private static Key toucheSprint = Key.LeftShift;
        private static Key toucheAttaque = Key.None;
        private static Key toucheBlocage = Key.None;

        // Noms FR (recommandés)
        public static Key ToucheHaut
        {
            get { return toucheHaut; }
            set { toucheHaut = value; }
        }

        public static Key ToucheBas
        {
            get { return toucheBas; }
            set { toucheBas = value; }
        }

        public static Key ToucheGauche
        {
            get { return toucheGauche; }
            set { toucheGauche = value; }
        }

        public static Key ToucheDroite
        {
            get { return toucheDroite; }
            set { toucheDroite = value; }
        }

        public static Key ToucheSprint
        {
            get { return toucheSprint; }
            set { toucheSprint = value; }
        }

        public static Key ToucheAttaque
        {
            get { return toucheAttaque; }
            set { toucheAttaque = value; }
        }

        public static Key ToucheBlocage
        {
            get { return toucheBlocage; }
            set { toucheBlocage = value; }
        }

        // Noms ENG (compatibilité)
        public static Key InputUp
        {
            get { return toucheHaut; }
            set { toucheHaut = value; }
        }

        public static Key InputDown
        {
            get { return toucheBas; }
            set { toucheBas = value; }
        }

        public static Key InputLeft
        {
            get { return toucheGauche; }
            set { toucheGauche = value; }
        }

        public static Key InputRight
        {
            get { return toucheDroite; }
            set { toucheDroite = value; }
        }

        public static Key InputSprint
        {
            get { return toucheSprint; }
            set { toucheSprint = value; }
        }

        public static Key InputAttack
        {
            get { return toucheAttaque; }
            set { toucheAttaque = value; }
        }

        public static Key InputBlock
        {
            get { return toucheBlocage; }
            set { toucheBlocage = value; }
        }

        // =========================================================
        // MUSIQUE + TIMER CRÉDITS
        // =========================================================
        private MediaPlayer lecteurMusique = new MediaPlayer();
        private DispatcherTimer timerCredits = new DispatcherTimer();

        // =========================================================
        // REBIND TOUCHES
        // =========================================================
        private Button boutonRebindEnCours = null;   // bouton qu'on est en train de modifier
        private string actionRebindEnCours = "";     // UP, DOWN, LEFT, RIGHT, SPRINT, ATTACK, BLOCK

        public MainWindow()
        {
            InitializeComponent();

            LancerMusique();

            // Initialise le slider du volume (si existe dans XAML)
            try
            {
                SliderVolume.Value = VolumeMusique;
            }
            catch
            {
            }

            // Timer crédits (≈ 60 FPS)
            timerCredits.Interval = TimeSpan.FromMilliseconds(16);
            timerCredits.Tick += TimerCredits_Tick;
        }

        /// <summary>
        /// Lance la musique du menu + boucle.
        /// </summary>
        private void LancerMusique()
        {
            try
            {
                lecteurMusique.Open(new Uri("Audio/BLACK OPS 2 ZOMBIES OFFICIAL Theme Song.mp3", UriKind.Relative));
                lecteurMusique.MediaEnded += LecteurMusique_MediaEnded;
                lecteurMusique.Volume = VolumeMusique;
                lecteurMusique.Play();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Quand la musique se termine, on la relance.
        /// </summary>
        private void LecteurMusique_MediaEnded(object sender, EventArgs e)
        {
            lecteurMusique.Position = TimeSpan.Zero;
            lecteurMusique.Play();
        }

        /// <summary>
        /// Animation crédits : fait défiler le texte vers le haut.
        /// </summary>
        private void TimerCredits_Tick(object sender, EventArgs e)
        {
            double topActuel = Canvas.GetTop(StackPanelCredit);

            // Si pas défini, on part du bas
            if (double.IsNaN(topActuel))
            {
                topActuel = 600;
            }

            topActuel = topActuel - 3.0;

            // Si le texte est sorti de l'écran, on le remet en bas
            if (topActuel < -StackPanelCredit.ActualHeight)
            {
                topActuel = ActualHeight;
            }

            Canvas.SetTop(StackPanelCredit, topActuel);
        }

        // =========================================================
        // BOUTONS MENU
        // =========================================================

        private void Button_Jouer_Click(object sender, RoutedEventArgs e)
        {
            lecteurMusique.Stop();

            Jeu jeu = new Jeu();
            jeu.Show();

            Close();
        }

        private void Button_Quitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ----- Paramètres -----
        private void Button_Parametre_Click(object sender, RoutedEventArgs e)
        {
            GridParametres.Visibility = Visibility.Visible;
        }

        private void Button_FermerParametre_Click(object sender, RoutedEventArgs e)
        {
            GridParametres.Visibility = Visibility.Collapsed;
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VolumeMusique = e.NewValue;
            lecteurMusique.Volume = VolumeMusique;
        }

        // ----- Règles -----
        private void Button_Regles_Click(object sender, RoutedEventArgs e)
        {
            GridRegles.Visibility = Visibility.Visible;
        }

        private void Button_FermerRegles_Click(object sender, RoutedEventArgs e)
        {
            GridRegles.Visibility = Visibility.Collapsed;
        }

        // ----- Crédits -----
        private void Button_Credit_Click(object sender, RoutedEventArgs e)
        {
            GridCredit.Visibility = Visibility.Visible;

            // Reset position + start animation
            Canvas.SetTop(StackPanelCredit, 600);
            timerCredits.Start();
        }

        private void Button_FermerCredit_Click(object sender, RoutedEventArgs e)
        {
            GridCredit.Visibility = Visibility.Collapsed;
            timerCredits.Stop();
        }

        // =========================================================
        // REBIND TOUCHES
        // =========================================================

        private void BtnBind_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            boutonRebindEnCours = btn;
            actionRebindEnCours = btn.Tag.ToString();

            // Visuel : attente touche
            btn.Content = "...";
            btn.Background = Brushes.LightGreen;

            // On écoute la prochaine touche pressée
            KeyDown += MainWindow_KeyDown_Rebind;
        }

        private void MainWindow_KeyDown_Rebind(object sender, KeyEventArgs e)
        {
            // On arrête l'écoute
            KeyDown -= MainWindow_KeyDown_Rebind;

            if (boutonRebindEnCours == null) return;

            // Mise à jour de la touche selon l'action
            if (actionRebindEnCours == "UP") ToucheHaut = e.Key;
            else if (actionRebindEnCours == "DOWN") ToucheBas = e.Key;
            else if (actionRebindEnCours == "LEFT") ToucheGauche = e.Key;
            else if (actionRebindEnCours == "RIGHT") ToucheDroite = e.Key;
            else if (actionRebindEnCours == "SPRINT") ToucheSprint = e.Key;
            else if (actionRebindEnCours == "ATTACK") ToucheAttaque = e.Key;
            else if (actionRebindEnCours == "BLOCK") ToucheBlocage = e.Key;

            boutonRebindEnCours.Content = e.Key.ToString();

            // Remet couleur d'origine
            BrushConverter convertisseur = new BrushConverter();
            boutonRebindEnCours.Background = (Brush)convertisseur.ConvertFromString("#DDB678");

            boutonRebindEnCours = null;
            actionRebindEnCours = "";
        }
    }
}
