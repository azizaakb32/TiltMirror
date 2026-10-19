using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TiltMirror.Core;

namespace TiltMirror
{
    // Owns the game state and rules. Visuals live in BoardView, UI in HudView / WinPanel.
    // Menus (MenuController) open levels through OpenLevel / CloseLevel.
    // Keyboard: arrow keys to move, Z to undo, R to restart.
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] LevelDatabase database;
        [SerializeField] BoardView boardView;   // optional: created automatically if empty
        [SerializeField] HudView hud;           // optional
        [SerializeField] WinPanel winPanel;     // optional
        [SerializeField] bool useSavedProgress = true;
        [Tooltip("On: starts a level immediately. Off: waits for the menu (MenuController) to open one.")]
        [SerializeField] bool autoStart = true;

        // Raised when the player wants to leave the level (back button, or after the last level).
        public event Action LevelsRequested;

        LevelData current;
        BoardSpec spec;
        SimState state;
        int levelIndex;
        int moves;
        int par;
        bool won;
        bool playing;
        TutorialHint hint;
        const int TutorialLevels = 3;   // levels 1-3 show the arrow until completed (LevelData.showHint adds more)
        readonly Stack<SimState> history = new Stack<SimState>();

        public int LevelCount { get { return database != null ? database.Count : 0; } }
        public int CurrentIndex { get { return levelIndex; } }

        void Start()
        {
            if (boardView == null)
                boardView = new GameObject("Board").AddComponent<BoardView>();

            AudioManager.Ensure();
            hint = TutorialHint.Create();

            if (database == null || database.Count == 0)
            {
                Debug.LogError("GameController: assign a LevelDatabase with at least one level.");
                enabled = false;
                return;
            }

            if (autoStart)
            {
                int start = useSavedProgress ? ProgressService.CurrentLevelIndex : 0;
                OpenLevel(Mathf.Clamp(start, 0, database.Count - 1));
            }
            else
            {
                boardView.gameObject.SetActive(false);
            }
        }

        // Shows the board and starts the given level.
        public void OpenLevel(int index)
        {
            if (database == null || database.Count == 0) return;
            if (boardView == null) boardView = new GameObject("Board").AddComponent<BoardView>();
            boardView.gameObject.SetActive(true);
            playing = true;
            LoadLevel(Mathf.Clamp(index, 0, database.Count - 1));
        }

        // Hides the board and stops accepting moves (used when going back to a menu).
        public void CloseLevel()
        {
            playing = false;
            StopAllCoroutines();
            if (hint != null) hint.Hide();
            if (winPanel != null) winPanel.Hide();
            if (boardView != null) boardView.gameObject.SetActive(false);
        }

        public void LoadLevel(int index)
        {
            levelIndex = index;
            current = database.Get(index);
            spec = current.ToSpec();
            par = current.par > 0 ? current.par : Mathf.Max(1, MirrorSim.Solve(spec));
            state = spec.StartState;
            moves = 0;
            won = false;
            history.Clear();

            boardView.Build(spec);
            if (hud != null) hud.Set(current.levelNumber, moves, par);
            if (winPanel != null) winPanel.Hide();
            ProgressService.CurrentLevelIndex = index;
            RefreshHint();
        }

        // Tutorial arrow: shown on the first levels until they are completed.
        void RefreshHint()
        {
            if (hint == null) return;
            if (!playing || won || (levelIndex >= TutorialLevels && !current.showHint) || ProgressService.GetStars(levelIndex) > 0)
            {
                hint.Hide();
                return;
            }
            List<Dir> path = new List<Dir>();
            if (MirrorSim.SolveFrom(spec, state, path) > 0) hint.Show(path[0]);
            else hint.Hide();   // stuck position: no hint (use Undo / Restart)
        }

        // Public one-argument-free methods so UI Buttons can call them from the Inspector.
        public void MoveUp() { Move(Dir.Up); }
        public void MoveDown() { Move(Dir.Down); }
        public void MoveLeft() { Move(Dir.Left); }
        public void MoveRight() { Move(Dir.Right); }

        public void Move(Dir dir)
        {
            if (!playing || won || boardView.IsAnimating) return;

            SimState next = MirrorSim.Step(state, dir, spec);
            if (next.Equals(state))
            {
                boardView.PlayBlocked(dir);   // nothing moved: costs no move
                Sfx("sfx_blocked");
                return;
            }

            if (hint != null) hint.Hide();
            bool gotKey = !state.HasKey && next.HasKey;
            history.Push(state);
            state = next;
            moves++;
            if (hud != null) hud.Set(current.levelNumber, moves, par);
            Sfx("sfx_move");
            if (gotKey) StartCoroutine(DelayedSfx("sfx_key", 0.14f));
            boardView.AnimateTo(state, dir, OnMoveFinished);
        }

        // Undo does not give moves back, so it cannot be used to cheat the star rating.
        public void Undo()
        {
            if (!playing || won || boardView.IsAnimating || history.Count == 0) return;
            state = history.Pop();
            boardView.SnapTo(state);
            Sfx("sfx_undo");
            RefreshHint();
        }

        public void Restart()
        {
            if (!playing) return;
            Sfx("sfx_click");
            LoadLevel(levelIndex);
        }

        public void Next()
        {
            if (!playing) return;
            Sfx("sfx_click");
            int next = levelIndex + 1;
            if (next >= database.Count)
            {
                BackToLevels();   // finished the last level: back to the level list
                return;
            }
            LoadLevel(next);
        }

        public void BackToLevels()
        {
            Sfx("sfx_click");
            CloseLevel();
            Action handler = LevelsRequested;
            if (handler != null) handler();
        }

        void OnMoveFinished()
        {
            if (!MirrorSim.IsSolved(state, spec))
            {
                RefreshHint();
                return;
            }

            won = true;
            int stars = ProgressService.StarsFor(moves, par);
            ProgressService.SaveResult(levelIndex, stars);
            boardView.PlayJoin();
            Sfx("sfx_join");
            StartCoroutine(ShowWinAfterDelay(0.5f, stars));
        }

        IEnumerator DelayedSfx(string clipName, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Sfx(clipName);
            yield return new WaitForSeconds(0.2f);
            Sfx("sfx_unlock");
        }

        IEnumerator ShowWinAfterDelay(float seconds, int stars)
        {
            yield return new WaitForSeconds(seconds);
            if (winPanel != null) winPanel.Show(stars, moves, par);
            for (int i = 1; i <= stars; i++)
            {
                Sfx("sfx_star_" + i);
                yield return new WaitForSeconds(0.25f);
            }
        }

        static void Sfx(string clipName)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(clipName);
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame) MoveUp();
                if (kb.downArrowKey.wasPressedThisFrame) MoveDown();
                if (kb.leftArrowKey.wasPressedThisFrame) MoveLeft();
                if (kb.rightArrowKey.wasPressedThisFrame) MoveRight();
                if (kb.zKey.wasPressedThisFrame) Undo();
                if (kb.rKey.wasPressedThisFrame) Restart();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.UpArrow)) MoveUp();
            if (Input.GetKeyDown(KeyCode.DownArrow)) MoveDown();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveLeft();
            if (Input.GetKeyDown(KeyCode.RightArrow)) MoveRight();
            if (Input.GetKeyDown(KeyCode.Z)) Undo();
            if (Input.GetKeyDown(KeyCode.R)) Restart();
#endif
        }
    }
}
