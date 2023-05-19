using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using animation;
using Sprites;
namespace runningMan2
{
    public enum attackingDecision { Shoot, Pass, Dribble }
    public class FieldPlayer : Player
    {
        public bool canChase = true;
        public int id;
        public float chaseCooldown = 0;
        public Vector2 prevVelocity;
        public Vector2 goalCenter;
        public stateAction state;
        public attackingDecision lastAction = attackingDecision.Shoot;
        public FieldPlayer(Texture2D texture) : base(texture)
        {

        }
        public FieldPlayer(Dictionary<string, Animation> animations, int id, int team, Vector2 startingPos, string animationName, Vector2 goalCenter)
            : base(animations, startingPos, animationName)
        {
            this.id = id;
            this.team = team;
            this.goalCenter = goalCenter;
        }
        //-----------------------------------------------------------------------------
        // Update
        // ------
        //
        // General : Updates the field player's state, position, and behavior based on the game state.
        //
        // Parameters :
        // gt - The current GameTime.
        // sprites - The list of all sprites in the game.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public override void Update(GameTime gt, List<Sprite> sprites)
        {
            // Move the field player
            Move();

            // Update cooldowns
            cooldown -= 0.01f;
            if (cooldown < 0)
            {
                cooldown = 0;
            }

            chaseCooldown -= 0.01f;
            if (chaseCooldown < 0)
            {
                chaseCooldown = 0;
            }

            // Update animations if the field player is moving
            if (this.Velocity != Vector2.Zero)
            {
                SetAnimations();
                animationManager.Update(gt);
            }

            if (Velocity != Vector2.Zero)
            {
                prevVelocity = Velocity;
            }

            Vector2 prevPos = position;

            // Check if the position is out of bounds or stepping into out of bounds
            if (!Logic.isInBounds(position))
            {
                // Adjust the position to stay within bounds
                position += Vector2.Normalize(position - Logic.centerOfPitch) * -Math.Abs(speed);
            }
            else
            {
                position += Velocity;
                if (!Logic.isInBounds(position))
                {
                    position = prevPos;
                }
            }

            // Create a copy of the sprites list for evaluation purposes
            List<Sprite> evaluationSprites = Sprite.copySprites(sprites);

            if (((Ball)sprites[8]).owner == this)
            {
                // Field player has the ball
                speed = team == 1 ? Math.Abs(speed) : -1 * Math.Abs(speed);
                attackingDecision attackerDecision = getThisCopy(evaluationSprites, this.id).decideOffenseMove(evaluationSprites, 5);
                lastAction = attackerDecision;

                switch (attackerDecision)
                {
                    case (attackingDecision.Shoot):
                        shootTheBall(Globals.ball);
                        break;
                    case (attackingDecision.Pass):
                        realPassTheBall(determinePassingPlayer(sprites), Globals.ball);
                        break;
                    case (attackingDecision.Dribble):
                        dribble(sprites);
                        break;
                }
            }
            else
            {
                determineSpeedByState(state);
                switch (state)
                {
                    case (stateAction.Offense):
                        moveOffenseNoBall(sprites);
                        break;
                    case (stateAction.Defense):
                        assignDefenseMove(sprites);
                        break;
                    case (stateAction.Neutral):
                        assignNeutralMove(sprites);
                        break;
                }
            }

            if (canTakeBall((Ball)sprites[8]) && cooldown == 0)
            {
                // Field player can take the ball
                cooldown = 0.5f;
                this.hasBall = true;
                Globals.ball.owner = this;
                ((Ball)sprites[8]).owner = this;
            }

            base.Update(gt, sprites);
        }

        //-----------------------------------------------------------------------------
        // evaluate
        // --------
        //
        // General : Evaluates the success chance of a specific attacking decision for the field player.
        //
        // Parameters :
        // decision - The attacking decision to evaluate (Shoot, Pass, or Dribble).
        // spritesCopy - A copy of the list of all sprites in the game.
        // moves - The number of moves remaining for evaluation.
        // successChance - The current success chance of the attacking decision.
        //
        // Return Value : The evaluated success chance of the attacking decision.
        //
        //-----------------------------------------------------------------------------
        public double evaluate(attackingDecision decision, List<Sprite> spritesCopy, int moves, double successChance)
        {
            if (moves == 0)
            {
                return successChance;
            }
            else
            {
                switch (decision)
                {
                    case attackingDecision.Shoot:
                        // Calculate the odds for scoring a goal if the player shoots.
                        return (chanceOfScoring(spritesCopy)) * successChance;
                    case attackingDecision.Pass:
                        FieldPlayer receiver = determinePassingPlayer(spritesCopy); // Decide where to pass the ball and return the receiver.
                        if (receiver == null)
                        {
                            return 0;
                        }
                        else
                        {
                            // Reaching here means the player can pass the ball (no interruptions).
                            double passRisk = calculatePassRisk(receiver) + 10;
                            passTheBall(receiver, (Ball)spritesCopy[8]);
                            attackingDecision afterPassAction = receiver.decideOffenseMove(spritesCopy, moves - 1); // Calculate the best action for the receiver.
                            return receiver.evaluate(afterPassAction, spritesCopy, moves - 1, successChance * passRisk); // Make the best action.
                        }
                    case attackingDecision.Dribble:
                        // Simulate dribbling.
                        dribble(spritesCopy);
                        double dribbleRisk = calculateDribbleRisk(spritesCopy); // Calculate the risk in dribbling.
                        attackingDecision afterDribbleAction = decideOffenseMove(spritesCopy, moves - 1); // Determine the next "right" move.
                        return evaluate(afterDribbleAction, spritesCopy, moves - 1, successChance * dribbleRisk); // Make the next move.
                }
            }
            return 0; // Will never reach here.
        }

        //-----------------------------------------------------------------------------
        // decideOffenseMove
        // -----------------
        //
        // General : Determines the best attacking move for the field player based on evaluation.
        //
        // Parameters :
        // cpySprites - A copy of the list of all sprites in the game.
        // moves - The number of moves remaining for evaluation.
        //
        // Return Value : The decided attacking move (Shoot, Pass, or Dribble).
        //
        //-----------------------------------------------------------------------------
        public attackingDecision decideOffenseMove(List<Sprite> cpySprites, int moves)
        {
            List<Sprite> tmp = Sprite.copySprites(cpySprites);
            double shootOutcome = getThisCopy(cpySprites, this.id).evaluate(attackingDecision.Shoot, cpySprites, moves, 100);
            cpySprites = tmp;
            double passOutcome = getThisCopy(cpySprites, this.id).evaluate(attackingDecision.Pass, cpySprites, moves, 100);
            cpySprites = tmp;
            double dribbleOutcome = getThisCopy(cpySprites, this.id).evaluate(attackingDecision.Dribble, cpySprites, moves, 100);
            cpySprites = tmp;
            if (shootOutcome >= passOutcome)
            {
                if (shootOutcome <= dribbleOutcome)
                    return attackingDecision.Dribble;
                else
                    return attackingDecision.Shoot
        ;
            }
            else
            {
                if (passOutcome >= dribbleOutcome)
                    return attackingDecision.Pass;
                else
                    return attackingDecision.Dribble;
            }
        }

        #region offense
        #region shoot
        //-----------------------------------------------------------------------------
        // shootTheBall
        // ------------
        //
        // General : Shoots the ball towards the goal.
        //
        // Parameters :
        // ball - The ball to be shot.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public void shootTheBall(Ball ball)
        {
            // Lower limit of shot power - won't shoot if the ball still stays in range
            Random random1 = new Random();
            int shootPow = random1.Next(1, 4); // Needs to be random
            double RandomFactor = random1.NextDouble() + 1;
            this.hasBall = false;
            ball.owner = null;
            Random random = new Random();
            Vector2 Randomness = new Vector2((float)(random.NextDouble() * RandomFactor), (float)(random.NextDouble() * RandomFactor));
            Randomness.Y = random.Next() % 2 == 0 ? -Randomness.Y : Randomness.Y; // Decides to shoot up or down
            Randomness.X = prevVelocity.X < 0 ? -Randomness.X : Randomness.X;
            ball.Velocity = prevVelocity * shootPow; // Could be a problem
                                                     //p.onShootCooldown = true;
        }

        //-----------------------------------------------------------------------------
        // chanceOfScoring
        // ---------------
        //
        // General : Calculates the chance of scoring a goal based on the field player's position and other factors.
        //
        // Parameters :
        // sprites - A list of all sprites in the game.
        //
        // Return Value : The chance of scoring a goal (xG value).
        //
        //-----------------------------------------------------------------------------
        public double chanceOfScoring(List<Sprite> sprites)
        {
            Vector2 goalPos = new Vector2(goalCenter.X + 20, goalCenter.Y);
            float distanceFromGoal = Vector2.Distance(position, goalPos);

            float angle = MathF.Atan2(goalCenter.Y - position.Y, goalCenter.X - position.X);

            // checks if a defender is blocking the shot
            foreach (var sprite in sprites.Where(sprite => sprite is FieldPlayer && this.isSameTeam((FieldPlayer)sprite)))
            {
                if (WillIntercept(position, goalCenter, sprite.position, 10f, 20f)) { return 0; }
            }

            float normalizedDistance = (distanceFromGoal - 100) / 1300;

            // Normalize the angle value to a range between 0 and 1
            float normalizedAngle = (angle + MathF.PI) / (2 * MathF.PI);

            // Calculate the xG value based on the normalized distance, angle, and blocking status
            float xG = 1.0f - normalizedDistance;
            xG *= normalizedAngle;
            // Return the xG value

            return Vector2.Distance(goalPos, position) < 200 ? xG : xG / 2.5;
        }

        #endregion
        #region pass
        //-----------------------------------------------------------------------------
        // realPassTheBall
        // ---------------
        //
        // General : Passes the ball to a target player with a calculated velocity.
        //
        // Parameters :
        // target - The target player to pass the ball to.
        // ball - The ball to be passed.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public void realPassTheBall(FieldPlayer target, Ball ball)
        {
            Random passingForce = new Random();

            // Calculate the direction to the target player
            Vector2 directionToTarget = Vector2.Normalize(target.position - position);

            // Calculate the velocity of the ball based on the player's current velocity
            Vector2 ballVelocity = directionToTarget * (float)passingForce.Next(3, 5) + Velocity;

            // Pass the ball to the target player
            ball.Velocity = ballVelocity;
            ball.owner = null;
            this.cooldown = 1;
        }

        //-----------------------------------------------------------------------------
        // passTheBall
        // -----------
        //
        // General : this is a 'fake' pass that happens when using the evaluate algorithm, the player passes the ball to a receiving player '
        //          and then the fucntion continues to evaluate as if a pass was made. 
        //
        // Parameters :
        // receiver - The receiver player to pass the ball to.
        // ball - The ball to be passed.
        //
        // Return Value : Void.
        //
        //-----------------------------------------------------------------------------
        public void passTheBall(FieldPlayer receiver, Ball ball)
        {
            this.hasBall = false;
            receiver.hasBall = true;

            ball.position = receiver.position; 
            ball.owner = receiver;
        }

        //-----------------------------------------------------------------------------
        // determinePassingPlayer
        // ----------------------
        //
        // General : Determines the best player to pass the ball to based on various criteria.
        //
        // Parameters :
        // cpySprites - A copy of all sprites in the game.
        //
        // Return Value : The best player to pass the ball to.
        //
        //-----------------------------------------------------------------------------
        public FieldPlayer determinePassingPlayer(List<Sprite> cpySprites)
        {
            double bestDistance = 1000;
            FieldPlayer bestPlayer = null;
            foreach (var sprite in cpySprites.Where(sprite => sprite is FieldPlayer))
            {
                FieldPlayer teamate = (FieldPlayer)sprite;
                if (this.isSameTeam(teamate))
                {
                    // Check on both defenders if one of them intercepts + check distance of pass
                    foreach (var defenderSprite in cpySprites.Where(defenderSprite => defenderSprite is FieldPlayer &&
                        !this.isSameTeam((FieldPlayer)defenderSprite)))
                    {
                        if (!WillIntercept(position, teamate.position, defenderSprite.position, 5f, 5f))
                        {
                            if (bestDistance > getDistance(teamate)) // Getting closer to the goal
                            {
                                // This can cause failure if decided to pass even though everyone is behind
                                if (teamate.goalCenter.X > 200 && position.X < teamate.position.X) // Meaning right goal
                                {
                                    bestDistance = getDistance(teamate);
                                    bestPlayer = teamate;
                                }
                                else if (teamate.goalCenter.X < 200 && position.X > teamate.position.X) // Meaning left goal
                                {
                                    bestDistance = getDistance(teamate);
                                    bestPlayer = teamate;
                                }
                            }
                        }
                    }
                }
            }
            return bestPlayer;
        }

        //-----------------------------------------------------------------------------
        // calculatePassRisk
        // -----------------
        //
        // General : Calculates the risk associated with passing the ball to a receiver player.
        //           This is done by finding the passDistance and dividing with the maximum distance allowed.
        //
        // Parameters :
        // receiver - The potential receiver.
        //
        // Return Value : The calculated pass risk - a value between 0 and 1.
        //
        //-----------------------------------------------------------------------------
        public double calculatePassRisk(FieldPlayer receiver)
        {
            float distance = Vector2.Distance(position, receiver.position);
            float maxPassDistance = Math.Min(Logic.field.Width, Logic.field.Height) * 0.75f; // Adjust the pass distance limit based on the field dimensions
            float passDistanceRatio = distance / maxPassDistance;

            float successChance = 1 - passDistanceRatio; // The closer the receiver, the higher the success chance
            successChance *= 1 - MathHelper.Clamp(distance / 1000f, 0, 0.5f); // Add more realism by reducing success chance for longer passes
            successChance *= 1 - MathHelper.Clamp(Math.Abs(position.Y - receiver.position.Y) / Logic.field.Height, 0, 0.5f); // Reduce success chance if the receiver is not in the same vertical lane as the passer

            // Additional logic: Reduce success chance if the passer is close to the goal
            if (Vector2.Distance(position, goalCenter) < 150f)
            {
                successChance *= 0.6f;
            }

            return successChance;
        }

        //-----------------------------------------------------------------------------
        // WillIntercept
        // -------------
        //
        // General : Checks whether a defender can intercept a pass between two players of opposing team.
        //
        // Parameters :
        // source - The source position of the pass.
        // destination - The destination position of the pass.
        // defender - The defender player's position.
        // ballRadius - The radius of the ball.
        // playerRadius - The radius of the defender player.
        //
        // Return Value : True if the defender can intercept the pass, false otherwise.
        //
        //-----------------------------------------------------------------------------
        public bool WillIntercept(Vector2 source, Vector2 destination, Vector2 defender, float ballRadius, float playerRadius)
        {
            // CJ code
            Vector2 direction = destination - source;
            float distance = direction.Length();
            Vector2 unitVector = direction / distance;

            Vector2 relativePosition = defender - source;
            // Multiplying the relativePosition with the unitVector.
            float dotProduct = Vector2.Dot(unitVector, relativePosition);

            // Non-relevant value
            if (dotProduct < 0 || dotProduct > distance)
            {
                return false;
            }

            Vector2 projection = source + unitVector * dotProduct;
            float distanceToProjection = Vector2.Distance(projection, defender);

            return distanceToProjection < ballRadius + playerRadius;
        }

        #endregion
        #region dribble
        //-----------------------------------------------------------------------------
        // calculateDribbleRisk
        // --------------------
        //
        // General : Calculates the risk associated with dribbling the ball.
        //
        // Parameters :
        // sprites - The list of sprites, including opposing players.
        //
        // Return Value : The calculated dribble risk.
        //
        //-----------------------------------------------------------------------------
        public double calculateDribbleRisk(List<Sprite> sprites)
        {
            FieldPlayer closestOpp = findClosestOpposingPlayer(sprites);
            Vector2 opponentPos = closestOpp.position;
            float distanceToOpponent = Vector2.Distance(position, opponentPos);

            // Calculate angle to opponent
            float angleToOpponent = MathHelper.ToDegrees((float)Math.Atan2(opponentPos.Y - position.Y, opponentPos.X - position.X));

            // Normalize the distance to a value between 0 and 1
            float normalizedDistance = distanceToOpponent / (Logic.field.Width - 25 - 10);

            // Calculate the chance of successful dribble
            double result = (1f - normalizedDistance) * (1f - Math.Abs(angleToOpponent) / 190f);

            // Nerf if close to the goal
            Vector2 goalPos = new Vector2(goalCenter.X + 20, goalCenter.Y);
            float distanceFromGoal = Vector2.Distance(position, goalPos);
            result *= distanceFromGoal < 200 ? 0.5 : 1;

            float speedDiff = Math.Abs(this.speed - closestOpp.speed); // 0 - 0.9

            // Modify result based on speed difference between the player and the closest opponent
            return pastPlayer(sprites) ? result * (closestOpp.speed > this.speed ? (1 + (speedDiff / 6)) : (1 + (speedDiff / 3))) : result;
        }

        //-----------------------------------------------------------------------------
        // pastPlayer
        // ----------
        //
        // General : Checks if the player has passed the opposing players.
        //
        // Parameters :
        // sprites - The list of sprites, including opposing players.
        //
        // Return Value : True if the player has passed the opposing players, false otherwise.
        //
        //-----------------------------------------------------------------------------
        public bool pastPlayer(List<Sprite> sprites)
        {
            foreach (var fieldPlayer in sprites.Where(fieldPlayer => fieldPlayer is FieldPlayer && !isSameTeam((FieldPlayer)fieldPlayer)))
            {
                // If attacking the right side, check if a defender's x is greater (in front of the player)
                // If attacking the left side, check if a defender's x is lower (in front of the player)
                if ((goalCenter.X > Logic.centerOfPitch.X && fieldPlayer.position.X > position.X) ||
                    (goalCenter.X < Logic.centerOfPitch.X && fieldPlayer.position.X < position.X))
                {
                    return false;
                }
            }
            return true;
        }

        //-----------------------------------------------------------------------------
        // dribble
        // -------
        //
        // General : Performs the dribble action.
        //
        // Parameters :
        // enemySprites - The list of sprites, including opposing players.
        //
        //-----------------------------------------------------------------------------
        public void dribble(List<Sprite> enemySprites)
        {
            Vector2 goalDirection = AbsVector(Vector2.Normalize(this.goalCenter - position));
            this.Velocity = speed * goalDirection;

            // Determine the Y velocity based on the direction to the goal
            this.Velocity.Y = (goalCenter.Y - position.Y > 0) ? Math.Abs(Velocity.Y) : Math.Abs(Velocity.Y) * -1;

            // If an opposing player is very close, move left or right randomly (diagonally)
            if (Vector2.Distance(this.position, findClosestOpposingPlayer(enemySprites).position) < 25)
            {
                this.Velocity.Y += 1; // Move diagonally
            }

            if (Globals.ball.isOut)
            {
                avoidOut();
            }
        }

        //-----------------------------------------------------------------------------
        // avoidOut
        // --------
        //
        // General : Adjusts the player's velocity to avoid the ball going out of bounds.
        //
        //-----------------------------------------------------------------------------
        public void avoidOut()
        {
            Vector2 goalDirection = (Vector2.Normalize(Logic.centerOfPitch - Globals.ball.position));
            this.Velocity = speed * goalDirection;
        }

        //-----------------------------------------------------------------------------
        // updatePosition
        // --------------
        //
        // General : Updates the player's position based on the current velocity.
        //
        //-----------------------------------------------------------------------------
        public void updatePosition()
        {
            Vector2 prevPos = position;
            if (!Logic.isInBounds(this) && !isAvoidingBounds(prevPos))
            {
                // If the player is out of bounds and not actively avoiding the bounds, revert to the previous position
                position = prevPos;
            }
            else
            {
                // Update the position based on the velocity
                position += Velocity;
            }
        }

        //-----------------------------------------------------------------------------
        // isAvoidingBounds
        // ----------------
        //
        // General : Checks if the player is actively avoiding going out of bounds.
        //
        // Parameters :
        // prevPos - The previous position of the player.
        //
        // Return Value : True if the player is actively avoiding the bounds, false otherwise.
        //
        //-----------------------------------------------------------------------------
        public bool isAvoidingBounds(Vector2 prevPos)
        {
            // Check if the player is on one of the bounds and if the current position is closer to the center of the pitch than the former position
            if (!Logic.isInBounds(this))
            {
                if (Vector2.Distance(position, Logic.centerOfPitch) < Vector2.Distance(prevPos, Logic.centerOfPitch))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
        //-----------------------------------------------------------------------------
        // moveOffenseNoBall
        // -----------------
        //
        // General : Moves the player when they are in offense and don't have the ball.
        //
        // Parameters :
        // sprites - The list of sprites in the game.
        //
        //-----------------------------------------------------------------------------
        public void moveOffenseNoBall(List<Sprite> sprites)
        {
            Vector2 ballPosition = ((Ball)sprites[8]).position; // Get the position of the ball

            // Get the vector pointing towards the goal
            Vector2 goalVector = goalCenter - position;

            // Get the vector pointing towards the ball handler
            Vector2 ballPointingVector = ballPosition - position;

            // Calculate the distance to the ball handler
            float distanceToBall = ballPointingVector.Length();

            // Calculate the angle between the goal vector and the ball pointing vector
            float angle = (float)Math.Acos(Vector2.Dot(goalVector, ballPointingVector) / (goalVector.Length() * ballPointingVector.Length()));

            // If the angle is less than 90 degrees (means the ball is in front of me), move towards the goal
            Random random = new Random();

            if (chaseCooldown == 0)
            {
                if (angle < MathHelper.PiOver2)
                {
                    // Move towards the goal
                    chaseTarget((int)goalCenter.X + random.Next(40), (int)goalCenter.Y + random.Next(30));
                }
                else
                {
                    // If the ball is too far away, move towards the ball handler
                    if (distanceToBall > 160f)
                    {
                        // Move towards the ball handler
                        chaseTarget((int)ballPosition.X + random.Next(50), (int)ballPosition.Y + random.Next(-20, 30));
                    }
                    else
                    {
                        // Otherwise, move towards the x-coordinate of the goal in the same y-coordinate range
                        chaseTarget((int)position.X - random.Next(-80, 40), (int)position.Y + random.Next(-80, 40));
                    }
                }
                chaseCooldown = 0.5f; // Set the chase cooldown to limit the frequency of movement changes
            }
        }

        #endregion
        #region neutral
        //-----------------------------------------------------------------------------
        // assignNeutralMove
        // ----------------
        //
        // General : Assigns the movement strategy for the player when they are in a
        //           neutral position.
        //
        // Parameters :
        // sprites - The list of sprites in the game.
        //
        //-----------------------------------------------------------------------------
        public void assignNeutralMove(List<Sprite> sprites)
        {
            Random random = new Random();

            // If the player is the closest to the ball in their team, chase the ball
            if (isClosestToBallInTeam(sprites))
            {
                chaseTarget((int)sprites[8].position.X, (int)sprites[8].position.Y); // Chase the ball
            }
            else
            {
                moveOffenseNoBall(sprites); // Move like offense with no ball
            }
        }

        //-----------------------------------------------------------------------------
        // canTakeBall
        // -----------
        //
        // General : Checks if the player can take possession of the ball.
        //
        // Parameters :
        // ball - The ball object.
        //
        // Returns :
        // bool - True if the player can take the ball, false otherwise.
        //
        //-----------------------------------------------------------------------------
        public bool canTakeBall(Ball ball)
        {
            return Vector2.Distance(position, ball.position) < 23 && ball.owner == null; // Check if the player is close enough to the ball and the ball is not owned by anyone
        }

        //-----------------------------------------------------------------------------
        // assignDefenseMove
        // -----------------
        //
        // General : Assigns the movement strategy for the player when they are in a
        //           defensive position.
        //
        // Parameters :
        // sprites - The list of sprites in the game.
        //
        //-----------------------------------------------------------------------------
        public void assignDefenseMove(List<Sprite> sprites)
        {
            if (isClosestToBallInTeam(sprites))
            {
                onBallDefender(); // Perform on-ball defense
            }
            else
            {
                offBallDefender(sprites); // Perform off-ball defense
            }
        }

        //-----------------------------------------------------------------------------
        // onBallDefender
        // --------------
        //
        // General : Handles the movement strategy for the player when they are
        //           defending against the ball handler.
        //
        //-----------------------------------------------------------------------------
        public void onBallDefender()
        {
            if (Globals.ball.owner != null)
            {
                if (Vector2.Distance(position, Globals.ball.owner.position) < 20)
                {
                    if (cooldown == 0)
                    {
                        tryToTackle(); // Try to tackle the ball handler
                    }
                }
                else
                {
                    chaseTarget((int)Globals.ball.position.X, (int)Globals.ball.position.Y); // Chase the ball
                }
            }
            else
            {
                chaseTarget((int)Globals.ball.position.X, (int)Globals.ball.position.Y); // Chase the ball
            }
        }

        public void tryToTackle()
        {
            int maxTackleDistance = 20;
            //  this.cooldown = 0.05f; // can try to tackle in 1 second

            Random rand = new Random();
            FieldPlayer opponent = (FieldPlayer)Globals.ball.owner;
            float distanceToOpponent = Vector2.Distance(this.position, opponent.position);

            // If the player is close enough to the opponent and facing in their direction, attempt a tackle
            if (distanceToOpponent <= maxTackleDistance)
            {
                float tackleSuccessChance = calculateTackleChance(distanceToOpponent, maxTackleDistance);

                // Generate a random number between 0 and 1
                float randomNum = (float)rand.NextDouble();

                // If the random number is less than the tackle success chance, successfully tackle the opponent
                if (randomNum < tackleSuccessChance)
                {
                    opponent.hasBall = false;
                    //this.hasBall = true;
                    Globals.ball.owner = null;
                }
            }
        }
        public float calculateTackleChance(float distanceToOpponent, int maxTackleDistance)
        {
            // Calculate the tackle success chance based on the distance to the opponent
            float successChance = 1 - distanceToOpponent / maxTackleDistance;

            // Clamp the success chance between 0 and 1
            return MathHelper.Clamp(successChance, 0, 1);
        }
        
        public void offBallDefender(List<Sprite> sprites)
        {
            FieldPlayer closestOpp = findClosestOpposingPlayer(sprites);
            chaseTarget((int)((this.position.X + closestOpp.position.X) / 2f), (int)((this.position.Y + closestOpp.position.Y) / 2f));
        }

        #endregion
        #region logic
        //-----------------------------------------------------------------------------
        // getThisCopy
        // -----------
        //
        // General : Retrieves a copy of the current FieldPlayer object from the
        //           provided list of sprites.
        //
        // Parameters :
        // spritesCopy - The list of sprites to search in.
        // id - The ID of the FieldPlayer object to retrieve.
        //
        // Returns :
        // FieldPlayer - The copied FieldPlayer object, or null if not found.
        //
        //-----------------------------------------------------------------------------
        public FieldPlayer getThisCopy(List<Sprite> spritesCopy, int id)
        {
            foreach (var sprite in spritesCopy.Where(sprite => sprite is FieldPlayer))
            {
                if (((FieldPlayer)sprite).id == id)
                {
                    return (FieldPlayer)sprite;
                }
            }
            return null;
        }

        //-----------------------------------------------------------------------------
        // AbsVector
        // ---------
        //
        // General : Returns a vector with absolute values for its components.
        //
        // Parameters :
        // vector - The vector to convert to absolute values.
        //
        // Returns :
        // Vector2 - The vector with absolute values.
        //
        //-----------------------------------------------------------------------------
        public Vector2 AbsVector(Vector2 vector)
        {
            return new Vector2(Math.Abs(vector.X), Math.Abs(vector.Y));
        }

        //-----------------------------------------------------------------------------
        // getDistance
        // -----------
        //
        // General : Calculates the distance between the current FieldPlayer and
        //           another FieldPlayer.
        //
        // Parameters :
        // fp - The FieldPlayer to calculate the distance to.
        //
        // Returns :
        // double - The distance between the two FieldPlayers.
        //
        //-----------------------------------------------------------------------------
        public double getDistance(FieldPlayer fp)
        {
            return Vector2.Distance(position, fp.position);
        }

        //-----------------------------------------------------------------------------
        // findClosestOpposingPlayer
        // -------------------------
        //
        // General : Finds the closest opposing FieldPlayer to the current FieldPlayer.
        //
        // Parameters :
        // cpySprites - The list of sprites to search in.
        //
        // Returns :
        // FieldPlayer - The closest opposing FieldPlayer, or null if not found.
        //
        //-----------------------------------------------------------------------------
        public FieldPlayer findClosestOpposingPlayer(List<Sprite> cpySprites)
        {
            float minDistance = float.MaxValue;
            FieldPlayer closest = null;
            foreach (var opponent in cpySprites.Where(opponent => opponent is FieldPlayer && !((FieldPlayer)opponent).isSameTeam(this)))
            {
                float currDistance = Vector2.Distance(this.position, opponent.position);
                if (currDistance < minDistance && currDistance != 0)
                {
                    minDistance = currDistance;
                    closest = (FieldPlayer)opponent;
                }
            }
            return closest;
        }

        //-----------------------------------------------------------------------------
        // isClosestToBallInTeam
        // ---------------------
        //
        // General : Checks if the current FieldPlayer is the closest player to the ball
        //           in their team.
        //
        // Parameters :
        // sprites - The list of sprites to compare against.
        //
        // Returns :
        // bool - True if the current FieldPlayer is the closest to the ball in their team,
        //        false otherwise.
        //
        //-----------------------------------------------------------------------------
        public bool isClosestToBallInTeam(List<Sprite> sprites)
        {
            float minDistance = Vector2.Distance(Globals.ball.position, position);
            foreach (var sprite in sprites.Where(sprite => sprite is FieldPlayer && this.isSameTeam((FieldPlayer)sprite)))
            {
                float currDistance = Vector2.Distance(Globals.ball.position, sprite.position);
                if (currDistance < minDistance)
                {
                    return false;
                }
            }
            return true;
        }

        public bool isSameTeam(FieldPlayer fp)
        {
            return team == fp.team && this != fp;
        }
        public void chaseTarget(int x, int y)
        {
            // Calculate the direction towards the target
            Vector2 targetPosition = new Vector2(x, y);
            Vector2 direction;
            if (targetPosition.X >= position.X)
            {
                direction = Vector2.Normalize(targetPosition - position);
            }
            else
            {
                direction = Vector2.Normalize(position - targetPosition);

            }

            // Calculate the velocity based on the player's maximum speed
            Velocity = direction * speed;

            // Update the player's position
            //position += velocity * TIME_DELTA;

        }

        #endregion
        public void determineSpeedByState(stateAction stateAct)
        {
            switch (stateAct)
            {
                case (stateAction.Offense):
                    speed = this.goalCenter.X < position.X ? Math.Abs(speed) * -1 : Math.Abs(speed);
                    break;
                case (stateAction.Defense):
                case (stateAction.Neutral):
                    speed = Globals.ball.position.X < position.X ? Math.Abs(speed) * -1 : Math.Abs(speed);
                    break;
            }
        }
        public override void SetAnimations()
        {
            if (Velocity.X > 0)
            {
                animationManager.Play(animations["WalkRight"], true);
            }
            else if (Velocity.X < 0)
            {
                animationManager.Play(animations["WalkLeft"], true);
            }
            else if (Velocity.Y > 0)
            {
                animationManager.Play(animations["WalkDown"], true);
            }
            else if (Velocity.Y < 0)
            {
                animationManager.Play(animations["WalkUp"], true);
            }
        }
        public override void Move()
        {
            string pressed = "";
            if (Keyboard.GetState().IsKeyDown(input.left)) { Velocity.X = -speed; pressed += "l"; }
            if (Keyboard.GetState().IsKeyDown(input.right)) { Velocity.X = speed; pressed += "r"; }
            if (Keyboard.GetState().IsKeyDown(input.up)) { Velocity.Y = -speed; pressed += "u"; }
            if (Keyboard.GetState().IsKeyDown(input.down)) { Velocity.Y = speed; pressed += "d"; }
            if (pressed.Length > 1) { Velocity.X /= 1.4f; Velocity.Y /= 1.4f; }
            // if (Keyboard.GetState().IsKeyDown(input.shoot){ }
        }
    }
}
