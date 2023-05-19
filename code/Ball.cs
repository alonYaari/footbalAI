using animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using runningMan2;
using Sprites;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


public class Ball : Sprite
{
    // addition:
    public float stuck = 0f;
    // end of addition
    public bool isOut = false;
    public bool returnToCenter = true;

    public Player owner = null;
    public Ball(Texture2D texture, Vector2 position)
        : base(texture)
    {
        this.position = position;
        this.Velocity = Vector2.Zero;
    }
    public Ball(Texture2D texture, Vector2 position, Vector2 velocity)
        : base(texture)
    {
        this.position = position;
        this.Velocity = velocity;
    }
    public Ball(Dictionary<string, Animation> animations, Vector2 startingPos) : base(animations, startingPos) { }

    //-----------------------------------------------------------------------------
    // Update
    // ------
    //
    // General : Updates the ball's position and behavior based on the game state.
    //
    // Parameters :
    // gameTime - The current game time.
    //
    //-----------------------------------------------------------------------------
    public void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Vector2 newPos = position += Velocity;

        // If the new position is out of bounds but not in a goal, reverse the velocity
        if (newPos != Vector2.Zero && !Logic.isInBounds(newPos) && Logic.isInGoal(this) == 0)
        {
            Velocity *= -0.8f; // was -1
        }

        if (Logic.isInBounds(this.position) || Logic.isInGoal(this) != 0)
        {
            position += Velocity;
            isOut = false;
        }
        else
        {
            isOut = true;
        }

        Vector2 prevPos = Vector2.Zero;

        if (owner != null)
        {
            Vector2 ballPosAdjast = determineBallPosAccordingToPlayer((FieldPlayer)owner);
            prevPos = position;
            position = Vector2.Lerp(position, ballPosAdjast, 0.23f);
        }

        Velocity -= Velocity / 35;

        Globals.ball = this;

        // Addition:
        if (position == prevPos)
        {
            stuck += 0.01f;
        }

        if (stuck == 1f)
        {
            stuck = 0f;
            Velocity = Vector2.One * 4;
            position += Velocity * 4;
        }
        // End of addition
    }

    //-----------------------------------------------------------------------------
    // determineBallPosAccordingToPlayer
    // ---------------------------------
    //
    // General : Determines the ball's position relative to the owning player.
    //
    // Parameters :
    // p - The owning FieldPlayer.
    //
    // Returns :
    // Vector2 - The adjusted position of the ball.
    //
    //-----------------------------------------------------------------------------
    public Vector2 determineBallPosAccordingToPlayer(FieldPlayer p)
    {
        Vector2 res = Vector2.Zero;
        bool direction = false;

        if (p.prevVelocity.X < 0)
        {
            direction = true;
            res = new Vector2(-16, 29);
        }

        if (p.prevVelocity.X > 0)
        {
            direction = true;
            res = new Vector2(16, 29);
        }

        if (p.prevVelocity.Y < 0)
        {
            if (!direction)
            {
                res += new Vector2(4, -6);
            }
            else
            {
                res += new Vector2(0, -6);
            }
        }

        if (p.prevVelocity.Y > 0)
        {
            if (!direction)
            {
                res += new Vector2(-4, 30);
            }
            else
            {
                res += new Vector2(0, 9);
            }
        }

        if (!Logic.isInBounds(position))
        {
            res = determineBallPosAccordingToBounds();
        }

        return res == Vector2.Zero ? Globals.ball.position : p.position + res;
    }

    //-----------------------------------------------------------------------------
    // determineBallPosAccordingToBounds
    // ---------------------------------
    //
    // General : Determines the ball's position based on its relation to the boundaries of the game field.
    //
    // Returns :
    // Vector2 - The adjusted position of the ball.
    //
    //-----------------------------------------------------------------------------
    public Vector2 determineBallPosAccordingToBounds()
    {
        Vector2 res = new Vector2(-23, 13);

        if (position.X >= Logic.rightGoal.X - 5)
        {
            res.X = -23;
        }

        if (position.Y > Logic.centerOfPitch.Y)
        {
            res.Y = 15;
        }

        return res;
    }

    public void reset()
    {
        Velocity = Vector2.Zero;
        position = Logic.centerOfPitch;
        owner = null;
    }
}
