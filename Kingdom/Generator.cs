using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kingdom
{
    class Generator
    {
        private int width, height;
        private Random rnd;
        private List<int[]> allRiverTiles;
        public int[,] map { get; private set; }

        public Generator(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.map = new int[width, height];
            this.rnd = new Random();
            this.allRiverTiles = new List<int[]>();
        }

        public void Generate()
        {
            GenSeasNearEdges(2);
            Smooth(7);
            GenRivers(60, 80, 3);
            SmoothRivers();
            SmoothRiverDiagonals();
            Smooth(3);
        }

        private void Smooth(int n = 1)
        {
            for (int k = 0; k < n; k++)
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        int number = map[i, j];
                        if (getNeighWith(i, j, number) <= 3)
                        {
                            if (getNeighWith(i, j, number - 1) > getNeighWith(i, j, number + 1))
                                map[i, j] = number - 1;
                            else
                                map[i, j] = number + 1;
                        }
                    }
                }
            }
        }

        private int getNeighWith(int x, int y, int target)
        {
            int neighb = 0;
            for (int i = x - 1; i <= x + 1; i++)
            {
                if (i < 0 || i >= width)
                    continue;
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (j < 0 || j >= height)
                        continue;

                    if (map[i, j] == target)
                        neighb++;
                }
            }
            return neighb;
        }

        private int getRiverNeigh(int x, int y)
        {
            int neighb = 0;
            for (int i = x - 1; i <= x + 1; i++)
            {
                if (i < 0 || i >= width)
                    continue;
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (j < 0 || j >= height)
                        continue;

                    if (map[i, j] == 0 && allRiverTiles.Where(a => a[0] == i && a[1] == j).Count() > 0)
                        neighb++;
                }
            }
            return neighb;
        }

        private void GenSeasNearEdges(int pow = 2)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int tilesFromEdge = Math.Min(Math.Min(i, width - i), Math.Min(j, height - j));
                    int chanceToBeOne = (int)Math.Pow(tilesFromEdge, pow);
                    int mult = 0;
                    while (chanceToBeOne >= 100)
                    {
                        mult++;
                        chanceToBeOne -= 100;
                    }
                    if (mult == 0 && chanceToBeOne > 20)
                        chanceToBeOne += 20;
                    map[i, j] = rnd.Next(101) <= chanceToBeOne ? 1 + mult : mult;
                }
            }
        }

        private void GenRivers(int startPointPercentFrom, int startPointPercentTo, int n = 1)
        {
            for (int k = 0; k < n; k++)
            {
                var q = map.OfType<int>();
                int max = q.Max();
                int minPoint = (int)((startPointPercentFrom / 100.0) * max);
                int maxPoint = (int)((startPointPercentTo / 100.0) * max);

                // Choose start point
                int startPoint = rnd.Next(minPoint, maxPoint + 1);
                List<int[]> possibleStartPoints = new List<int[]>();
                int[] rndStartPointCoord = new int[2];
                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                        if (map[i, j] == startPoint)
                            possibleStartPoints.Add(new int[] { i, j });
                if (possibleStartPoints.Count == 0)
                {
                    // TODO: Infinite recursion may occur
                    GenRivers(startPointPercentFrom, startPointPercentTo, n - k);
                    return;
                }
                rndStartPointCoord = possibleStartPoints[rnd.Next(possibleStartPoints.Count)];
                // TODO: Possible infinite recursion may occur
                if (map[rndStartPointCoord[0], rndStartPointCoord[1]] == 0)
                {
                    GenRivers(startPointPercentFrom, startPointPercentTo, n - k);
                    return;
                }

                int lastHeight = map[rndStartPointCoord[0], rndStartPointCoord[1]];
                map[rndStartPointCoord[0], rndStartPointCoord[1]] = 0;
                int power = 1;
                int[] lastCoords = new int[] { rndStartPointCoord[0], rndStartPointCoord[1] };
                List<int[]> riverCoords = new List<int[]>();
                riverCoords.Add(lastCoords);
                // Note: This cycle can be ended prematurely, if the river meets water
                while (power > 0)
                {
                    // Get list of coords of lower neighbours, if there is water in any tile, go there
                    List<int[]> lowestNeighb = new List<int[]>();
                    bool chosen = false;
                    for (int i = lastCoords[0] - 1; i <= lastCoords[0] + 1; i++)
                    {
                        if (i < 0 || i >= width)
                            continue;
                        if (chosen)
                            break;
                        for (int j = lastCoords[1] - 1; j <= lastCoords[1] + 1; j++)
                        {
                            if (i < 0 || i >= height || riverCoords.Where(x => x[0] == i && x[1] == j).Count() > 0)
                                continue;

                            if (map[i, j] == 0)
                            {
                                lowestNeighb.Clear();
                                lowestNeighb.Add(new int[] { i, j });
                                riverCoords.Add(lowestNeighb[0]);
                                allRiverTiles.Add(lowestNeighb[0]);
                                chosen = true;
                                break;
                            }
                            else if (map[i, j] <= lastHeight)
                            {
                                lowestNeighb.Add(new int[] { i, j });
                                riverCoords.Add(lowestNeighb.Last());
                                allRiverTiles.Add(lowestNeighb.Last());
                            }
                        }
                    }

                    // If there was nothing found, create lake
                    if (lowestNeighb.Count == 0)
                    {
                        // Increase lowest height by 1, decrease power
                        lastHeight++;
                        power--;
                    }
                    else
                    {
                        int minHeight = lowestNeighb.Select(x => map[x[0], x[1]]).Min();
                        var coords = lowestNeighb.Where(x => map[x[0], x[1]] == minHeight).ToArray();
                        int[] coord = coords[rnd.Next(coords.Length)];
                        lastHeight = minHeight;
                        map[coord[0], coord[1]] = 0;
                        power++;
                        lastCoords = new int[] { coord[0], coord[1] };
                        if (minHeight == 0)
                            break;
                    }
                }
            }
        }

        private void SmoothRivers(int n = 1)
        {
            for (int k = 0; k < n; k++)
            {
                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                        if (getRiverNeigh(i, j) >= 4)
                            map[i, j] = 0;
            }
        }

        private void SmoothRiverDiagonals()
        {
            // For each river tiles: If there is diagonal tile and not up/down/right/left , add them there

            foreach (int[] tile in allRiverTiles)
            {
                int x = tile[0];
                int y = tile[1];
                // - - -
                // - # -
                // - - +
                if (x + 1 < width && y + 1 < height &&
                    map[x + 1, y + 1] == 0 &&
                    map[x + 1, y] != 0 && map[x, y + 1] != 0)
                    if (rnd.Next(2) == 1)
                        map[x + 1, y] = 0;
                    else
                        map[x, y + 1] = 0;

                // + - -
                // - # -
                // - - -
                if (x - 1 >= 0 && y - 1 >= 0 &&
                    map[x - 1, y - 1] == 0 &&
                    map[x - 1, y] != 0 && map[x, y - 1] != 0)
                    if (rnd.Next(2) == 1)
                        map[x - 1, y] = 0;
                    else
                        map[x, y - 1] = 0;

                // - - +
                // - # -
                // - - -
                if (x + 1 < width && y - 1 >= 0 &&
                    map[x + 1, y - 1] == 0 &&
                    map[x + 1, y] != 0 && map[x, y - 1] != 0)
                    if (rnd.Next(2) == 1)
                        map[x + 1, y] = 0;
                    else
                        map[x, y + 1] = 0;

                // - - -
                // - # -
                // + - -
                if (x - 1 >= 0 && y + 1 < height &&
                    map[x - 1, y + 1] == 0 &&
                    map[x - 1, y] != 0 && map[x, y + 1] != 0)
                    if (rnd.Next(2) == 1)
                        map[x - 1, y] = 0;
                    else
                        map[x, y + 1] = 0;
            }
        }
    }
}
