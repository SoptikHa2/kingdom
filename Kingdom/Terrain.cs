using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    class Terrain : MapObject
    {
        public int type;
        public bool[] isFogForPlayers;
        private const char fogSeparator = (char)10002;

        public enum Types
        {
            Land, Sea, Mountain
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">Type of terain, see enum [Types]</param>
        public Terrain(int type, App.Textures texture, int x, int y, int z, bool makeFog, int numberOfPlayers)
        {
            this.type = type;
            this.texture = texture;
            this.x = x;
            this.y = y;
            this.z = z;
            List<bool> fog = new List<bool>();
            for (int i = 0; i < numberOfPlayers; i++)
                fog.Add(makeFog);
            isFogForPlayers = fog.ToArray();
        }

        public Terrain() { }

        public override void NextTurn() { }

        public override string saveString()
        {
            string s = $"{type}{separator}{(int)texture}{separator}{x}{separator}{y}{separator}{z}{separator}";
            for (int i = 0; i < isFogForPlayers.Length; i++)
            {
                s += isFogForPlayers[i];
                if (i != isFogForPlayers.Length - 1)
                    s += fogSeparator;
            }
            return s;
        }

        /// <summary>
        /// <see cref="separator"/> = (char)1000 ;;;;;;; type:texture:x:y:z 
        /// </summary>
        /// <param name="save"></param>
        public override void loadFromString(string save)
        {
            string[] saves = save.Split(separator);

            type = int.Parse(saves[0]);
            texture = (App.Textures)int.Parse(saves[1]);
            x = int.Parse(saves[2]);
            y = int.Parse(saves[3]);
            z = int.Parse(saves[4]);

            string[] fog = saves[5].Split(fogSeparator);
            List<bool> mf = new List<bool>();
            foreach(string s in fog)
            {
                mf.Add(s == "True");
            }
            isFogForPlayers = mf.ToArray();
        }

        public override void loadFromStringSecondRound(string save)
        {
        }
    }
}
