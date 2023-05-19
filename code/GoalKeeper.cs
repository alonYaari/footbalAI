using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using runningMan2;
using Sprites;

namespace runningMan2
{
    public delegate void InformGkState();
    //public delegate int InformGkState(int val);

    public class GoalKeeper : Player
    {
        public GoalKeeper(Texture2D texture) : base(texture)
        {

        }


        public GoalKeeper(Dictionary<string, Animation> animations, int team, Vector2 startingPos, string animationName)
            : base(animations, startingPos, animationName)
        {
            this.team = team;
        }

        //-----------------------------------------------------------------------------
        // Update
        // ------
        //
        // General : Updates the FieldPlayer's position and behavior based on the game state.
        //
        // Parameters :
        // gt - The current game time.
        // sprites - The list of sprites in the game.
        //
        //-----------------------------------------------------------------------------
        public override void Update(GameTime gt, List<Sprite> sprites)
        {
            Ball ball = (Ball)(sprites[8]);
            cooldown = cooldown == 0 ? 0 : cooldown - 0.01f;

            if (isOffense(sprites))
            {
                // If the goalkeeper's team is in offense, return to the middle of the goal (default position)
                position = Vector2.Lerp(position, defaultPos + new Vector2(0, -12), 0.02f);
            }
            else
            {
                // Move and try to close the angle of the ball to the goal
                moveForBall(ball);
            }

            if (isHitting(ball))
            {
                clearBall(ball);
            }

            if (Vector2.Distance(position, ball.position) < 50 && Vector2.Distance(position, defaultPos) < 40)
            {
                position = Vector2.Lerp(position, defaultPos, 0.05f);
            }

            position += Velocity;
            Velocity = Vector2.Zero;

            base.Update(gt, sprites);
        }

        //-----------------------------------------------------------------------------
        // moveForBall
        // -----------
        //
        // General : Moves the goalkeeper towards the ball while considering position constraints.
        //
        // Parameters :
        // ball - The ball object.
        //
        //-----------------------------------------------------------------------------
        public void moveForBall(Ball ball)
        {
            float maxDistanceFromOriginal = 40f;
            float maxDistanceFromBall = 300f;
            float distanceY = ball.position.Y - this.position.Y - 30;

            if (Math.Abs(distanceY) >= 0 && distanceY < maxDistanceFromBall && distanceY > -maxDistanceFromBall)
            {
                if (Math.Abs(position.Y - defaultPos.Y) < maxDistanceFromOriginal)
                {
                    // Move up or down based on the position of the ball
                    this.Velocity.Y = distanceY > 0 ? 0.16f : -0.16f;
                }
                else
                {
                    // Stay in the current position
                    position = Vector2.Lerp(position, defaultPos + new Vector2(0, -10), 0.02f);
                }
            }
        }

        //-----------------------------------------------------------------------------
        // isOffense
        // ---------
        //
        // General : Checks if the goalkeeper's team is in the offense state.
        //
        // Parameters :
        // sprites - The list of sprites to compare against.
        //
        // Returns :
        // bool - True if the FieldPlayer is in the offense state, false otherwise.
        //
        //-----------------------------------------------------------------------------
        public bool isOffense(List<Sprite> sprites)
        {
            foreach (var sprite in sprites.Where(sprite => sprite is FieldPlayer))
            {
                FieldPlayer currPlayer = (FieldPlayer)sprite;
                if (currPlayer.team == team && currPlayer.state == stateAction.Offense)
                {
                    return true;
                }
            }
            return false;
        }

        //-----------------------------------------------------------------------------
        // clearBall
        // ---------
        //
        // General : Clears the ball by modifying its velocity.
        //
        // Parameters :
        // ball - The ball object.
        //
        //-----------------------------------------------------------------------------
        public void clearBall(Ball ball)
        {
            if (cooldown == 0f)
            {
                cooldown = 1f;
                float velocityFactor = ball.Velocity.X > 4f ? 2.2f : 2.7f;
                ball.Velocity = -velocityFactor * ball.Velocity;
            }
        }

    }
}
