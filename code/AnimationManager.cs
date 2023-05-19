using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
//using models;

namespace animation
{
    public class AnimationManager
    {
        private Animation animation;
        private float timer;
        public Vector2 position { get; set; }
        public AnimationManager(Animation animation) 
        {
            this.animation = animation;
        }
        public void Draw(SpriteBatch spriteBatch) 
        {
            spriteBatch.Draw(animation.Texture, position,
                new Rectangle(animation.CurrentFrame * animation.FrameWidth, 0, animation.FrameWidth, animation.FrameHeight), Color.White);
        }
        public void Play(Animation animation, bool isMoving)
        {
            if (!isMoving) {
                animation.CurrentFrame = 0;
                this.timer = 0f;
                return;
            }
            if (this.animation == animation) { return; }
            this.animation = animation;
            this.animation.CurrentFrame = 0;
            this.timer = 0;
        }
        public void Update(GameTime gt) 
        {
            this.timer += (float)gt.ElapsedGameTime.TotalSeconds;
            if (timer > animation.FrameSpeed) 
            {
                this.timer = 0f;
                animation.CurrentFrame++;
            }
            if (animation.CurrentFrame >= animation.FrameCount) 
            {
                animation.CurrentFrame = 0;
            }
        }
    }
}
