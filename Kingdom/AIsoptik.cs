using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    class AIsoptik : baseAI
    {
        public AIsoptik(Player player)
        {
            this.me = player;
        }

        public override void Play()
        {
            if (App.debug)
            {
                Bridge.Script.Call("console.log", "AI turn begin. Player:");
                Bridge.Script.Call("console.log", me);
            }
            var query = Game.game.Map.OfType<MapObject>();
            var units = query.Where(x => x is Unit).Select(x => x as Unit).Where(x => x.owner == me);
            units.ForEach(x => PlayUnit(x));
            var cities = query.Where(x => x is City).Select(x => x as City).Where(x => x.owner == me || x.CanCapture());
            // TODO: Force recruit in recruit tents
            // TODO: Calc how much money can I use in recruit, build, research
            cities.ForEach(x => RecruitCity(x));
            cities.ForEach(x => BuildCity(x));
            Research();
            // TODO: Build recruit tents if I have more money than I need
        }

        private List<City> BannedCities = new List<City>();
        private void PlayUnit(Unit unit)
        {
            MapObject[,,] map = Game.game.Map;
            int x = unit.x;
            int y = unit.y;
            if (App.debug)
                Bridge.Script.Call("console.log", unit);

            // TODO: Special strategy for berserks

            // Priority 1
            //       If city is not captured, capture. If cannot capture this turn, wait
            if (map[x, y, 1] is City)
            {
                City city = map[x, y, 1] as City;
                if (city.CanCapture())
                {
                    city.Capture();
                    return;
                }
                else if (city.owner != unit.owner && !city.readyToCapture)
                {
                    return;
                }
            }


            // Priority 2
            //       If in range of city || (in city range more near then enemy && not enemy city), go to city
            var nearestNonOccupiedNotYourCities = map.OfType<MapObject>().Where(a => a is City).Select(a => a as City)
                .Where(a => a.owner != unit.owner && map[a.x, a.y, 2] == null).OrderBy(a => Math.Abs(unit.x - a.x) + Math.Abs(unit.y - a.y)).ToArray();

            City c = null;
            int i = 0;
            bool done = false;
            // TODO: Add cities into banned cities list only if I can never reach it
            
            do
            {
                if (i >= nearestNonOccupiedNotYourCities.Count())
                    c = null;
                else
                    c = nearestNonOccupiedNotYourCities[i];
                i++;
                if (BannedCities.Contains(c) || c == null)
                    continue;
                done = TryToMoveToNearestCity(unit, c);
                if (!done)
                    BannedCities.Add(c);
            } while (!done);
            if (done)
                return;
            // Priority 3
            //       If in range of enemy recruit tent, go there 
            //       TODO: the strongest unit, same for capturing city when can enemy go there


            // Priority 4
            //       Go to nearest enemy/city
        }

        private bool TryToMoveToNearestCity(Unit unit, City city)
        {
            // TODO: Pathfinding
            if (unit.canMove(city.x, city.y, unit.owner))
            {
                return unit.Move(city.x, city.y, unit.owner);
            }
            else
            {
                // TODO: Test for city, that I'll never reach
                int turns = unit.turns;
                int tx = unit.x;
                int ty = unit.y;
                while (turns > 0)
                {
                    if (tx < city.x && unit.canMove(tx + 1, ty, unit.owner))
                        tx++;
                    else if (tx > city.x && unit.canMove(tx - 1, ty, unit.owner))
                        tx--;
                    if (ty < city.y && unit.canMove(tx, ty + 1, unit.owner))
                        ty++;
                    else if (ty > city.y && unit.canMove(tx, ty - 1, unit.owner))
                        ty--;
                    turns--;
                }
                return unit.Move(tx, ty, unit.owner);
            }
        }

        private void RecruitCity(City c)
        {
            if (App.debug)
                Bridge.Script.Call("console.log", c);

            // TODO: How much can I use?

            // Priority 1
            // Recruit best unit: warrior -> archer -> hinf -> musketeer
            // Always have at best at least one berserk
        }

        private void BuildCity(City c)
        {
            if (App.debug)
                Bridge.Script.Call("console.log", c);

            // If not enough science level, build science buildings
            // If in danger, build walls (if possible)
            // Else, build economy buildings
        }

        private void Research()
        {
            if (App.debug)
                Bridge.Script.Call("console.log", "Starting research: none");

            // Research in some order... If possible research general researches, gunpowder, ecnonomy, ...
        }
    }
}
