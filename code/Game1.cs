using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

// important
using System.Collections.Generic;
using Sprites;
using Models;
using animation;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace runningMan2
{
    public enum stateAction {Neutral, Offense, Defense};
    public delegate void BallOwnerDelegate(Player owner);
    public class Game1 : Game
    {
        public BallOwnerDelegate informBallOwner;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont scoreFont;
        private SpriteFont finalScoreFont;
        private Texture2D backgroundTexture;
        private Texture2D finalScoreBackground;
        public static List<Sprite> sprites;
        public static List<FieldPlayer> fieldPlayers = new List<FieldPlayer>();
        public static int teamAScore = 0;
        public static int teamBScore = 0;
        int gameMinutesSinceStart;
        float elapsedTime = 0;
        int addedTime = 0;
        bool afterHalf = false;
        bool isInET = false;
        bool gameEnds = false;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            bool debug = false;
            int addition = debug ? 4000 : 0;

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load textures
            backgroundTexture = Content.Load<Texture2D>("soccerField");
            finalScoreBackground = Content.Load<Texture2D>("finalBackGround");

            // Load fonts
            scoreFont = Content.Load<SpriteFont>("Arial");
            finalScoreFont = Content.Load<SpriteFont>("finalScoreFont");

            #region animations
            var ballAnimation = new Dictionary<string, Animation>()
            {
                // Load ball animation with frame count of 1
                {"smol_ball1", new Animation(Content.Load<Texture2D>("ball/smol_ball"),1 ) },
            };

            // Load animations for blue team players
            var animations_blue = new Dictionary<string, Animation>()
            {
                {"WalkRight", new Animation(Content.Load<Texture2D>("player/WalkRight"),3 ) },
                {"WalkUp", new Animation(Content.Load<Texture2D>("player/WalkUp"),3 ) },
                {"WalkDown", new Animation(Content.Load<Texture2D>("player/WalkDown"),3 ) },
                {"WalkLeft", new Animation(Content.Load<Texture2D>("player/WalkLeft"),3 ) },
            };

            // Load animations for red team players
            var animations_red = new Dictionary<string, Animation>()
            {
                {"WalkRight", new Animation(Content.Load<Texture2D>("player/WalkRight_red"),3 ) },
                {"WalkUp", new Animation(Content.Load<Texture2D>("player/WalkUp_red"),3 ) },
                {"WalkDown", new Animation(Content.Load<Texture2D>("player/WalkDown_red"),3 ) },
                {"WalkLeft", new Animation(Content.Load<Texture2D>("player/WalkLeft_red"),3 ) },
            };
            #endregion

            sprites = new List<Sprite>()
            {
                // First team.
                new GoalKeeper(animations_red, 1,Logic.leftGoaliePos, "WalkRight")
                {position = Logic.leftGoaliePos, input = new Input(){up = Keys.W, down = Keys.S, left = Keys.A, right = Keys.D,} },
                new FieldPlayer(animations_red,10, 1, Logic.playerAStartingPoint + new Vector2(0,0), "WalkRight",Logic.rightGoalCenter)
                { position = Logic.playerAStartingPoint +new Vector2(0,0),input = new Input() { up = Keys.W, down = Keys.S, left = Keys.A, right = Keys.D, },},
                new FieldPlayer(animations_red,20, 1, Logic.playerAStartingPoint + new Vector2(50+addition,100), "WalkRight", Logic.rightGoalCenter)
                { position = Logic.playerAStartingPoint + new Vector2(50+addition,100), input = new Input() { up = Keys.W, down = Keys.S, left = Keys.A, right = Keys.D, },},
                new FieldPlayer(animations_red,30, 1, Logic.playerAStartingPoint - new Vector2(-50+addition,100), "WalkRight", Logic.rightGoalCenter)
                { position = Logic.playerAStartingPoint - new Vector2(-50+addition,100),input = new Input() { up = Keys.W, down = Keys.S, left = Keys.A, right = Keys.D, },},

                // Second Team
                new GoalKeeper(animations_blue, 2,Logic.rightGoaliePos, "WalkLeft")
                {position = Logic.rightGoaliePos, input = new Input(){up = Keys.W, down = Keys.S, left = Keys.A, right = Keys.D,} },
                new FieldPlayer(animations_blue,40, 2, Logic.playerBStartingPoint + new Vector2(0,0), "WalkLeft", Logic.leftGoalCenter)
                { position = Logic.playerBStartingPoint,input = new Input() { up = Keys.W, down = Keys.S, left = Keys.A, right = Keys.D, },},
                new FieldPlayer(animations_blue,50, 2, Logic.playerBStartingPoint + new Vector2(-50 + addition,100), "WalkLeft", Logic.leftGoalCenter)
                { position = Logic.playerBStartingPoint + new Vector2(-50 + addition, 100), input = new Input() { up = Keys.W, down = Keys.S, left = Keys.A, right = Keys.D, },},
                new FieldPlayer(animations_blue,60, 2, Logic.playerBStartingPoint - new Vector2(50 + addition, 100), "WalkLeft", Logic.leftGoalCenter)
                { position = Logic.playerBStartingPoint - new Vector2(50 + addition, 100),input = new Input() { up = Keys.W, down = Keys.S, left = Keys.A, right = Keys.D, },},
                new Ball(ballAnimation, Logic.centerOfPitch) {position=Logic.centerOfPitch} ,
            };
            // sprite[0-3] - team1
            // sprite[4-7] - team2
            // sprite[8] - BALL
            setRandomSpeed();
            Globals.ball = (Ball)sprites[8];

            foreach (var fieldPlayer in sprites.Where(fieldPlayer => fieldPlayer is FieldPlayer)){fieldPlayers.Add((FieldPlayer)fieldPlayer);}
        }
        protected override void Update(GameTime gameTime)
        {
            int timeForBreak = afterHalf ? 90 : 45;  // Set the time at which a break occurs based on the current game state

            checkIfGoal();  // Check if a goal has been scored
            determineState();  // Determine the  state of the players   

            foreach (var sprite in sprites)
            {
                if (sprite is FieldPlayer)
                {
                    ((FieldPlayer)sprite).Update(gameTime, sprites);  // Update the position and behavior of field players
                }
                else if (sprite is GoalKeeper)
                {
                    ((GoalKeeper)sprite).Update(gameTime, sprites);  // Update the position and behavior of goalkeepers
                }
                else
                {
                    ((Ball)sprite).Update(gameTime);  // Update the position and behavior of the ball
                }
            }

            // Check if it's time to add extra time
            if (addedTime == 0 && gameMinutesSinceStart == timeForBreak)
            {
                addedTime = teamAScore + teamBScore;  // Calculate the total score of both teams
                Random random = new Random();
                addedTime += random.Next(1, 3);  // Add a random amount of extra time (1 or 2 minutes)
            }

            // Check if we are currently in the added time period
            if (addedTime != 0 && gameMinutesSinceStart >= timeForBreak && gameMinutesSinceStart <= timeForBreak + addedTime)
            {
                isInET = true;  // Set the flag to indicate that we are in extra time
                CheckEndOfAddedHalfTime(addedTime);  // Check if the added time period has ended
            }

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (elapsedTime >= 2f) // Change this to the desired interval
            {
                gameMinutesSinceStart++;
                elapsedTime = 0f; // Reset the elapsed time counter
                // Do any other actions that should happen every minute here

            }
           
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw the background texture to fill the entire window
            _spriteBatch.Begin();
            _spriteBatch.Draw(backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);

            // Draw team A name and goals
            _spriteBatch.DrawString(scoreFont, "Team A :", new Vector2(650, 5), Color.White);
            _spriteBatch.DrawString(scoreFont, teamAScore.ToString(), new Vector2(715, 5), Color.White);

            // Draw team B name and goals
            _spriteBatch.DrawString(scoreFont, "Team B :", new Vector2(880, 5), Color.White);
            _spriteBatch.DrawString(scoreFont, teamBScore.ToString(), new Vector2(945, 5), Color.White);

            // Draw timer
            _spriteBatch.DrawString(scoreFont, gameMinutesSinceStart.ToString(), new Vector2(800, 5), Color.White);

            // Draw added time
            if (isInET)
            {
                _spriteBatch.DrawString(scoreFont, '+' + addedTime.ToString(), new Vector2(820, 5), Color.White);
                if (gameEnds)
                {
                    _spriteBatch.Draw(finalScoreBackground, GraphicsDevice.Viewport.Bounds, Color.White);
                    _spriteBatch.DrawString(finalScoreFont, teamAScore.ToString(), new Vector2(680, 397), Color.Black);
                    _spriteBatch.DrawString(finalScoreFont, teamBScore.ToString(), new Vector2(895, 397), Color.Black);
                }

            }

            if (!gameEnds)
            {
                foreach (var sprite in sprites)
                {
                    sprite.Draw(_spriteBatch);
                }
            }
            
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        //-----------------------------------------------------------------------------
        // determineState
        // --------------
        //
        // General : Determines the states of the players based on the ball ownership.
        //
        // Parameters : None.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public void determineState()
        {
            // Check if the ball has an owner
            if (Globals.ball.owner == null)
            {
                neutralize();  // No owner, neutralize all players' states
            }
            else
            {
                switch (Globals.ball.owner.team)
                {
                    case (1):
                        // Set offensive and defensive states for team 1 players
                        for (int playerNum = 1; playerNum < 4; playerNum++)
                        {
                            ((FieldPlayer)sprites[playerNum]).state = stateAction.Offense;
                            ((FieldPlayer)sprites[playerNum + 4]).state = stateAction.Defense;
                        }
                        break;
                    case (2):
                        // Set offensive and defensive states for team 2 players
                        for (int playerNum = 5; playerNum < 8; playerNum++)
                        {
                            ((FieldPlayer)sprites[playerNum]).state = stateAction.Offense;
                            ((FieldPlayer)sprites[playerNum - 4]).state = stateAction.Defense;
                        }
                        break;
                }
            }
        }

        //-----------------------------------------------------------------------------
        // neutralize
        // ----------
        //
        // General : Sets the state of all non-ball and non-goalkeeper players to neutral.
        //
        // Parameters : None.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public void neutralize()
        {
            // Set the state of all non-ball and non-goalkeeper players to neutral
            foreach (var sprite in sprites)
            {
                if (sprite is not Ball && sprite is not GoalKeeper)
                {
                    ((FieldPlayer)sprite).state = stateAction.Neutral;
                }
            }
        }

        //-----------------------------------------------------------------------------
        // checkIfGoal
        // -----------
        //
        // General : Checks if a goal has been scored and updates the score and ball position accordingly.
        //
        // Parameters : None.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public void checkIfGoal()
        {
            // Check if a goal has been scored
            int teamNum = Logic.isInGoal(Globals.ball);
            if (teamNum != 0 && Globals.ball.returnToCenter)
            {
                Globals.ball.returnToCenter = false;
                setNoSpeed();

                // Delay for 2 seconds before resetting the ball and updating the score
                var t = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    setRandomSpeed();
                    Globals.ball.reset();
                    Logic.afterGoalPositions(sprites);
                    ScoreBoard.addScoreByTeam(teamNum);
                });
            }
        }

        //-----------------------------------------------------------------------------
        // setNoSpeed
        // ----------
        //
        // General : Sets the speed of all sprites to 0.
        //
        // Parameters : None.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public static void setNoSpeed()
        {
            // Set the speed of all sprites to 0
            foreach (var sprite in sprites)
            {
                sprite.speed = 0f;
            }
        }

        //-----------------------------------------------------------------------------
        // setRandomSpeed
        // --------------
        //
        // General : Sets a random speed for all sprites (except the ball).
        //
        // Parameters : None.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public void setRandomSpeed()
        {
            Random random = new Random();
            double doub = 0.3;

            // Set a random speed for all sprites (except the ball)
            foreach (var sprite in sprites)
            {
                if (sprite is not Ball)
                {
                    sprite.speed = 0.5f;
                    sprite.speed = (float)(random.Next(3, 6) * doub);
                }
            }
        }

        //-----------------------------------------------------------------------------
        // CheckEndOfAddedHalfTime
        // -----------------------
        //
        // General : Checks if the added half-time has ended and performs necessary actions.
        //
        // Parameters :
        // addedTime - The duration of the added half-time in minutes.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public void CheckEndOfAddedHalfTime(int addedTime)
        {
            // Determine the regular break time based on whether it is after the first or second half
            int timeForBreak = afterHalf ? 90 : 45;

            // Check if the added half-time has ended
            if (gameMinutesSinceStart == timeForBreak + addedTime)
            {
                // Adjust the goal center positions for field players
                foreach (var fieldPlayer in fieldPlayers)
                {
                    if (fieldPlayer.goalCenter.X > 20)
                    {
                        fieldPlayer.goalCenter = Logic.leftGoalCenter;
                    }
                    else
                    {
                        fieldPlayer.goalCenter = Logic.rightGoalCenter;
                    }
                }

                if (!afterHalf)
                {
                    // Perform necessary actions after the first half
                    Logic.afterGoalPositions(sprites);
                    gameMinutesSinceStart = 46;  // Set the game minutes to 46 (if it's 90, then the game should end)
                    isInET = false;
                    addedTime = 0;
                    afterHalf = true;
                }
                else
                {
                    // Perform necessary actions after the second half
                    setNoSpeed();
                    gameEnds = true;
                }
            }
        }

    }
}