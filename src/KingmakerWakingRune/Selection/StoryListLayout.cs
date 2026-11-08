using UnityEngine;
using UnityEngine.UI;

namespace KingmakerWakingRune.Selection
{
    /// <summary>
    /// The native scenario list sits in a fixed-height frame ("Selector", sized for three entries)
    /// above the description block. Once the list is laid out, grows the frame downward by exactly
    /// the missing height so every entry is visible without scrolling, and shrinks the description
    /// block from its top by the same amount. Every show starts from the captured native values, so
    /// reopening the window never accumulates.
    /// </summary>
    internal sealed class StoryListLayout : MonoBehaviour
    {
        private ScrollRect m_Scroll;
        private RectTransform m_Content;
        private RectTransform m_Viewport;
        private RectTransform m_Frame;
        private RectTransform m_DescriptionBlock;
        private Vector2 m_FrameSize;
        private Vector2 m_FramePosition;
        private Vector2 m_BlockSize;
        private Vector2 m_BlockPosition;
        private bool m_Pending;

        public static void Apply(RectTransform storyContainer, RectTransform description)
        {
            ScrollRect scroll = storyContainer.GetComponentInParent<ScrollRect>();
            if (scroll == null)
            {
                Log.Error("Scenario list has no ScrollRect; layout left native.");
                return;
            }
            var layout = scroll.GetComponent<StoryListLayout>();
            if (layout == null)
            {
                layout = scroll.gameObject.AddComponent<StoryListLayout>();
                layout.Capture(scroll, storyContainer, description);
            }
            layout.Restore();
            layout.m_Pending = true;
        }

        private void Capture(ScrollRect scroll, RectTransform content, RectTransform description)
        {
            m_Scroll = scroll;
            m_Content = content;
            m_Viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)content.parent;
            // The frame is the first ancestor whose size is not driven by a parent layout group.
            m_Frame = (RectTransform)scroll.transform;
            while (m_Frame.parent != null && m_Frame.parent.GetComponent<LayoutGroup>() != null)
            {
                m_Frame = (RectTransform)m_Frame.parent;
            }
            // The description text is laid out by its block; move the block, not the text.
            m_DescriptionBlock = description.parent != null && description.parent.GetComponent<LayoutGroup>() != null
                ? (RectTransform)description.parent
                : description;
            m_FrameSize = m_Frame.sizeDelta;
            m_FramePosition = m_Frame.anchoredPosition;
            m_BlockSize = m_DescriptionBlock.sizeDelta;
            m_BlockPosition = m_DescriptionBlock.anchoredPosition;
        }

        private void Restore()
        {
            m_Frame.sizeDelta = m_FrameSize;
            m_Frame.anchoredPosition = m_FramePosition;
            m_DescriptionBlock.sizeDelta = m_BlockSize;
            m_DescriptionBlock.anchoredPosition = m_BlockPosition;
        }

        private void LateUpdate()
        {
            if (!m_Pending || m_Viewport.rect.height <= 1f)
            {
                return;
            }
            m_Pending = false;
            // Measure the restored native layout, not a stale one from the previous show.
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Frame);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
            float needed = LayoutUtility.GetPreferredHeight(m_Content);
            float deficit = Mathf.Ceil(needed - m_Viewport.rect.height);
            if (deficit <= 0f)
            {
                Log.Info($"Scenario list fits natively (content {needed:0}, viewport {m_Viewport.rect.height:0}).");
                return;
            }
            // Keep the frame's top edge and the block's bottom edge where the native layout put them.
            m_Frame.sizeDelta = m_FrameSize + new Vector2(0f, deficit);
            m_Frame.anchoredPosition = m_FramePosition - new Vector2(0f, deficit * (1f - m_Frame.pivot.y));
            m_DescriptionBlock.sizeDelta = m_BlockSize - new Vector2(0f, deficit);
            m_DescriptionBlock.anchoredPosition = m_BlockPosition - new Vector2(0f, deficit * (1f - m_DescriptionBlock.pivot.y));
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Frame);
            m_Scroll.verticalNormalizedPosition = 1f;
            Log.Info($"Scenario list frame grown by {deficit:0} (content {needed:0}, viewport was {m_Viewport.rect.height:0}).");
        }
    }
}
