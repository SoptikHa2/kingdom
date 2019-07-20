using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    class RecruitTent : OwnerObject
    {
        public const int price = 7;

        public override void NextTurn()
        {
            MapObject o = Game.game.Map[x, y, 2];
            if(o != null && o is Unit)
            {
                Unit u = o as Unit;
                if(u.owner != owner)
                {
                    Game.game.Map[x, y, z] = null;
                    u.owner.resources += 4;
                    if(u.owner == Game.game.players[0])
                    {
                        Achievement.recruitTentsDestroyed++;
                        Achievement.Check();
                    }
                }
                else
                {
                    u.hp++;
                    if (u.hp > u.maxhp)
                        u.hp = u.maxhp;
                }
            }
        }

        public static RecruitTent Build(Player p, int x, int y)
        {
            if (p.resources >= price && (Game.game.Map[x, y, 0] as Terrain).type == (int)Terrain.Types.Land && Game.game.Map[x, y, 1] == null && Game.game.Map[x, y, 2] != null && (Game.game.Map[x, y, 2] as Unit).owner == p)
            {
                p.resources -= price;
                App.Textures t = App.Textures.border;
                switch (p.color)
                {
                    case 0:
                        t = App.Textures.city_recruitTent_blue;
                        break;
                    case 1:
                        t = App.Textures.city_recruitTent_red;
                        break;
                    case 2:
                        t = App.Textures.city_recruitTent_green;
                        break;
                    case 3:
                        t = App.Textures.city_recruitTent_yellow;
                        break;
                }

                if(p == Game.game.players[0])
                {
                    Achievement.recruitTentsBuilt++;
                    Achievement.Check();
                }

                return new RecruitTent(p, t, x, y);
            }

            return null;
        }

        private RecruitTent(Player owner, App.Textures texture, int x, int y, int z = 1)
        {
            this.owner = owner;
            this.x = x;
            this.y = y;
            this.z = z;
            this.texture = texture;
        }

        public RecruitTent() { }

        public override string saveString()
        {
            return $"{Game.getPlayerId(owner)}{separator}{(int)texture}{separator}{x}{separator}{y}{separator}{z}";
        }

        public override void loadFromString(string save)
        {
            string[] s = save.Split(separator);
            owner = Game.game.players[int.Parse(s[0])];
            texture = (App.Textures)int.Parse(s[1]);
            x = int.Parse(s[2]);
            y = int.Parse(s[3]);
            z = int.Parse(s[4]);
        }

        public override void loadFromStringSecondRound(string save)
        {
        }
    }
}
