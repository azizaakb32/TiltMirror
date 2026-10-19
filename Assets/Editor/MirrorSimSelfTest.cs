using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TiltMirror.Core;

namespace TiltMirror.EditorTools
{
    // Tilt Mirror > Run Logic Self-Test
    // Quick check of the rules and the solver. Results go to the Console.
    public static class MirrorSimSelfTest
    {
        static int failures;

        [MenuItem("Tilt Mirror/Run Logic Self-Test")]
        static void Run()
        {
            failures = 0;

            // 1. Both sample levels solve in the expected number of moves, and the
            //    returned path really ends on the goals.
            for (int i = 0; i < SampleLevels.All.Length; i++)
            {
                SampleLevels.Sample s = SampleLevels.All[i];
                BoardSpec spec = SampleLevels.ToSpec(s);
                List<Dir> path = new List<Dir>();
                int par = MirrorSim.Solve(spec, path);
                Check(par == s.ExpectedPar, "Level " + s.Number + " par is " + par + ", expected " + s.ExpectedPar);

                SimState state = spec.StartState;
                for (int k = 0; k < path.Count; k++) state = MirrorSim.Step(state, path[k], spec);
                Check(MirrorSim.IsSolved(state, spec), "Level " + s.Number + " solution path does not reach the goals");
            }

            // 2. Mirroring on an empty board: Right sends the Sun right and the Moon left.
            BoardSpec empty = new BoardSpec();
            empty.SunStart = new Int2(0, 7);
            empty.MoonStart = new Int2(3, 7);
            empty.SunGoal = new Int2(3, 0);
            empty.MoonGoal = new Int2(0, 0);
            SimState afterRight = MirrorSim.Step(empty.StartState, Dir.Right, empty);
            Check(afterRight.Sun.Equals(new Int2(3, 7)), "Sun should slide to (3,7), got " + afterRight.Sun);
            Check(afterRight.Moon.Equals(new Int2(0, 7)), "Moon should slide to (0,7), got " + afterRight.Moon);

            // 3. Up moves both the same way.
            SimState afterUp = MirrorSim.Step(empty.StartState, Dir.Up, empty);
            Check(afterUp.Sun.Equals(new Int2(0, 0)) && afterUp.Moon.Equals(new Int2(3, 0)), "Up should send both to the top row");

            // 4. A wall stops only the half-disc it belongs to.
            BoardSpec blocked = new BoardSpec();
            blocked.SunStart = new Int2(0, 7);
            blocked.MoonStart = new Int2(3, 7);
            blocked.SunGoal = new Int2(3, 0);
            blocked.MoonGoal = new Int2(0, 0);
            blocked.SunWalls.Add(new Int2(1, 7));
            SimState afterBlocked = MirrorSim.Step(blocked.StartState, Dir.Right, blocked);
            Check(afterBlocked.Sun.Equals(new Int2(0, 7)), "Sun should be blocked by the wall");
            Check(afterBlocked.Moon.Equals(new Int2(0, 7)), "Moon should still slide to (0,7)");

            // 5. An unreachable goal is reported as unsolvable.
            BoardSpec impossible = new BoardSpec();
            impossible.SunStart = new Int2(0, 7);
            impossible.MoonStart = new Int2(3, 7);
            impossible.SunGoal = new Int2(3, 0);
            impossible.MoonGoal = new Int2(0, 0);
            impossible.SunWalls.Add(new Int2(2, 0));
            impossible.SunWalls.Add(new Int2(3, 1));
            Check(MirrorSim.Solve(impossible) == -1, "Walled-in goal should be unsolvable");

            // 6. Key and lock: the Moon passes over the key while sliding up, so the lock on
            //    the Sun board is still closed for THAT move and open for the next one.
            BoardSpec keyed = new BoardSpec();
            keyed.SunStart = new Int2(0, 7);
            keyed.MoonStart = new Int2(3, 7);
            keyed.SunGoal = new Int2(0, 0);
            keyed.MoonGoal = new Int2(3, 0);
            keyed.HasKey = true;
            keyed.KeyOnSun = false;
            keyed.KeyPos = new Int2(3, 5);
            keyed.SunLocks.Add(new Int2(0, 4));
            SimState k1 = MirrorSim.Step(keyed.StartState, Dir.Up, keyed);
            Check(k1.HasKey, "Moon should pick up the key while sliding over it");
            Check(k1.Sun.Equals(new Int2(0, 5)), "Closed lock should stop the Sun at (0,5), got " + k1.Sun);
            SimState k2 = MirrorSim.Step(k1, Dir.Up, keyed);
            Check(k2.Sun.Equals(new Int2(0, 0)), "Open lock should let the Sun reach (0,0), got " + k2.Sun);
            Check(MirrorSim.Solve(keyed) == 2, "Keyed test level should be solvable in 2 moves");

            // 7. A key that is never reachable keeps the level unsolvable.
            keyed.KeyPos = new Int2(2, 7);   // not on the Moon's slide path (Up)
            keyed.MoonWalls.Add(new Int2(2, 7));   // wall on the key cell: key can never be reached
            keyed.SunLocks.Add(new Int2(1, 0));    // locks wall in the Sun's goal
            keyed.SunLocks.Add(new Int2(0, 1));
            Check(MirrorSim.Solve(keyed) == -1, "Level with an unreachable key and a closed lock should be unsolvable");

            if (failures == 0) Debug.Log("Tilt Mirror self-test: all checks passed.");
            else Debug.LogError("Tilt Mirror self-test: " + failures + " check(s) failed.");
        }

        static void Check(bool ok, string message)
        {
            if (ok) return;
            failures++;
            Debug.LogError("FAILED: " + message);
        }
    }
}
