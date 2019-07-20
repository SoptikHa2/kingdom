using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    public class Player
    {
        public const char separator = (char)10000;
        public const char researchedSeparator = (char)15000;
        public const char listSeparator = (char)18000;

        public int resources;
        /// <summary>
        /// 0=blue, 1=red, 2=green, 3=yellow
        /// </summary>
        public int color;
        public string name = "Unknown Player";
        public bool end = false;
        public Research[] researches = Research.Get();
        public Research currentlyResearching;
        public double researchMultiplier;
        public baseAI currAI;

        public string getString()
        {
            string s = $"{resources}{separator}{color}{separator}{name}{separator}{currAI != null}{separator}{end}{separator}{getCurrResearchId()}{separator}{researchMultiplier}{separator}{(currentlyResearching != null ? currentlyResearching.turnsRemaining.ToString() : "")}{separator}";
            for (int i = 0; i < researches.Length; i++)
            {
                s += (int)researches[i].state;
                if (i != researches.Length - 1)
                    s += researchedSeparator;
            }
            return s;
        }

        public void fromString(string data)
        {
            string[] s = data.Split(separator);
            resources = int.Parse(s[0]);
            color = int.Parse(s[1]);
            name = s[2];
            bool AI = s[3] == "True";
            end = s[4] == "True";
            currentlyResearching = getCurrResearch(int.Parse(s[5]));
            if (currentlyResearching != null)
            {
                currentlyResearching.state = Research.States.InProgress;
                currentlyResearching.turnsRemaining = int.Parse(s[7]);
            }
            researchMultiplier = double.Parse(s[6]);
            string[] a = s[8].Split(researchedSeparator);
            for (int i = 0; i < researches.Count(); i++)
            {
                researches[i].state = (Research.States)int.Parse(a[i]);
            }
            if (AI)
                currAI = new AIsoptik(this);
        }

        private int id = -1;
        public void revealTilesNear(int x, int y, int around = 2)
        {
            if (id == -1)
                for (int i = 0; i < Game.game.players.Count; i++)
                {
                    if (Game.game.players[i] == this)
                    {
                        id = i;
                        break;
                    }
                }

            for (int i = x - around; i <= x + around; i++)
            {
                if (i < 0 || i >= App.tiles)
                    continue;
                for (int j = y - around; j <= y + around; j++)
                {
                    if (j < 0 || j >= App.tiles)
                        continue;

                    Terrain t = (Game.game.Map[i, j, 0] as Terrain);
                    t.isFogForPlayers[id] = false;
                }
            }
        }
        private int getCurrResearchId()
        {
            int id = -1;
            int i = 0;
            foreach (Research b in researches)
            {
                if (b == currentlyResearching)
                {
                    id = i;
                    break;
                }
                i++;
            }
            return id;
        }

        private Research getCurrResearch(int id)
        {
            if (id == -1)
                return null;
            return researches[id];
        }
    }
}
