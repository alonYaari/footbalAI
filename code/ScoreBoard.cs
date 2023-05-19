using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace runningMan2
{
    internal class ScoreBoard
    {
        private SpriteFont font;
        private Vector2 position;
        private TimeSpan remainingTime;
        private int teamAScore;
        private int teamBScore;
        private string teamAName;
        private string teamBName;

        public ScoreBoard(SpriteFont font, Vector2 position, string teamAName, string teamBName)
        {
            this.font = font;
            this.position = position;
            this.teamAName = teamAName;
            this.teamBName = teamBName;
            this.teamAScore = 0;
            this.teamBScore = 0;
            this.remainingTime = TimeSpan.Zero;
        }

        public void Update(TimeSpan gameTime)
        {
            remainingTime -= gameTime;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            string timeString = string.Format("{0}:{1:D2}", remainingTime.Minutes, remainingTime.Seconds);
            string scoreString = string.Format("{0} - {1}", teamAScore, teamBScore);
            Vector2 scoreSize = font.MeasureString(scoreString);
            Vector2 timeSize = font.MeasureString(timeString);
            Vector2 teamASize = font.MeasureString(teamAName);
            Vector2 teamBSize = font.MeasureString(teamBName);
            float totalWidth = scoreSize.X + timeSize.X + teamASize.X + teamBSize.X + 40; // 40 is for padding
            Vector2 scorePos = new Vector2(position.X - totalWidth / 2, position.Y);
            Vector2 timePos = new Vector2(scorePos.X + scoreSize.X + 10, position.Y);
            Vector2 teamAPos = new Vector2(scorePos.X + scoreSize.X + timeSize.X + 20, position.Y);
            Vector2 teamBPos = new Vector2(scorePos.X + scoreSize.X + timeSize.X + teamASize.X + 30, position.Y);

            spriteBatch.DrawString(font, scoreString, scorePos, Color.White);
            spriteBatch.DrawString(font, timeString, timePos, Color.White);
            spriteBatch.DrawString(font, teamAName, teamAPos, Color.White);
            spriteBatch.DrawString(font, teamBName, teamBPos, Color.White);
        }

        public void SetRemainingTime(TimeSpan time)
        {
            this.remainingTime = time;
        }

        public static void addScoreByTeam(int teamNumber)
        {
            if (teamNumber == 1)
                Game1.teamAScore++;
            else
                Game1.teamBScore++;
        }
    }

}
