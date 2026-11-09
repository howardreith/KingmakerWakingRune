using System;

namespace KingmakerWakingRune.Sanctum
{
    internal enum CheckpointProgress
    {
        Running,
        Succeeded,
        Failed,
    }

    /// <summary>What the transfer needs from the game. Kept narrow so the failure paths can be driven without it.</summary>
    internal interface ITransferHost
    {
        float Now { get; }
        float FadeSeconds { get; }

        /// <summary>True when the area load has nothing left queued.</summary>
        bool LoadIdle { get; }

        /// <summary>Asks open windows to close; true once none remain.</summary>
        bool WindowsClosed();

        /// <summary>Starts the save. Returns null when started, or why it may not.</summary>
        string StartCheckpoint();

        CheckpointProgress PollCheckpoint(out string failure);
        void FadeOut();
        void LoadDestination();
        void CommitArrival();
        void FadeIn();
        void Warn(string text);
        void ReportStranded(string text);
        void Info(string text);
        void Error(string text, Exception exception);
    }

    /// <summary>
    /// The token's journey as a state machine ticked once a frame: checkpoint, fade, native load, commit on a ready
    /// arrival, fade back. Every way out of it restores the screen and clears <see cref="InProgress"/>; the token is
    /// spent only by a commit, and a commit happens only once the destination has reported itself ready.
    /// </summary>
    internal sealed class RefugeTransferFlow
    {
        public const float WindowSeconds = 3f;
        public const float CheckpointSeconds = 90f;
        public const float ArrivalSeconds = 240f;
        public const float SettleSeconds = 30f;
        /// <summary>How long the load queue may sit empty with no arrival before the load is taken to have died.</summary>
        public const float DeadLoadSeconds = 3f;

        public const string CheckpointUnconfirmed = "The token's magic falters: the game could not confirm a save before leaving. Nothing was spent; try again.";

        private enum Stage
        {
            Idle,
            ClosingWindows,
            Checkpointing,
            FadingOut,
            Travelling,
            Settling,
        }

        private readonly ITransferHost m_Host;
        private Stage m_Stage = Stage.Idle;
        private float m_Deadline;
        private float m_IdleSince = -1f;
        private bool m_Committed;
        private string m_ArrivalFailure;

        public RefugeTransferFlow(ITransferHost host)
        {
            m_Host = host;
        }

        public bool InProgress => m_Stage != Stage.Idle;

        public bool Committed => m_Committed;

        private bool Departed => m_Stage == Stage.Travelling || m_Stage == Stage.Settling;

        public void Begin()
        {
            m_Committed = false;
            m_ArrivalFailure = null;
            m_IdleSince = -1f;
            m_Stage = Stage.ClosingWindows;
            m_Deadline = m_Host.Now + WindowSeconds;
        }

        public void Tick()
        {
            if (m_Stage == Stage.Idle)
            {
                return;
            }
            try
            {
                Step();
            }
            catch (Exception ex)
            {
                m_Host.Error("Refuge transfer step failed.", ex);
                if (Departed)
                {
                    Strand("The Sanctum could not be reached.");
                }
                else
                {
                    Abandon(CheckpointUnconfirmed);
                }
            }
        }

        private void Step()
        {
            switch (m_Stage)
            {
                case Stage.ClosingWindows:
                    if (m_Host.WindowsClosed() || m_Host.Now >= m_Deadline)
                    {
                        string refusal = m_Host.StartCheckpoint();
                        if (refusal != null)
                        {
                            Abandon(refusal);
                            return;
                        }
                        m_Stage = Stage.Checkpointing;
                        m_Deadline = m_Host.Now + CheckpointSeconds;
                    }
                    break;
                case Stage.Checkpointing:
                    switch (m_Host.PollCheckpoint(out string failure))
                    {
                        case CheckpointProgress.Succeeded:
                            m_Host.FadeOut();
                            m_Stage = Stage.FadingOut;
                            m_Deadline = m_Host.Now + m_Host.FadeSeconds + 0.2f;
                            break;
                        case CheckpointProgress.Failed:
                            Abandon(failure ?? CheckpointUnconfirmed);
                            break;
                        default:
                            // The native save is left to finish on its own; it is never interrupted and no load starts over it.
                            if (m_Host.Now >= m_Deadline)
                            {
                                Abandon(CheckpointUnconfirmed);
                            }
                            break;
                    }
                    break;
                case Stage.FadingOut:
                    if (m_Host.Now >= m_Deadline)
                    {
                        m_Stage = Stage.Travelling;
                        m_Deadline = m_Host.Now + ArrivalSeconds;
                        m_Host.LoadDestination();
                    }
                    break;
                case Stage.Travelling:
                    StepTravelling();
                    break;
                case Stage.Settling:
                    if (m_Host.LoadIdle || m_Host.Now >= m_Deadline)
                    {
                        m_Host.FadeIn();
                        m_Stage = Stage.Idle;
                        m_Host.Info("Refuge transfer complete; control restored.");
                    }
                    break;
            }
        }

        private void StepTravelling()
        {
            if (m_ArrivalFailure != null)
            {
                Strand(m_ArrivalFailure);
                return;
            }
            if (m_Committed)
            {
                m_Stage = Stage.Settling;
                m_Deadline = m_Host.Now + SettleSeconds;
                return;
            }
            if (m_Host.Now >= m_Deadline)
            {
                Strand("The journey to the Sanctum did not finish.");
                return;
            }
            if (!m_Host.LoadIdle)
            {
                m_IdleSince = -1f;
                return;
            }
            if (m_IdleSince < 0f)
            {
                m_IdleSince = m_Host.Now;
            }
            else if (m_Host.Now - m_IdleSince >= DeadLoadSeconds)
            {
                Strand("The journey to the Sanctum was interrupted.");
            }
        }

        /// <summary>Called when the destination has finished loading. Commits at most once, and only into a ready area.</summary>
        public void OnArrived(bool destinationReady, bool alreadyEntered, string problem)
        {
            if (m_Stage != Stage.Travelling || m_Committed || m_ArrivalFailure != null)
            {
                return;
            }
            if (!destinationReady)
            {
                m_Host.Error($"Refuge arrival refused: {problem}.", null);
                m_ArrivalFailure = "The Sanctum could not be prepared.";
                return;
            }
            if (SanctumPolicy.ShouldCommitArrival(true, true, alreadyEntered))
            {
                try
                {
                    m_Host.CommitArrival();
                    m_Committed = true;
                }
                catch (Exception ex)
                {
                    m_Host.Error("Refuge arrival could not be committed.", ex);
                    m_ArrivalFailure = "The Sanctum could not be entered.";
                }
            }
        }

        /// <summary>A save load or new game replaced the world; whatever was in flight ends with it.</summary>
        public void Cancel(string reason)
        {
            if (m_Stage == Stage.Idle)
            {
                return;
            }
            m_Host.Info($"Refuge transfer state cleared: {reason}.");
            m_Stage = Stage.Idle;
            m_Host.FadeIn();
        }

        /// <summary>Nothing has left the lodge: the party simply stays and may try again.</summary>
        private void Abandon(string message)
        {
            m_Stage = Stage.Idle;
            m_Host.Info($"Refuge transfer abandoned before departure: {message}");
            m_Host.FadeIn();
            m_Host.Warn(message);
        }

        /// <summary>The area may already have changed, so nothing is rolled back: the verified checkpoint is the way home.</summary>
        private void Strand(string what)
        {
            m_Stage = Stage.Idle;
            m_Host.FadeIn();
            m_Host.ReportStranded(what);
        }
    }
}
