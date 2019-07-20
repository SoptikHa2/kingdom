using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    public abstract class MapObject
    {
        public const char separator = (char)10000;
        public App.Textures texture;
        public int x, y, z;

        public abstract void NextTurn();

        public abstract void loadFromString(string save);

        public abstract void loadFromStringSecondRound(string save);

        public abstract string saveString();

    }
}
