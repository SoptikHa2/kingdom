using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    public class City : OwnerObject
    {
        public const char referenceSeparator = (char)30000;
        public const char buildingStatesSeparator = (char)35000;

        public Building[] buildings = Building.Get();
        public Building currentlyBuilding = null;

        public string Name;
        public int Level, Maxpop = 2, Production;
        private int _currpop;
        public int Currpop
        {
            get { return _currpop; }
            set
            {
                _currpop = value;
                while (value >= Maxpop)
                {
                    _currpop = value - Maxpop;
                    Maxpop += 2;
                    Level++;
                    Production++;

                    if (App.debug)
                        Bridge.Script.Call("console.log", "City leveled up  (+2 maxpop, +1 level, +1 production)");
                    App.Announce("One of your cities has leveled up!");
                }
                while (value < 0)
                {
                    Maxpop -= 2;
                    _currpop = value + Maxpop;
                    Level--;
                    Production--;

                    if (App.debug)
                        Bridge.Script.Call("console.log", "City lost 1 level (-2 maxpop, -1 level, -1 production)");
                    App.Announce("One of your cities lost one level due to lack of prosperity.");
                }
            }
        }
        public bool readyToCapture = false;

        public City(Player owner, App.Textures texture, string name, int x, int y, int z)
        {
            this.owner = owner;
            this.Name = name;
            this.Level = 0;
            this.Production = 1;
            this.Currpop = 0;
            this.x = x;
            this.y = y;
            this.z = z;
            this.texture = texture;
        }

        public City() { }

        public override void NextTurn()
        {
            if (Game.game.Map[x, y, 2] != null && (Game.game.Map[x, y, 2] as Unit).owner != owner)
            {
                if (!readyToCapture)
                    readyToCapture = true;

                int pState = Game.game.playerState;

                if (readyToCapture && (Game.game.Map[x, y, 2] as Unit).owner == Game.game.players[pState == 0 ? Game.game.players.Count - 1 : pState - 1])
                    App.Announce("There is a city ready to capture!");

                if (readyToCapture && (Game.game.Map[x, y, 2] as Unit).owner != owner && owner == Game.game.players[pState == 0 ? Game.game.players.Count - 1 : pState - 1])
                    App.Announce("One of your cities is under siege!");
            }
            else if (readyToCapture)
                readyToCapture = false;

            if (owner != null && Game.game.players[Game.game.playerState] == owner)
                owner.resources += Production;

            if (Game.game.Map[x, y, 2] != null && (Game.game.Map[x, y, 2] as Unit).owner == owner && Game.game.players[Game.game.playerState] == owner)
            {
                (Game.game.Map[x, y, 2] as Unit).hp += 2;
                if ((Game.game.Map[x, y, 2] as Unit).hp > (Game.game.Map[x, y, 2] as Unit).maxhp)
                    (Game.game.Map[x, y, 2] as Unit).hp = (Game.game.Map[x, y, 2] as Unit).maxhp;
            }
        }

        public bool Capture()
        {
            if (CanCapture())
            {
                owner = (Game.game.Map[x, y, 2] as Unit).owner;

                switch (owner.color)
                {
                    case 0:
                        texture = App.Textures.city_blue;
                        break;
                    case 1:
                        texture = App.Textures.city_red;
                        break;
                    case 2:
                        texture = App.Textures.city_green;
                        break;
                    case 3:
                        texture = App.Textures.city_yellow;
                        break;
                }

                readyToCapture = false;
                (Game.game.Map[x, y, 2] as Unit).turns = 0;
                (Game.game.Map[x, y, 2] as Unit).canAttack = false;

                owner.revealTilesNear(x, y, 4);

                if (App.debug)
                    Bridge.Script.Call("console.log", "A city was captured");

                App.DisplayPlayerInfo();

                if(owner == Game.game.players[0])
                {
                    Achievement.citiesCaptured++;
                    Achievement.Check();
                }

                return true;
            }
            return false;
        }

        public bool CanCapture()
        {
            return readyToCapture && Game.game.Map[x, y, 2] != null && (Game.game.Map[x, y, 2] as Unit).owner != owner && (Game.game.Map[x, y, 2] as Unit).owner == Game.game.players[Game.game.playerState];
        }

        public override void loadFromString(string save)
        {
            string[] s = save.Split(separator);
            owner = s[0] == "-1" ? null : Game.game.players[int.Parse(s[0])];
            Name = s[1];
            Level = int.Parse(s[2]);
            Production = int.Parse(s[3]);
            Currpop = int.Parse(s[4]);
            x = int.Parse(s[5]);
            y = int.Parse(s[6]);
            z = int.Parse(s[7]);
            texture = (App.Textures)int.Parse(s[8]);
            Maxpop = int.Parse(s[9]);
            readyToCapture = s[10] == "True";
            buildings = Building.Get();
            currentlyBuilding = getCurrBuilding(int.Parse(s[11]));
            if (currentlyBuilding != null)
            {
                currentlyBuilding.state = Building.States.InProgress;
                currentlyBuilding.turnsRemaining = int.Parse(s[13]);
            }

            for (int i = 0; i < buildings.Count(); i++)
            {
                string[] bs = s[12].Split(buildingStatesSeparator);
                buildings[i].state = (Building.States)int.Parse(bs[i]);
            }
        }

        public override void loadFromStringSecondRound(string save)
        {
        }

        public override string saveString()
        {
            string s = $"{Game.getPlayerId(owner)}{separator}{Name}{separator}{Level}{separator}{Production}{separator}{Currpop}{separator}{x}{separator}{y}{separator}{z}{separator}{(int)texture}{separator}" +
                $"{Maxpop}{separator}{readyToCapture}{separator}{getCurrBuildingId()}{separator}";
            for (int i = 0; i < buildings.Count(); i++)
            {
                s += (int)buildings[i].state;
                if (i != buildings.Count() - 1)
                    s += buildingStatesSeparator;
            }
            return s + $"{separator}{(currentlyBuilding != null ? currentlyBuilding.turnsRemaining.ToString() : "")}";
        }

        private int getCurrBuildingId()
        {
            int id = -1;
            int i = 0;
            foreach (Building b in buildings)
            {
                if (b == currentlyBuilding)
                {
                    id = i;
                    break;
                }
                i++;
            }
            return id;
        }

        private Building getCurrBuilding(int id)
        {
            if (id == -1)
                return null;
            return buildings[id];
        }

        public static string SaveReference(City c)
        {
            return $"{c.x}{referenceSeparator}{c.y}";
        }
    }
}
