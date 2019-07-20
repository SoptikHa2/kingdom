using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    public class Unit : OwnerObject
    {
        public const char reqResearchSeparator = (char)40000;

        [Flags]
        public enum Ability
        {
            None = 0,
            Walk = 1, // Can move on ground
            Swim = 2, // Can move on water
            Climb = 4, // Can move on mountains
            Dash = 8, // Can move after attack
            Combo = 16, // Regain all move & attack points after killing target
            Regeneration = 32, // At the end of each turn, regen 2 HP
            RangedAttack = 64 // Only another units with ranged attack can hit this unit when defending
        }

        protected const int basicExpNeeded = 10;
        protected const int expIncreasePerLevel = 5;
        protected const int hpIncreasePerLevel = 5;

        public string name;
        public int hp, maxhp, turns, maxturns, level, attack, defense, cost;
        public bool canAttack, alive;
        public Ability ability;
        public App.Textures weaponTexture;
        public int[] reqResearch;

        public int exp
        {
            get { return _exp; }
            set
            {
                int maxExp = basicExpNeeded + level * expIncreasePerLevel;
                while (value >= maxExp)
                {
                    value -= maxExp;
                    level++;
                    maxhp += hpIncreasePerLevel;
                    hp = maxhp;
                }

                _exp = value;
            }
        }
        private int _exp;

        public override void NextTurn()
        {
            if (owner.researches[(int)Research.researches.Battle_Medicine].state == Research.States.Researched)
            {
                hp++;
                if (hp > maxhp)
                    hp = maxhp;
            }

            turns = maxturns;
            canAttack = true;

            if (ability.HasFlag(Ability.Regeneration))
            {
                hp += 2;
                if (hp > maxhp)
                    hp = maxhp;
            }
        }

        public void Attack(Unit target)
        {
            bool debug_attBack = false;
            int debug_att1 = 0;
            int debug_att2 = 0;

            if (canAttack && target.owner != owner && (!App.useFog || !(Game.game.Map[target.x, target.y, 0] as Terrain).isFogForPlayers[Game.game.playerState]))
            {
                //App.Anim(x, y, (x + target.x) / 2, (y + target.y) / 2, this, () => { App.Anim((x + target.x) / 2, (y + target.y) / 2, x, y, this, () => { }); });

                canAttack = false;
                if (!ability.HasFlag(Ability.Dash))
                    turns = 0;

                int attackStrength = attack * 2;
                /*if (hp < ((double)maxhp / 100) * 40)
                    attackStrength = attack;*/
                attackStrength -= target.defense;

                // If defending unit stands in city with walls, substract another one attack point
                if (Game.game.Map[target.x, target.y, 1] is City && (Game.game.Map[target.x, target.y, 1] as City).buildings[(int)Building.EBuildings.Walls].state == Building.States.Builded)
                    attackStrength--;

                // If attacking unit is in water, substract another attack point
                if ((Game.game.Map[x, y, 0] as Terrain).type == (int)Terrain.Types.Sea)
                    attackStrength--;
                // If defeding unit is in water, add one attack point
                if ((Game.game.Map[target.x, target.y, 0] as Terrain).type == (int)Terrain.Types.Sea)
                    attackStrength++;

                    if (attackStrength < 1)
                    attackStrength = 1;

                debug_att1 = attackStrength;

                target.hp -= attackStrength;

                if (target.hp <= 0)
                {
                    target.alive = false;
                    Game.game.Map[target.x, target.y, target.z] = null;
                    if (ability.HasFlag(Ability.Combo))
                    {
                        canAttack = true;
                        turns = maxturns;
                    }
                    if (owner == Game.game.players[0])
                    {
                        Achievement.enemiesKilled++;
                        Achievement.Check();
                    }
                    else if (target.owner == Game.game.players[0])
                    {
                        Achievement.unitsLost++;
                        Achievement.Check();
                    }
                }
                // If enemy is alive AND (you attack melee OR you both attack ranged), attack back
                else if (!ability.HasFlag(Ability.RangedAttack) || (ability.HasFlag(Ability.RangedAttack) && target.ability.HasFlag(Ability.RangedAttack)))
                {
                    debug_attBack = true;

                    attackStrength = target.attack * 2;
                    /*if (target.hp < ((double)target.maxhp / 100) * 80)
                        attackStrength = target.attack;*/
                    attackStrength -= defense;
                    if (attackStrength < 1)
                        attackStrength = 1;

                    debug_att2 = attackStrength;

                    hp -= attackStrength;

                    if (hp <= 0)
                    {
                        alive = false;
                        Game.game.Map[x, y, z] = null;
                        if (target.owner == Game.game.players[0])
                        {
                            Achievement.enemiesKilled++;
                            App.KilledInDefense++;
                        }
                        else if (owner == Game.game.players[0])
                        {
                            Achievement.unitsLost++;
                            Achievement.Check();
                        }
                    }
                }
            }

            if (App.debug)
                Bridge.Script.Call("console.log", $"Unit {name} (player: {owner.color}) attacked {target.name} (player: {target.owner.color}). Attack back: {debug_attBack}. Attack 1: {debug_att1}. Attack 2: {debug_att2}");
        }

        public enum eUnits
        {
            Warrior, Explorer, Archer, HeavyInfantry, Catapult, Chivalry, Ship
        }

        /// <summary>
        /// Unit types are listed in enum eUnits
        /// </summary>
        public static Unit[] units = {
            new Unit(null, "Warrior", 10, 3, 2, 1, 2, Ability.Walk | Ability.Climb, App.Textures.unavalibeTile, App.Textures.unit_warrior, new int[0], -1, -1, -1),
            new Unit(null, "Explorer", 5, 5, 1, 0, 2, Ability.Walk | Ability.Climb | Ability.Regeneration | Ability.Swim, App.Textures.unavalibeTile, App.Textures.unit_explorer, new int[0], -1, -1, -1),
            new Unit(null, "Archer", 7, 3, 3, 0, 3, Ability.Walk | Ability.Climb | Ability.RangedAttack | Ability.Swim, App.Textures.unavalibeTile, App.Textures.unit_archer, new int[]{ (int)Research.researches.Archery }, -1, -1, -1),
            new Unit(null, "Heavy Infantry", 15, 2, 3, 1, 5, Ability.Walk, App.Textures.unavalibeTile, App.Textures.unit_heavyInfantry, new int[]{ (int)Research.researches.Ironsmithing }, -1, -1, -1),
            new Unit(null, "Cataput", 7, 2, 4, 0, 5, Ability.Walk | Ability.RangedAttack, App.Textures.unavalibeTile, App.Textures.unit_catapult, new int[]{ (int)Research.researches.Mathematics }, -1, -1, -1),
            new Unit(null, "Berserk", 12, 3, 3, 0, 8, Ability.Walk | Ability.Dash | Ability.Combo, App.Textures.unavalibeTile, App.Textures.unit_berserk, new int[]{ (int)Research.researches.AdvancedWarfare }, -1, -1, -1),
            //new Unit(null, "Ship", 10, 10, 3, 2, 3, Ability.Swim, App.textures.unavalibeTile, App.textures.unit_ship, -1, -1, -1)
            new Unit(null, "Musketeer", 10, 3, 6, 1, 16, Ability.Walk | Ability.Climb | Ability.RangedAttack, App.Textures.unavalibeTile, App.Textures.unit_musketeer, new int[]{ (int)Research.researches.Gunpowder }, -1, -1, -1)
        };

        private Unit(Player owner, string name, int maxhp, int turns, int attack, int defense, int cost, Ability ability, App.Textures texture, App.Textures weaponTexture, int[] reqResearch, int x, int y, int z)
        {
            this.owner = owner;
            this.name = name;
            this.hp = maxhp;
            this.maxhp = maxhp;
            this.turns = 0;
            this.maxturns = turns;
            this.attack = attack;
            this.defense = defense;
            this.cost = cost;
            this.ability = ability;
            this.level = 0;
            this._exp = 0;
            this.canAttack = false;
            this.alive = true;
            this.texture = texture;
            this.weaponTexture = weaponTexture;
            this.reqResearch = reqResearch;
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Unit() { }

        /// <summary>
        /// Create new unit
        /// </summary>
        /// <param name="owner">Player as owner of unit</param>
        /// <param name="pattern">Pattern, unit stats will be copied from this pattern. Get patterns from Unit.units array</param>
        /// <returns></returns>
        public static Unit getUnit(Player owner, App.Textures texture, Unit pattern, int x, int y)
        {
            return new Unit(owner, pattern.name, pattern.maxhp, pattern.maxturns, pattern.attack, pattern.defense, pattern.cost, pattern.ability, texture, pattern.weaponTexture, pattern.reqResearch, x, y, 2);
        }

        public bool Move(int tX, int tY, Player player)
        {
            // TODO: pri vstupu na vodu zdvojnasobit cenu pohybu [?]
            if (canMove(tX, tY, player) /*new Path(x, y, tX, tY, this).pth.Count != 0*/)
            {
                if ((Game.game.Map[tX, tY, 2] as Unit) != null)
                {
                    // TODO: Fix somehow (tedka kdyz utocim na nepritele a je moc daleko, seru na to a nepohnu se, utocim jen kdyz je na 1 policko daleko
                    //  -> MUZE BYT FIXNUTO: budu chodit krok po kroku
                    if (Math.Max(Math.Abs(x - tX), Math.Abs(y - tY)) <= 1)
                    {

                        if ((Game.game.Map[tX, tY, 2] as Unit).owner != owner)
                            Attack((Game.game.Map[tX, tY, 2] as Unit));
                    }
                    return false;
                }

                turns -= Math.Max(Math.Abs(x - tX), Math.Abs(y - tY));
                Game.game.Map[tX, tY, 2] = this;
                Game.game.Map[x, y, z] = null;
                x = tX;
                y = tY;
                owner.revealTilesNear(x, y);
                return true;
            }
            return false;
        }

        public bool canMove(int tX, int tY, Player player)
        {
            bool terrain = false;
            if ((Game.game.Map[tX, tY, 0] as Terrain).type == (int)Terrain.Types.Land)
            {
                terrain = ability.HasFlag(Ability.Walk);
                if (App.debug)
                    Bridge.Script.Call("console.log", $"Unit asks if can move to Land. Ability Walk: " + terrain);
            }
            else if ((Game.game.Map[tX, tY, 0] as Terrain).type == (int)Terrain.Types.Mountain)
            {
                terrain = ability.HasFlag(Ability.Walk) && ability.HasFlag(Ability.Climb);
                if (App.debug)
                    Bridge.Script.Call("console.log", $"Unit asks if can move to Mountain. Ability Walk & Climb: " + terrain);
            }
            else if ((Game.game.Map[tX, tY, 0] as Terrain).type == (int)Terrain.Types.Sea)
            {
                terrain = ability.HasFlag(Ability.Swim);
                if (App.debug)
                    Bridge.Script.Call("console.log", $"Unit asks if can move to Sea. Ability Swim: " + terrain);
            }

            if (App.debug)
                Bridge.Script.Call("console.log", ability);

            return (!App.useFog || !(Game.game.Map[tX, tY, 0] as Terrain).isFogForPlayers[Game.game.playerState]) && player == owner &&
                ((Math.Max(Math.Abs(x - tX), Math.Abs(y - tY))) <= turns || (Math.Max(Math.Abs(x - tX), Math.Abs(y - tY)) <= 1 &&
                Game.game.Map[tX, tY, 2] != null && (Game.game.Map[tX, tY, 2] as Unit).owner != owner && canAttack)) && terrain;
        }

        public bool canAttackToSomebody(int tX = -1, int tY = -1)
        {
            if (!canAttack)
                return false;

            if (tX == -1 || tY == -1)
            {
                for (int i = x - 1; i <= x + 1; i++)
                    if (i < 0 || i >= App.tiles)
                        continue;
                    else
                        for (int j = y - 1; j <= y + 1; j++)
                            if (j < 0 || j >= App.tiles)
                                continue;
                            else
                                if (Game.game.Map[i, j, 2] is Unit && (Game.game.Map[i, j, 2] as Unit).owner != owner)
                                return true;
            }
            else
                return Game.game.Map[tX, tY, 2] is Unit && (Game.game.Map[tX, tY, 2] as Unit).owner != owner;
            return false;
        }

        public string getAbilitiesInfo()
        {
            string s = "";
            if (ability.HasFlag(Ability.Walk))
                s += "<strong>Walk</strong>: This unit can move thru 'land' terrain.<br />";
            if (ability.HasFlag(Ability.Climb))
                s += "<strong>Climb</strong>: This unit can move thru 'mountain' terrain.<br />";
            if (ability.HasFlag(Ability.Swim))
                s += "<strong>Swim</strong>: This unit can move thru 'sea' terrain.<br />";
            if (ability.HasFlag(Ability.Dash))
                s += "<strong>Dash</strong>: Attack doesn't end this unit's turn. (Can move after attacking)<br />";
            if (ability.HasFlag(Ability.Combo))
                s += "<strong>Combo</strong>: Gains all movement points and ability to attack once more after killing an unit. (Can move & attack after killing opponent)<br />";
            if (ability.HasFlag(Ability.RangedAttack))
                s += "<strong>Ranged Attack</strong>: Units without Ranged Attack ability cannot attack back when attacked by this unit.<br />";
            if (ability.HasFlag(Ability.Regeneration))
                s += "<strong>Regeneration</strong>: This unit regenerates 2 HP each turn, making it much harder to kill.<br />";
            return s;
        }

        public override void loadFromString(string save)
        {
            string[] s = save.Split(separator);
            owner = Game.game.players[int.Parse(s[0])];
            name = s[1];
            maxhp = int.Parse(s[2]);
            hp = int.Parse(s[3]);
            maxturns = int.Parse(s[4]);
            turns = int.Parse(s[5]);
            attack = int.Parse(s[6]);
            defense = int.Parse(s[7]);
            cost = int.Parse(s[8]);
            ability = (Ability)int.Parse(s[9]);
            texture = (App.Textures)int.Parse(s[10]);
            weaponTexture = (App.Textures)int.Parse(s[11]);
            reqResearch = getReqResearches(s[12]);
            x = int.Parse(s[13]);
            y = int.Parse(s[14]);
            z = int.Parse(s[15]);
            _exp = int.Parse(s[16]);
            level = int.Parse(s[17]);
            canAttack = s[18] == "True";
            alive = s[19] == "True";
        }

        public override void loadFromStringSecondRound(string save)
        {
        }

        public override string saveString()
        {
            return $"{Game.getPlayerId(owner)}{separator}{name}{separator}{maxhp}{separator}{hp}{separator}{maxturns}{separator}{turns}{separator}{attack}{separator}" +
                $"{defense}{separator}{cost}{separator}{(int)ability}{separator}{(int)texture}{separator}{(int)weaponTexture}{separator}{getReqResearchSaveString()}{separator}" +
                $"{x}{separator}{y}{separator}{z}{separator}{exp}{separator}{level}{separator}{canAttack}{separator}{alive}";
        }

        private string getReqResearchSaveString()
        {
            string s = "";
            for (int i = 0; i < reqResearch.Length; i++)
            {
                s += reqResearch[i];
                if (i != reqResearch.Length - 1)
                    s += reqResearchSeparator;
            }
            return s;
        }

        private int[] getReqResearches(string s)
        {
            List<int> i = new List<int>();
            foreach (string a in s.Split(reqResearchSeparator))
            {
                if (a.Trim() == "")
                    continue;
                //Bridge.Script.Call("console.log", a);
                i.Add(int.Parse(a));
            }
            return i.ToArray();
        }
    }

    class Path
    {
        public List<Point> pth = new List<Point>();
        private float[,] map;

        public Path(int x1, int y1, int x2, int y2, Unit u)
        {
            map = new float[App.tiles, App.tiles];
            map[x2, y2] = float.PositiveInfinity;

            for (int x = 0; x < App.tiles; x++)
            {
                for (int y = 0; y < App.tiles; y++)
                {
                    if (!u.canMove(x, y, u.owner))
                        map[x, y] = float.PositiveInfinity;
                }
            }

            markThisTileAndAllNeighbours(x1, y1, 1);

            int posX = x2;
            int posY = y2;
            pth.Add(new Point(posX, posY));

            while (true)
            {
                if (App.debug)
                    Bridge.Script.Call("console.log", $"Running cycle in Path constructor | {posX} | {posY}");
                Point p = getBestNeighbour(posX, posY);
                if (p.x == -1 && p.y == -1)
                    break;
                pth.Add(p);
                posX = p.x;
                posY = p.y;
            }

            pth.Reverse();
        }

        private Point getBestNeighbour(int x, int y)
        {
            if (App.debug)
                Bridge.Script.Call("console.log", $"Get best neighbour | {x} | {y}");
            Dictionary<Point, float> d = new Dictionary<Point, float>();

            for (int i = x - 1; i <= x + 1; i++)
            {
                if (i < 0 || i >= App.tiles)
                    continue;
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (j < 0 || j >= App.tiles)
                        continue;

                    if (map[i, j] != 0)
                        d.Add(new Point(i, j), map[i, j]);
                }
            }

            if (d.Count == 0)
                // No path
                return new Point(-1, -1);

            // Get point with lowest value
            Point lowestValue = d.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            if (lowestValue.x == x && lowestValue.y == y)
                return new Point(-1, -1);
            return lowestValue;
        }

        private List<Point> calledToMark = new List<Point>();
        private void markThisTileAndAllNeighbours(int x, int y, int number)
        {
            if (App.debug)
                Bridge.Script.Call("console.log", $"Mark this tile and all neighbours | {x} | {y} | {number}");
            calledToMark.Add(new Point(x, y));
            if (map[x, y] == 0)
            {
                map[x, y] = number;
                for (int i = x - 1; i <= x + 1; i++)
                {
                    if (i < 0 || i >= App.tiles)
                        continue;
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (j < 0 || j >= App.tiles)
                            continue;

                        if (calledToMark.Where(a => a.x == i && a.y == y).Count() == 0)
                            markThisTileAndAllNeighbours(i, j, number + 1);
                    }
                }
            }
        }
    }
}
