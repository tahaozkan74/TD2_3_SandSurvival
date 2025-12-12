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

        // VITESSE JOUEUR
        private bool isRunning = false;
        private double walkSpeed = 10;
        private double runSpeed = 18;

        // --- ENNEMI (MUMMY) ---
        private List<BitmapImage> mummySprites = new List<BitmapImage>();
        private double mummySpeed = 6; // Plus lent que le joueur pour qu'on puisse fuir
        private int mummyFrame = 0;
        private int mummyFrameCounter = 0;

        // --- ANIMATIONS JOUEUR ---
        private List<BitmapImage> walkSprites = new List<BitmapImage>();
        private List<BitmapImage> attackSprites = new List<BitmapImage>();
        private List<BitmapImage> runSprites = new List<BitmapImage>();

        private bool isAttacking = false;
        private int currentFrame = 0;
        private int frameCounter = 0;
        private int frameDelay = 5;

        // --- MAP INFINIE ---
        private List<BitmapImage> solTextures = new List<BitmapImage>();
        private Dictionary<string, Image> tuilesActives = new Dictionary<string, Image>();
        private int tailleTuile = 1024;
        private int distanceVue = 2;

        public Jeu()
        {
            InitializeComponent();
            LoadAssets();

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
                    solTextures.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/image de fond/imageFond{i}.png")));

                // 2. Perso (Marche, Attaque, Course)
                for (int i = 0; i < 8; i++)
                    walkSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/walk{i}.png")));
                for (int i = 1; i <= 3; i++)
                    attackSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/attaque{i}.png")));
                for (int i = 1; i <= 3; i++)
                    runSprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/Player/course{i}.png")));

                // 3. MUMMY (marche1 à marche6)
                // Attention : dossier 'mommy'
                for (int i = 1; i <= 6; i++)
                {
                    mummySprites.Add(new BitmapImage(new Uri($"pack://application:,,,/Images/mommy/marche{i}.png")));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message + "\nVérifie que le dossier 'Images/mommy' existe avec marche1.png à marche6.png");
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            double currentSpeed = isRunning ? runSpeed : walkSpeed;

            // --- 1. DÉPLACEMENT JOUEUR ---
            double x = Canvas.GetLeft(Player);
            double y = Canvas.GetTop(Player);
            bool isMoving = false;

            if (goUp) { y -= currentSpeed; isMoving = true; }
            if (goDown) { y += currentSpeed; isMoving = true; }
            if (goLeft) { x -= currentSpeed; isMoving = true; PlayerScale.ScaleX = -1; }
            if (goRight) { x += currentSpeed; isMoving = true; PlayerScale.ScaleX = 1; }

            Canvas.SetLeft(Player, x);
            Canvas.SetTop(Player, y);

            // --- 2. IA MUMMY (POURSUITE) ---
            DeplacerMummy(x, y);

            // --- 3. CAMÉRA ---
            double screenCenterX = this.ActualWidth / 2;
            double screenCenterY = this.ActualHeight / 2;
            Camera.X = screenCenterX - x - (Player.Width / 2);
            Camera.Y = screenCenterY - y - (Player.Height / 2);

            // --- 4. MAP & ANIMATION ---
            MiseAJourMap(x, y);
            GererAnimationJoueur(isMoving);
        }

        private void DeplacerMummy(double targetX, double targetY)
        {
            // Récupérer position actuelle
            double mX = Canvas.GetLeft(Mummy);
            double mY = Canvas.GetTop(Mummy);

            // Calcul distance
            double diffX = targetX - mX;
            double diffY = targetY - mY;
            double distance = Math.Sqrt(diffX * diffX + diffY * diffY);

            if (distance > 10)
            {
                // Mouvement
                double moveX = (diffX / distance) * mummySpeed;
                double moveY = (diffY / distance) * mummySpeed;

                mX += moveX;
                mY += moveY;

                Canvas.SetLeft(Mummy, mX);
                Canvas.SetTop(Mummy, mY);

                // --- CORRECTION DE L'ORIENTATION (SPÉCIAL MUMMY) ---
                // Ton image de base regarde vers la GAUCHE.

                if (diffX > 0)
                {
                    // Le joueur est à DROITE.
                    // Il faut donc inverser l'image (ScaleX = -1) pour qu'il regarde à droite.
                    MummyScale.ScaleX = -1;
                }
                else
                {
                    // Le joueur est à GAUCHE.
                    // On garde l'image normale (ScaleX = 1) car elle regarde déjà à gauche.
                    MummyScale.ScaleX = 1;
                }

                // Animation
                mummyFrameCounter++;
                if (mummyFrameCounter > 5)
                {
                    mummyFrame++;
                    if (mummyFrame >= mummySprites.Count) mummyFrame = 0;
                    Mummy.Source = mummySprites[mummyFrame];

                    // IMPORTANT : On applique bien la transformation
                    Mummy.RenderTransform = MummyScale;
                    mummyFrameCounter = 0;
                }
            }
        }
        
        

        // --- MAP INFINIE ---
        private void MiseAJourMap(double playerX, double playerY)
        {
            int gridX = (int)Math.Floor(playerX / tailleTuile);
            int gridY = (int)Math.Floor(playerY / tailleTuile);
            HashSet<string> tuilesGardees = new HashSet<string>();

            for (int i = -distanceVue; i <= distanceVue; i++)
            {
                for (int j = -distanceVue; j <= distanceVue; j++)
                {
                    int tuileX = gridX + i;
                    int tuileY = gridY + j;
                    string key = $"{tuileX}_{tuileY}";
                    tuilesGardees.Add(key);

                    if (!tuilesActives.ContainsKey(key)) CreerTuile(tuileX, tuileY, key);
                }
            }

            List<string> aSupprimer = new List<string>();
            foreach (var kvp in tuilesActives)
            {
                if (!tuilesGardees.Contains(kvp.Key))
                {
                    MondeDeJeu.Children.Remove(kvp.Value);
                    aSupprimer.Add(kvp.Key);
                }
            }
            foreach (string key in aSupprimer) tuilesActives.Remove(key);
        }

        private void CreerTuile(int gridX, int gridY, string key)
        {
            Image tuile = new Image();
            int seed = gridX * 73856093 ^ gridY * 19349663;
            Random rndTuile = new Random(seed);

            int index = rndTuile.Next(0, solTextures.Count);
            tuile.Source = solTextures[index];

            tuile.Width = tailleTuile + 2;
            tuile.Height = tailleTuile + 2;
            tuile.Stretch = Stretch.Fill;

            Canvas.SetLeft(tuile, (gridX * tailleTuile) - 1);
            Canvas.SetTop(tuile, (gridY * tailleTuile) - 1);
            Panel.SetZIndex(tuile, 0);

            MondeDeJeu.Children.Add(tuile);
            tuilesActives.Add(key, tuile);
        }

        // --- ANIMATIONS JOUEUR ---
        private void GererAnimationJoueur(bool isMoving)
        {
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
                        if (walkSprites.Count > 0) Player.Source = walkSprites[0];
                    }
                    else Player.Source = attackSprites[currentFrame];
                    frameCounter = 0;
                }
            }
            else if (isMoving)
            {
                List<BitmapImage> animList = isRunning ? runSprites : walkSprites;
                frameDelay = isRunning ? 3 : 5;

                frameCounter++;
                if (frameCounter > frameDelay)
                {
                    currentFrame++;
                    if (currentFrame >= animList.Count) currentFrame = 0;
                    Player.Source = animList[currentFrame];
                    frameCounter = 0;
                }
            }
            else
            {
                if (walkSprites.Count > 0) Player.Source = walkSprites[0];
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