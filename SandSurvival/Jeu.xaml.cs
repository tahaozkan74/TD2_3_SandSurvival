using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Shapes;

namespace SandSurvival
{
    public class Ennemi
    {
        public Image Sprite { get; set; }
        public ProgressBar HealthBar { get; set; }
        public ScaleTransform Scale { get; set; }
        public int HP { get; set; }
        public bool IsDead { get; set; }
        public bool IsDying { get; set; }
        public int FrameIndex { get; set; }
        public int FrameCounter { get; set; }

        // AJOUT ICI :
        public bool IsBoss { get; set; }
    }

    public partial class Jeu : Window
    {
        private DispatcherTimer gameTimer = new DispatcherTimer();
        private bool goUp, goDown, goLeft, goRight;
        private bool isPaused = false;
        private MediaPlayer musicPlayer = new MediaPlayer();

        private bool isRunning = false;
        private double walkSpeed = 4;
        private double runSpeed = 6;
        private double stamina = 100;
        private bool canRun = true;
        private bool isBlocking = false;
        private double shieldEnergy = 100;
        private bool canBlock = true;
        private bool bossSpawnedInWave = false; // Pour savoir si le boss est déjà là

        // --- Variables pour l'EASTER EGG ---
        private bool isTitanMode = false; // Mode Titan (Touche T)
        private bool hasUsedKamikaze = false; // Pour limiter à 1 utilisation
        private int totalKills = 0; // Compteur total de kills

        private List<Image> healthKits = new List<Image>();
        private int survivalKillsCounter = 0;
        private List<Ennemi> enemies = new List<Ennemi>();

        private List<BitmapImage> mummySprites = new List<BitmapImage>();
        private List<BitmapImage> mummyAttackSprites = new List<BitmapImage>();
        private List<BitmapImage> mummyDeathSprites = new List<BitmapImage>();
        private List<BitmapImage> walkSprites = new List<BitmapImage>();
        private List<BitmapImage> attackSprites = new List<BitmapImage>();
        private List<BitmapImage> runSprites = new List<BitmapImage>();
        private List<BitmapImage> playerDeathSprites = new List<BitmapImage>();

        private BitmapImage blockSprite;
        private BitmapImage kitSprite;
        private BitmapImage fondTexture; // Texture pour le sol infini

        private int vieJoueur = 100;
        private bool estInvulnerable = false;
        private int tempsInvulnerabilite = 0;
        private bool isAttacking = false;
        private bool isPlayerDying = false;
        private int currentFrame = 0;
        private int frameCounter = 0;
        private int frameDelay = 5;

        // --- MAP INFINIE (5x5 tuiles de 2048px) ---
        private double mapMinX = 0;
        private double mapMaxX = 2048 * 5; // = 10240 pixels
        private double mapMinY = 0;
        private double mapMaxY = 2048 * 5; // = 10240 pixels

        // Pas de collisions murs

        private int currentWave = 1;
        private int enemiesToKillTotal = 3;
        private int enemiesKilledInWave = 0;
        private int enemiesSpawnedInWave = 0;
        private bool isSurvivalMode = false;
        private double spawnTimer = 0;
        private bool isWaveActive = false;
        private bool isCountingDown = true;
        private bool isWaveFinishedMessage = false;
        private double waveCountdown = 10.0;
        private double messageTimer = 0;

        public Jeu()
        {
            InitializeComponent();
            LoadAssets();

            this.Loaded += (s, e) =>
            {
                InitExtendedMap(); // Crée la vraie map infinie
                LancerMusiqueJeu();

                // Cache le calque de debug s'il existe
                if (this.FindName("CalqueCollisions") is Canvas c) c.Visibility = Visibility.Collapsed;
            };

            BarreDeVie.Value = vieJoueur;
            BarreStamina.Value = stamina;
            BarreBouclier.Value = shieldEnergy;

            try
            {
                if (this.FindName("PauseVolumeSlider") is Slider v) v.Value = MainWindow.MusicVolume;
                if (this.FindName("SpeedSlider") is Slider s) s.Value = walkSpeed;
                if (this.FindName("DifficultySlider") is Slider d) d.Value = 3;
            }
            catch { }

            this.MouseRightButtonDown += Window_MouseRightButtonDown;
            this.MouseRightButtonUp += Window_MouseRightButtonUp;

            SetupWave(1);

            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            this.Focus();
        }

        // --- CHARGEMENT ASSETS (AVEC FOND.PNG) ---
        private void LoadAssets()
        {
            try
            {
                fondTexture = new BitmapImage(new Uri("pack://application:,,,/Images/image de fond/fond.png"));

                for (int i = 0; i < 8; i++) walkSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/walk{i}.png")));
                for (int i = 1; i <= 3; i++) attackSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/attaque{i}.png")));
                for (int i = 1; i <= 3; i++) runSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/course{i}.png")));
                for (int i = 1; i <= 3; i++) playerDeathSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/mort{i}.png")));

                blockSprite = new BitmapImage(new Uri("pack://application:,,,/Images/Player/block.png"));
                kitSprite = new BitmapImage(new Uri("pack://application:,,,/Images/Objets/soin.png"));

                for (int i = 1; i <= 6; i++) mummySprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/mommy/marche{i}.png")));
                for (int i = 1; i <= 3; i++) mummyAttackSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/mommy/attaque{i}.png")));
                for (int i = 1; i <= 5; i++) mummyDeathSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/mommy/meurt{i}.png")));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur Assets : " + ex.Message);
            }
        }

        // --- GENERATION MAP INFINIE ---
        private void InitExtendedMap()
        {
            if (fondTexture == null) return;

            double tailleTuile = 2048; // Taille de ton image fond.png

            // On crée une grille de 5x5 images
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    Image tuile = new Image();
                    tuile.Source = fondTexture;
                    tuile.Width = tailleTuile;
                    tuile.Height = tailleTuile;
                    tuile.Stretch = Stretch.Fill;

                    // Positionnement
                    Canvas.SetLeft(tuile, x * tailleTuile);
                    Canvas.SetTop(tuile, y * tailleTuile);

                    // On met ZIndex négatif pour que le joueur marche DESSUS
                    Panel.SetZIndex(tuile, -1000);

                    MondeDeJeu.Children.Add(tuile);
                }
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (isPaused) return;
            if (isPlayerDying) { GererAnimationMortJoueur(); return; }

            GererVaguesEtSpawns();
            GererStamina();
            // GererCooldownDash(); // Tu as dit de l'enlever, donc je le commente
            GererBouclier();
            GererRamassageKits();

            // --- GESTION TAILLE TITAN (CORRECTION BUG) ---
            // On définit la taille : 3 si Titan, 1 si Normal
            double scaleFactor = isTitanMode ? 3.0 : 1.0;

            // On applique la hauteur
            PlayerScale.ScaleY = scaleFactor;

            // On applique la largeur en gardant la direction actuelle (Gauche ou Droite)
            // Si ScaleX est positif (droite), on met scaleFactor. Si négatif (gauche), -scaleFactor.
            if (PlayerScale.ScaleX > 0) PlayerScale.ScaleX = scaleFactor;
            else PlayerScale.ScaleX = -scaleFactor;

            // --- DEPLACEMENTS ---
            double baseSpeed = (isRunning && canRun) ? runSpeed : walkSpeed;
            if (isTitanMode) baseSpeed = 10; // Vitesse rapide en Titan

            double currentX = Canvas.GetLeft(Player);
            double currentY = Canvas.GetTop(Player);

            double potentialX = currentX;
            double potentialY = currentY;
            bool isMoving = false;

            if (goUp) { potentialY -= baseSpeed; isMoving = true; }
            if (goDown) { potentialY += baseSpeed; isMoving = true; }

            if (goLeft)
            {
                potentialX -= baseSpeed;
                isMoving = true;
                PlayerScale.ScaleX = -scaleFactor; // On tourne à gauche en gardant la taille
            }
            if (goRight)
            {
                potentialX += baseSpeed;
                isMoving = true;
                PlayerScale.ScaleX = scaleFactor; // On tourne à droite en gardant la taille
            }

            // --- GESTION DES LIMITES (PAS DE COLLISIONS MURS) ---
            double nextX = currentX;
            double nextY = currentY;

            if (potentialX >= mapMinX && potentialX <= mapMaxX - 60) nextX = potentialX;
            if (potentialY >= mapMinY && potentialY <= mapMaxY - 60) nextY = potentialY;

            Canvas.SetLeft(Player, nextX);
            Canvas.SetTop(Player, nextY);

            UpdateEnemies(nextX, nextY);

            if (estInvulnerable)
            {
                tempsInvulnerabilite--;
                if (tempsInvulnerabilite <= 0)
                {
                    estInvulnerable = false;
                    Player.Opacity = 1;
                }
            }

            // Caméra centrée
            double zoom = 1.5;
            double screenCenterX = this.ActualWidth / 2;
            double screenCenterY = this.ActualHeight / 2;
            if (screenCenterX <= 1) screenCenterX = 600;
            if (screenCenterY <= 1) screenCenterY = 400;

            Camera.X = screenCenterX - ((nextX + Player.Width / 2) * zoom);
            Camera.Y = screenCenterY - ((nextY + Player.Height / 2) * zoom);

            GererAnimationJoueur(isMoving);
        }

        // --- METHODES UTILITAIRES ---

        private void LancerMusiqueJeu()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", "BLACK OPS 2 ZOMBIES OFFICIAL Theme Song.mp3");
                if (File.Exists(path))
                {
                    musicPlayer.Open(new Uri(path, UriKind.Absolute));
                    musicPlayer.MediaEnded += (s, e) => { musicPlayer.Position = TimeSpan.Zero; musicPlayer.Play(); };
                    musicPlayer.Volume = MainWindow.MusicVolume;
                    musicPlayer.Play();
                }
            }
            catch { }
        }

        private void JouerEffetSonore(string nomFichier)
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", nomFichier);
                if (File.Exists(path))
                {
                    MediaPlayer sfx = new MediaPlayer();
                    sfx.Open(new Uri(path, UriKind.Absolute));
                    sfx.Volume = MainWindow.MusicVolume;
                    sfx.Play();
                }
            }
            catch { }
        }

        private BitmapImage ChargerImage(string relativePath)
        {
            try
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                if (File.Exists(fullPath)) return new BitmapImage(new Uri(fullPath, UriKind.Absolute));
                else return null;
            }
            catch { return null; }
        }

        // --- LOGIQUE DE JEU ---

        private void SetupWave(int waveNumber)
        {
            currentWave = waveNumber;
            enemiesKilledInWave = 0;
            enemiesSpawnedInWave = 0;

            foreach (var ennemi in enemies)
            {
                MondeDeJeu.Children.Remove(ennemi.Sprite);
                MondeDeJeu.Children.Remove(ennemi.HealthBar);
            }
            enemies.Clear();

            if (currentWave <= 3)
            {
                isSurvivalMode = false;
                enemiesToKillTotal = 3;
                TexteAnnonceVague.Text = "LA VAGUE " + currentWave + " VA COMMENCER DANS";
                TexteChrono.Text = "5";
                waveCountdown = 5.0;
                GridSurvie.Visibility = Visibility.Collapsed;
            }
            else
            {
                isSurvivalMode = true;
                TexteAnnonceVague.Text = "PRÉPAREZ-VOUS À SURVIVRE...";
                TexteChrono.Text = "5";
                waveCountdown = 5.0;
            }

            SpawnHealthKit();
            GridCompteARebours.Visibility = Visibility.Visible;
            isCountingDown = true;
            isWaveActive = false;
        }

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
                    // VAGUE STANDARD

                    // Tant qu'on n'a pas tué assez de momies, on en fait spawn
                    if (enemiesKilledInWave < enemiesToKillTotal)
                    {
                        int max = 1;
                        if (enemiesSpawnedInWave < enemiesToKillTotal && GetActiveEnemiesCount() < max)
                            SpawnEnemy();
                    }
                    // Quand toutes les momies normales sont mortes...
                    else
                    {
                        // ... On fait apparaître le BOSS s'il n'est pas encore là
                        if (!bossSpawnedInWave)
                        {
                            SpawnBoss();
                        }
                        // Si le Boss est mort (il n'est plus dans la liste), la vague est finie
                        else if (GetActiveBossCount() == 0)
                        {
                            isWaveActive = false;
                            TexteFinVague.Text = "VAGUE " + currentWave + " TERMINÉE";
                            GridVagueFinie.Visibility = Visibility.Visible;
                            isWaveFinishedMessage = true;
                            messageTimer = 5.0;
                            bossSpawnedInWave = false; // Reset pour sécurité
                        }
                    }
                }
                else
                {
                    // MODE SURVIE
                    spawnTimer -= 0.016;
                    if (spawnTimer <= 0)
                    {
                        if (enemies.Count < 50) SpawnEnemy();
                        spawnTimer = 0.5;
                    }
                }
            }
        }

        // Petite fonction utilitaire pour vérifier si le boss est vivant
        private int GetActiveBossCount()
        {
            int c = 0;
            foreach (var e in enemies) { if (e.IsBoss && !e.IsDead) c++; }
            return c;
        }

        private int GetActiveEnemiesCount()
        {
            int c = 0;
            foreach (var e in enemies) { if (!e.IsDead && !e.IsDying) c++; }
            return c;
        }

        private void GererStamina()
        {
            if (isRunning && (goUp || goDown || goLeft || goRight))
            {
                stamina -= 0.5;
                if (stamina <= 0) { stamina = 0; canRun = false; isRunning = false; }
            }
            else
            {
                if (stamina < 100) { stamina += 0.3; if (stamina >= 20) canRun = true; }
            }
            BarreStamina.Value = stamina;
            BarreStamina.Foreground = canRun ? Brushes.Orange : Brushes.Gray;
        }

        private void GererAnimationMortJoueur()
        {
            frameCounter++;
            if (frameCounter > 15)
            {
                if (currentFrame < playerDeathSprites.Count)
                {
                    Player.Source = playerDeathSprites[currentFrame];
                    currentFrame++;
                }
                else
                {
                    gameTimer.Stop();
                    GridGameOver.Visibility = Visibility.Visible;
                }
                frameCounter = 0;
            }
        }

        private void GererBouclier()
        {
            if (isBlocking && canBlock)
            {
                shieldEnergy -= 0.5;
                if (shieldEnergy <= 0) { shieldEnergy = 0; canBlock = false; isBlocking = false; }
            }
            else
            {
                if (shieldEnergy < 100) { shieldEnergy += 0.2; if (shieldEnergy >= 20) canBlock = true; }
            }
            BarreBouclier.Value = shieldEnergy;
        }

        private void SpawnHealthKit()
        {
            if (healthKits.Count > 0) return;
            if (kitSprite == null) return;

            Random rnd = new Random();
            double pX = Canvas.GetLeft(Player);
            double pY = Canvas.GetTop(Player);

            // Spawn proche du joueur (rayon 500px)
            double angle = rnd.NextDouble() * Math.PI * 2;
            double distance = rnd.Next(200, 500);
            double kX = pX + Math.Cos(angle) * distance;
            double kY = pY + Math.Sin(angle) * distance;

            // Garde fou limites map
            if (kX < mapMinX) kX = mapMinX + 50;
            if (kX > mapMaxX) kX = mapMaxX - 50;
            if (kY < mapMinY) kY = mapMinY + 50;
            if (kY > mapMaxY) kY = mapMaxY - 50;

            Image kit = new Image();
            kit.Source = kitSprite;
            kit.Width = 30;
            kit.Height = 30;
            Canvas.SetLeft(kit, kX);
            Canvas.SetTop(kit, kY);
            Panel.SetZIndex(kit, 5000);
            MondeDeJeu.Children.Add(kit);
            healthKits.Add(kit);
        }

        private void GererRamassageKits()
        {
            double pX = Canvas.GetLeft(Player);
            double pY = Canvas.GetTop(Player);
            Rect rPlayer = new Rect(pX + 15, pY + 15, 30, 30);

            for (int i = healthKits.Count - 1; i >= 0; i--)
            {
                Image kit = healthKits[i];
                double kX = Canvas.GetLeft(kit);
                double kY = Canvas.GetTop(kit);
                Rect rKit = new Rect(kX, kY, 30, 30);

                if (rPlayer.IntersectsWith(rKit))
                {
                    vieJoueur += 30;
                    if (vieJoueur > 100) vieJoueur = 100;
                    BarreDeVie.Value = vieJoueur;
                    MondeDeJeu.Children.Remove(kit);
                    healthKits.RemoveAt(i);
                }
            }
        }

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

                    // SI LE BOSS MEURT
                    if (ennemi.IsBoss)
                    {
                        if (this.FindName("TexteBoss") is TextBlock tb) tb.Visibility = Visibility.Collapsed;
                        bossSpawnedInWave = false; // Reset pour la prochaine fois
                    }

                    // Score
                    totalKills++;
                    if (this.FindName("TexteScore") is TextBlock t) t.Text = "MOMIES TUÉES : " + totalKills;

                    if (isSurvivalMode)
                    {
                        survivalKillsCounter++;
                        if (survivalKillsCounter >= 5) { SpawnHealthKit(); survivalKillsCounter = 0; }
                    }
                    continue;
                }

                if (ennemi.IsDying) { AnimateDeath(ennemi); continue; }

                // --- MOUVEMENT ---
                double eX = Canvas.GetLeft(ennemi.Sprite);
                double eY = Canvas.GetTop(ennemi.Sprite);
                double diffX = playerX - eX;
                double diffY = playerY - eY;
                double dist = Math.Sqrt(diffX * diffX + diffY * diffY);

                if (diffX > 0) ennemi.Scale.ScaleX = -1; else ennemi.Scale.ScaleX = 1;

                if (dist > 10)
                {
                    // VITESSE : Le Boss court très vite (8.0), les autres normalement (5.5)
                    double speed = ennemi.IsBoss ? 8.0 : 5.5;

                    double nextEX = eX + (diffX / dist) * speed;
                    double nextEY = eY + (diffY / dist) * speed;
                    eX = nextEX; eY = nextEY;

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
                    // Attaque
                    ennemi.FrameCounter++;
                    if (ennemi.FrameCounter > 3)
                    {
                        ennemi.FrameIndex++;
                        if (ennemi.FrameIndex == 1) JouerEffetSonore("MomieAttaque.mp4");
                        if (ennemi.FrameIndex >= mummyAttackSprites.Count) ennemi.FrameIndex = 0;
                        if (mummyAttackSprites.Count > 0) ennemi.Sprite.Source = mummyAttackSprites[ennemi.FrameIndex];
                        ennemi.FrameCounter = 0;
                    }
                }

                Canvas.SetLeft(ennemi.Sprite, eX);
                Canvas.SetTop(ennemi.Sprite, eY);
                Canvas.SetLeft(ennemi.HealthBar, eX + 10);
                Canvas.SetTop(ennemi.HealthBar, eY - 10);

                // --- DEGATS ---
                Rect rPlayer = new Rect(playerX + 15, playerY + 15, 30, 30);
                Rect rEnemy = new Rect(eX + 15, eY + 15, 30, 30);

                if (rPlayer.IntersectsWith(rEnemy))
                {
                    // Le Boss inflige 34 dégâts (tue en 3 coups car 34*3 > 100)
                    // Les autres infligent 10
                    int damage = ennemi.IsBoss ? 34 : 10;
                    PrendreDegats(damage);
                }
            }
        }

        private void PrendreDegats(int degats)
        {
            if (isPlayerDying || estInvulnerable) return;

            if (isBlocking)
            {
                shieldEnergy -= 5;
                estInvulnerable = true;
                tempsInvulnerabilite = 30;
                JouerEffetSonore("Bloque.mp4");
                return;
            }

            vieJoueur -= degats;
            if (vieJoueur < 0) vieJoueur = 0;
            BarreDeVie.Value = vieJoueur;
            Player.Opacity = 0.5;
            estInvulnerable = true;
            tempsInvulnerabilite = 120;

            if (vieJoueur <= 0)
            {
                isPlayerDying = true;
                JouerEffetSonore("MortPerso.mp4");
                currentFrame = 0; frameCounter = 0;
                if (playerDeathSprites.Count > 0) Player.Source = playerDeathSprites[0];
            }
            else
            {
                JouerEffetSonore("Degat.mp4");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { TenterAttaque(); }
        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e) { CommencerBlocage(); }
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e) { ArreterBlocage(); }

        private void AnimateDeath(Ennemi e)
        {
            e.HealthBar.Visibility = Visibility.Collapsed;
            e.FrameCounter++;
            if (e.FrameCounter > 8)
            {
                e.FrameIndex++;
                if (e.FrameIndex >= mummyDeathSprites.Count) e.IsDying = false;
                else e.Sprite.Source = mummyDeathSprites[e.FrameIndex];
                e.FrameCounter = 0;
            }
        }

        private void SpawnEnemy()
        {
            enemiesSpawnedInWave++;
            Ennemi n = new Ennemi();
            n.Sprite = new Image { Width = 60, Height = 60 };

            if (mummySprites.Count > 0) n.Sprite.Source = mummySprites[0];
            else return;

            n.Sprite.RenderTransformOrigin = new Point(0.5, 0.5);
            n.Scale = new ScaleTransform();
            n.Sprite.RenderTransform = n.Scale;
            Panel.SetZIndex(n.Sprite, 9998);

            n.HealthBar = new ProgressBar { Width = 40, Height = 5, Foreground = Brushes.Red, Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
            Panel.SetZIndex(n.HealthBar, 10000);

            int baseDifficulty = 3;
            if (this.FindName("DifficultySlider") is Slider d) baseDifficulty = (int)d.Value;

            if (isSurvivalMode) n.HP = baseDifficulty + 3;
            else n.HP = baseDifficulty + ((currentWave - 1) * 2);

            n.HealthBar.Maximum = n.HP;
            n.HealthBar.Value = n.HP;

            // --- SPAWN CORRIGÉ : PROCHE DU JOUEUR ---
            Random r = new Random();
            double pX = Canvas.GetLeft(Player);
            double pY = Canvas.GetTop(Player);

            // On choisit une direction et une distance aléatoire autour du joueur
            // Entre 400 et 800 pixels (pour qu'ils ne pop pas SUR le joueur, mais pas trop loin)
            double angle = r.NextDouble() * Math.PI * 2;
            double distance = r.Next(400, 800);

            double sX = pX + Math.Cos(angle) * distance;
            double sY = pY + Math.Sin(angle) * distance;

            // On vérifie qu'on reste dans la map
            if (sX < mapMinX + 50) sX = mapMinX + 50;
            if (sX > mapMaxX - 50) sX = mapMaxX - 50;
            if (sY < mapMinY + 50) sY = mapMinY + 50;
            if (sY > mapMaxY - 50) sY = mapMaxY - 50;

            Canvas.SetLeft(n.Sprite, sX);
            Canvas.SetTop(n.Sprite, sY);
            Canvas.SetLeft(n.HealthBar, sX + 10);
            Canvas.SetTop(n.HealthBar, sY - 10);

            MondeDeJeu.Children.Add(n.Sprite);
            MondeDeJeu.Children.Add(n.HealthBar);
            enemies.Add(n);
        }

        private void GererAnimationJoueur(bool m)
        {
            if (isPlayerDying) return;
            if (walkSprites.Count == 0 || runSprites.Count == 0) return;

            if (isAttacking && attackSprites.Count > 0)
            {
                frameCounter++;
                if (frameCounter > frameDelay)
                {
                    currentFrame++;
                    if (currentFrame >= attackSprites.Count)
                    {
                        isAttacking = false;
                        currentFrame = 0;
                        if (isBlocking && blockSprite != null) Player.Source = blockSprite;
                        else if (walkSprites.Count > 0) Player.Source = walkSprites[0];
                    }
                    else
                    {
                        Player.Source = attackSprites[currentFrame];
                    }
                    frameCounter = 0;
                }
                return;
            }

            if (isBlocking)
            {
                if (blockSprite != null) Player.Source = blockSprite;
                return;
            }

            if (m)
            {
                List<BitmapImage> l = isRunning ? runSprites : walkSprites;
                frameDelay = isRunning ? 3 : 5;
                frameCounter++;
                if (frameCounter > frameDelay)
                {
                    currentFrame++;
                    if (currentFrame >= l.Count) currentFrame = 0;
                    Player.Source = l[currentFrame];
                    frameCounter = 0;
                }
            }
        }

        private void Window_KeyDown(object s, KeyEventArgs e)
        {
            if (isPaused) return;

            if (e.Key == MainWindow.InputUp) goUp = true;
            if (e.Key == MainWindow.InputDown) goDown = true;
            if (e.Key == MainWindow.InputLeft) goLeft = true;
            if (e.Key == MainWindow.InputRight) goRight = true;
            if (e.Key == MainWindow.InputSprint) isRunning = true;

            if (e.Key == MainWindow.InputAttack && MainWindow.InputAttack != Key.None) TenterAttaque();
            if (e.Key == MainWindow.InputBlock && MainWindow.InputBlock != Key.None) CommencerBlocage();

            // --- EASTER EGG TITAN (T) ---
            if (e.Key == Key.T)
            {
                isTitanMode = !isTitanMode;
                if (isTitanMode)
                {
                    PlayerScale.ScaleX = 3.0; // Grossir
                    PlayerScale.ScaleY = 3.0;
                    walkSpeed = 10;
                    runSpeed = 15;
                    vieJoueur = 1000;
                    BarreDeVie.Value = 1000;
                }
                else
                {
                    PlayerScale.ScaleX = 1.0; // Normal
                    PlayerScale.ScaleY = 1.0;
                    walkSpeed = 4;
                    runSpeed = 6;
                    vieJoueur = 100;
                    BarreDeVie.Value = vieJoueur;
                }
            }

            if (e.Key == Key.K && isSurvivalMode && !hasUsedKamikaze)
            {
                hasUsedKamikaze = true; // On verrouille, c'est fini pour cette partie !

                // On tue tous les ennemis présents
                foreach (var ennemi in enemies)
                {
                    ennemi.HP = 0;
                    ennemi.IsDead = true;
                    ennemi.IsDying = true;
                    ennemi.FrameIndex = 0;
                    if (mummyDeathSprites.Count > 0) ennemi.Sprite.Source = mummyDeathSprites[0];
                }

                // Le sacrifice : Le joueur tombe à 10 PV
                vieJoueur = 10;
                BarreDeVie.Value = vieJoueur;
                Player.Opacity = 0.5;
                JouerEffetSonore("Degat.mp4");
            }

        }

        private void Window_KeyUp(object s, KeyEventArgs e)
        {
            if (e.Key == MainWindow.InputUp) goUp = false;
            if (e.Key == MainWindow.InputDown) goDown = false;
            if (e.Key == MainWindow.InputLeft) goLeft = false;
            if (e.Key == MainWindow.InputRight) goRight = false;
            if (e.Key == MainWindow.InputSprint) isRunning = false;
            if (e.Key == MainWindow.InputBlock) ArreterBlocage();
        }

        private void Button_Menu_Click(object s, RoutedEventArgs e)
        {
            gameTimer.Stop();
            if (musicPlayer != null) musicPlayer.Stop();
            MainWindow m = new MainWindow();
            m.Show();
            this.Close();
        }

        private void Button_Reessayer_Click(object s, RoutedEventArgs e)
        {
            if (musicPlayer != null) musicPlayer.Stop();
            Jeu j = new Jeu();
            j.Show();
            this.Close();
        }

        private void Button_Pause_Click(object sender, RoutedEventArgs e)
        {
            isPaused = true;
            gameTimer.Stop();
            if (this.FindName("GridPause") is Grid g) g.Visibility = Visibility.Visible;
        }

        private void Button_Reprendre_Click(object sender, RoutedEventArgs e)
        {
            isPaused = false;
            if (this.FindName("GridPause") is Grid g) g.Visibility = Visibility.Collapsed;
            gameTimer.Start();
            this.Focus();
        }

        private void PauseVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (musicPlayer != null)
            {
                musicPlayer.Volume = e.NewValue;
                MainWindow.MusicVolume = e.NewValue;
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            walkSpeed = e.NewValue;
            runSpeed = walkSpeed * 1.6;
        }

        private void CommencerBlocage()
        {
            if (canBlock && !isPlayerDying && !isPaused) isBlocking = true;
        }

        private void ArreterBlocage()
        {
            isBlocking = false;
        }

        private void TenterAttaque()
        {
            if (isPlayerDying || isPaused) return;

            if (!isAttacking && attackSprites.Count > 0)
            {
                isAttacking = true;
                currentFrame = 0;
                frameCounter = 0;
                Player.Source = attackSprites[0];
                JouerEffetSonore("AttaqueEffect.mp4");

                double pX = Canvas.GetLeft(Player) + 30;
                double pY = Canvas.GetTop(Player) + 30;

                foreach (var ennemi in enemies)
                {
                    if (ennemi.IsDead || ennemi.IsDying) continue;

                    double eX = Canvas.GetLeft(ennemi.Sprite) + 30;
                    double eY = Canvas.GetTop(ennemi.Sprite) + 30;
                    double dist = Math.Sqrt(Math.Pow(pX - eX, 2) + Math.Pow(pY - eY, 2));

                    if (dist < 80)
                    {
                        // MODE TITAN
                        if (isTitanMode) ennemi.HP -= 100;
                        else ennemi.HP--;

                        ennemi.HealthBar.Value = ennemi.HP;
                        ennemi.Sprite.Opacity = 0.5;

                        DispatcherTimer t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                        Ennemi target = ennemi;
                        t.Tick += (s, args) => { if (!target.IsDying) target.Sprite.Opacity = 1; t.Stop(); };
                        t.Start();

                        if (ennemi.HP <= 0)
                        {
                            JouerEffetSonore("MomieDeath.mp4");
                            ennemi.IsDead = true;
                            ennemi.IsDying = true;
                            ennemi.FrameIndex = 0;
                            if (mummyDeathSprites.Count > 0) ennemi.Sprite.Source = mummyDeathSprites[0];
                        }

                        // IMPORTANT : On arrête la boucle ici pour ne frapper qu'UN SEUL ennemi
                        break;
                    }
                }
            }
        }

        private void SpawnBoss()
        {
            bossSpawnedInWave = true;

            // Affiche le texte d'alerte
            if (this.FindName("TexteBoss") is TextBlock tb) tb.Visibility = Visibility.Visible;
            JouerEffetSonore("MomieAttaque.mp4"); // Son d'apparition (ou un autre si tu as)

            Ennemi boss = new Ennemi();
            boss.IsBoss = true; // C'est le CHEF

            boss.Sprite = new Image { Width = 90, Height = 90 }; // Il est plus gros (90px au lieu de 60)
            if (mummySprites.Count > 0) boss.Sprite.Source = mummySprites[0];

            boss.Sprite.RenderTransformOrigin = new Point(0.5, 0.5);
            boss.Scale = new ScaleTransform();
            boss.Sprite.RenderTransform = boss.Scale;
            Panel.SetZIndex(boss.Sprite, 9999); // Au dessus des autres

            // Barre de vie du boss (plus grande)
            boss.HealthBar = new ProgressBar { Width = 80, Height = 10, Foreground = Brushes.DarkRed, Background = Brushes.Black, BorderBrush = Brushes.White, BorderThickness = new Thickness(1) };
            Panel.SetZIndex(boss.HealthBar, 10000);

            // STATS DU BOSS
            boss.HP = 10; // Résistant (10 coups min)
            boss.HealthBar.Maximum = boss.HP;
            boss.HealthBar.Value = boss.HP;

            // Spawn aléatoire mais loin du joueur pour laisser le temps de lire le texte
            Random r = new Random();
            double sX = r.Next((int)mapMinX + 100, (int)mapMaxX - 100);
            double sY = r.Next((int)mapMinY + 100, (int)mapMaxY - 100);

            Canvas.SetLeft(boss.Sprite, sX); Canvas.SetTop(boss.Sprite, sY);
            Canvas.SetLeft(boss.HealthBar, sX + 5); Canvas.SetTop(boss.HealthBar, sY - 20);

            MondeDeJeu.Children.Add(boss.Sprite);
            MondeDeJeu.Children.Add(boss.HealthBar);
            enemies.Add(boss);
        }



    }
}