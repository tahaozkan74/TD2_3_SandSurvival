using System;
using System.Collections.Generic;
using System.Linq; // Important pour gérer les listes
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SandSurvival
{
    // Classe simple pour gérer chaque Momie individuellement
    public class Ennemi
    {
        public Image Sprite { get; set; }
        public ProgressBar HealthBar { get; set; }
        public ScaleTransform Scale { get; set; }
        public int HP { get; set; }
        public bool IsDead { get; set; }
        public bool IsDying { get; set; }

        // Animation propre à cet ennemi
        public int FrameIndex { get; set; }
        public int FrameCounter { get; set; }
    }

    public partial class Jeu : Window
    {
        // --- MOTEUR ---
        private DispatcherTimer gameTimer = new DispatcherTimer();
        private bool goUp, goDown, goLeft, goRight;

        // VITESSE & STAMINA
        private bool isRunning = false;
        private double walkSpeed = 10;
        private double runSpeed = 18;
        private double stamina = 100;
        private bool canRun = true;

        // --- LISTE DES ENNEMIS ---
        private List<Ennemi> enemies = new List<Ennemi>();

        // --- RESSOURCES GRAPHIQUES ---
        private List<BitmapImage> mummySprites = new List<BitmapImage>();
        private List<BitmapImage> mummyAttackSprites = new List<BitmapImage>();
        private List<BitmapImage> mummyDeathSprites = new List<BitmapImage>();

        // --- ANIMATIONS JOUEUR ---
        private List<BitmapImage> walkSprites = new List<BitmapImage>();
        private List<BitmapImage> attackSprites = new List<BitmapImage>();
        private List<BitmapImage> runSprites = new List<BitmapImage>();

        // Stats Joueur
        private int vieJoueur = 100;
        private bool estInvulnerable = false;
        private int tempsInvulnerabilite = 0;
        private bool isAttacking = false;
        private int currentFrame = 0;
        private int frameCounter = 0;
        private int frameDelay = 5;

        // --- MAP INFINIE ---
        private List<BitmapImage> solTextures = new List<BitmapImage>();
        private Dictionary<string, Image> tuilesActives = new Dictionary<string, Image>();

        // IMPORTANT : Si tes images font 1024x1024, laisse 1024. Si elles sont plus petites, ajuste ce nombre.
        private int tailleTuile = 1024;
        private int distanceVue = 2;

        // --- SYSTEME DE VAGUES ---
        private int currentWave = 1;
        private int enemiesToKillTotal = 3;
        private int enemiesKilledInWave = 0;
        private int enemiesSpawnedInWave = 0;

        private bool isSurvivalMode = false;
        private double spawnTimer = 0;

        // Etats de transition
        private bool isWaveActive = false;
        private bool isCountingDown = true;
        private bool isWaveFinishedMessage = false;
        private double waveCountdown = 10.0;
        private double messageTimer = 0;

        public Jeu()
        {
            InitializeComponent();
            LoadAssets();

            BarreDeVie.Value = vieJoueur;
            BarreStamina.Value = stamina;

            SetupWave(1);

            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.Focus();
        }

        private void SetupWave(int waveNumber)
        {
            currentWave = waveNumber;
            enemiesKilledInWave = 0;
            enemiesSpawnedInWave = 0;

            // Nettoyage
            foreach (var ennemi in enemies)
            {
                MondeDeJeu.Children.Remove(ennemi.Sprite);
                MondeDeJeu.Children.Remove(ennemi.HealthBar);
            }
            enemies.Clear();

            if (currentWave <= 3)
            {
                // Vagues classiques
                isSurvivalMode = false;
                enemiesToKillTotal = 3;
                TexteAnnonceVague.Text = "LA VAGUE " + currentWave + " VA COMMENCER DANS";
                TexteChrono.Text = "10";
                waveCountdown = 10.0;
                GridSurvie.Visibility = Visibility.Collapsed;
            }
            else
            {
                // MODE SURVIE
                isSurvivalMode = true;
                TexteAnnonceVague.Text = "PRÉPAREZ-VOUS À SURVIVRE...";
                TexteChrono.Text = "5";
                waveCountdown = 5.0;
            }

            GridCompteARebours.Visibility = Visibility.Visible;
            isCountingDown = true;
            isWaveActive = false;
        }

        private void LoadAssets()
        {
            try
            {
                // --- MODIFICATION ICI : CHARGEMENT DES 4 NOUVEAUX FONDS ---
                // On charge fond1.png, fond2.png, fond3.png, fond4.png
                for (int i = 1; i <= 4; i++)
                    solTextures.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/image de fond/fond{i}.png")));

                // Perso
                for (int i = 0; i < 8; i++) walkSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/walk{i}.png")));
                for (int i = 1; i <= 3; i++) attackSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/attaque{i}.png")));
                for (int i = 1; i <= 3; i++) runSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/course{i}.png")));
                // Momie
                for (int i = 1; i <= 6; i++) mummySprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/mommy/marche{i}.png")));
                for (int i = 1; i <= 3; i++) mummyAttackSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/mommy/attaque{i}.png")));
                for (int i = 1; i <= 5; i++) mummyDeathSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/mommy/meurt{i}.png")));
            }
            catch (Exception ex) { MessageBox.Show("Erreur Assets: " + ex.Message); }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            GererVaguesEtSpawns();
            GererStamina();

            double currentSpeed = (isRunning && canRun) ? runSpeed : walkSpeed;

            // Déplacement Joueur
            double x = Canvas.GetLeft(Player);
            double y = Canvas.GetTop(Player);
            bool isMoving = false;

            if (goUp) { y -= currentSpeed; isMoving = true; }
            if (goDown) { y += currentSpeed; isMoving = true; }
            if (goLeft) { x -= currentSpeed; isMoving = true; PlayerScale.ScaleX = -1; }
            if (goRight) { x += currentSpeed; isMoving = true; PlayerScale.ScaleX = 1; }

            Canvas.SetLeft(Player, x);
            Canvas.SetTop(Player, y);

            UpdateEnemies(x, y);

            if (estInvulnerable)
            {
                tempsInvulnerabilite--;
                if (tempsInvulnerabilite <= 0) { estInvulnerable = false; Player.Opacity = 1; }
            }

            double screenCenterX = this.ActualWidth / 2;
            double screenCenterY = this.ActualHeight / 2;
            Camera.X = screenCenterX - x - (Player.Width / 2);
            Camera.Y = screenCenterY - y - (Player.Height / 2);

            MiseAJourMap(x, y);
            GererAnimationJoueur(isMoving);
        }

        // --- GESTION ENDURANCE ---
        private void GererStamina()
        {
            if (isRunning && (goUp || goDown || goLeft || goRight))
            {
                stamina -= 0.5;
                if (stamina <= 0)
                {
                    stamina = 0;
                    canRun = false;
                    isRunning = false;
                }
            }
            else
            {
                if (stamina < 100)
                {
                    stamina += 0.3;
                    if (stamina >= 20) canRun = true;
                }
            }
            BarreStamina.Value = stamina;
            BarreStamina.Foreground = canRun ? Brushes.Orange : Brushes.Gray;
        }

        // --- LOGIQUE DES VAGUES ---
        private void GererVaguesEtSpawns()
        {
            if (isWaveFinishedMessage)
            {
                messageTimer -= 0.016;
                if (messageTimer <= 0)
                {
                    isWaveFinishedMessage = false;
                    GridVagueFinie.Visibility = Visibility.Collapsed;
                    SetupWave(currentWave + 1);
                }
                return;
            }

            if (isCountingDown)
            {
                waveCountdown -= 0.016;
                TexteChrono.Text = Math.Ceiling(waveCountdown).ToString();
                if (waveCountdown <= 0)
                {
                    isCountingDown = false;
                    isWaveActive = true;
                    GridCompteARebours.Visibility = Visibility.Collapsed;

                    if (isSurvivalMode) GridSurvie.Visibility = Visibility.Visible;
                }
                return;
            }

            if (isWaveActive)
            {
                if (!isSurvivalMode)
                {
                    int maxSimultanes = 1; // Un seul ennemi à la fois pour l'instant

                    if (enemiesSpawnedInWave < enemiesToKillTotal && GetActiveEnemiesCount() < maxSimultanes)
                    {
                        SpawnEnemy();
                    }

                    if (enemiesKilledInWave >= enemiesToKillTotal)
                    {
                        isWaveActive = false;
                        TexteFinVague.Text = "VAGUE " + currentWave + " TERMINÉE";
                        GridVagueFinie.Visibility = Visibility.Visible;
                        isWaveFinishedMessage = true;
                        messageTimer = 5.0;
                    }
                }
                else
                {
                    // MODE SURVIE
                    spawnTimer -= 0.016;
                    if (spawnTimer <= 0)
                    {
                        if (enemies.Count < 20) SpawnEnemy();
                        spawnTimer = 2.0;
                    }
                }
            }
        }

        private int GetActiveEnemiesCount()
        {
            int count = 0;
            foreach (var e in enemies) if (!e.IsDead && !e.IsDying) count++;
            return count;
        }

        // --- CRÉATION D'ENNEMI ---
        private void SpawnEnemy()
        {
            enemiesSpawnedInWave++;

            Ennemi newEnemy = new Ennemi();
            newEnemy.Sprite = new Image();
            newEnemy.Sprite.Width = 130;
            newEnemy.Sprite.Height = 130;
            newEnemy.Sprite.Source = mummySprites[0];
            newEnemy.Sprite.RenderTransformOrigin = new Point(0.5, 0.5);
            newEnemy.Scale = new ScaleTransform();
            newEnemy.Sprite.RenderTransform = newEnemy.Scale;
            Panel.SetZIndex(newEnemy.Sprite, 9998);

            newEnemy.HealthBar = new ProgressBar();
            newEnemy.HealthBar.Width = 80;
            newEnemy.HealthBar.Height = 10;
            newEnemy.HealthBar.Foreground = Brushes.Red;
            newEnemy.HealthBar.Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            newEnemy.HealthBar.BorderBrush = Brushes.Black;
            newEnemy.HealthBar.BorderThickness = new Thickness(1);
            Panel.SetZIndex(newEnemy.HealthBar, 10000);

            if (isSurvivalMode) newEnemy.HP = 4;
            else newEnemy.HP = 3 + ((currentWave - 1) * 2);

            newEnemy.HealthBar.Maximum = newEnemy.HP;
            newEnemy.HealthBar.Value = newEnemy.HP;

            Random rnd = new Random();
            double angle = rnd.NextDouble() * Math.PI * 2;
            double dist = 800;

            double spawnX = Canvas.GetLeft(Player) + Math.Cos(angle) * dist;
            double spawnY = Canvas.GetTop(Player) + Math.Sin(angle) * dist;

            Canvas.SetLeft(newEnemy.Sprite, spawnX);
            Canvas.SetTop(newEnemy.Sprite, spawnY);
            Canvas.SetLeft(newEnemy.HealthBar, spawnX + 25);
            Canvas.SetTop(newEnemy.HealthBar, spawnY - 15);

            MondeDeJeu.Children.Add(newEnemy.Sprite);
            MondeDeJeu.Children.Add(newEnemy.HealthBar);

            enemies.Add(newEnemy);
        }

        // --- GESTION ENNEMIS ---
        private void UpdateEnemies(double playerX, double playerY)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var ennemi = enemies[i];

                if (ennemi.IsDead && !ennemi.IsDying)
                {
                    MondeDeJeu.Children.Remove(ennemi.Sprite);
                    MondeDeJeu.Children.Remove(ennemi.HealthBar);
                    enemies.RemoveAt(i);
                    enemiesKilledInWave++;
                    continue;
                }

                if (ennemi.IsDying)
                {
                    AnimateDeath(ennemi);
                    continue;
                }

                double eX = Canvas.GetLeft(ennemi.Sprite);
                double eY = Canvas.GetTop(ennemi.Sprite);

                double diffX = playerX - eX;
                double diffY = playerY - eY;
                double dist = Math.Sqrt(diffX * diffX + diffY * diffY);

                if (diffX > 0) ennemi.Scale.ScaleX = -1; else ennemi.Scale.ScaleX = 1;

                if (dist > 10)
                {
                    double speed = 6;
                    eX += (diffX / dist) * speed;
                    eY += (diffY / dist) * speed;

                    ennemi.FrameCounter++;
                    if (ennemi.FrameCounter > 5)
                    {
                        ennemi.FrameIndex++;
                        if (ennemi.FrameIndex >= mummySprites.Count) ennemi.FrameIndex = 0;
                        ennemi.Sprite.Source = mummySprites[ennemi.FrameIndex];
                        ennemi.FrameCounter = 0;
                    }
                }
                else
                {
                    ennemi.FrameCounter++;
                    if (ennemi.FrameCounter > 3)
                    {
                        ennemi.FrameIndex++;
                        if (ennemi.FrameIndex >= mummyAttackSprites.Count) ennemi.FrameIndex = 0;
                        if (mummyAttackSprites.Count > 0) ennemi.Sprite.Source = mummyAttackSprites[ennemi.FrameIndex];
                        ennemi.FrameCounter = 0;
                    }
                }

                Canvas.SetLeft(ennemi.Sprite, eX);
                Canvas.SetTop(ennemi.Sprite, eY);
                Canvas.SetLeft(ennemi.HealthBar, eX + 25);
                Canvas.SetTop(ennemi.HealthBar, eY - 15);

                Rect rPlayer = new Rect(playerX + 40, playerY + 40, Player.Width - 80, Player.Height - 80);
                Rect rEnemy = new Rect(eX + 40, eY + 40, ennemi.Sprite.Width - 80, ennemi.Sprite.Height - 80);

                if (rPlayer.IntersectsWith(rEnemy))
                {
                    PrendreDegats(10);
                }
            }
        }

        private void AnimateDeath(Ennemi e)
        {
            e.HealthBar.Visibility = Visibility.Collapsed;
            e.FrameCounter++;
            if (e.FrameCounter > 8)
            {
                e.FrameIndex++;
                if (e.FrameIndex >= mummyDeathSprites.Count)
                {
                    e.IsDying = false;
                }
                else
                {
                    e.Sprite.Source = mummyDeathSprites[e.FrameIndex];
                }
                e.FrameCounter = 0;
            }
        }

        // --- INPUTS ---
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isAttacking && attackSprites.Count > 0)
            {
                isAttacking = true;
                currentFrame = 0;
                frameCounter = 0;
                Player.Source = attackSprites[0];

                double pX = Canvas.GetLeft(Player) + 65;
                double pY = Canvas.GetTop(Player) + 65;

                foreach (var ennemi in enemies)
                {
                    if (ennemi.IsDead || ennemi.IsDying) continue;

                    double eX = Canvas.GetLeft(ennemi.Sprite) + 65;
                    double eY = Canvas.GetTop(ennemi.Sprite) + 65;
                    double dist = Math.Sqrt(Math.Pow(pX - eX, 2) + Math.Pow(pY - eY, 2));

                    if (dist < 120)
                    {
                        ennemi.HP--;
                        ennemi.HealthBar.Value = ennemi.HP;
                        ennemi.Sprite.Opacity = 0.5;

                        DispatcherTimer t = new DispatcherTimer();
                        t.Interval = TimeSpan.FromMilliseconds(100);
                        Ennemi target = ennemi;
                        t.Tick += (s, args) => { if (!target.IsDying) target.Sprite.Opacity = 1; t.Stop(); };
                        t.Start();

                        if (ennemi.HP <= 0)
                        {
                            ennemi.IsDead = true;
                            ennemi.IsDying = true;
                            ennemi.FrameIndex = 0;
                            if (mummyDeathSprites.Count > 0) ennemi.Sprite.Source = mummyDeathSprites[0];
                        }
                    }
                }
            }
        }

        private void MiseAJourMap(double playerX, double playerY)
        {
            int gridX = (int)Math.Floor(playerX / tailleTuile);
            int gridY = (int)Math.Floor(playerY / tailleTuile);
            HashSet<string> tuilesGardees = new HashSet<string>();
            for (int i = -distanceVue; i <= distanceVue; i++)
            {
                for (int j = -distanceVue; j <= distanceVue; j++)
                {
                    int tuileX = gridX + i; int tuileY = gridY + j;
                    string key = $"{tuileX}_{tuileY}"; tuilesGardees.Add(key);
                    if (!tuilesActives.ContainsKey(key)) CreerTuile(tuileX, tuileY, key);
                }
            }
            List<string> aSupprimer = new List<string>();
            foreach (var kvp in tuilesActives) if (!tuilesGardees.Contains(kvp.Key)) { MondeDeJeu.Children.Remove(kvp.Value); aSupprimer.Add(kvp.Key); }
            foreach (string key in aSupprimer) tuilesActives.Remove(key);
        }

        private void CreerTuile(int gridX, int gridY, string key)
        {
            Image tuile = new Image();
            int seed = gridX * 73856093 ^ gridY * 19349663;
            Random rnd = new Random(seed);

            // --- ON CHOISIT AU HASARD PARMI LES 4 FONDS ---
            int index = rnd.Next(0, solTextures.Count);
            tuile.Source = solTextures[index];

            tuile.Width = tailleTuile + 2; tuile.Height = tailleTuile + 2; tuile.Stretch = Stretch.Fill;
            Canvas.SetLeft(tuile, (gridX * tailleTuile) - 1); Canvas.SetTop(tuile, (gridY * tailleTuile) - 1);
            Panel.SetZIndex(tuile, 0);
            MondeDeJeu.Children.Add(tuile); tuilesActives.Add(key, tuile);
        }

        private void GererAnimationJoueur(bool isMoving)
        {
            if (isAttacking && attackSprites.Count > 0)
            {
                frameCounter++;
                if (frameCounter > frameDelay)
                {
                    currentFrame++;
                    if (currentFrame >= attackSprites.Count) { isAttacking = false; currentFrame = 0; if (walkSprites.Count > 0) Player.Source = walkSprites[0]; }
                    else Player.Source = attackSprites[currentFrame];
                    frameCounter = 0;
                }
            }
            else if (isMoving)
            {
                List<BitmapImage> animList = isRunning ? runSprites : walkSprites;
                frameDelay = isRunning ? 3 : 5;
                frameCounter++;
                if (frameCounter > frameDelay) { currentFrame++; if (currentFrame >= animList.Count) currentFrame = 0; Player.Source = animList[currentFrame]; frameCounter = 0; }
            }
        }
        private void PrendreDegats(int degats)
        {
            if (estInvulnerable) return;
            vieJoueur -= degats;
            if (vieJoueur < 0) vieJoueur = 0;
            BarreDeVie.Value = vieJoueur;
            Player.Opacity = 0.5;
            estInvulnerable = true;
            tempsInvulnerabilite = 120;
            if (vieJoueur <= 0) { gameTimer.Stop(); GridGameOver.Visibility = Visibility.Visible; }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z || e.Key == Key.Up) goUp = true;
            if (e.Key == Key.S || e.Key == Key.Down) goDown = true;
            if (e.Key == Key.Q || e.Key == Key.Left) goLeft = true;
            if (e.Key == Key.D || e.Key == Key.Right) goRight = true;
            if (e.Key == Key.LeftShift) isRunning = true;
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z || e.Key == Key.Up) goUp = false;
            if (e.Key == Key.S || e.Key == Key.Down) goDown = false;
            if (e.Key == Key.Q || e.Key == Key.Left) goLeft = false;
            if (e.Key == Key.D || e.Key == Key.Right) goRight = false;
            if (e.Key == Key.LeftShift) isRunning = false;
        }
        private void Button_Menu_Click(object sender, RoutedEventArgs e) { gameTimer.Stop(); MainWindow m = new MainWindow(); m.Show(); this.Close(); }
        private void Button_Reessayer_Click(object sender, RoutedEventArgs e) { Jeu j = new Jeu(); j.Show(); this.Close(); }
    }
}