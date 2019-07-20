using Bridge.Html5;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    /// <summary>
    /// This class is used to store & restore state of game
    /// <see cref="Player"/> list, separator | 
    /// <see cref="Game"/> game settings, except map, separator |
    /// <see cref="MapObject"/> array (game map)
    /// Exceptions:
    /// - <see cref="Player"/> in mapObejcts is saved as number, reffering to player's position in players list
    /// - <see cref="City"/> is reffered by city position in map
    /// - All objects without reference to any other object in map (e.g. to City) is loaded in First Round
    /// - All objects with reference are loaded partly in First Round and the property referring to city is loaded in Second Round
    /// </summary>
    class SaveManager
    {
        private const char basicSeparator = (char)1000;
        private const char insideSeparator = (char)2000;
        private const char listSeparator = (char)3000;

        private enum moTypes
        {
            City,
            RecruitTent,
            Resource,
            Terrain,
            Unit
        }

        public static string Save()
        {
            Stopwatch stop = null;
            if (App.debug)
            {
                stop = new Stopwatch();
                stop.Start();
            }
            int i = 0;

            // Save players
            StringBuilder s = new StringBuilder();
            for (i = 0; i < Game.game.players.Count; i++)
            {
                s.Append(Game.game.players[i].getString());
                if (i != Game.game.players.Count - 1)
                    s.Append(listSeparator);
            }
            s.Append(basicSeparator);

            // Save game
            s.Append(Game.game.getString());
            s.Append(basicSeparator);

            // Save map
            var array = Game.game.Map.OfType<MapObject>().ToArray();
            i = 0; // **** javascript... When using for cycle before, it works. Now, [i] is always 2. Always...
            for (; i < array.Count();)
            {
                MapObject o = array[i];
                moTypes type;

                if (o is City)
                    type = moTypes.City;
                else if (o is RecruitTent)
                    type = moTypes.RecruitTent;
                else if (o is Resource)
                    type = moTypes.Resource;
                else if (o is Terrain)
                    type = moTypes.Terrain;
                else if (o is Unit)
                    type = moTypes.Unit;
                else
                    throw new Exception("Unknown type of MapObject");


                s.Append(((int)type).ToString() + insideSeparator + o.saveString());
                if (i != Game.game.Map.OfType<MapObject>().Count() - 1)
                    s.Append(listSeparator);

                i++;
            }


            if (App.debug)
            {
                stop.Stop();
                Bridge.Script.Call("console.log", "Saving game took ms: " + stop.ElapsedMilliseconds);
                stop = null;
            }

            return s.ToString();
        }

        public static void Load(string save)
        {
            Stopwatch stop = null;
            if (App.debug)
            {
                stop = new Stopwatch();
                stop.Start();
            }

            App.isAnnounceEnabled = false;

            Achievement.Load();

            string[] mainParts = save.Split(basicSeparator);
            Game g = new Game(64, "", 2, false, false);
            g.fromString(mainParts[1]);
            Game.game.players.Clear();

            foreach (string s in mainParts[0].Split(listSeparator))
            {
                Player p = new Player();
                p.fromString(s);
                Game.game.players.Add(p);
            }

            // Load map part
            foreach (string s in mainParts[2].Split(listSeparator))
            {
                string[] a = s.Split(insideSeparator);
                MapObject m = null;
                switch (int.Parse(a[0]))
                {
                    case (int)moTypes.City:
                        m = new City();
                        break;
                    case (int)moTypes.RecruitTent:
                        m = new RecruitTent();
                        break;
                    case (int)moTypes.Resource:
                        m = new Resource();
                        break;
                    case (int)moTypes.Terrain:
                        m = new Terrain();
                        break;
                    case (int)moTypes.Unit:
                        m = new Unit();
                        break;
                }
                m.loadFromString(a[1]);
                Game.game.Map[m.x, m.y, m.z] = m;
            }

            // I hate javascript
            App.Textures tile0_0textureWas = Game.game.Map[0, 0, 0].texture;

            foreach (string s in mainParts[2].Split(listSeparator))
            {
                string[] a = s.Split(insideSeparator);
                MapObject m = null;
                switch (int.Parse(a[0]))
                {
                    case (int)moTypes.City:
                        m = new City();
                        break;
                    case (int)moTypes.RecruitTent:
                        m = new RecruitTent();
                        break;
                    case (int)moTypes.Resource:
                        m = new Resource();
                        break;
                    case (int)moTypes.Terrain:
                        m = new Terrain();
                        break;
                    case (int)moTypes.Unit:
                        m = new Unit();
                        break;
                }
                m.loadFromStringSecondRound(a[1]);

                Game.game.Map[m.x, m.y, m.z] = m;
            }

            // I hate javascript
            Game.game.Map[0, 0, 0].texture = tile0_0textureWas;

            App.isAnnounceEnabled = true;

            if (App.debug)
            {
                stop.Stop();
                Bridge.Script.Call("console.log", "Loading game took ms: " + stop.ElapsedMilliseconds);
                stop = null;
            }
        }
    }
}
