// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;

namespace Reporter.Views;

/// <summary>
/// Fire-and-forget haptic feedback that degrades silently on platforms without a haptic engine.
/// </summary>
internal static class Haptics
{
    /// <summary>
    /// Plays a light selection tick (filter tabs, toggles).
    /// </summary>
    public static void Tick()
    {
        Perform(HapticFeedbackType.Click);
    }

    /// <summary>
    /// Plays a stronger confirmation (article dismissed, saved).
    /// </summary>
    public static void Confirm()
    {
        Perform(HapticFeedbackType.LongPress);
    }

    private static void Perform(HapticFeedbackType type)
    {
        try
        {
            if (HapticFeedback.Default.IsSupported)
            {
                HapticFeedback.Default.Perform(type);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Haptics.Perform failed: {ex.Message}");
        }
    }
}
