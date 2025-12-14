using System;
using System.Collections.Generic;
using System.Linq;
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
        public ProgressBar HealthBar { get; set; }
        public ScaleTransform Scale { get; set; }
        public int HP { get; set; }
        public bool IsDead { get; set; }
        public bool IsDying { get; set; }
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
        private double walkSpeed = 4;
        private double runSpeed = 7;
        private double stamina = 100;
        private bool canRun = true;

        // --- BOUCLIER ---
        private bool isBlocking = false;
        private double shieldEnergy = 100;
        private bool canBlock = true;

        // --- KITS DE SOIN ---
        private List<Image> healthKits = new List<Image>();
        private int survivalKillsCounter = 0;

        // --- LISTE DES ENNEMIS ---
        private List<Ennemi> enemies = new List<Ennemi>();

        // --- RESSOURCES ---
        private List<BitmapImage> mummySprites = new List<BitmapImage>();
        private List<BitmapImage> mummyAttackSprites = new List<BitmapImage>();
        private List<BitmapImage> mummyDeathSprites = new List<BitmapImage>();

        private List<BitmapImage> walkSprites = new List<BitmapImage>();
        private List<BitmapImage> attackSprites = new List<BitmapImage>();
        private List<BitmapImage> runSprites = new List<BitmapImage>();

        private List<BitmapImage> playerDeathSprites = new List<BitmapImage>();

        private BitmapImage blockSprite;
        private BitmapImage kitSprite;
        private BitmapImage fondTexture;

        // Stats Joueur
        private int vieJoueur = 100;
        private bool estInvulnerable = false;
        private int tempsInvulnerabilite = 0;
        private bool isAttacking = false;
        private bool isPlayerDying = false;
        private int currentFrame = 0;
        private int frameCounter = 0;
        private int frameDelay = 5;

        // --- MAP & COLLISIONS ---
        private List<Rect> zonesEauLocales = new List<Rect>(); // Zones d'eau sur UNE tuile

        // Configuration de la Map 3x3
        // Une tuile fait 1024. On en met 3 en largeur, 3 en hauteur.
        // De -1024 à +2048
        private double mapMinX = -1024;
        private double mapMaxX = 2048; // 1024 * 2
        private double mapMinY = -1024;
        private double mapMaxY = 2048;

        // --- VAGUES ---
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

            // Map étendue (3x3 images)
            InitExtendedMap();
            // Définition de l'eau sur le motif de base
            InitZonesEau();

            BarreDeVie.Value = vieJoueur;
            BarreStamina.Value = stamina;
            BarreBouclier.Value = shieldEnergy;

            this.MouseRightButtonDown += Window_MouseRightButtonDown;
            this.MouseRightButtonUp += Window_MouseRightButtonUp;

            SetupWave(1);

            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.Focus();
        }

        private void InitExtendedMap()
        {
            if (fondTexture == null) return;

            // On crée une grille de 3x3 images pour agrandir la map
            // x va de -1 à 1, y va de -1 à 1
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Image fond = new Image();
                    fond.Source = fondTexture;
                    fond.Width = 1024;
                    fond.Height = 1024;
                    fond.Stretch = Stretch.Fill;

                    // Positionnement des tuiles
                    Canvas.SetLeft(fond, x * 1024);
                    Canvas.SetTop(fond, y * 1024);
                    Panel.SetZIndex(fond, 0);
                    MondeDeJeu.Children.Add(fond);
                }
            }
        }

        private void InitZonesEau()
        {
            // On définit où est l'eau sur L'IMAGE DE BASE (0 à 1024)
            // Le code s'occupera d'appliquer ça à toutes les copies de la map

            // Le grand lac en bas à droite (selon ta photo)
            zonesEauLocales.Add(new Rect(550, 600, 350, 250));

            // La rivière en haut à gauche (selon ta photo)
            zonesEauLocales.Add(new Rect(0, 0, 350, 400));
        }

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

        private void GameLoop(object sender, EventArgs e)
        {
            if (isPlayerDying) { GererAnimationMortJoueur(); return; }

            GererVaguesEtSpawns();
            GererStamina();
            GererBouclier();
            GererRamassageKits();

            double baseSpeed = (isRunning && canRun) ? runSpeed : walkSpeed;

            double currentX = Canvas.GetLeft(Player);
            double currentY = Canvas.GetTop(Player);

            // Ralentissement EAU : Seulement le joueur
            if (EstSurEau(currentX, currentY)) baseSpeed = baseSpeed / 2.5; // Ralentissement fort

            double nextX = currentX;
            double nextY = currentY;
            bool isMoving = false;

            if (goUp) { nextY -= baseSpeed; isMoving = true; }
            if (goDown) { nextY += baseSpeed; isMoving = true; }
            if (goLeft) { nextX -= baseSpeed; isMoving = true; PlayerScale.ScaleX = -1; }
            if (goRight) { nextX += baseSpeed; isMoving = true; PlayerScale.ScaleX = 1; }

            // --- LIMITES DE LA MAP ---
            // On bloque le joueur entre les limites de la map 3x3
            // mapMinX = -1024, mapMaxX = 2048 (car 2*1024)
            if (nextX < mapMinX) nextX = mapMinX;
            if (nextX > mapMaxX - 60) nextX = mapMaxX - 60; // -60 pour la largeur du joueur
            if (nextY < mapMinY) nextY = mapMinY;
            if (nextY > mapMaxY - 60) nextY = mapMaxY - 60;

            Canvas.SetLeft(Player, nextX);
            Canvas.SetTop(Player, nextY);

            UpdateEnemies(nextX, nextY);

            if (estInvulnerable)
            {
                tempsInvulnerabilite--;
                if (tempsInvulnerabilite <= 0) { estInvulnerable = false; Player.Opacity = 1; }
            }

            // Caméra
            double zoom = 2.5;
            double screenCenterX = this.ActualWidth / 2;
            double screenCenterY = this.ActualHeight / 2;
            Camera.X = screenCenterX - ((nextX + Player.Width / 2) * zoom);
            Camera.Y = screenCenterY - ((nextY + Player.Height / 2) * zoom);

            GererAnimationJoueur(isMoving);
        }

        // --- SYSTEME INTELLIGENT EAU ---
        // Vérifie si le joueur est sur l'eau, peu importe sur quelle copie de la map il est
        private bool EstSurEau(double worldX, double worldY)
        {
            // On ramène la coordonnée Monde (ex: 1500) en coordonnée Locale (0 à 1024)
            // L'opérateur % en C# peut renvoyer du négatif, on corrige ça.
            double localX = worldX % 1024;
            double localY = worldY % 1024;
            if (localX < 0) localX += 1024;
            if (localY < 0) localY += 1024;

            // Hitbox pieds
            Rect pieds = new Rect(localX + 20, localY + 50, 20, 10);

            foreach (var zone in zonesEauLocales)
            {
                if (pieds.IntersectsWith(zone)) return true;
            }
            return false;
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
            // --- REGLE : PAS DE DOUBLE KIT ---
            if (healthKits.Count > 0) return;
            if (kitSprite == null) return;

            Random rnd = new Random();
            // Spawn n'importe où dans la grande map 3x3
            double kX = rnd.Next((int)mapMinX + 100, (int)mapMaxX - 100);
            double kY = rnd.Next((int)mapMinY + 100, (int)mapMaxY - 100);

            // On vérifie juste qu'on ne spawn pas dans l'eau pour être sympa
            if (!EstSurEau(kX, kY))
            {
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

                    if (isSurvivalMode)
                    {
                        survivalKillsCounter++;
                        if (survivalKillsCounter >= 5)
                        {
                            SpawnHealthKit();
                            survivalKillsCounter = 0;
                        }
                    }
                    continue;
                }

                if (ennemi.IsDying) { AnimateDeath(ennemi); continue; }

                double eX = Canvas.GetLeft(ennemi.Sprite);
                double eY = Canvas.GetTop(ennemi.Sprite);
                double diffX = playerX - eX;
                double diffY = playerY - eY;
                double dist = Math.Sqrt(diffX * diffX + diffY * diffY);

                if (diffX > 0) ennemi.Scale.ScaleX = -1; else ennemi.Scale.ScaleX = 1;

                if (dist > 10)
                {
                    // Vitesse momie constante (elles ne sont PAS ralenties par l'eau)
                    double speed = 4.5;

                    double nextEX = eX + (diffX / dist) * speed;
                    double nextEY = eY + (diffY / dist) * speed;
                    eX = nextEX;
                    eY = nextEY;

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
                Canvas.SetLeft(ennemi.HealthBar, eX + 10);
                Canvas.SetTop(ennemi.HealthBar, eY - 10);

                Rect rPlayer = new Rect(playerX + 15, playerY + 15, 30, 30);
                Rect rEnemy = new Rect(eX + 15, eY + 15, 30, 30);

                if (rPlayer.IntersectsWith(rEnemy))
                {
                    PrendreDegats(10);
                }
            }
        }

        private void PrendreDegats(int degats)
        {
            if (isPlayerDying) return;
            if (estInvulnerable) return;

            if (isBlocking)
            {
                shieldEnergy -= 5;
                estInvulnerable = true;
                tempsInvulnerabilite = 30;
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
                currentFrame = 0;
                frameCounter = 0;
                if (playerDeathSprites.Count > 0) Player.Source = playerDeathSprites[0];
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isPlayerDying) return;

            if (!isAttacking && attackSprites.Count > 0)
            {
                isAttacking = true;
                currentFrame = 0;
                frameCounter = 0;
                Player.Source = attackSprites[0];

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

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (canBlock && !isPlayerDying) isBlocking = true;
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            isBlocking = false;
        }

        private void AnimateDeath(Ennemi e)
        {
            e.HealthBar.Visibility = Visibility.Collapsed; e.FrameCounter++;
            if (e.FrameCounter > 8) { e.FrameIndex++; if (e.FrameIndex >= mummyDeathSprites.Count) e.IsDying = false; else e.Sprite.Source = mummyDeathSprites[e.FrameIndex]; e.FrameCounter = 0; }
        }

        private void GererStamina()
        {
            if (isRunning && (goUp || goDown || goLeft || goRight)) { stamina -= 0.5; if (stamina <= 0) { stamina = 0; canRun = false; isRunning = false; } }
            else { if (stamina < 100) { stamina += 0.3; if (stamina >= 20) canRun = true; } }
            BarreStamina.Value = stamina; BarreStamina.Foreground = canRun ? Brushes.Orange : Brushes.Gray;
        }
        private void GererVaguesEtSpawns()
        {
            if (isWaveFinishedMessage) { messageTimer -= 0.016; if (messageTimer <= 0) { isWaveFinishedMessage = false; GridVagueFinie.Visibility = Visibility.Collapsed; SetupWave(currentWave + 1); } return; }
            if (isCountingDown) { waveCountdown -= 0.016; TexteChrono.Text = Math.Ceiling(waveCountdown).ToString(); if (waveCountdown <= 0) { isCountingDown = false; isWaveActive = true; GridCompteARebours.Visibility = Visibility.Collapsed; if (isSurvivalMode) GridSurvie.Visibility = Visibility.Visible; } return; }
            if (isWaveActive)
            {
                if (!isSurvivalMode)
                {
                    int max = 1; if (enemiesSpawnedInWave < enemiesToKillTotal && GetActiveEnemiesCount() < max) SpawnEnemy();
                    if (enemiesKilledInWave >= enemiesToKillTotal) { isWaveActive = false; TexteFinVague.Text = "VAGUE " + currentWave + " TERMINÉE"; GridVagueFinie.Visibility = Visibility.Visible; isWaveFinishedMessage = true; messageTimer = 5.0; }
                }
                else { spawnTimer -= 0.016; if (spawnTimer <= 0) { if (enemies.Count < 20) SpawnEnemy(); spawnTimer = 2.0; } }
            }
        }
        private int GetActiveEnemiesCount() { int c = 0; foreach (var e in enemies) if (!e.IsDead && !e.IsDying) c++; return c; }
        private void SpawnEnemy()
        {
            enemiesSpawnedInWave++; Ennemi n = new Ennemi(); n.Sprite = new Image();
            n.Sprite.Width = 60; n.Sprite.Height = 60;
            if (mummySprites.Count > 0) n.Sprite.Source = mummySprites[0]; else return;
            n.Sprite.RenderTransformOrigin = new Point(0.5, 0.5); n.Scale = new ScaleTransform(); n.Sprite.RenderTransform = n.Scale; Panel.SetZIndex(n.Sprite, 9998);
            n.HealthBar = new ProgressBar(); n.HealthBar.Width = 40; n.HealthBar.Height = 5; n.HealthBar.Foreground = Brushes.Red; n.HealthBar.Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)); n.HealthBar.BorderBrush = Brushes.Black; n.HealthBar.BorderThickness = new Thickness(1); Panel.SetZIndex(n.HealthBar, 10000);

            // --- AJUSTEMENT DIFFICULTÉ ---
            if (isSurvivalMode) n.HP = 6; // 6 Coups (plus dur)
            else n.HP = 3 + ((currentWave - 1) * 2);

            n.HealthBar.Maximum = n.HP; n.HealthBar.Value = n.HP;
            Random r = new Random();
            // Spawn n'importe où sur la map 3x3
            double sX = r.Next((int)mapMinX + 100, (int)mapMaxX - 100);
            double sY = r.Next((int)mapMinY + 100, (int)mapMaxY - 100);

            Canvas.SetLeft(n.Sprite, sX); Canvas.SetTop(n.Sprite, sY); Canvas.SetLeft(n.HealthBar, sX + 10); Canvas.SetTop(n.HealthBar, sY - 10);
            MondeDeJeu.Children.Add(n.Sprite); MondeDeJeu.Children.Add(n.HealthBar); enemies.Add(n);
        }

        private void GererAnimationJoueur(bool m)
        {
            if (isPlayerDying) return;
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

        private void Window_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Z || e.Key == Key.Up) goUp = true; if (e.Key == Key.S || e.Key == Key.Down) goDown = true; if (e.Key == Key.Q || e.Key == Key.Left) goLeft = true; if (e.Key == Key.D || e.Key == Key.Right) goRight = true; if (e.Key == Key.LeftShift) isRunning = true; }
        private void Window_KeyUp(object s, KeyEventArgs e) { if (e.Key == Key.Z || e.Key == Key.Up) goUp = false; if (e.Key == Key.S || e.Key == Key.Down) goDown = false; if (e.Key == Key.Q || e.Key == Key.Left) goLeft = false; if (e.Key == Key.D || e.Key == Key.Right) goRight = false; if (e.Key == Key.LeftShift) isRunning = false; }
        private void Button_Menu_Click(object s, RoutedEventArgs e) { gameTimer.Stop(); MainWindow m = new MainWindow(); m.Show(); this.Close(); }
        private void Button_Reessayer_Click(object s, RoutedEventArgs e) { Jeu j = new Jeu(); j.Show(); this.Close(); }
    }
}