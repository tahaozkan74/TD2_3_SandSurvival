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
        private static double volumeMusique = 0.5;

        public static double VolumeMusique
        {
            get { return volumeMusique; }
            set { volumeMusique = value; }
        }
        private static Key toucheHaut = Key.Z;
        private static Key toucheBas = Key.S;
        private static Key toucheGauche = Key.Q;
        private static Key toucheDroite = Key.D;

        private static Key toucheSprint = Key.LeftShift;
        private static Key toucheAttaque = Key.None;
        private static Key toucheBlocage = Key.None;

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

        private MediaPlayer lecteurMusique = new MediaPlayer();
        private DispatcherTimer timerCredits = new DispatcherTimer();

        private Button boutonRebindEnCours = null;   
        private string actionRebindEnCours = "";     

        public MainWindow()
        {
            InitializeComponent();

            LancerMusique();
            try
            {
                SliderVolume.Value = VolumeMusique;
            }
            catch
            {
            }
            timerCredits.Interval = TimeSpan.FromMilliseconds(16);
            timerCredits.Tick += TimerCredits_Tick;
        }
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
        private void LecteurMusique_MediaEnded(object sender, EventArgs e)
        {
            lecteurMusique.Position = TimeSpan.Zero;
            lecteurMusique.Play();
        }
        private void TimerCredits_Tick(object sender, EventArgs e)
        {
            double topActuel = Canvas.GetTop(StackPanelCredit);

            if (double.IsNaN(topActuel))
            {
                topActuel = 600;
            }

            topActuel = topActuel - 3.0;

            if (topActuel < -StackPanelCredit.ActualHeight)
            {
                topActuel = ActualHeight;
            }

            Canvas.SetTop(StackPanelCredit, topActuel);
        }
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
        private void Button_Regles_Click(object sender, RoutedEventArgs e)
        {
            GridRegles.Visibility = Visibility.Visible;
        }

        private void Button_FermerRegles_Click(object sender, RoutedEventArgs e)
        {
            GridRegles.Visibility = Visibility.Collapsed;
        }
        private void Button_Credit_Click(object sender, RoutedEventArgs e)
        {
            GridCredit.Visibility = Visibility.Visible;

            Canvas.SetTop(StackPanelCredit, 600);
            timerCredits.Start();
        }

        private void Button_FermerCredit_Click(object sender, RoutedEventArgs e)
        {
            GridCredit.Visibility = Visibility.Collapsed;
            timerCredits.Stop();
        }
        private void BtnBind_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            boutonRebindEnCours = btn;
            actionRebindEnCours = btn.Tag.ToString();

            btn.Content = "...";
            btn.Background = Brushes.LightGreen;

            KeyDown += MainWindow_KeyDown_Rebind;
        }

        private void MainWindow_KeyDown_Rebind(object sender, KeyEventArgs e)
        {
            KeyDown -= MainWindow_KeyDown_Rebind;

            if (boutonRebindEnCours == null) return;

            if (actionRebindEnCours == "UP") ToucheHaut = e.Key;
            else if (actionRebindEnCours == "DOWN") ToucheBas = e.Key;
            else if (actionRebindEnCours == "LEFT") ToucheGauche = e.Key;
            else if (actionRebindEnCours == "RIGHT") ToucheDroite = e.Key;
            else if (actionRebindEnCours == "SPRINT") ToucheSprint = e.Key;
            else if (actionRebindEnCours == "ATTACK") ToucheAttaque = e.Key;
            else if (actionRebindEnCours == "BLOCK") ToucheBlocage = e.Key;

            boutonRebindEnCours.Content = e.Key.ToString();

            BrushConverter convertisseur = new BrushConverter();
            boutonRebindEnCours.Background = (Brush)convertisseur.ConvertFromString("#DDB678");

            boutonRebindEnCours = null;
            actionRebindEnCours = "";
        }
    }
}
