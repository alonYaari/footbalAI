using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sprites;
namespace runningMan2
{
    public class Player:Sprite
    {
        public bool hasBall = false;
        public float cooldown = 0f;
        public int team;

        public Player(Texture2D texture) : base(texture)
        {
        }
        public Player() {}

        public Player(Dictionary<string, Animation> animations, Vector2 startingPosition, string animationName) : base(animations, startingPosition, animationName)
        {
            
        }
        public virtual void Update(GameTime gt, List<Sprite> sprites)
        {
            base.Update(gt, sprites);
        }
        public virtual void SetAnimations() { }
        public virtual void Move() { }
        public void setHasBall()
        {
            this.hasBall = true;
        }
    }
}
