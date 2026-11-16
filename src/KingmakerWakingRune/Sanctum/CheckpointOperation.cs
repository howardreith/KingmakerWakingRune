using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.SettingsUI;
using KingmakerWakingRune.Diagnostics;
using UnityEngine;

namespace KingmakerWakingRune.Sanctum
{
    /// <summary>
    /// One requested scenario checkpoint (the token's save before leaving, the portal's before returning), followed to a
    /// terminal result under <see cref="CheckpointRules"/> (WR-PLAYTEST-02). The operation is the exact routine handed to the
    /// native loading queue: it is pending while the queue holds it, it ends normally only through its own completion
    /// callback, and its record is the one the native writer created inside that routine. Global loading and commit flags
    /// are not consulted, because a deferring save coordinator (Mounted Combat 0.2.11 holds a queued save until running
    /// actions and projectiles finish, reporting the loading queue idle meanwhile) leaves them idle before anything is
    /// written. A given-up operation can never lead to travel; its late result is only logged.
    /// </summary>
    internal sealed class CheckpointOperation
    {
        private static readonly FieldInfo CurrentField = AccessTools.Field(typeof(LoadingProcess), "m_CurrentProcess");
        private static readonly FieldInfo QueueField = AccessTools.Field(typeof(LoadingProcess), "m_Queue");
        private static readonly FieldInfo ProcessField = AccessTools.Field(AccessTools.Inner(typeof(LoadingProcess), "QueuedProcess"), "Process");

        private static readonly List<CheckpointOperation> s_Live = new List<CheckpointOperation>();
        private static int s_Sequence;

        /// <summary>The saver whose commit the harness asked to fail (an injected write error); read on the native save thread.</summary>
        private static volatile object s_FailCommitOf;

        public readonly int Id;
        public readonly string Requested;
        private readonly Player m_World;
        private readonly string m_GameId;
        private readonly DateTime m_StartedUtc;
        private readonly float m_Started;
        private IEnumerator m_Process;
        private SaveInfo m_Record;
        private bool m_Completed;
        private bool m_InjectWriteError;
        private bool m_InjectReadBack;
        private bool m_DeferralSeen;
        private CheckpointStage m_Stage = CheckpointStage.Queued;
        private CheckpointFailure m_Failure;
        private string m_Abandoned;

        private CheckpointOperation(string requested)
        {
            Id = ++s_Sequence;
            Requested = requested;
            m_World = Game.Instance.Player;
            m_GameId = m_World.GameId;
            m_StartedUtc = DateTime.UtcNow;
            m_Started = Time.realtimeSinceStartup;
        }

        /// <summary>The verified record, once <see cref="Poll"/> has reported success.</summary>
        public SaveInfo Checkpoint { get; private set; }

        public CheckpointFailure Failure => m_Failure;

        /// <summary>Where it stands, for diagnostics (a timeout names this).</summary>
        public string Status => $"checkpoint #{Id} '{Requested}' {m_Stage}{(m_DeferralSeen ? " (held back before writing)" : "")} after {Time.realtimeSinceStartup - m_Started:0.0} s";

        public float Age => Time.realtimeSinceStartup - m_Started;

        /// <summary>A checkpoint still held by the native queue, even one already given up: no second may start beside it.</summary>
        public static bool AnyPending
        {
            get
            {
                Sweep();
                return s_Live.Count > 0;
            }
        }

        /// <summary>
        /// Asks for a save named <paramref name="name"/> (made unique by the game). Returns the operation, or null with the
        /// reason when the game may not save now (ironman, saving disallowed, another save still running).
        /// </summary>
        public static CheckpointOperation Start(string name, out CheckpointRefusal refusal)
        {
            refusal = CheckpointRefusal.None;
            SaveManager saves = Game.Instance.SaveManager;
            if (FaultInjection.Take("save-refused"))
            {
                Log.Info("INJECTED FAULT save-refused: reporting saving as disallowed.");
                refusal = CheckpointRefusal.Disallowed;
                return null;
            }
            if (SettingsRoot.Instance.OnlyOneSave.CurrentValue)
            {
                refusal = CheckpointRefusal.SingleSave;
                return null;
            }
            if (!saves.IsSaveAllowed())
            {
                refusal = CheckpointRefusal.Disallowed;
                return null;
            }
            if (AnyPending || saves.CommitInProgress)
            {
                refusal = CheckpointRefusal.Busy;
                return null;
            }
            SaveInfo request = saves.CreateNewSave(name);
            var operation = new CheckpointOperation(request.Name);
            if (FaultInjection.Take("save-stale"))
            {
                // Negative control: this operation writes nothing, while an unrelated save with the identical name is written
                // beside it through the native queue. Only this operation's own record may satisfy it, so it must fail.
                Log.Info($"INJECTED FAULT save-stale: checkpoint #{operation.Id} writes nothing; an unrelated save named '{request.Name}' is written beside it.");
                operation.m_Process = Nothing();
                SaveInfo unrelated = saves.CreateNewSave(name);
                unrelated.Name = request.Name;
                LoadingProcess.Instance.StartLoadingProcess(saves.SaveRoutine(unrelated), null, LoadingProcessTag.Save);
            }
            else
            {
                operation.m_InjectWriteError = FaultInjection.Take("save-write-error");
                operation.m_InjectReadBack = FaultInjection.Take("save-readback");
                if (FaultInjection.Take("save-delay"))
                {
                    // A controlled delayed completion: a five-second operation is queued ahead of the save.
                    Log.Info($"INJECTED FAULT save-delay: checkpoint #{operation.Id} queued behind a 5 s operation.");
                    LoadingProcess.Instance.StartLoadingProcess(Hold(5f), null, LoadingProcessTag.Save);
                }
                operation.m_Process = saves.SaveRoutine(request);
            }
            s_Live.Add(operation);
            LoadingProcess.Instance.StartLoadingProcess(operation.m_Process, operation.OnCompleted, LoadingProcessTag.Save);
            Log.Info($"Checkpoint #{operation.Id} requested: '{request.Name}' ({operation.m_Process.GetType().Name}).");
            return operation;
        }

        /// <summary>Called by the native writer's PrepareSave: the record created inside this operation's own routine is its record.</summary>
        public static void ObservePrepared(SaveInfo save)
        {
            if (save == null || s_Live.Count == 0)
            {
                return;
            }
            IEnumerator current = CurrentProcess();
            foreach (CheckpointOperation operation in s_Live)
            {
                if (operation.m_Record == null && ReferenceEquals(operation.m_Process, current) && save.Name == operation.Requested)
                {
                    operation.m_Record = save;
                    if (operation.m_InjectWriteError)
                    {
                        s_FailCommitOf = save.Saver;
                    }
                    Log.Info($"Checkpoint #{operation.Id} record prepared: '{save.Name}' -> {Path.GetFileName(save.FolderName)}.");
                }
            }
        }

        /// <summary>Called on the native save thread before an archive is committed; throws only for the harness's injected write error.</summary>
        public static void BeforeCommit(object saver)
        {
            if (saver != null && ReferenceEquals(saver, s_FailCommitOf))
            {
                s_FailCommitOf = null;
                throw new IOException("INJECTED FAULT save-write-error: the archive commit was refused.");
            }
        }

        /// <summary>Reads the operation's state; success only once its own record is committed and reads back.</summary>
        public CheckpointProgress Poll()
        {
            if (m_Stage == CheckpointStage.Failed)
            {
                return CheckpointProgress.Failed;
            }
            if (Checkpoint != null)
            {
                return CheckpointProgress.Succeeded;
            }
            CheckpointStage stage = CheckpointRules.Assess(Facts(), out CheckpointFailure failure);
            if (stage != m_Stage)
            {
                Log.Info($"Checkpoint #{Id} '{Requested}': {m_Stage} -> {stage}{(failure != CheckpointFailure.None ? $" ({failure})" : "")} after {Age:0.0} s.");
                m_Stage = stage;
            }
            if (stage == CheckpointStage.Running && !m_DeferralSeen && !LoadingProcess.Instance.IsLoadingInProcess)
            {
                m_DeferralSeen = true;
                Log.Info($"Checkpoint #{Id} is current in the loading queue while the queue reports idle: another mod is holding it back before writing; still waiting for this save.");
            }
            if (stage == CheckpointStage.Failed)
            {
                Fail(failure);
                return CheckpointProgress.Failed;
            }
            if (stage != CheckpointStage.Written)
            {
                return CheckpointProgress.Running;
            }
            if (!IsReadable(m_Record))
            {
                Log.Error($"Checkpoint #{Id}: '{m_Record.Name}' could not be read back from '{m_Record.FolderName}'.");
                m_Stage = CheckpointStage.Failed;
                Fail(CheckpointFailure.ReadBack);
                return CheckpointProgress.Failed;
            }
            Checkpoint = m_Record;
            s_Live.Remove(this);
            Log.Info($"Checkpoint #{Id} verified: {m_Record.Name} ({Path.GetFileName(m_Record.FolderName)}, {new FileInfo(m_Record.FolderName).Length} bytes) after {Age:0.0} s.");
            return CheckpointProgress.Succeeded;
        }

        /// <summary>
        /// Gives the operation up (a timeout, a load, a canceled journey): it can no longer lead anywhere. A save the native
        /// queue still holds is never interrupted; it is watched until it leaves the queue so no second save starts beside it.
        /// </summary>
        public void Abandon(string reason)
        {
            if (m_Abandoned != null || Checkpoint != null)
            {
                return;
            }
            m_Abandoned = reason;
            Log.Info($"{Status}: given up ({reason}); it cannot lead to travel.");
            if (m_Stage == CheckpointStage.Failed)
            {
                s_Live.Remove(this);
            }
        }

        private void Fail(CheckpointFailure failure)
        {
            m_Failure = failure;
            Log.Error($"Checkpoint #{Id} '{Requested}' failed: {failure} after {Age:0.0} s{(m_Record != null ? $" (record {Path.GetFileName(m_Record.FolderName ?? "none")})" : "")}.");
            Sweep();
        }

        private void OnCompleted()
        {
            m_Completed = true;
            if (m_Abandoned != null)
            {
                Log.Info($"Checkpoint #{Id} '{Requested}' finished late, after it was given up ({m_Abandoned}); recorded only, nothing follows from it.");
                s_Live.Remove(this);
            }
        }

        private CheckpointFacts Facts()
        {
            bool current = ReferenceEquals(CurrentProcess(), m_Process);
            var facts = new CheckpointFacts
            {
                SameWorld = ReferenceEquals(Game.Instance.Player, m_World) && Game.Instance.Player.GameId == m_GameId,
                Current = current,
                Owned = current || Queued().Contains(m_Process),
                Completed = m_Completed,
                Prepared = m_Record != null,
            };
            if (m_Record != null)
            {
                facts.Listed = Game.Instance.SaveManager.Contains(m_Record);
                facts.HasArchive = m_Record.IsActuallySaved;
                facts.Committing = m_Record.OperationState != SaveInfo.StateType.None;
            }
            return facts;
        }

        /// <summary>Drops operations that have reached an end and that the native queue no longer holds.</summary>
        private static void Sweep()
        {
            IEnumerator current = CurrentProcess();
            List<IEnumerator> queued = Queued();
            s_Live.RemoveAll(o => (o.m_Completed || o.m_Stage == CheckpointStage.Failed) && !ReferenceEquals(o.m_Process, current) && !queued.Contains(o.m_Process)
                && (o.m_Record == null || o.m_Record.OperationState == SaveInfo.StateType.None));
        }

        private bool IsReadable(SaveInfo save)
        {
            if (m_InjectReadBack)
            {
                Log.Info($"INJECTED FAULT save-readback: checkpoint #{Id}'s archive is reported unreadable.");
                return false;
            }
            try
            {
                // The record is this operation's own; the freshness test additionally refuses an older archive at the same path.
                return save.IsActuallySaved && File.Exists(save.FolderName) && new FileInfo(save.FolderName).Length > 0
                    && File.GetLastWriteTimeUtc(save.FolderName) >= m_StartedUtc.AddSeconds(-2)
                    && !string.IsNullOrEmpty(save.Saver?.ReadHeader());
            }
            catch (Exception ex)
            {
                Log.Error($"Checkpoint #{Id} could not be read back.", ex);
                return false;
            }
        }

        private static IEnumerator CurrentProcess()
        {
            object record = CurrentField.GetValue(LoadingProcess.Instance);
            return record == null ? null : (IEnumerator)ProcessField.GetValue(record);
        }

        private static List<IEnumerator> Queued()
        {
            return ((IEnumerable)QueueField.GetValue(LoadingProcess.Instance)).Cast<object>().Select(r => (IEnumerator)ProcessField.GetValue(r)).ToList();
        }

        private static IEnumerator<object> Nothing()
        {
            yield return null;
        }

        private static IEnumerator<object> Hold(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }
    }

    internal enum CheckpointRefusal
    {
        None,
        Disallowed,
        SingleSave,
        Busy,
    }
}
