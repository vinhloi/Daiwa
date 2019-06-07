using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Drawing;

namespace Daiwa
{
    public class Location
    {
        public int X;
        public int Y;
        public int F;
        public int G;
        public int H;
        public Location Parent;

        public Location(Point square)
        {
            X = square.X;
            Y = square.Y;
        }

        public Location(int x, int y)
        {
            X = x;
            Y = y;
        }

    }

    public class AStarPathfinding
    {
        public Stack<Point> A_StarFindPath(Point startPoint, Point endPoint)
        {
            Location current = null;
            var start = new Location(startPoint);
            var goal = new Location(endPoint);
            var openList = new List<Location>();
            var closedList = new List<Location>();
            int g = 0;

            // start by adding the original position to the open list
            openList.Add(start);

            while (openList.Count > 0)
            {
                // get the square with the lowest F score
                var lowest = openList.Min(l => l.F);
                current = openList.First(l => l.F == lowest);

                // if current = goal, we reach the destination
                if (current.X == goal.X && current.Y == goal.Y)
                {
                    return ReconstructPath(current);
                }

                openList.Remove(current);
                closedList.Add(current);

                var adjacentSquares = GetWalkableAdjacentSquares(current);
                g++;

                foreach (var adjacentSquare in adjacentSquares)
                {
                    // if this adjacent square is already in the closed list, ignore it
                    if (closedList.FirstOrDefault(l => l.X == adjacentSquare.X
                            && l.Y == adjacentSquare.Y) != null)
                        continue;

                    // if it's not in the open list...
                    if (openList.FirstOrDefault(l => l.X == adjacentSquare.X
                            && l.Y == adjacentSquare.Y) == null)
                    {
                        // compute its score, set the parent
                        adjacentSquare.G = g;
                        adjacentSquare.H = ComputeHScore(adjacentSquare.X, adjacentSquare.Y, goal.X, goal.Y);
                        adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                        adjacentSquare.Parent = current;

                        // and add it to the open list
                        openList.Insert(0, adjacentSquare);
                    }
                    else
                    {
                        // test if using the current G score makes the adjacent square's F score
                        // lower, if yes update the parent because it means it's a better path
                        if (g + adjacentSquare.H < adjacentSquare.F)
                        {
                            adjacentSquare.G = g;
                            adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                            adjacentSquare.Parent = current;
                        }
                    }
                }
            }

            // We can't find the path
            Program.Print("Error: can not find the path from " + startPoint.ToString() + " to " + endPoint.ToString());
            return null;
        }

        private List<Location> GetWalkableAdjacentSquares(Location currentSquare)
        {
            int x = currentSquare.X;
            int y = currentSquare.Y;

            var proposedLocations = new List<Location>()
            {
                new Location ( x, y - 1 ),
                new Location ( x, y + 1 ),
                new Location ( x - 1, y ),
                new Location ( x + 1, y ),
            };

            // retur Adjacent Squares which are moveable (value = 0)
            return proposedLocations.Where(l => Warehouse.Map[l.Y, l.X] == 0).ToList();
        }

        private Stack<Point> ReconstructPath(Location current)
        {
            Stack<Point> total_path = new Stack<Point>();

            while (current != null)
            {
                total_path.Push(new Point(current.X, current.Y));
                current = current.Parent;
            }

            return total_path;
        }

        public int ComputeHScore(int x, int y, int targetX, int targetY)
        {
            return Math.Abs(targetX - x) + Math.Abs(targetY - y);
        }
    }
}
