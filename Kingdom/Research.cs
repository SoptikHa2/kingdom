using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    public class Research
    {
        public string name, description;
        public int[] reqResearch;
        public int cost, turnsRemaining, maximumTurnsRemaining;
        public double reqScienceLevel;
        public States state = States.NotResearched;

        public enum States
        {
            NotResearched,
            InProgress,
            Researched
        }

        public enum researches
        {
            Battle_Medicine,
            Archery,
            Storages,
            GeneralResearch_I,
            Architecture,
            Ironsmithing,
            GeneralResearch_II,
            Mathematics,
            AdvancedWarfare,
            AgricultureResearch,
            Gunpowder
        }

        private static Research[] rssrch = new Research[]
        {
            new Research("Battla Medicine", "All your units are healed by additional 1 HP every turn.", new int[0], 4, 2),
            new Research("Archery", "Bow & Arrow, unlocks archers.", new int[0], 4, 2),
            new Research("Storages", "Unlocks building 'Granary'", new int[0], 4, 3),
            new Research("General Research I", "This research is needed to unlock new researches and buildings.", new int[0], 10, 4, 1),

            new Research("Architecture", "Your nation learns how to build better structures, allowing you to build walls.", new int[]{ (int)researches.GeneralResearch_I }, 6, 3),
            new Research("Ironsmithing", "Learn how to work with iron, unlock Heavy Infantry", new int[]{(int)researches.GeneralResearch_I }, 7, 4),
            new Research("General Research II", "Advanced Research, that is needed to unlock new researches and buildings", new int[]{(int)researches.GeneralResearch_I }, 12, 5, 2.5 ),

            new Research("Mathematics", "Mother of science. Unlocks Catapult unit", new int[]{ (int)researches.GeneralResearch_II }, 10, 4),
            new Research("Advanced Warfare", "Researching new warfare techniques unlocks Berserk unit. This powerful melee unit gains back all movement and attack points after kill.", new int[]{ (int)researches.GeneralResearch_II, (int)researches.Ironsmithing, (int)researches.Architecture }, 13, 5),
            new Research("Agriculture Research", "Starts agriculture research, allowing you to build Agriculture centrum, strong economy building.", new int[]{ (int)researches.GeneralResearch_II, (int)researches.Architecture }, 18, 5),

            new Research("Gunpowder", "Brought from China, allows you to build to strongest unit in the game, the Musketeer.", new int[]{ (int)researches.GeneralResearch_II, (int)researches.Ironsmithing, (int)researches.Mathematics }, 30, 10, 6)
        };

        private Research(string name, string description, int[] reqResearch, int cost, int reqTurns, double reqScienceLevel = 0)
        {
            this.name = name;
            this.description = description;
            this.reqResearch = reqResearch;
            this.maximumTurnsRemaining = reqTurns;
            this.cost = cost;
            this.turnsRemaining = reqTurns;
            this.reqScienceLevel = reqScienceLevel;
        }

        public bool StartResearch(Player player)
        {
            bool everythingResearched = reqResearch.Count() == 0;
            if (!everythingResearched)
            {
                foreach (int i in reqResearch)
                {
                    if (player.researches[i].state == States.Researched)
                        everythingResearched = true;
                    else
                    {
                        everythingResearched = false;
                        break;
                    }
                }
            }
            if (everythingResearched && state == States.NotResearched && cost <= player.resources && player.currentlyResearching == null && reqScienceLevel <= player.researchMultiplier)
            {
                player.resources -= cost;
                state = States.InProgress;
                player.currentlyResearching = this;

                /*
                int minusDueToSciencePoints = 0;
                if(reqScienceLevel + 1 < player.researchMultiplier)
                {
                    int percent = (int)(10 * (reqScienceLevel - player.researchMultiplier));
                    minusDueToSciencePoints = maximumTurnsRemaining * percent / 100;
                }
                turnsRemaining -= minusDueToSciencePoints;
                maximumTurnsRemaining -= minusDueToSciencePoints;*/

                App.Announce($"Research {name} started! It'll be finished in {turnsRemaining} turns.");
                return true;
            }
            else
            {
                if (!everythingResearched)
                    App.Announce("You need to research another researches before researching this");
                else if (state != States.NotResearched)
                    if (state == States.InProgress)
                        App.Announce("You are already researching this research");
                    else
                        App.Announce("You have already researched this research");
                else if (player.currentlyResearching != null)
                    App.Announce("You are already researching something");
                else if (reqScienceLevel > player.researchMultiplier)
                    App.Announce("Your science level isn't big enough. Build more science buildings.");
                else
                    App.Announce("You don't have enough resources to start research");
                return false;
            }
        }

        public void OnPlayerTurnBegin(Player player)
        {
            if (state == States.InProgress)
            {
                turnsRemaining--;
                if (turnsRemaining <= 0)
                {
                    state = States.Researched;
                    player.currentlyResearching = null;
                    App.Announce($"{name} research completed! Go to research tab and choose another research.");
                    if(player == Game.game.players[0])
                    {
                        if (name == rssrch[(int)researches.Ironsmithing].name)
                            Achievement.achievements[(int)Achievement.eAchievs.Ironman].isSpecial = false;
                        if (name == rssrch[(int)researches.Gunpowder].name)
                            Achievement.achievements[(int)Achievement.eAchievs.PowerOfGunpowder].isSpecial = false;
                        Achievement.researchesResearched++;
                        Achievement.Check();
                    }
                }
            }
        }

        private static Research Get(int researchPattern)
        {
            Research r = rssrch[(int)researchPattern];
            return new Research(r.name, r.description, r.reqResearch, r.cost, r.turnsRemaining, r.reqScienceLevel);
        }

        public static Research[] Get()
        {
            List<Research> r = new List<Research>();
            for (int i = 0; i < rssrch.Length; i++)
                r.Add(Get(i));

            return r.ToArray();
        }
    }
}
