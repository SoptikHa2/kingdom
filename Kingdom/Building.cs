using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    public class Building
    {
        public int[] researchReq;
        public double researchMultiplier;
        public int cost, turnsRemaining, maxTurnsRemaining;
        public string name, description;
        public int bonusProsperityEachTurn, bonusProsperityFirstTurn, bonusOutputEachTurn;
        public States state = States.NotBuild;

        private Building(string name, string description, int[] researchReq, int cost, int turnsRemaining, int maxTurnsRemaining, double researchMultiplier, int bonusProsperityFirstTurn, int bonusProsperityEachTurn, int bonusOutputEachTurn)
        {
            this.researchReq = researchReq;
            this.researchMultiplier = researchMultiplier;
            this.cost = cost;
            this.turnsRemaining = turnsRemaining;
            this.maxTurnsRemaining = maxTurnsRemaining;
            this.name = name;
            this.description = description;
            this.bonusProsperityEachTurn = bonusProsperityEachTurn;
            this.bonusProsperityFirstTurn = bonusProsperityFirstTurn;
            this.bonusOutputEachTurn = bonusOutputEachTurn;
        }

        public enum States
        {
            NotBuild,
            InProgress,
            Builded
        }

        public enum EBuildings
        {
            Church,
            Barracks,
            Granary,
            School,
            Walls,
            Marketplace,
            University,
            AgrCentrum
        }

        public static Building[] buildings = new Building[]
        {
            new Building("Church", "In the middleage, churches were centers of education. This building increases science level, unlocking powerful researches.", new int[0], 4, 3, 3, 0.5, 0, 0, 0),
            new Building("Barracks", "This building allows you to train better soldiers, increasing their HP by 5", new int[0], 5, 3, 3, 0, 0, 0, 0),
            new Building("Granary", "Granary increases prosperity of city by number of all resources near city.", new int[]{ (int)Research.researches.Storages }, 11, 5, 5, 0, 0, 0, 0),

            new Building("School", "School is the second scientific building, that is avalibe to build. Increases your science level by 0.8", new int[]{ (int)Research.researches.GeneralResearch_I },9, 4, 4, 0.8, 0, 0, 0),
            new Building("Walls", "As your civilization grows, new threats appears. Build walls to support defending units in this city.", new int[]{ (int)Research.researches.GeneralResearch_I, (int)Research.researches.Architecture }, 5, 4, 4, 0, 0, 0, 0),
            new Building("Marketplace", "Marketplace can be built only in the biggest cities in your empire, making their prosperity and output even bigger!", new int[]{ (int)Research.researches.GeneralResearch_I, (int)Research.researches.Architecture }, 15, 5, 5, 0, 2, 0, 1),

            new Building("University", "The best science building, gives your city prosperity points and increases your science level greatly. If you have more than 1 university, you gain additional 0.1 science level per university per turn", new int[]{ (int)Research.researches.GeneralResearch_II, (int)Research.researches.Architecture }, 18, 5, 5, 1, 4, 0, 0),

            // TODO: Test for negative bonus
            new Building("Agriculture Centrum", "When your city grows, you need to feed your people. Agriculture center lowers your city resources output, but increases your prosperity over turn.", new int[]{ (int)Research.researches.GeneralResearch_II, (int)Research.researches.Architecture, (int)Research.researches.AgricultureResearch }, 24, 7, 7, 0, 0, 1, -2)
        };


        private static Building Get(Building pattern)
        {
            return new Building(pattern.name, pattern.description, pattern.researchReq, pattern.cost, pattern.turnsRemaining, pattern.maxTurnsRemaining, pattern.researchMultiplier, pattern.bonusProsperityFirstTurn, pattern.bonusProsperityEachTurn, pattern.bonusOutputEachTurn);
        }

        public static Building[] Get()
        {
            List<Building> l = new List<Building>();
            for (int i = 0; i < buildings.Length; i++)
                l.Add(Get(buildings[i]));
            return l.ToArray();
        }

        public bool StartBuilding(City c)
        {
            Player p = c.owner;

            bool everythingResearched = researchReq.Count() == 0;
            if (!everythingResearched)
            {
                foreach (int i in researchReq)
                {
                    if (p.researches[i].state == Research.States.Researched)
                        everythingResearched = true;
                    else
                    {
                        everythingResearched = false;
                        break;
                    }
                }
            }

            if (state == States.NotBuild && p.resources >= cost && c.currentlyBuilding == null && everythingResearched)
            {
                p.resources -= cost;
                state = States.InProgress;
                c.currentlyBuilding = this;
                App.Announce($"{name} is now building in one of your cities, it'll be done in {turnsRemaining} turns");
                return true;
            }
            else
            {
                if (state == States.Builded)
                    App.Announce("This building is already built");
                else if (state == States.InProgress)
                    App.Announce("You are already building this building");
                else if (c.currentlyBuilding != null)
                    App.Announce("You are already building something");
                else if (!everythingResearched)
                    App.Announce("There are some researches, you have to do before building " + name);
                else if (p.resources < cost)
                    App.Announce("You don't have enough resources");
                else
                    App.Announce("You cannot build this building");
                return false;
            }
        }

        public void OnPlayerTurnBegin(City c)
        {
            Player p = c.owner;
            if (state == States.InProgress)
            {
                turnsRemaining--;
                if (turnsRemaining == 0)
                {
                    App.Announce($"{name} has been built!");
                    c.currentlyBuilding = null;
                    state = States.Builded;
                    c.Currpop += bonusProsperityFirstTurn;

                    if(name == "Granary")
                    {
                        // Add prosperity equal to number of resources connected to this city
                        int res = Game.game.Map.OfType<MapObject>().Where(x => x is Resource).Select(x => x as Resource).Where(x => x.cityOwner == c).Count();
                        c.Currpop += res;
                        App.Announce("Granary added " + res + " prosperity to one of your cities");
                    }
                }
            }
            else if(state == States.Builded)
            {
                p.resources += bonusOutputEachTurn;
                c.Currpop += bonusProsperityEachTurn;
            }
        }
    }
}
