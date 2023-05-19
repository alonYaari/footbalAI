using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Sprites;

namespace runningMan2
{
    public class Logic
    {
        // changed y field to 46 instead of 35
        public static Rectangle field = new Rectangle(105, 46, 1378, 813);
        public static Vector2 centerOfPitch = new Vector2(787, 436);
        public static Vector2 playerAStartingPoint = new Vector2(600, 432);
        public static Vector2 playerBStartingPoint = new Vector2(980, 432);
        //                                            was 1477
        public static Rectangle rightGoal = new Rectangle(1474, 355, 88, 165);
        public static Vector2 rightGoalCenter = new Vector2(rightGoal.Center.X, rightGoal.Center.Y);
        public static Rectangle leftGoal = new Rectangle(21, 335, 88, 165);
        public static Vector2 leftGoalCenter = new Vector2(leftGoal.Center.X, leftGoal.Center.Y);
        // changed right goalie to 1462 from 1470
        // changed left goalie to 120 from 115
        public static Vector2 rightGoaliePos = new Vector2(1462, 440);
        public static Vector2 leftGoaliePos = new Vector2(120, 440);

        public static Rectangle rightBox = new Rectangle(1260, 213, 210, 451);
        public static Rectangle leftBox = new Rectangle(102, 213, 210, 451);


        public static bool isInBounds(Sprite sp)
        {
            return field.Contains(sp.hitbox);
        }
        public static bool isInAttackingBox(FieldPlayer fp)
        {
            // determine attaking box by player's goal center
            Rectangle attackingBox = fp.goalCenter.X < centerOfPitch.X ? leftBox : rightBox;
            return attackingBox.Contains(fp.position);
        }
        public static int isInGoal(Ball b)
        {
            Rectangle ballRect = new Rectangle((int)b.position.X, (int)b.position.Y, 2, 2);
            return rightGoal.Intersects(ballRect) ? 1 : leftGoal.Intersects(ballRect) ? 2 : 0;
        }
        public static void afterGoalPositions(List<Sprite> sprites)
        {
            foreach (Sprite sp in sprites)
            {
                sp.position = sp.defaultPos;
                sp.Velocity = Vector2.Zero;
                if (sp is Ball)
                    ((Ball)sp).returnToCenter = true;
                if (sp is FieldPlayer)
                {
                    ((FieldPlayer)sp).chaseCooldown = 0;
                }
            }
        }

        public static bool isInBounds(Vector2 position)
        {
            // check right side
            int buffX = 0;
            int buffY = 0;
            if (position.X > centerOfPitch.X)
            {
                buffX = 6;
            }
            else { buffX = -6; }
            if (position.Y > centerOfPitch.Y)
            {
                buffY = 14;
            }
            else
            {
                buffY = 32;
            }
            Vector2 buff = new Vector2(buffX, buffY);
            position += buff;
            return field.Contains(position);
        }
        public static bool checkOutOfBounds(Ball ball)
        {
            if (!field.Contains(ball.position) && !rightGoal.Contains(ball.position) && !leftGoal.Contains(ball.position))
            {
                ball.Velocity = Vector2.Zero;
                if (ball.position.X >= rightGoal.X)
                {
                    ball.position = rightGoaliePos + new Vector2(-10, -6);
                    return true;

                }
            }
            return false;

        }
    }
}
