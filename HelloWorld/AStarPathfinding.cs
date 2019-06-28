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

    public static class AStarPathfinding
    {
        public static Stack<Point> FindPath(Point startPoint, Point endPoint, out bool noPath)
        {
            noPath = false;

            var start = new Location(startPoint);
            var goal = new Location(endPoint);
            var openList = new List<Location>();
            var closedList = new List<Location>();
            start.G = 0;

            // start by adding the original position to the open list
            openList.Add(start);

            while (openList.Count > 0)
            {
                Location current = null;

                // get the square with the lowest F score
                foreach (Location loc in openList)
                {
                    if (current == null || current.F > loc.F)
                        current = loc;
                }

                // if current = goal, we reach the destination
                if (current.X == goal.X && current.Y == goal.Y)
                {
                    return ReconstructPath(current);
                }

                openList.Remove(current);
                closedList.Add(current);

                var adjacentSquares = GetWalkableAdjacentSquares(current, goal);

                foreach (var adjacentSquare in adjacentSquares)
                {
                    // if this adjacent square is already in the closed list, ignore it
                    if (closedList.FirstOrDefault(l => l.X == adjacentSquare.X
                            && l.Y == adjacentSquare.Y) != null)
                        continue;

                    // The distance from start to a neighbor
                    int g = current.G + 1;

                    Location temp = openList.FirstOrDefault(l => l.X == adjacentSquare.X
                            && l.Y == adjacentSquare.Y);
                    if (temp == null) // If it's not in the open list --> Discover a new node
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
                        if (g + temp.H < temp.F)
                        {
                            temp.G = g;
                            temp.F = temp.G + temp.H;
                            temp.Parent = current;
                        }
                    }
                }
            }

            // We can't find the path
            Program.Print("No path from " + startPoint + " to " + endPoint + ":");
            foreach (Robot robot in Warehouse._AllMovingRobots.Values)
                if (robot._state != robot_state.free)
                    Program.Print(robot._location.ToString());

            noPath = true;
            return new Stack<Point>();
        }

        private static List<Location> GetWalkableAdjacentSquares(Location current, Location goal)
        {
            int x = current.X;
            int y = current.Y;

            var proposedLocations = new List<Location>()
            {
                new Location ( x - 1, y ),
                new Location ( x + 1, y ),
                new Location ( x, y - 1 ),
                new Location ( x, y + 1 ),
            };

            // retur Adjacent Squares which are moveable (value = 0)
            return proposedLocations.Where(l => Warehouse.Map[l.Y, l.X] == 0 || (l.X == goal.X && l.Y == goal.Y)).ToList();
        }

        private static Stack<Point> ReconstructPath(Location current)
        {
            Stack<Point> total_path = new Stack<Point>();

            while (current != null)
            {
                total_path.Push(new Point(current.X, current.Y));
                current = current.Parent;
            }

            // remove the first node which is the current location
            total_path.Pop();

            return total_path;
        }

        public static int ComputeHScore(int x, int y, int targetX, int targetY)
        {
            return Math.Abs(targetX - x) + Math.Abs(targetY - y);
        }
    }
}
