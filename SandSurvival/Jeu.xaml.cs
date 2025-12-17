using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SandSurvival
{
    public class Ennemi
    {
        public Image Sprite { get; set; }
        public ProgressBar BarreVie { get; set; }
        public ScaleTransform Symetrie { get; set; }

        public int PointsDeVie { get; set; }
        public bool EstMort { get; set; }
        public bool EnTrainDeMourir { get; set; }

        public int IndexFrame { get; set; }
        public int CompteurFrame { get; set; }

        public bool EstBoss { get; set; }
    }

    public partial class Jeu : Window
    {

        private DispatcherTimer timerJeu = new DispatcherTimer();
        private bool vaHaut;
        private bool vaBas;
        private bool vaGauche;
        private bool vaDroite;

        private bool estEnPause = false;

        private MediaPlayer lecteurMusique = new MediaPlayer();

        private bool estEnCourse = false;
        private double vitesseMarche = 4;
        private double vitesseCourse = 6;

        private double endurance = 100;
        private bool peutCourir = true;

        private bool estEnBlocage = false;
        private double energieBouclier = 100;
        private bool peutBloquer = true;

        private bool bossApparuDansVague = false;

        private bool modeTitan = false;
        private bool kamikazeUtilise = false;
        private int totalKills = 0;

        private List<Image> kitsSoin = new List<Image>();
        private int compteurKillsSurvie = 0;
        private List<Ennemi> ennemis = new List<Ennemi>();

        private List<BitmapImage> spritesMomieMarche = new List<BitmapImage>();
        private List<BitmapImage> spritesMomieAttaque = new List<BitmapImage>();
        private List<BitmapImage> spritesMomieMort = new List<BitmapImage>();

        private List<BitmapImage> spritesJoueurMarche = new List<BitmapImage>();
        private List<BitmapImage> spritesJoueurAttaque = new List<BitmapImage>();
        private List<BitmapImage> spritesJoueurCourse = new List<BitmapImage>();
        private List<BitmapImage> spritesJoueurMort = new List<BitmapImage>();

        private BitmapImage spriteBlocage;
        private BitmapImage spriteKitSoin;
        private BitmapImage textureFond;

        private int vieJoueur = 100;
        private bool estInvulnerable = false;
        private int tempsInvulnerabilite = 0;

        private bool estEnAttaque = false;
        private bool joueurEnTrainDeMourir = false;

        private int frameActuelle = 0;
        private int compteurFrame = 0;
        private int delaiFrame = 5;

        private double mapMinX = 0;
        private double mapMaxX = 2048 * 5;
        private double mapMinY = 0;
        private double mapMaxY = 2048 * 5;

        private int vagueActuelle = 1;
        private int ennemisATuerTotal = 3;
        private int ennemisTuesDansVague = 0;
        private int ennemisSpawnDansVague = 0;

        private bool modeSurvie = false;
        private double timerSpawnSurvie = 0;

        private bool vagueActive = false;
        private bool compteARebours = true;
        private bool messageFinVague = false;

        private double tempsCompteARebours = 10.0;
        private double timerMessage = 0;

        private Random aleatoire = new Random();

        public Jeu()
        {
            InitializeComponent();

            ChargerRessources();

            Loaded += Jeu_Loaded;

            BarreDeVie.Value = vieJoueur;
            BarreStamina.Value = endurance;
            BarreBouclier.Value = energieBouclier;

            try
            {
                PauseVolumeSlider.Value = MainWindow.VolumeMusique;
                SpeedSlider.Value = vitesseMarche;
                DifficultySlider.Value = 3;
            }
            catch
            {
            }

            MouseRightButtonDown += Window_MouseRightButtonDown;
            MouseRightButtonUp += Window_MouseRightButtonUp;

            InitialiserVague(1);

            timerJeu.Interval = TimeSpan.FromMilliseconds(16);
            timerJeu.Tick += BoucleJeu;
            timerJeu.Start();

            Focus();
        }
        private void Jeu_Loaded(object sender, RoutedEventArgs e)
        {
            InitialiserMapEtendue();
            LancerMusiqueJeu();
            try
            {
                CalqueCollisions.Visibility = Visibility.Collapsed;
            }
            catch
            {
            }
        }
        private void ChargerRessources()
        {
            try
            {
                textureFond = new BitmapImage(new Uri("pack://application:,,,/Images/image de fond/fond.png"));

                int i;

                for (i = 0; i < 8; i = i + 1)
                {
                    spritesJoueurMarche.Add(new BitmapImage(new Uri("pack://application:,,,/Images/Player/walk" + i + ".png")));
                }

                for (i = 1; i <= 3; i = i + 1)
                {
                    spritesJoueurAttaque.Add(new BitmapImage(new Uri("pack://application:,,,/Images/Player/attaque" + i + ".png")));
                }

                for (i = 1; i <= 3; i = i + 1)
                {
                    spritesJoueurCourse.Add(new BitmapImage(new Uri("pack://application:,,,/Images/Player/course" + i + ".png")));
                }

                for (i = 1; i <= 3; i = i + 1)
                {
                    spritesJoueurMort.Add(new BitmapImage(new Uri("pack://application:,,,/Images/Player/mort" + i + ".png")));
                }

                spriteBlocage = new BitmapImage(new Uri("pack://application:,,,/Images/Player/block.png"));
                spriteKitSoin = new BitmapImage(new Uri("pack://application:,,,/Images/Objets/soin.png"));

                for (i = 1; i <= 6; i = i + 1)
                {
                    spritesMomieMarche.Add(new BitmapImage(new Uri("pack://application:,,,/Images/mommy/marche" + i + ".png")));
                }

                for (i = 1; i <= 3; i = i + 1)
                {
                    spritesMomieAttaque.Add(new BitmapImage(new Uri("pack://application:,,,/Images/mommy/attaque" + i + ".png")));
                }

                for (i = 1; i <= 5; i = i + 1)
                {
                    spritesMomieMort.Add(new BitmapImage(new Uri("pack://application:,,,/Images/mommy/meurt" + i + ".png")));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur Assets : " + ex.Message);
            }
        }
        private void InitialiserMapEtendue()
        {
            if (textureFond == null) return;

            double tailleTuile = 2048;

            int x;
            int y;

            for (x = 0; x < 5; x = x + 1)
            {
                for (y = 0; y < 5; y = y + 1)
                {
                    Image tuile = new Image();
                    tuile.Source = textureFond;
                    tuile.Width = tailleTuile;
                    tuile.Height = tailleTuile;
                    tuile.Stretch = Stretch.Fill;

                    Canvas.SetLeft(tuile, x * tailleTuile);
                    Canvas.SetTop(tuile, y * tailleTuile);

                    Panel.SetZIndex(tuile, -1000);
                    MondeDeJeu.Children.Add(tuile);
                }
            }
        }
        private void LancerMusiqueJeu()
        {
            try
            {
                string chemin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "BLACK OPS 2 ZOMBIES OFFICIAL Theme Song.mp3");
                if (File.Exists(chemin))
                {
                    lecteurMusique.Open(new Uri(chemin, UriKind.Absolute));
                    lecteurMusique.MediaEnded += LecteurMusique_MediaEnded;
                    lecteurMusique.Volume = MainWindow.VolumeMusique;
                    lecteurMusique.Play();
                }
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

        private void JouerEffetSonore(string nomFichier)
        {
            try
            {
                string chemin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", nomFichier);
                if (File.Exists(chemin))
                {
                    MediaPlayer sfx = new MediaPlayer();
                    sfx.Open(new Uri(chemin, UriKind.Absolute));
                    sfx.Volume = MainWindow.VolumeMusique;
                    sfx.Play();
                }
            }
            catch
            {
            }
        }
        private void BoucleJeu(object sender, EventArgs e)
        {
            if (estEnPause) return;

            if (joueurEnTrainDeMourir)
            {
                GererMortJoueur();
                return;
            }

            GererVaguesEtSpawn();
            GererEndurance();
            GererBouclier();
            GererRamassageKits();

            AppliquerModeTitan();

            double vitesse = vitesseMarche;
            if (estEnCourse && peutCourir) vitesse = vitesseCourse;
            if (modeTitan) vitesse = 10;

            double x = Canvas.GetLeft(Player);
            double y = Canvas.GetTop(Player);

            double xVoulu = x;
            double yVoulu = y;

            bool joueurBouge = false;

            if (vaHaut) { yVoulu = yVoulu - vitesse; joueurBouge = true; }
            if (vaBas) { yVoulu = yVoulu + vitesse; joueurBouge = true; }

            if (vaGauche)
            {
                xVoulu = xVoulu - vitesse;
                joueurBouge = true;
                MettreDirectionJoueurGauche();
            }
            if (vaDroite)
            {
                xVoulu = xVoulu + vitesse;
                joueurBouge = true;
                MettreDirectionJoueurDroite();
            }

            double xFinal = x;
            double yFinal = y;

            if (xVoulu >= mapMinX && xVoulu <= mapMaxX - 60) xFinal = xVoulu;
            if (yVoulu >= mapMinY && yVoulu <= mapMaxY - 60) yFinal = yVoulu;

            Canvas.SetLeft(Player, xFinal);
            Canvas.SetTop(Player, yFinal);

            MettreAJourEnnemis(xFinal, yFinal);

            if (estInvulnerable)
            {
                tempsInvulnerabilite = tempsInvulnerabilite - 1;
                if (tempsInvulnerabilite <= 0)
                {
                    estInvulnerable = false;
                    Player.Opacity = 1;
                }
            }

            CentrerCamera(xFinal, yFinal);

            GererAnimationJoueur(joueurBouge);
        }
        private void AppliquerModeTitan()
        {
            double facteur = 1.0;
            if (modeTitan) facteur = 3.0;

            PlayerScale.ScaleY = facteur;

            if (PlayerScale.ScaleX >= 0) PlayerScale.ScaleX = facteur;
            else PlayerScale.ScaleX = -facteur;
        }

        private void MettreDirectionJoueurGauche()
        {
            double facteur = 1.0;
            if (modeTitan) facteur = 3.0;
            PlayerScale.ScaleX = -facteur;
        }

        private void MettreDirectionJoueurDroite()
        {
            double facteur = 1.0;
            if (modeTitan) facteur = 3.0;
            PlayerScale.ScaleX = facteur;
        }

        private void CentrerCamera(double xJoueur, double yJoueur)
        {
            double zoom = 1.5;

            double centreX = ActualWidth / 2;
            double centreY = ActualHeight / 2;

            if (centreX <= 1) centreX = 600;
            if (centreY <= 1) centreY = 400;

            Camera.X = centreX - ((xJoueur + Player.Width / 2) * zoom);
            Camera.Y = centreY - ((yJoueur + Player.Height / 2) * zoom);
        }
        private void InitialiserVague(int numero)
        {
            vagueActuelle = numero;
            ennemisTuesDansVague = 0;
            ennemisSpawnDansVague = 0;
            bossApparuDansVague = false;

            int i;
            for (i = ennemis.Count - 1; i >= 0; i = i - 1)
            {
                MondeDeJeu.Children.Remove(ennemis[i].Sprite);
                MondeDeJeu.Children.Remove(ennemis[i].BarreVie);
                ennemis.RemoveAt(i);
            }

            if (vagueActuelle <= 3)
            {
                modeSurvie = false;
                ennemisATuerTotal = 3;

                TexteAnnonceVague.Text = "LA VAGUE " + vagueActuelle + " VA COMMENCER DANS";
                TexteChrono.Text = "5";
                tempsCompteARebours = 5.0;

                GridSurvie.Visibility = Visibility.Collapsed;
            }
            else
            {
                modeSurvie = true;
                TexteAnnonceVague.Text = "PRÉPAREZ-VOUS À SURVIVRE...";
                TexteChrono.Text = "5";
                tempsCompteARebours = 5.0;
            }

            FaireApparaitreKitSoin();

            GridCompteARebours.Visibility = Visibility.Visible;
            compteARebours = true;
            vagueActive = false;
        }

        private void GererVaguesEtSpawn()
        {
            if (messageFinVague)
            {
                timerMessage = timerMessage - 0.016;
                if (timerMessage <= 0)
                {
                    messageFinVague = false;
                    GridVagueFinie.Visibility = Visibility.Collapsed;
                    InitialiserVague(vagueActuelle + 1);
                }
                return;
            }

            if (compteARebours)
            {
                tempsCompteARebours = tempsCompteARebours - 0.016;
                TexteChrono.Text = Math.Ceiling(tempsCompteARebours).ToString();

                if (tempsCompteARebours <= 0)
                {
                    compteARebours = false;
                    vagueActive = true;
                    GridCompteARebours.Visibility = Visibility.Collapsed;

                    if (modeSurvie) GridSurvie.Visibility = Visibility.Visible;
                }
                return;
            }

            if (!vagueActive) return;

            if (!modeSurvie)
            {
                if (ennemisTuesDansVague < ennemisATuerTotal)
                {
                    int max = 1;
                    if (ennemisSpawnDansVague < ennemisATuerTotal && CompterEnnemisActifs() < max)
                    {
                        FaireApparaitreEnnemi(false);
                    }
                }
                else
                {
                    if (!bossApparuDansVague)
                    {
                        FaireApparaitreBoss();
                    }
                    else
                    {
                        if (CompterBossActifs() == 0)
                        {
                            vagueActive = false;
                            TexteFinVague.Text = "VAGUE " + vagueActuelle + " TERMINÉE";
                            GridVagueFinie.Visibility = Visibility.Visible;

                            messageFinVague = true;
                            timerMessage = 5.0;
                            bossApparuDansVague = false;
                        }
                    }
                }
            }
            else
            {
                timerSpawnSurvie = timerSpawnSurvie - 0.016;
                if (timerSpawnSurvie <= 0)
                {
                    if (ennemis.Count < 50)
                    {
                        FaireApparaitreEnnemi(false);
                    }
                    timerSpawnSurvie = 0.5;
                }
            }
        }

        private int CompterBossActifs()
        {
            int c = 0;
            int i;

            for (i = 0; i < ennemis.Count; i = i + 1)
            {
                if (ennemis[i].EstBoss && !ennemis[i].EstMort) c = c + 1;
            }

            return c;
        }

        private int CompterEnnemisActifs()
        {
            int c = 0;
            int i;

            for (i = 0; i < ennemis.Count; i = i + 1)
            {
                if (!ennemis[i].EstMort && !ennemis[i].EnTrainDeMourir) c = c + 1;
            }

            return c;
        }

        private void GererEndurance()
        {
            if (estEnCourse && (vaHaut || vaBas || vaGauche || vaDroite))
            {
                endurance = endurance - 0.5;

                if (endurance <= 0)
                {
                    endurance = 0;
                    peutCourir = false;
                    estEnCourse = false;
                }
            }
            else
            {
                if (endurance < 100)
                {
                    endurance = endurance + 0.3;
                    if (endurance >= 20) peutCourir = true;
                }
            }

            BarreStamina.Value = endurance;

            if (peutCourir) BarreStamina.Foreground = Brushes.Orange;
            else BarreStamina.Foreground = Brushes.Gray;
        }

        private void GererBouclier()
        {
            if (estEnBlocage && peutBloquer)
            {
                energieBouclier = energieBouclier - 0.5;

                if (energieBouclier <= 0)
                {
                    energieBouclier = 0;
                    peutBloquer = false;
                    estEnBlocage = false;
                }
            }
            else
            {
                if (energieBouclier < 100)
                {
                    energieBouclier = energieBouclier + 0.2;
                    if (energieBouclier >= 20) peutBloquer = true;
                }
            }

            BarreBouclier.Value = energieBouclier;
        }
        private void FaireApparaitreKitSoin()
        {
            if (kitsSoin.Count > 0) return;
            if (spriteKitSoin == null) return;

            double pX = Canvas.GetLeft(Player);
            double pY = Canvas.GetTop(Player);

            double angle = aleatoire.NextDouble() * Math.PI * 2;
            double distance = aleatoire.Next(200, 500);

            double x = pX + Math.Cos(angle) * distance;
            double y = pY + Math.Sin(angle) * distance;

            if (x < mapMinX) x = mapMinX + 50;
            if (x > mapMaxX) x = mapMaxX - 50;
            if (y < mapMinY) y = mapMinY + 50;
            if (y > mapMaxY) y = mapMaxY - 50;

            Image kit = new Image();
            kit.Source = spriteKitSoin;
            kit.Width = 30;
            kit.Height = 30;

            Canvas.SetLeft(kit, x);
            Canvas.SetTop(kit, y);

            Panel.SetZIndex(kit, 5000);

            MondeDeJeu.Children.Add(kit);
            kitsSoin.Add(kit);
        }

        private void GererRamassageKits()
        {
            double pX = Canvas.GetLeft(Player);
            double pY = Canvas.GetTop(Player);

            Rect rectJoueur = new Rect(pX + 15, pY + 15, 30, 30);

            int i;
            for (i = kitsSoin.Count - 1; i >= 0; i = i - 1)
            {
                Image kit = kitsSoin[i];

                double kX = Canvas.GetLeft(kit);
                double kY = Canvas.GetTop(kit);

                Rect rectKit = new Rect(kX, kY, 30, 30);

                if (rectJoueur.IntersectsWith(rectKit))
                {
                    vieJoueur = vieJoueur + 30;
                    if (vieJoueur > 100) vieJoueur = 100;

                    BarreDeVie.Value = vieJoueur;

                    MondeDeJeu.Children.Remove(kit);
                    kitsSoin.RemoveAt(i);
                }
            }
        }
        private void MettreAJourEnnemis(double xJoueur, double yJoueur)
        {
            int i;
            for (i = ennemis.Count - 1; i >= 0; i = i - 1)
            {
                Ennemi ennemi = ennemis[i];

                if (ennemi.EstMort && !ennemi.EnTrainDeMourir)
                {
                    MondeDeJeu.Children.Remove(ennemi.Sprite);
                    MondeDeJeu.Children.Remove(ennemi.BarreVie);
                    ennemis.RemoveAt(i);

                    ennemisTuesDansVague = ennemisTuesDansVague + 1;

                    if (ennemi.EstBoss)
                    {
                        try
                        {
                            TexteBoss.Visibility = Visibility.Collapsed;
                        }
                        catch
                        {
                        }
                        bossApparuDansVague = false;
                    }

                    totalKills = totalKills + 1;
                    try
                    {
                        TexteScore.Text = "MOMIES TUÉES : " + totalKills;
                    }
                    catch
                    {
                    }

                    if (modeSurvie)
                    {
                        compteurKillsSurvie = compteurKillsSurvie + 1;
                        if (compteurKillsSurvie >= 5)
                        {
                            FaireApparaitreKitSoin();
                            compteurKillsSurvie = 0;
                        }
                    }

                    continue;
                }

                if (ennemi.EnTrainDeMourir)
                {
                    AnimerMortEnnemi(ennemi);
                    continue;
                }

                double eX = Canvas.GetLeft(ennemi.Sprite);
                double eY = Canvas.GetTop(ennemi.Sprite);

                double dx = xJoueur - eX;
                double dy = yJoueur - eY;

                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dx > 0) ennemi.Symetrie.ScaleX = -1;
                else ennemi.Symetrie.ScaleX = 1;

                if (dist > 10)
                {
                    double vitesse = 5.5;
                    if (ennemi.EstBoss) vitesse = 8.0;

                    double prochainX = eX;
                    double prochainY = eY;

                    if (dist != 0)
                    {
                        prochainX = eX + (dx / dist) * vitesse;
                        prochainY = eY + (dy / dist) * vitesse;
                    }

                    eX = prochainX;
                    eY = prochainY;

                    ennemi.CompteurFrame = ennemi.CompteurFrame + 1;
                    if (ennemi.CompteurFrame > 5)
                    {
                        ennemi.IndexFrame = ennemi.IndexFrame + 1;
                        if (ennemi.IndexFrame >= spritesMomieMarche.Count) ennemi.IndexFrame = 0;

                        ennemi.Sprite.Source = spritesMomieMarche[ennemi.IndexFrame];
                        ennemi.CompteurFrame = 0;
                    }
                }
                else
                {
                    ennemi.CompteurFrame = ennemi.CompteurFrame + 1;
                    if (ennemi.CompteurFrame > 3)
                    {
                        ennemi.IndexFrame = ennemi.IndexFrame + 1;

                        if (ennemi.IndexFrame == 1) JouerEffetSonore("MomieAttaque.mp4");

                        if (ennemi.IndexFrame >= spritesMomieAttaque.Count) ennemi.IndexFrame = 0;

                        if (spritesMomieAttaque.Count > 0)
                        {
                            ennemi.Sprite.Source = spritesMomieAttaque[ennemi.IndexFrame];
                        }

                        ennemi.CompteurFrame = 0;
                    }
                }

                Canvas.SetLeft(ennemi.Sprite, eX);
                Canvas.SetTop(ennemi.Sprite, eY);

                Canvas.SetLeft(ennemi.BarreVie, eX + 10);
                Canvas.SetTop(ennemi.BarreVie, eY - 10);

                Rect rectJoueur = new Rect(xJoueur + 15, yJoueur + 15, 30, 30);
                Rect rectEnnemi = new Rect(eX + 15, eY + 15, 30, 30);

                if (rectJoueur.IntersectsWith(rectEnnemi))
                {
                    int degats = 10;
                    if (ennemi.EstBoss) degats = 34;
                    PrendreDegats(degats);
                }
            }
        }

        private void AnimerMortEnnemi(Ennemi ennemi)
        {
            ennemi.BarreVie.Visibility = Visibility.Collapsed;

            ennemi.CompteurFrame = ennemi.CompteurFrame + 1;
            if (ennemi.CompteurFrame > 8)
            {
                ennemi.IndexFrame = ennemi.IndexFrame + 1;

                if (ennemi.IndexFrame >= spritesMomieMort.Count)
                {
                    ennemi.EnTrainDeMourir = false;
                }
                else
                {
                    ennemi.Sprite.Source = spritesMomieMort[ennemi.IndexFrame];
                }

                ennemi.CompteurFrame = 0;
            }
        }
        private void FaireApparaitreEnnemi(bool boss)
        {
            ennemisSpawnDansVague = ennemisSpawnDansVague + 1;

            if (spritesMomieMarche.Count == 0) return;

            Ennemi n = new Ennemi();
            n.EstBoss = boss;

            n.Sprite = new Image();
            n.Sprite.Width = 60;
            n.Sprite.Height = 60;

            if (boss)
            {
                n.Sprite.Width = 90;
                n.Sprite.Height = 90;
            }

            n.Sprite.Source = spritesMomieMarche[0];

            n.Sprite.RenderTransformOrigin = new Point(0.5, 0.5);
            n.Symetrie = new ScaleTransform();
            n.Sprite.RenderTransform = n.Symetrie;

            Panel.SetZIndex(n.Sprite, 9998);

            n.BarreVie = new ProgressBar();
            n.BarreVie.Foreground = Brushes.Red;
            n.BarreVie.BorderBrush = Brushes.Black;
            n.BarreVie.BorderThickness = new Thickness(1);
            n.BarreVie.Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));

            if (boss)
            {
                n.BarreVie.Width = 80;
                n.BarreVie.Height = 10;
                n.BarreVie.Foreground = Brushes.DarkRed;
            }
            else
            {
                n.BarreVie.Width = 40;
                n.BarreVie.Height = 5;
            }

            Panel.SetZIndex(n.BarreVie, 10000);

            int difficulte = 3;
            try
            {
                difficulte = (int)DifficultySlider.Value;
            }
            catch
            {
                difficulte = 3;
            }

            if (boss)
            {
                n.PointsDeVie = 10;
            }
            else
            {
                if (modeSurvie) n.PointsDeVie = difficulte + 3;
                else n.PointsDeVie = difficulte + ((vagueActuelle - 1) * 2);
            }

            n.BarreVie.Maximum = n.PointsDeVie;
            n.BarreVie.Value = n.PointsDeVie;

            double pX = Canvas.GetLeft(Player);
            double pY = Canvas.GetTop(Player);

            double angle = aleatoire.NextDouble() * Math.PI * 2;
            double distance = aleatoire.Next(400, 800);

            double x = pX + Math.Cos(angle) * distance;
            double y = pY + Math.Sin(angle) * distance;

            if (x < mapMinX + 50) x = mapMinX + 50;
            if (x > mapMaxX - 50) x = mapMaxX - 50;
            if (y < mapMinY + 50) y = mapMinY + 50;
            if (y > mapMaxY - 50) y = mapMaxY - 50;

            Canvas.SetLeft(n.Sprite, x);
            Canvas.SetTop(n.Sprite, y);

            Canvas.SetLeft(n.BarreVie, x + 10);
            Canvas.SetTop(n.BarreVie, y - 10);

            MondeDeJeu.Children.Add(n.Sprite);
            MondeDeJeu.Children.Add(n.BarreVie);
            ennemis.Add(n);
        }

        private void FaireApparaitreBoss()
        {
            bossApparuDansVague = true;

            try
            {
                TexteBoss.Visibility = Visibility.Visible;
            }
            catch
            {
            }

            JouerEffetSonore("MomieAttaque.mp4");
            FaireApparaitreEnnemi(true);
        }
        private void PrendreDegats(int degats)
        {
            if (joueurEnTrainDeMourir) return;
            if (estInvulnerable) return;

            if (estEnBlocage)
            {
                energieBouclier = energieBouclier - 5;

                estInvulnerable = true;
                tempsInvulnerabilite = 30;

                JouerEffetSonore("Bloque.mp4");
                return;
            }

            vieJoueur = vieJoueur - degats;
            if (vieJoueur < 0) vieJoueur = 0;

            BarreDeVie.Value = vieJoueur;

            Player.Opacity = 0.5;
            estInvulnerable = true;
            tempsInvulnerabilite = 120;

            if (vieJoueur <= 0)
            {
                joueurEnTrainDeMourir = true;
                JouerEffetSonore("MortPerso.mp4");

                frameActuelle = 0;
                compteurFrame = 0;

                if (spritesJoueurMort.Count > 0)
                {
                    Player.Source = spritesJoueurMort[0];
                }
            }
            else
            {
                JouerEffetSonore("Degat.mp4");
            }
        }
        private void CommencerBlocage()
        {
            if (peutBloquer && !joueurEnTrainDeMourir && !estEnPause)
            {
                estEnBlocage = true;
            }
        }

        private void ArreterBlocage()
        {
            estEnBlocage = false;
        }

        private void TenterAttaque()
        {
            if (joueurEnTrainDeMourir) return;
            if (estEnPause) return;

            if (estEnAttaque) return;
            if (spritesJoueurAttaque.Count == 0) return;

            estEnAttaque = true;
            frameActuelle = 0;
            compteurFrame = 0;

            Player.Source = spritesJoueurAttaque[0];
            JouerEffetSonore("AttaqueEffect.mp4");

            double pX = Canvas.GetLeft(Player) + 30;
            double pY = Canvas.GetTop(Player) + 30;

            int i;
            for (i = 0; i < ennemis.Count; i = i + 1)
            {
                Ennemi ennemi = ennemis[i];
                if (ennemi.EstMort || ennemi.EnTrainDeMourir) continue;

                double eX = Canvas.GetLeft(ennemi.Sprite) + 30;
                double eY = Canvas.GetTop(ennemi.Sprite) + 30;

                double dx = pX - eX;
                double dy = pY - eY;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < 80)
                {
                    int degats = 1;
                    if (modeTitan) degats = 100;

                    ennemi.PointsDeVie = ennemi.PointsDeVie - degats;
                    ennemi.BarreVie.Value = ennemi.PointsDeVie;

                    ennemi.Sprite.Opacity = 0.5;

                    DispatcherTimer t = new DispatcherTimer();
                    t.Interval = TimeSpan.FromMilliseconds(100);
                    t.Tick += FlashHit_Tick;
                    t.Tag = ennemi;
                    t.Start();

                    if (ennemi.PointsDeVie <= 0)
                    {
                        JouerEffetSonore("MomieDeath.mp4");
                        ennemi.EstMort = true;
                        ennemi.EnTrainDeMourir = true;
                        ennemi.IndexFrame = 0;

                        if (spritesMomieMort.Count > 0)
                        {
                            ennemi.Sprite.Source = spritesMomieMort[0];
                        }
                    }

                    break;
                }
            }
        }

        private void FlashHit_Tick(object sender, EventArgs e)
        {
            DispatcherTimer t = sender as DispatcherTimer;
            if (t == null) return;

            Ennemi ennemi = t.Tag as Ennemi;
            if (ennemi != null)
            {
                if (!ennemi.EnTrainDeMourir)
                {
                    ennemi.Sprite.Opacity = 1;
                }
            }

            t.Stop();
        }

        private void GererAnimationJoueur(bool bouge)
        {
            if (joueurEnTrainDeMourir) return;

            if (spritesJoueurMarche.Count == 0) return;
            if (spritesJoueurCourse.Count == 0) return;

            if (estEnAttaque && spritesJoueurAttaque.Count > 0)
            {
                compteurFrame = compteurFrame + 1;

                if (compteurFrame > delaiFrame)
                {
                    frameActuelle = frameActuelle + 1;

                    if (frameActuelle >= spritesJoueurAttaque.Count)
                    {
                        estEnAttaque = false;
                        frameActuelle = 0;

                        if (estEnBlocage && spriteBlocage != null) Player.Source = spriteBlocage;
                        else Player.Source = spritesJoueurMarche[0];
                    }
                    else
                    {
                        Player.Source = spritesJoueurAttaque[frameActuelle];
                    }

                    compteurFrame = 0;
                }
                return;
            }

            if (estEnBlocage)
            {
                if (spriteBlocage != null) Player.Source = spriteBlocage;
                return;
            }

            if (bouge)
            {
                List<BitmapImage> liste = spritesJoueurMarche;
                delaiFrame = 5;

                if (estEnCourse)
                {
                    liste = spritesJoueurCourse;
                    delaiFrame = 3;
                }

                compteurFrame = compteurFrame + 1;

                if (compteurFrame > delaiFrame)
                {
                    frameActuelle = frameActuelle + 1;
                    if (frameActuelle >= liste.Count) frameActuelle = 0;

                    Player.Source = liste[frameActuelle];
                    compteurFrame = 0;
                }
            }
        }

        private void GererMortJoueur()
        {
            compteurFrame = compteurFrame + 1;

            if (compteurFrame > 15)
            {
                if (frameActuelle < spritesJoueurMort.Count)
                {
                    Player.Source = spritesJoueurMort[frameActuelle];
                    frameActuelle = frameActuelle + 1;
                }
                else
                {
                    timerJeu.Stop();
                    GridGameOver.Visibility = Visibility.Visible;
                }

                compteurFrame = 0;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TenterAttaque();
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            CommencerBlocage();
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ArreterBlocage();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (estEnPause) return;

            if (e.Key == MainWindow.ToucheHaut) vaHaut = true;
            if (e.Key == MainWindow.ToucheBas) vaBas = true;
            if (e.Key == MainWindow.ToucheGauche) vaGauche = true;
            if (e.Key == MainWindow.ToucheDroite) vaDroite = true;
            if (e.Key == MainWindow.ToucheSprint) estEnCourse = true;

            if (MainWindow.ToucheAttaque != Key.None)
            {
                if (e.Key == MainWindow.ToucheAttaque) TenterAttaque();
            }

            if (MainWindow.ToucheBlocage != Key.None)
            {
                if (e.Key == MainWindow.ToucheBlocage) CommencerBlocage();
            }

            if (e.Key == Key.T)
            {
                modeTitan = !modeTitan;

                if (modeTitan)
                {
                    vitesseMarche = 10;
                    vitesseCourse = 15;
                    vieJoueur = 1000;
                    BarreDeVie.Maximum = 1000;
                    BarreDeVie.Value = vieJoueur;
                }
                else
                {
                    vitesseMarche = 4;
                    vitesseCourse = 6;
                    vieJoueur = 100;
                    BarreDeVie.Maximum = 100;
                    BarreDeVie.Value = vieJoueur;
                }
            }

            if (e.Key == Key.K)
            {
                if (modeSurvie && !kamikazeUtilise)
                {
                    kamikazeUtilise = true;

                    int i;
                    for (i = 0; i < ennemis.Count; i = i + 1)
                    {
                        ennemis[i].PointsDeVie = 0;
                        ennemis[i].EstMort = true;
                        ennemis[i].EnTrainDeMourir = true;
                        ennemis[i].IndexFrame = 0;

                        if (spritesMomieMort.Count > 0)
                        {
                            ennemis[i].Sprite.Source = spritesMomieMort[0];
                        }
                    }

                    vieJoueur = 10;
                    BarreDeVie.Value = vieJoueur;
                    Player.Opacity = 0.5;

                    JouerEffetSonore("Degat.mp4");
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == MainWindow.ToucheHaut) vaHaut = false;
            if (e.Key == MainWindow.ToucheBas) vaBas = false;
            if (e.Key == MainWindow.ToucheGauche) vaGauche = false;
            if (e.Key == MainWindow.ToucheDroite) vaDroite = false;

            if (e.Key == MainWindow.ToucheSprint) estEnCourse = false;

            if (e.Key == MainWindow.ToucheBlocage)
            {
                ArreterBlocage();
            }
        }
        private void Button_Menu_Click(object sender, RoutedEventArgs e)
        {
            timerJeu.Stop();
            lecteurMusique.Stop();

            MainWindow menu = new MainWindow();
            menu.Show();
            Close();
        }

        private void Button_Reessayer_Click(object sender, RoutedEventArgs e)
        {
            lecteurMusique.Stop();

            Jeu jeu = new Jeu();
            jeu.Show();
            Close();
        }

        private void Button_Pause_Click(object sender, RoutedEventArgs e)
        {
            estEnPause = true;
            timerJeu.Stop();
            GridPause.Visibility = Visibility.Visible;
        }

        private void Button_Reprendre_Click(object sender, RoutedEventArgs e)
        {
            estEnPause = false;
            GridPause.Visibility = Visibility.Collapsed;
            timerJeu.Start();
            Focus();
        }

        private void PauseVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lecteurMusique.Volume = e.NewValue;
            MainWindow.VolumeMusique = e.NewValue;
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vitesseMarche = e.NewValue;
            vitesseCourse = vitesseMarche * 1.6;
        }
    }
}
