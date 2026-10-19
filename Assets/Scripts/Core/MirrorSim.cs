using System;
using System.Collections.Generic;

namespace TiltMirror.Core
{
    // Pure C# game logic. No UnityEngine dependency, so it can be unit-tested
    // and reused by the level generator.
    //
    // Coordinates: x grows to the right, y grows DOWN (row 0 is the top row).
    // Both boards use the same coordinates. A move is mirrored on the X axis:
    // when the Sun slides right, the Moon slides left. Up/Down are the same for both.

    public enum Dir { Up, Down, Left, Right }

    public readonly struct Int2 : IEquatable<Int2>
    {
        public readonly int X;
        public readonly int Y;

        public Int2(int x, int y) { X = x; Y = y; }

        public bool Equals(Int2 o) { return X == o.X && Y == o.Y; }
        public override bool Equals(object obj) { return obj is Int2 o && Equals(o); }
        public override int GetHashCode() { return (X * 397) ^ Y; }
        public override string ToString() { return "(" + X + "," + Y + ")"; }
    }

    public readonly struct SimState : IEquatable<SimState>
    {
        public readonly Int2 Sun;
        public readonly Int2 Moon;
        public readonly bool HasKey;   // Chapter 2: the key has been picked up, locks are open

        public SimState(Int2 sun, Int2 moon, bool hasKey = false) { Sun = sun; Moon = moon; HasKey = hasKey; }

        public bool Equals(SimState o) { return Sun.Equals(o.Sun) && Moon.Equals(o.Moon) && HasKey == o.HasKey; }
        public override bool Equals(object obj) { return obj is SimState s && Equals(s); }
        public override int GetHashCode() { return (((Sun.GetHashCode() * 31) ^ Moon.GetHashCode()) * 2) + (HasKey ? 1 : 0); }
    }

    public sealed class BoardSpec
    {
        public int Width = 4;
        public int Height = 8;
        public Int2 SunStart;
        public Int2 MoonStart;
        public Int2 SunGoal;
        public Int2 MoonGoal;
        public readonly HashSet<Int2> SunWalls = new HashSet<Int2>();
        public readonly HashSet<Int2> MoonWalls = new HashSet<Int2>();

        // Chapter 2. One key (lying on the Sun board or the Moon board). Locks act like walls
        // until the key has been picked up; after that they open on both boards.
        public bool HasKey;
        public bool KeyOnSun;
        public Int2 KeyPos;
        public readonly HashSet<Int2> SunLocks = new HashSet<Int2>();
        public readonly HashSet<Int2> MoonLocks = new HashSet<Int2>();

        public SimState StartState { get { return new SimState(SunStart, MoonStart); } }
    }

    public static class MirrorSim
    {
        static readonly Dir[] AllDirs = { Dir.Up, Dir.Down, Dir.Left, Dir.Right };

        public static void Delta(Dir d, out int dx, out int dy)
        {
            switch (d)
            {
                case Dir.Up: dx = 0; dy = -1; break;
                case Dir.Down: dx = 0; dy = 1; break;
                case Dir.Left: dx = -1; dy = 0; break;
                default: dx = 1; dy = 0; break;
            }
        }

        // Slides until the next cell is a wall or outside the board.
        public static Int2 Slide(Int2 from, int dx, int dy, int width, int height, HashSet<Int2> walls)
        {
            int x = from.X;
            int y = from.Y;
            while (true)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) break;
                if (walls.Contains(new Int2(nx, ny))) break;
                x = nx;
                y = ny;
            }
            return new Int2(x, y);
        }

        // Same as Slide, but closed locks also stop the slide, and hitKey is set when the
        // slide passes over (or stops on) the key cell.
        static Int2 SlideWithKey(Int2 from, int dx, int dy, BoardSpec b, HashSet<Int2> walls,
            HashSet<Int2> locks, bool locksClosed, bool watchKey, out bool hitKey)
        {
            hitKey = false;
            int x = from.X;
            int y = from.Y;
            while (true)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || nx >= b.Width || ny < 0 || ny >= b.Height) break;
                Int2 c = new Int2(nx, ny);
                if (walls.Contains(c)) break;
                if (locksClosed && locks.Contains(c)) break;
                x = nx;
                y = ny;
                if (watchKey && c.Equals(b.KeyPos)) hitKey = true;
            }
            return new Int2(x, y);
        }

        // Locks are judged by the state at the START of the move. A key picked up during
        // the slide opens the locks for the NEXT move.
        public static SimState Step(SimState s, Dir d, BoardSpec b)
        {
            int dx, dy;
            Delta(d, out dx, out dy);
            bool closed = b.HasKey && !s.HasKey;
            bool hitSun;
            bool hitMoon;
            Int2 sun = SlideWithKey(s.Sun, dx, dy, b, b.SunWalls, b.SunLocks, closed, closed && b.KeyOnSun, out hitSun);
            Int2 moon = SlideWithKey(s.Moon, -dx, dy, b, b.MoonWalls, b.MoonLocks, closed, closed && !b.KeyOnSun, out hitMoon);
            return new SimState(sun, moon, s.HasKey || hitSun || hitMoon);
        }

        public static bool IsSolved(SimState s, BoardSpec b)
        {
            return s.Sun.Equals(b.SunGoal) && s.Moon.Equals(b.MoonGoal);
        }

        // Breadth-first search. Returns the length of the shortest solution
        // (this is the level's "par"), or -1 if the level cannot be solved.
        // If path is not null, it is filled with the moves of one shortest solution.
        public static int Solve(BoardSpec b, List<Dir> path = null)
        {
            return SolveFrom(b, b.StartState, path);
        }

        // Same search, but starting from any position (used by the tutorial hint).
        public static int SolveFrom(BoardSpec b, SimState from, List<Dir> path = null)
        {
            int w = b.Width;
            int cells = w * b.Height;
            int total = cells * cells * 2;

            int[] prev = new int[total];
            sbyte[] moveUsed = new sbyte[total];
            bool[] seen = new bool[total];

            int start = Encode(from, w, cells);
            seen[start] = true;
            prev[start] = -1;

            if (IsSolved(from, b))
            {
                if (path != null) path.Clear();
                return 0;
            }

            Queue<int> queue = new Queue<int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                SimState s = Decode(cur, w, cells);

                for (int i = 0; i < AllDirs.Length; i++)
                {
                    SimState next = Step(s, AllDirs[i], b);
                    int ni = Encode(next, w, cells);
                    if (seen[ni]) continue;

                    seen[ni] = true;
                    prev[ni] = cur;
                    moveUsed[ni] = (sbyte)i;

                    if (IsSolved(next, b))
                    {
                        List<Dir> reversed = new List<Dir>();
                        int node = ni;
                        while (prev[node] != -1)
                        {
                            reversed.Add(AllDirs[moveUsed[node]]);
                            node = prev[node];
                        }
                        reversed.Reverse();
                        if (path != null)
                        {
                            path.Clear();
                            path.AddRange(reversed);
                        }
                        return reversed.Count;
                    }

                    queue.Enqueue(ni);
                }
            }
            return -1;
        }

        static int Encode(SimState s, int w, int cells)
        {
            int sunIdx = s.Sun.Y * w + s.Sun.X;
            int moonIdx = s.Moon.Y * w + s.Moon.X;
            return (sunIdx * cells + moonIdx) * 2 + (s.HasKey ? 1 : 0);
        }

        static SimState Decode(int code, int w, int cells)
        {
            bool hasKey = (code & 1) == 1;
            code >>= 1;
            int sunIdx = code / cells;
            int moonIdx = code % cells;
            return new SimState(new Int2(sunIdx % w, sunIdx / w), new Int2(moonIdx % w, moonIdx / w), hasKey);
        }
    }
}
