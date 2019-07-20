using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    public class Resource : OwnerObject
    {
        public City cityOwner;
        public bool ready;
        private bool destroyAfterDone;
        public int everyTurn_productionBoost;
        public int firstTurn_productionBoost;

        private int type;

        public int cost;
        public App.Textures readyTexture;

        public enum eResources { FishDeerBush, Farm }


        public static Resource[] resources = {
            new Resource(null, 0, 1, 2, App.Textures.unavalibeTile, App.Textures.unavalibeTile, true, -1, -1, -1), // Fish, Deer, Bush, etc.
            new Resource(null, 0, 2, 5, App.Textures.unavalibeTile, App.Textures.unavalibeTile, false, -1, -1, -1) // Farm
        };

        private Resource(City cityOwner, int et_prodBoost, int ft_prodBoost, int cost, App.Textures readyTexture, App.Textures texture, bool destroyAfterDone, int x, int y, int z)
        {
            this.cityOwner = cityOwner;
            this.owner = cityOwner == null ? null : cityOwner.owner;
            this.ready = false;
            this.everyTurn_productionBoost = et_prodBoost;
            this.firstTurn_productionBoost = ft_prodBoost;
            this.cost = cost;
            this.readyTexture = readyTexture;
            this.texture = texture;
            this.x = x;
            this.y = y;
            this.z = z;
            this.destroyAfterDone = destroyAfterDone;
        }

        public Resource() { }

        public static Resource getResource(City cityOwner, Resource pattern, App.Textures texture, App.Textures readyTexture, int x, int y)
        {
            Resource r = new Resource(cityOwner, pattern.everyTurn_productionBoost, pattern.firstTurn_productionBoost, pattern.cost, readyTexture, texture, pattern.destroyAfterDone, x, y, 1);
            r.type = pattern == resources[1] ? 1 : 0;
            return r;
        }

        public override void NextTurn()
        {
            if (ready && cityOwner != null && cityOwner.owner != null && everyTurn_productionBoost != 0)
            {
                cityOwner.Currpop += everyTurn_productionBoost;
            }
        }

        public bool Rebuild()
        {
            if (canRebuild())
            {
                owner = cityOwner.owner;
                cityOwner.owner.resources -= cost;
                ready = true;
                texture = readyTexture;
                cityOwner.Currpop += firstTurn_productionBoost;

                if (destroyAfterDone)
                    Game.game.Map[x, y, 1] = null;

                if(owner == Game.game.players[0])
                {
                    Achievement.resourcesRebuilt++;
                    Achievement.Check();
                }

                return true;
            }
            return false;
        }

        public bool canRebuild()
        {
            return !ready && cityOwner != null && cityOwner.owner != null && cityOwner.owner.resources >= cost;
        }

        public override string saveString()
        {
            return $"{City.SaveReference(cityOwner)}{separator}{ready}{separator}{(int)texture}{separator}{(int)readyTexture}{separator}{x}{separator}{y}{separator}{type}";
        }

        public override void loadFromString(string save)
        {
            string[] s = save.Split(separator);
            ready = s[1] == "True";
            texture = (App.Textures)int.Parse(s[2]);
            readyTexture = (App.Textures)int.Parse(s[3]);
            x = int.Parse(s[4]);
            y = int.Parse(s[5]);
            z = 1;
            type = int.Parse(s[6]);
            Resource pattern = resources[type];
            everyTurn_productionBoost = pattern.everyTurn_productionBoost;
            firstTurn_productionBoost = pattern.firstTurn_productionBoost;
            cost = pattern.cost;
            destroyAfterDone = pattern.destroyAfterDone;
            
        }

        public override void loadFromStringSecondRound(string save)
        {
            string[] s = save.Split(separator)[0].Split(City.referenceSeparator);
            cityOwner = Game.game.Map[int.Parse(s[0]), int.Parse(s[1]), 1] as City;
            loadFromString(save);
        }
    }
}
