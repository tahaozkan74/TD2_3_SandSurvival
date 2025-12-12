using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SandSurvival
{
    public partial class Jeu : Window
    {
        // --- MOTEUR ---
        private DispatcherTimer gameTimer = new DispatcherTimer();
        private bool goUp, goDown, goLeft, goRight;
        
        // VITESSE
        private bool isRunning = false;
        private double walkSpeed = 10;
        private double runSpeed = 18;

        // --- ANIMATIONS ---
        private List<BitmapImage> walkSprites = new List<BitmapImage>();
        private List<BitmapImage> attackSprites = new List<BitmapImage>();
        private List<BitmapImage> runSprites = new List<BitmapImage>();
        
        private bool isAttacking = false;
        private int currentFrame = 0;
        private int frameCounter = 0;
        private int frameDelay = 5;

        // --- MAP INFINIE (SYSTÈME DE CHUNK) ---
        private List<BitmapImage> solTextures = new List<BitmapImage>();
        
        // Dictionnaire pour se souvenir des tuiles déjà posées (Clé = "X_Y")
        private Dictionary<string, Image> tuilesActives = new Dictionary<string, Image>();
        
        private int tailleTuile = 1024; // Taille géante
        private int distanceVue = 2;    // Nombre de tuiles à afficher autour du joueur (2 = écran rempli)

        public Jeu()
        {
            InitializeComponent();

            LoadAssets();
            
            // On lance la boucle
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            
            this.Focus();
        }

        private void LoadAssets()
        {
            try
            {
                // 1. Textures Sol
                for (int i = 1; i <= 9; i++)
                {
                    string path = $"pack://application:,,,/Images/image de fond/imageFond{i}.png";
                    solTextures.Add(new BitmapImage(new Uri(path)));
                }

                // 2. Perso Marche
                for (int i = 0; i < 8; i++)
                {
                    walkSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/walk{i}.png")));
                }
                // 3. Perso Attaque
                for (int i = 1; i <= 3; i++)
                {
                    attackSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/attaque{i}.png")));
                }
                // 4. Perso Course
                for (int i = 1; i <= 3; i++)
                {
                    runSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/course{i}.png")));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message);
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            double currentSpeed = isRunning ? runSpeed : walkSpeed;

            // --- 1. DÉPLACEMENT (INFINI) ---
            double x = Canvas.GetLeft(Player);
            double y = Canvas.GetTop(Player);
            bool isMoving = false;

            // Note : On a enlevé les limites (MondeDeJeu.Width) car le monde est infini
            if (goUp) { y -= currentSpeed; isMoving = true; }
            if (goDown) { y += currentSpeed; isMoving = true; }
            if (goLeft) { x -= currentSpeed; isMoving = true; PlayerScale.ScaleX = -1; }
            if (goRight) { x += currentSpeed; isMoving = true; PlayerScale.ScaleX = 1; }

            Canvas.SetLeft(Player, x);
            Canvas.SetTop(Player, y);

            // --- 2. CAMÉRA ---
            double screenCenterX = this.ActualWidth / 2;
            double screenCenterY = this.ActualHeight / 2;
            Camera.X = screenCenterX - x - (Player.Width / 2);
            Camera.Y = screenCenterY - y - (Player.Height / 2);

            // --- 3. GÉNÉRATION DE LA MAP INFINIE ---
            MiseAJourMap(x, y);

            // --- 4. ANIMATION ---
            GererAnimation(isMoving);
        }

        // --- C'EST ICI QUE LA MAGIE OPÈRE ---
        private void MiseAJourMap(double playerX, double playerY)
        {
            // On calcule sur quelle case de la grille se trouve le joueur
            // Math.Floor est important pour gérer les coordonnées négatives correctement
            int gridX = (int)Math.Floor(playerX / tailleTuile);
            int gridY = (int)Math.Floor(playerY / tailleTuile);

            // Liste des clés qu'on veut garder (pour supprimer les vieilles après)
            HashSet<string> tuilesGardees = new HashSet<string>();

            // On génère les tuiles autour du joueur (Carré de 3x3 ou 5x5 selon distanceVue)
            for (int i = -distanceVue; i <= distanceVue; i++)
            {
                for (int j = -distanceVue; j <= distanceVue; j++)
                {
                    int tuileX = gridX + i;
                    int tuileY = gridY + j;
                    string key = $"{tuileX}_{tuileY}";

                    tuilesGardees.Add(key);

                    // Si la tuile n'existe pas encore, on la crée
                    if (!tuilesActives.ContainsKey(key))
                    {
                        CreerTuile(tuileX, tuileY, key);
                    }
                }
            }

            // OPTIMISATION : Supprimer les tuiles trop loin (libérer mémoire)
            // On crée une liste temporaire car on ne peut pas modifier un dico pendant qu'on le lit
            List<string> aSupprimer = new List<string>();
            foreach (var kvp in tuilesActives)
            {
                if (!tuilesGardees.Contains(kvp.Key))
                {
                    MondeDeJeu.Children.Remove(kvp.Value); // Enlever visuellement
                    aSupprimer.Add(kvp.Key);
                }
            }
            // Suppression effective du dictionnaire
            foreach (string key in aSupprimer)
            {
                tuilesActives.Remove(key);
            }
        }

        private void CreerTuile(int gridX, int gridY, string key)
        {
            Image tuile = new Image();

            // GÉNÉRATION PROCÉDURALE STABLE
            // On utilise les coordonnées comme "graine" (Seed) pour le hasard.
            // Comme ça, la tuile (5, 10) sera TOUJOURS la même image, même si on part et qu'on revient.
            // C'est ça qui rend la map cohérente.
            int seed = gridX * 73856093 ^ gridY * 19349663; // Formule de hachage simple
            Random rndTuile = new Random(seed);
            
            int index = rndTuile.Next(0, solTextures.Count);
            tuile.Source = solTextures[index];

            // ASTUCE ANTI-LIGNE BLANCHE (+2 pixels et -1 offset)
            tuile.Width = tailleTuile + 2;
            tuile.Height = tailleTuile + 2;
            tuile.Stretch = Stretch.Fill;

            Canvas.SetLeft(tuile, (gridX * tailleTuile) - 1);
            Canvas.SetTop(tuile, (gridY * tailleTuile) - 1);
            
            Panel.SetZIndex(tuile, 0); // Au fond

            MondeDeJeu.Children.Add(tuile);
            tuilesActives.Add(key, tuile);
        }

        private void GererAnimation(bool isMoving)
        {
            // Priorité : Attaque -> Course -> Marche -> Idle
            if (isAttacking && attackSprites.Count > 0)
            {
                JouerAnimation(attackSprites, true);
            }
            else if (isMoving && isRunning && runSprites.Count > 0)
            {
                frameDelay = 3; // Course rapide
                JouerAnimation(runSprites, false);
            }
            else if (isMoving && walkSprites.Count > 0)
            {
                frameDelay = 5; // Marche normale
                JouerAnimation(walkSprites, false);
            }
            else if (walkSprites.Count > 0)
            {
                Player.Source = walkSprites[0];
            }
        }

        private void JouerAnimation(List<BitmapImage> sprites, bool isAttackAnim)
        {
            frameCounter++;
            if (frameCounter > frameDelay)
            {
                currentFrame++;
                if (currentFrame >= sprites.Count)
                {
                    if (isAttackAnim) 
                    {
                        isAttacking = false; // Fin de l'attaque
                        currentFrame = 0;
                        if (walkSprites.Count > 0) Player.Source = walkSprites[0];
                        return;
                    }
                    currentFrame = 0; // Boucle
                }
                Player.Source = sprites[currentFrame];
                frameCounter = 0;
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
            }
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

        private void Button_Menu_Click(object sender, RoutedEventArgs e)
        {
            gameTimer.Stop();
            MainWindow menu = new MainWindow();
            menu.Show();
            this.Close();
        }
    }
}