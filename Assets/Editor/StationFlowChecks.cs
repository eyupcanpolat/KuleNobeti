using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace KuleNobeti.Editor
{
    /// <summary>
    /// Real Play mode smoke checks. Uses the production command handlers, live NavMesh,
    /// CharacterController collisions and the configured local Ollama service.
    /// It does not synthesize keyboard events, inject replies or force a door/finish state.
    /// </summary>
    [InitializeOnLoad]
    public static class StationFlowChecks
    {
        private const string ScenePath = "Assets/Scenes/KuleNobeti.unity";
        private const string ActiveKey = "KuleNobeti.StationFlowChecks.Active";
        private const string OutcomeKey = "KuleNobeti.StationFlowChecks.Outcome";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly string ProjectRoot = Directory.GetParent(Application.dataPath).FullName;
        private static readonly string ReportPath = Path.Combine(ProjectRoot, "Logs/StationFlowChecks.txt");
        private static readonly string StatePath = Path.Combine(ProjectRoot, "Logs/StationFlowChecks-state.txt");
        private static readonly string RequestPath = Path.Combine(ProjectRoot, "Temp/StationFlowChecks.request");
        private static readonly string[] Keys = {
            "scene", "panel-access", "route-F1", "capture-service", "ollama-0", "finish-0",
            "reset-after-0", "route-F2", "panels", "capture-panels", "ollama-2", "finish-2",
            "capture-finish", "reset-after-2", "route-F3", "reset-after-F3", "native-keys"
        };
        private static readonly Dictionary<string, string> Results = new Dictionary<string, string>();
        private static readonly Stack<IEnumerator> Running = new Stack<IEnumerator>();
        private static StationGame game;
        private static StationWorld world;
        private static DialogueController dialogue;
        private static string progress = "Ready";
        private static double nextPoll;
        private static bool started, stopping, buildQueued;

        static StationFlowChecks()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += PlayStateChanged;
            string previousOutcome = SessionState.GetString(OutcomeKey, "");
            WriteState(SessionState.GetBool(ActiveKey, false) ? "WAITING_FOR_PLAY" :
                previousOutcome.Length > 0 ? previousOutcome : "READY");
        }

        [MenuItem("YazLab/6 - İstasyon Akışlarını Kontrol Et")]
        public static void Run()
        {
            if (!CanStart(out string reason))
            {
                Debug.LogWarning("İstasyon akış kontrolleri başlatılmadı: " + reason);
                WriteState("REFUSED: " + reason);
                return;
            }
            Results.Clear();
            foreach (string key in Keys) Results[key] = "UNTESTED | Not reached.";
            Results["native-keys"] = "UNTESTED | Physical Q/E/F/F1–F4/F8/R events need a manual keyboard check; these checks invoke their production handlers.";
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetString(OutcomeKey, "");
            started = stopping = false;
            progress = "Entering Play mode";
            WriteReport();
            WriteState("ENTERING_PLAY");
            EditorApplication.isPlaying = true;
        }

        private static bool CanStart(out string reason)
        {
            reason = "";
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                reason = $"Editor must be idle, in Edit mode (play/pending={EditorApplication.isPlayingOrWillChangePlaymode}, compiling={EditorApplication.isCompiling}, importing={EditorApplication.isUpdating}).";
            else if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != ScenePath)
                reason = "Open only the existing KuleNobeti scene first; this check never opens/replaces a scene.";
            else if (SceneManager.GetActiveScene().isDirty)
                reason = "The scene has unsaved edits; save or resolve them manually first.";
            else if (!Array.Exists(EditorBuildSettings.scenes, s => s.enabled && s.path == ScenePath))
                reason = "The existing scene is absent from Build Settings; Restart cannot be checked safely.";
            return reason.Length == 0;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false))
            {
                // Keep a requested check pending while Unity is still importing/compiling it.
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                if (EditorApplication.timeSinceStartup < nextPoll || buildQueued) return;
                nextPoll = EditorApplication.timeSinceStartup + .5;
                if (!File.Exists(RequestPath)) return;
                string request;
                try { request = File.ReadAllText(RequestPath).Trim(); }
                catch (IOException) { return; }
                if (request != "run" && request != "build") return;
                File.Delete(RequestPath); // A single-use request created specifically for this bridge.
                if (request == "run") Run();
                else QueueBuild();
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || stopping) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (!started)
            {
                started = true;
                Application.runInBackground = true;
                if (Results.Count == 0)
                {
                    foreach (string key in Keys) Results[key] = "UNTESTED | Not reached.";
                    Results["native-keys"] = "UNTESTED | Physical keys were not synthesized. Production handlers were invoked directly.";
                }
                Running.Push(Suite());
                WriteState("RUNNING");
            }
            try
            {
                if (!Advance(Running)) Finish();
            }
            catch (Exception e)
            {
                Results["harness"] = "FAIL | " + e;
                Debug.LogException(e);
                Finish();
            }
        }

        private static void QueueBuild()
        {
            if (!CanStart(out string reason))
            {
                WriteState("BUILD_REFUSED: " + reason);
                return;
            }
            buildQueued = true;
            EditorApplication.delayCall += () => {
                try
                {
                    if (!CanStart(out string delayedReason))
                    {
                        WriteState("BUILD_REFUSED: " + delayedReason);
                        return;
                    }
                    WriteState("BUILDING_NEW_PREVIEW");
                    StationEditor.BuildWindows();
                    SessionState.SetString(OutcomeKey, "BUILD_SUCCEEDED");
                    WriteState("BUILD_SUCCEEDED");
                }
                catch (Exception e)
                {
                    SessionState.SetString(OutcomeKey, "BUILD_FAILED: " + e.Message);
                    WriteState("BUILD_FAILED: " + e.Message);
                    Debug.LogException(e);
                }
                finally { buildQueued = false; }
            };
        }

        private static void PlayStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode || !SessionState.GetBool(ActiveKey, false)) return;
            if (!stopping)
            {
                Results["harness"] = "UNTESTED | Run was interrupted before completion.";
                progress = "Interrupted";
                WriteReport();
                SessionState.SetString(OutcomeKey, "INTERRUPTED");
                WriteState("INTERRUPTED");
            }
            SessionState.SetBool(ActiveKey, false);
            DisposeStack(Running);
        }

        private static IEnumerator Suite()
        {
            yield return Check("scene", FindScene());
            if (!Passed("scene")) yield break;
            yield return Check("panel-access", CheckPanelAccess());
            yield return Check("route-F1", DriveRobot(0));
            if (Passed("route-F1"))
            {
                yield return Check("capture-service", CaptureAt("01-service", new Vector3(0, 0, -2)));
                yield return Check("ollama-0", OpenArchiveWithLiveDialogue(0));
                if (Passed("ollama-0")) yield return Check("finish-0", ReachExit(0));
            }
            yield return Check("reset-after-0", RestartAndCheck());
            if (!Passed("reset-after-0")) yield break;
            yield return Check("route-F2", DriveRobot(1));
            if (Passed("route-F2"))
            {
                yield return Check("panels", ActivatePanels());
                yield return Check("capture-panels", CaptureAt("02-panels", new Vector3(0, 0, 7)));
                if (Passed("panels"))
                {
                    yield return Check("ollama-2", OpenArchiveWithLiveDialogue(2));
                    if (Passed("ollama-2"))
                    {
                        yield return Check("finish-2", ReachExit(2));
                        if (Passed("finish-2")) yield return Check("capture-finish", Capture("03-finish"));
                    }
                }
            }
            yield return Check("reset-after-2", RestartAndCheck());
            if (!Passed("reset-after-2")) yield break;
            yield return Check("route-F3", DriveRobot(2));
            yield return Check("reset-after-F3", RestartAndCheck());
        }

        private static IEnumerator FindScene()
        {
            double deadline = EditorApplication.timeSinceStartup + 20;
            while ((game == null || WorldOf(game) == null) && EditorApplication.timeSinceStartup < deadline)
            {
                game = UnityEngine.Object.FindFirstObjectByType<StationGame>();
                yield return null;
            }
            Require(game != null, "StationGame not created.");
            world = WorldOf(game);
            Require(world != null && world.Player != null && world.Tasks != null, "Runtime world/tasks not initialized.");
            dialogue = game.GetComponent<DialogueController>();
            Require(dialogue != null && world.Companion.GetComponent<NavMeshAgent>().isOnNavMesh, "Dialogue/NavMesh agent missing.");
            Require(!world.Player.Finished && !world.PhysicalGate.IsOpen && !world.ArchiveGate.IsOpen && world.Tasks.CompletedCount == 0,
                "Fresh Play scene has unexpected progress.");
            CheckDecorState(false, false);
        }

        private static IEnumerator CheckPanelAccess()
        {
            Vector3 original = world.Player.transform.position;
            Require(!world.Tasks.TryInteract() && world.Tasks.InteractionHint.Length == 0, "Remote interaction was accepted.");
            try
            {
                WarpForAccessCheck(StationTasks.VentilationPosition);
                Require(!world.Tasks.TryInteract() && world.Tasks.CompletedCount == 0, "Ventilation worked before the first gate.");
                WarpForAccessCheck(StationTasks.RadioPosition);
                Require(!world.Tasks.TryInteract() && world.Tasks.CompletedCount == 0, "Radio worked before the first gate.");
            }
            finally { WarpForAccessCheck(original); }
            yield return null;
        }

        private static IEnumerator DriveRobot(int startIndex)
        {
            Require(!world.PhysicalGate.IsOpen, "Route test needs a closed physical gate.");
            Require(world.Companion.Relocate(StationWorld.TestStarts[startIndex]), "F" + (startIndex + 1) + " Relocate rejected.");
            world.Companion.Order(); // Q uses this exact production handler.
            bool crossedTunnel = false;
            double deadline = EditorApplication.timeSinceStartup + 48;
            while (!world.PhysicalGate.IsOpen && world.Companion.State != CompanionAgent.Phase.Failed &&
                   EditorApplication.timeSinceStartup < deadline)
            {
                Vector3 robot = world.Companion.transform.position;
                crossedTunnel |= robot.x < -4.9f && robot.z > -4.3f && robot.z < 4.3f;
                yield return null;
            }
            Require(world.Companion.State == CompanionAgent.Phase.Succeeded && world.PhysicalGate.IsOpen,
                "Actual agent/sensor failed: " + world.Companion.Status);
            Require(crossedTunnel, "No actual low tunnel traversal was observed.");
            yield return WaitUntil(() => !world.PhysicalGate.GetComponent<Collider>().enabled, 5, "Physical gate did not clear the route.");
            Require(!world.Tasks.TryInteract() && world.Tasks.CompletedCount == 0, "Remote panel activation was accepted after the gate.");
        }

        private static IEnumerator ActivatePanels()
        {
            yield return MoveTo(new Vector3(0, 0, 2));
            yield return MoveTo(new Vector3(-3.5f, 0, 6));
            yield return MoveTo(StationTasks.VentilationPosition);
            Require(world.Tasks.InHazard, "Marked side lane is not hazardous while ventilation is off.");
            Require(world.Tasks.InteractionHint.Contains("F"), "Ventilation interaction hint missing.");
            world.Player.HasFocusForDialogue = true;
            bool blockedDuringChat = !world.Tasks.TryInteract() && !world.Tasks.VentilationOn;
            world.Player.HasFocusForDialogue = false;
            Require(blockedDuringChat, "Panel worked while dialogue had focus.");
            Require(world.Tasks.TryInteract() && world.Tasks.VentilationOn && !world.Tasks.InHazard, "Ventilation did not clear the live hazard state.");
            Require(!world.Tasks.TryInteract() && world.Tasks.CompletedCount == 1, "Duplicate panel interaction changed progress.");
            yield return MoveTo(new Vector3(0, 0, 9.5f));
            yield return MoveTo(StationTasks.RadioPosition);
            Require(world.Tasks.TryInteract() && world.Tasks.RadioOn && world.Tasks.CompletedCount == 2, "Radio did not activate.");
            Require(!world.Tasks.TryInteract() && world.Tasks.CompletedCount == 2, "Duplicate radio interaction changed progress.");
            yield return null; // Allow StationDecor.Update to show the new system state.
            CheckDecorState(true, true);
        }

        private static IEnumerator OpenArchiveWithLiveDialogue(int expectedTasks)
        {
            yield return MoveTo(new Vector3(0, 0, 2));
            yield return MoveTo(new Vector3(0, 0, 8.9f));
            Require(world.Tasks.CompletedCount == expectedTasks, "Optional task count changed during traversal.");
            Require(dialogue.CanSpeak && !world.ArchiveGate.IsOpen, "Guard cannot be contacted at the real approach position.");
            const string plan = "Tesiste yangın ve duman var. İçerideki personeli güvenli acil çıkıştan dışarıdaki toplanma alanına götürüp orada toplayacağım.";
            Require(EvacuationPolicy.CanConsiderOpening(plan), "Test plan does not meet the unchanged evacuation rule.");
            dialogue.Send(plan); // Actual configured OllamaClient + production JSON parser/policy.
            Require(dialogue.Busy, "Production dialogue request was not started.");
            double deadline = EditorApplication.timeSinceStartup + OllamaSettings.Load().timeoutSeconds + 12;
            while (dialogue.Busy && EditorApplication.timeSinceStartup < deadline) yield return null;
            Require(!dialogue.Busy && !dialogue.HasError && !string.IsNullOrWhiteSpace(dialogue.Response),
                "Live local Ollama did not return a valid dynamic reply: " + dialogue.Response);
            Require(world.ArchiveGate.IsOpen, "Live JSON/policy response did not authorize the archive gate: " + dialogue.Response);
            yield return WaitUntil(() => !world.ArchiveGate.GetComponent<Collider>().enabled, 5, "Archive gate did not physically clear.");
        }

        private static IEnumerator ReachExit(int expectedTasks)
        {
            yield return MoveTo(new Vector3(0, 0, 15.1f));
            yield return WaitUntil(() => world.Player.Finished, 3, "Walking into the real exit did not finish the game.");
            Require(world.Tasks.CompletedCount == expectedTasks, "Finish lost optional task status.");
            Require(world.Tasks.VentilationOn == (expectedTasks == 2) && world.Tasks.RadioOn == (expectedTasks == 2), "Wrong completed/skipped task state at finish.");
            Require(!world.Tasks.TryInteract(), "Panels accepted interaction after finishing.");
        }

        private static IEnumerator RestartAndCheck()
        {
            StationGame oldGame = game;
            game.Restart(); // The exact F8/R scene reset handler; never a manual state reset.
            game = null;
            double deadline = EditorApplication.timeSinceStartup + 20;
            do
            {
                game = UnityEngine.Object.FindFirstObjectByType<StationGame>();
                yield return null;
            }
            while ((game == null || game == oldGame || WorldOf(game) == null) && EditorApplication.timeSinceStartup < deadline);
            Require(game != null && game != oldGame, "Restart did not replace the runtime scene.");
            world = WorldOf(game);
            dialogue = game.GetComponent<DialogueController>();
            Require(world != null && world.Tasks != null && !world.PhysicalGate.IsOpen && !world.ArchiveGate.IsOpen,
                "Restart did not close both gates.");
            Require(!world.Player.Finished && !world.Player.HasFocusForDialogue && !world.Tasks.VentilationOn &&
                    !world.Tasks.RadioOn && world.Tasks.CompletedCount == 0 && !world.Tasks.InHazard,
                "Restart retained player/task progress.");
            Require(!dialogue.Busy && dialogue.Response.Length == 0 && dialogue.LastInput.Length == 0, "Restart retained dialogue state.");
            Require(world.Companion.State == CompanionAgent.Phase.Following && world.Companion.GetComponent<NavMeshAgent>().isOnNavMesh,
                "Restart did not restore the real companion agent.");
            yield return null;
            CheckDecorState(false, false);
        }

        private static IEnumerator MoveTo(Vector3 target)
        {
            PlayerMotor motor = world.Player;
            CharacterController controller = motor.GetComponent<CharacterController>();
            bool enabled = motor.enabled;
            motor.enabled = false; // Prevent human keys from interfering; still use the real collision controller.
            double deadline = EditorApplication.timeSinceStartup + 25;
            int lastFrame = -1;
            try
            {
                while (true)
                {
                    Vector3 delta = Vector3.ProjectOnPlane(target - motor.transform.position, Vector3.up);
                    if (delta.magnitude < .12f) break;
                    Require(EditorApplication.timeSinceStartup < deadline, "CharacterController route blocked toward " + target + "; at " + motor.transform.position);
                    if (Time.frameCount != lastFrame)
                    {
                        lastFrame = Time.frameCount;
                        float dt = Mathf.Clamp(Time.deltaTime, .001f, .05f);
                        float speed = world.Tasks.InHazard ? 2.4f : 5f;
                        motor.transform.rotation = Quaternion.LookRotation(delta);
                        controller.Move(Vector3.ClampMagnitude(delta, speed * dt) + Vector3.down * 2f * dt);
                    }
                    yield return null;
                }
            }
            finally { if (motor != null) motor.enabled = enabled; }
        }

        private static void WarpForAccessCheck(Vector3 position)
        {
            CharacterController controller = world.Player.GetComponent<CharacterController>();
            controller.enabled = false;
            world.Player.transform.position = position + Vector3.up * .08f;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private static IEnumerator CaptureAt(string suffix, Vector3 position)
        {
            yield return MoveTo(position);
            yield return Capture(suffix);
        }

        private static IEnumerator Capture(string suffix)
        {
            double settleUntil = EditorApplication.timeSinceStartup + .7;
            while (EditorApplication.timeSinceStartup < settleUntil) yield return null;
            string path = Path.Combine(ProjectRoot, "Logs/StationFlowChecks-" + suffix + ".png");
            // These are harness-owned screenshots; remove only this exact prior output
            // so a stale image cannot count as a successful capture.
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            yield return WaitUntil(() => File.Exists(path), 8, "ScreenCapture did not write " + path);
            for (int i = 0; i < 3; i++) yield return null;
        }

        private static IEnumerator WaitUntil(Func<bool> predicate, double seconds, string failure)
        {
            double deadline = EditorApplication.timeSinceStartup + seconds;
            while (!predicate())
            {
                Require(EditorApplication.timeSinceStartup < deadline, failure);
                yield return null;
            }
        }

        private static void CheckDecorState(bool ventilation, bool radio)
        {
            Require(world.Tasks.VentilationOn == ventilation && world.Tasks.RadioOn == radio, "System state changed unexpectedly.");
            StationDecor decor = game.GetComponent<StationDecor>();
            Require(decor != null, "Presentation component missing.");
            GameObject hazard = GetPrivate<GameObject>(decor, "hazard");
            Require(hazard != null && hazard.activeSelf == !ventilation, "Visible side-lane hazard does not match ventilation state.");
            List<Light> beacons = GetPrivate<List<Light>>(decor, "exitBeacons");
            Require(beacons.Count == 4, "Expected four radio-controlled route lights.");
            foreach (Light beacon in beacons) Require(beacon != null && beacon.enabled == radio, "Radio-controlled route light has wrong enabled state.");
            List<Renderer> lamps = GetPrivate<List<Renderer>>(decor, "exitLights");
            Require(lamps.Count == 8, "Expected eight radio-controlled direction markers.");
            Color expected = radio ? new Color(.28f, .66f, .37f) : new Color(.13f, .23f, .17f);
            var block = new MaterialPropertyBlock();
            foreach (Renderer lamp in lamps)
            {
                lamp.GetPropertyBlock(block);
                Color actual = block.GetColor("_Color");
                Require(Mathf.Abs(actual.r - expected.r) + Mathf.Abs(actual.g - expected.g) + Mathf.Abs(actual.b - expected.b) < .02f,
                    "Direction marker color does not match the radio state.");
            }
            Transform presentation = GetPrivate<Transform>(decor, "decor");
            foreach (Collider collider in presentation.GetComponentsInChildren<Collider>(true))
                Require(!collider.enabled, "New decoration can block movement or sensor rays: " + collider.name);
        }

        private static StationWorld WorldOf(StationGame station) => station == null ? null : GetPrivate<StationWorld>(station, "world");
        private static T GetPrivate<T>(object owner, string name) => (T)owner.GetType().GetField(name, PrivateInstance).GetValue(owner);
        private static void Require(bool condition, string failure) { if (!condition) throw new InvalidOperationException(failure); }
        private static bool Passed(string key) => Results.TryGetValue(key, out string result) && result.StartsWith("PASS", StringComparison.Ordinal);

        private static IEnumerator Check(string key, IEnumerator routine)
        {
            progress = key;
            WriteState("RUNNING: " + key);
            WriteReport();
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            Exception failure = null;
            while (stack.Count > 0)
            {
                try { Advance(stack); }
                catch (Exception e) { failure = e; DisposeStack(stack); }
                if (failure != null) break;
                yield return null;
            }
            Results[key] = failure == null ? "PASS | Completed in Unity Play mode." : "FAIL | " + failure.Message;
            if (failure != null) Debug.LogWarning("Station flow " + key + ": " + failure.Message);
            WriteReport();
        }

        private static bool Advance(Stack<IEnumerator> stack)
        {
            while (stack.Count > 0)
            {
                IEnumerator current = stack.Peek();
                if (!current.MoveNext())
                {
                    (current as IDisposable)?.Dispose();
                    stack.Pop();
                    continue;
                }
                if (current.Current is IEnumerator nested) { stack.Push(nested); continue; }
                return true;
            }
            return false;
        }

        private static void DisposeStack(Stack<IEnumerator> stack)
        {
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
        }

        private static void Finish()
        {
            stopping = true;
            bool failed = false;
            foreach (string result in Results.Values) failed |= result.StartsWith("FAIL", StringComparison.Ordinal);
            progress = failed ? "Completed with failures" : "Completed";
            WriteReport();
            string outcome = failed ? "COMPLETE_WITH_FAILURES" : "COMPLETE";
            SessionState.SetString(OutcomeKey, outcome);
            WriteState(outcome);
            SessionState.SetBool(ActiveKey, false);
            DisposeStack(Running);
            EditorApplication.isPlaying = false;
        }

        private static void WriteReport()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            var report = new StringBuilder("Kule Nöbeti / Unity integrated station flow checks\n");
            report.AppendLine("UTC: " + DateTime.UtcNow.ToString("O"));
            report.AppendLine("Progress: " + progress);
            report.AppendLine("Real Play mode; no door, finish, client or policy replacement. Optional helper movement uses CharacterController.Move, not physical WASD.");
            report.AppendLine("Hazard assertions cover live hazard state/clearance; a native WASD speed measurement remains manual.");
            foreach (var result in Results) report.AppendLine(result.Key + " | " + result.Value);
            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
        }

        private static void WriteState(string value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
            File.WriteAllText(StatePath, DateTime.UtcNow.ToString("O") + " | " + value + Environment.NewLine, Encoding.UTF8);
        }
    }
}
