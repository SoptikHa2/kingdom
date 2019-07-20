using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    class Game
    {
        public const char separator = (char)10000;

        public int length;
        public List<Player> players = new List<Player>();
        private int _playerState;
        public int playerState { get { return _playerState; } set { if (value < 0) _playerState = 0; else if (value >= players.Count) _playerState = 0; else _playerState = value; } }

        public static Game game;

        /// <summary>
        /// X => width of map, Y => height of map, Z => 0 (land, water, mountain) => 1 (forest, wheat, iron, ...) => 2 (player units)
        /// </summary>
        public MapObject[,,] Map;

        public Game(int length = 64, string save = "", int numberOfPlayers = 2, bool playAI = false, bool genMapIfNewGame = true)
        {
            if (save == "")
            {
                // New game
                this.length = length;
                rnd = new Random();
                game = this;
                for (int i = 0; i < numberOfPlayers; i++)
                {
                    Player p = new Player() { color = i, name = "Player " + (i + 1) };
                    p.currAI = i != 0 && playAI ? new AIsoptik(p) : null;
                    players.Add(p);
                }

                if (genMapIfNewGame)
                    Map = GenerateMap(length);
            }
            else
            {
                try
                {
                    game = this;
                    SaveManager.Load(save);
                }
                catch (Exception ex)
                {
                    App.isAnnounceEnabled = true;
                    Bridge.Script.Call("console.log", ex);
                    App.Announce("Unable to load game, save is corrupted. Generating new game...");
                    Bridge.Html5.Window.LocalStorage["game_continue"] = "";

                    this.length = length;
                    rnd = new Random();
                    game = this;
                    for (int i = 0; i < numberOfPlayers; i++)
                    {
                        Player p = new Player() { color = i, name = "Player " + (i + 1) };
                        p.currAI = i != 0 && playAI ? new AIsoptik(p) : null;
                        players.Add(p);
                    }
                    Map = GenerateMap(length);
                    //new Game(length, "", numberOfPlayers, playAI);
                }
            }
        }

        private Random rnd;
        private MapObject[,,] GenerateMap(int length)
        {
            Stopwatch s = new Stopwatch();
            if (App.debug)
                s.Start();
            MapObject[,,] map = new MapObject[length, length, 3];
            Generator gen = new Generator(length, length);
            gen.Generate();
            int[,] heightMap = /*new HeightMapGen(length, length).map*/gen.map;
            game.Map = map;

            // 1) Fill tiles with land, sea, or mountains
            int max = heightMap.OfType<int>().Max();
            int mountainMin = (int)(max * 0.95);
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (heightMap[i, j] <= 0)
                        map[i, j, 0] = new Terrain((int)Terrain.Types.Sea, App.Textures.terrain_sea, i, j, 0, App.useFog, players.Count);
                    else if (heightMap[i, j] >= mountainMin && heightMap[i, j] >= 4)
                        map[i, j, 0] = new Terrain((int)Terrain.Types.Mountain, App.Textures.terrain_mountain, i, j, 0, App.useFog, players.Count);
                    else
                        map[i, j, 0] = new Terrain((int)Terrain.Types.Land, App.Textures.terrain_land, i, j, 0, App.useFog, players.Count);
                }
            }

            // 2) Add some cities
            #region PlayerCities
            List<Point> cities = new List<Point>();
            // As player 1
            int x = 0;
            int y = 0;
            while (true)
            {
                x++;
                y++;

                // TODO: Pridat odchylku (odchylka: x => Math.Sqrt(x); ) (tedy nevyhledavat v 1ny diagonale, ale i kousek vedle)

                if (x >= length || y >= length)
                    return GenerateMap(length);

                bool a = true;
                for (int i = x - 1; i <= x + 1; i++)
                {
                    if (!a)
                        break;
                    if (i < 0 || i >= length)
                        continue;
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (j < 0 || j >= length)
                            continue;

                        if ((map[i, j, 0] as Terrain).type != (int)Terrain.Types.Land)
                        {
                            a = false;
                            break;
                        }
                    }
                }

                if (a)
                {
                    cities.Add(new Point(x, y));
                    map[x, y, 1] = new City(players[0], App.Textures.city_blue, "Blue Home City", x, y, 1);
                    map[x, y, 2] = Unit.getUnit(players[0], App.Textures.unit_basic_blue, Unit.units[(int)Unit.eUnits.Warrior], x, y);
                    (map[x, y, 2] as Unit).turns = (map[x, y, 2] as Unit).maxturns;
                    players[0].revealTilesNear(x, y, 4);
                    break;
                }
            }
            // As player 2
            x = length - 1;
            y = length - 1;
            while (true)
            {
                x--;
                y--;

                if (x >= length || y >= length)
                    return GenerateMap(length);

                bool a = true;
                for (int i = x - 1; i <= x + 1; i++)
                {
                    if (!a)
                        break;
                    if (i < 0 || i >= length)
                        continue;
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (j < 0 || j >= length)
                            continue;

                        if ((map[i, j, 0] as Terrain).type != (int)Terrain.Types.Land)
                        {
                            a = false;
                            break;
                        }
                    }
                }

                if (a)
                {
                    cities.Add(new Point(x, y));
                    map[x, y, 1] = new City(players[1], App.Textures.city_red, "Red Home City", x, y, 1);
                    map[x, y, 2] = Unit.getUnit(players[1], App.Textures.unit_basic_red, Unit.units[(int)Unit.eUnits.Warrior], x, y);
                    (map[x, y, 2] as Unit).turns = (map[x, y, 2] as Unit).maxturns;
                    players[1].revealTilesNear(x, y, 4);
                    break;
                }
            }
            if (players.Count > 2)
            {
                // As player 3
                x = 0;
                y = length - 1;
                while (true)
                {
                    x++;
                    y--;

                    if (x >= length || y >= length)
                        return GenerateMap(length);

                    bool a = true;
                    for (int i = x - 1; i <= x + 1; i++)
                    {
                        if (!a)
                            break;
                        if (i < 0 || i >= length)
                            continue;
                        for (int j = y - 1; j <= y + 1; j++)
                        {
                            if (j < 0 || j >= length)
                                continue;

                            if ((map[i, j, 0] as Terrain).type != (int)Terrain.Types.Land)
                            {
                                a = false;
                                break;
                            }
                        }
                    }

                    if (a)
                    {
                        cities.Add(new Point(x, y));
                        map[x, y, 1] = new City(players[2], App.Textures.city_green, "Green Home City", x, y, 1);
                        map[x, y, 2] = Unit.getUnit(players[2], App.Textures.unit_basic_green, Unit.units[(int)Unit.eUnits.Warrior], x, y);
                        (map[x, y, 2] as Unit).turns = (map[x, y, 2] as Unit).maxturns;
                        players[2].revealTilesNear(x, y, 4);
                        break;
                    }
                }
                if (players.Count > 3)
                {
                    // As player 4
                    x = length - 1;
                    y = 0;
                    while (true)
                    {
                        x--;
                        y++;

                        if (x >= length || y >= length)
                            return GenerateMap(length);

                        bool a = true;
                        for (int i = x - 1; i <= x + 1; i++)
                        {
                            if (!a)
                                break;
                            if (i < 0 || i >= length)
                                continue;
                            for (int j = y - 1; j <= y + 1; j++)
                            {
                                if (j < 0 || j >= length)
                                    continue;

                                if ((map[i, j, 0] as Terrain).type != (int)Terrain.Types.Land)
                                {
                                    a = false;
                                    break;
                                }
                            }
                        }

                        if (a)
                        {
                            cities.Add(new Point(x, y));
                            map[x, y, 1] = new City(players[3], App.Textures.city_yellow, "Yellow Home City", x, y, 1);
                            map[x, y, 2] = Unit.getUnit(players[3], App.Textures.unit_basic_yellow, Unit.units[(int)Unit.eUnits.Warrior], x, y);
                            (map[x, y, 2] as Unit).turns = (map[x, y, 2] as Unit).maxturns;
                            players[3].revealTilesNear(x, y, 4);
                            break;
                        }
                    }
                }
            }

            // Add neutral cities
            int count = cities.Count;
            for (int o = 0; o < count; o++)
            {
                Point p = cities[o];

                int numberOfNeutralCities = length / 2 - 4;
                for (int i = 0; i < numberOfNeutralCities; i += 4)
                {
                    List<Point> coords = new List<Point>();

                    int l = 4 + i;

                    for (int f = p.x - l; f <= p.x + l; f++)
                    {
                        if (f < 0 || f >= length)
                            continue;
                        for (int g = p.y - l; g <= p.y + l; g++)
                        {
                            if (g < 0 || g >= length)
                                continue;

                            if (!(f == p.x + l || g == p.y + l || f == p.x - l || g == p.y - l))
                                continue;

                            if ((map[f, g, 0] as Terrain).type == (int)Terrain.Types.Land && map[f, g, 1] == null)
                                coords.Add(new Point(f, g));
                        }
                    }

                    if (coords.Count >= 1)
                    {
                        Point c = coords[rnd.Next(coords.Count)];

                        map[c.x, c.y, 1] = new City(null, App.Textures.city_neutral, "Neutral City", c.x, c.y, 1);
                        cities.Add(c);
                    }
                }
            }
            #endregion
            // 3) Add resources
            count = cities.Count;
            for (int i = 0; i < count; i++)
            {
                Point city = cities[i];
                List<Point> resources = new List<Point>();

                for (int cx = city.x - 2; cx <= city.x + 2; cx++)
                {
                    if (cx < 0 || cx >= length)
                        continue;
                    for (int cy = city.y - 2; cy <= city.y + 2; cy++)
                    {
                        if (cy < 0 || cy >= length || (cy == city.y && cx == city.x))
                            continue;

                        if (map[cx, cy, 1] == null)
                            resources.Add(new Point(cx, cy));
                    }
                }

                int k = 5;
                int tr = 0;

                while (k > 0)
                {
                    if (tr >= 10)
                        break;

                    if (resources.Count < 1)
                        break;

                    int q = rnd.Next(resources.Count);

                    int typeOfResource = -1;
                    Resource r = null;

                    Terrain t = map[resources[q].x, resources[q].y, 0] as Terrain;

                    if (t.type == (int)Terrain.Types.Land)
                    {
                        typeOfResource = rnd.Next(3);
                    }
                    else if (t.type == (int)Terrain.Types.Mountain)
                    {
                        typeOfResource = 1;
                    }
                    else if (t.type == (int)Terrain.Types.Sea)
                    {
                        typeOfResource = 3;
                    }
                    switch (typeOfResource)
                    {
                        case 0:
                            r = Resource.getResource(map[city.x, city.y, 1] as City, Resource.resources[(int)Resource.eResources.FishDeerBush], App.Textures.resource_bush, App.Textures.unavalibeTile, resources[q].x, resources[q].y);
                            break;
                        case 1:
                            r = Resource.getResource(map[city.x, city.y, 1] as City, Resource.resources[(int)Resource.eResources.FishDeerBush], App.Textures.resource_deer, App.Textures.unavalibeTile, resources[q].x, resources[q].y);
                            break;
                        case 2:
                            r = Resource.getResource(map[city.x, city.y, 1] as City, Resource.resources[(int)Resource.eResources.Farm], App.Textures.resource_farm, App.Textures.resource_farm_done, resources[q].x, resources[q].y);
                            break;
                        case 3:
                            r = Resource.getResource(map[city.x, city.y, 1] as City, Resource.resources[(int)Resource.eResources.FishDeerBush], App.Textures.resource_fish, App.Textures.unavalibeTile, resources[q].x, resources[q].y);
                            break;
                    }

                    map[resources[q].x, resources[q].y, 1] = r;

                    if (r != null)
                    {
                        k -= r.firstTurn_productionBoost;
                        tr++;
                    }
                }
            }

            if (App.debug)
            {
                s.Stop();
                Bridge.Script.Call("console.log", "Map generated in " + s.ElapsedMilliseconds + " ms");
            }
            return map;
        }

        /// <summary>
        /// Process 'NextTurn' for player, who's turn is ending. Automatically set playerState to next player.
        /// </summary>
        /// <param name="player">Player who's turn is ending</param>
        public void NextTurn(Player player)
        {
            if (App.debug)
                Bridge.Script.Call("console.log", $"Processing NextTurn for player {player.color}");
            var query = Map.OfType<MapObject>().Where(x => x is OwnerObject).Select(x => (x as OwnerObject));
            query.Where(x => x.owner == player || x.owner == null || x is City).ForEach(x => x.NextTurn());
            // University bonus
            int universities = 0;
            Map.OfType<MapObject>().Where(x => x is City).Select(x => x as City).Where(x => x.owner == player).ForEach(x => universities += x.buildings[(int)Building.EBuildings.University].state == Building.States.Builded ? 1 : 0);
            if (universities >= 2)
                player.researchMultiplier += universities * 0.1;
            playerState++;
            Player playerWhosTurnIsStarting = players[playerState];
            playerWhosTurnIsStarting.researchMultiplier = 0;
            // Research
            playerWhosTurnIsStarting.researches.ForEach(x => x.OnPlayerTurnBegin(players[playerState]));
            // Building
            query.Where(x => x.owner == playerWhosTurnIsStarting && x is City)
                .Select(x => x as City).ForEach(x => x.buildings.ForEach(a => a.OnPlayerTurnBegin(x)));
            // Research point
            query.Where(x => x.owner == playerWhosTurnIsStarting && x is City)
                .Select(x => x as City).ForEach(x => x.buildings.Where(a => a.state == Building.States.Builded)
                .ForEach(a => playerWhosTurnIsStarting.researchMultiplier += a.researchMultiplier));

            /*
             -- LINQ WITHOUT LAMBDA -- (except .ForEach function :P)
            (from a in
                 from x in Map.OfType<MapObject>()
                 where x is OwnerObject
                 select x as OwnerObject
             where a.owner == playerWhosTurnIsStarting && a is City
             select a as City).ForEach(x =>
                              (from k in x.buildings
                               where k.Value != 0
                               select k.Key).ForEach(a => playerWhosTurnIsStarting.researchMultiplier += a.researchMultiplier);


              -- WITHOUT LINQ --
            foreach (MapObject mo in Map)
            {
                if (mo is OwnerObject)
                {
                    OwnerObject oo = mo as OwnerObject;
                    if (oo.owner == playerWhosTurnIsStarting && oo is City)
                    {
                        City c = oo as City;
                        foreach (var kvp in c.buildings)
                        {
                            if (kvp.Value != 0)
                            {
                                Building b = kvp.Key;
                                playerWhosTurnIsStarting.researchMultiplier += b.researchMultiplier;
                            }
                        }
                    }
                }
            }

           */
        }

        public static int getPlayerId(Player p)
        {
            return game.players.IndexOf(p);
        }

        public string getString()
        {
            return $"{length}{separator}{_playerState}{separator}{App.useFog}";
        }

        public void fromString(string data)
        {
            string[] s = data.Split(separator);
            length = int.Parse(s[0]);
            _playerState = int.Parse(s[1]);
            App.tiles = length;
            rnd = new Random();
            Map = new MapObject[length, length, 3];
            App.useFog = s[2] == "True";
        }
    }
}