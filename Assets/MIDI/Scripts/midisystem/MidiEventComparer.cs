using System;
using System.Collections.Generic;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// MIDI event sorter for playback
    /// </summary>
    public class MidiEventComparer : Comparer<MidiEvent>
    {
        /// <inheritdoc cref="Comparer{T}" />
        public override int Compare(MidiEvent lhs, MidiEvent rhs)
        {
            if (lhs == null)
            {
                throw new ArgumentNullException(nameof(lhs));
            }
            if (rhs == null)
            {
                throw new ArgumentNullException(nameof(rhs));
            }

            // sort by tick
            var tickDifference = (int)(lhs.GetTick() - rhs.GetTick());
            if (tickDifference != 0)
            {
                return tickDifference * 256;
            }

            var lhsMessage = lhs.GetMessage().GetMessage();
            var rhsMessage = rhs.GetMessage().GetMessage();

            // apply zero if message is empty
            if (lhsMessage == null || lhsMessage.Length < 1)
            {
                lhsMessage = new byte[] { 0 };
            }

            if (rhsMessage == null || rhsMessage.Length < 1)
            {
                rhsMessage = new byte[] { 0 };
            }

            // same timing
            // sort by the MIDI data priority order, as:
            // system message > control messages > note on > note off
            // swap the priority of note on, and note off
            var lhsInt = lhsMessage[0] & 0xf0;
            var rhsInt = rhsMessage[0] & 0xf0;

            if ((lhsInt & 0x90) == 0x80)
            {
                lhsInt |= 0x10;
            }
            else
            {
                lhsInt &= ~0x10;
            }

            if ((rhsInt & 0x90) == 0x80)
            {
                rhsInt |= 0x10;
            }
            else
            {
                rhsInt &= ~0x10;
            }

            return -(lhsInt - rhsInt);
        }
    }
}