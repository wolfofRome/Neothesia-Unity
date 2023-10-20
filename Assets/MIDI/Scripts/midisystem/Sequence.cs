using System;
using System.Collections.Generic;
using System.Linq;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Represents MIDI Sequence
    /// </summary>
    public class Sequence
    {
        private const float Nearly = 0.00001f;

        public const float Ppq = 0.0f;
        public const float Smpte24 = 24.0f;
        public const float Smpte25 = 25.0f;
        public const float Smpte30 = 30.0f;
        public const float Smpte30Drop = 29.969999313354492f;

        /// <summary>
        /// Compares two divisionTypes are equal
        /// </summary>
        /// <param name="a">DivisionType</param>
        /// <param name="b">DivisionType</param>
        /// <returns>true if equals</returns>
        public static bool DivisionTypeEquals(float a, float b)
        {
            return Math.Abs(a - b) < Nearly;
        }

        private static readonly float[] SupportingDivisionTypes = { Ppq, Smpte24, Smpte25, Smpte30, Smpte30Drop };

        private readonly float divisionType;
        private readonly int resolution;
        private readonly List<Track> tracks;

        /// <summary>
        /// Create <see cref="Sequence" /> with divisionType and resolution.
        /// </summary>
        /// <param name="divisionType">
        /// <see cref="Ppq" />, <see cref="Smpte24" />, <see cref="Smpte25" />,
        /// <see cref="Smpte30Drop" />, or <see cref="Smpte30" />.
        /// </param>
        /// <param name="resolution">
        ///     <ul>
        ///         <li>divisionType == <see cref="Ppq" /> : 0 - 0x7fff. typically 24, 480</li>
        ///         <li>
        ///         divisionType == <see cref="Smpte24" />, <see cref="Smpte25" />, <see cref="Smpte30Drop" />,
        ///         <see cref="Smpte30" /> : 0 - 0xff
        ///         </li>
        ///     </ul>
        /// </param>
        /// <exception cref="InvalidMidiDataException">Invalid parameter specified.</exception>
        public Sequence(float divisionType, int resolution)
        {
            if (IsSupportingDivisionType(divisionType) == false)
            {
                throw new InvalidMidiDataException("Unsupported division type: " + divisionType);
            }

            this.divisionType = divisionType;
            this.resolution = resolution;
            tracks = new List<Track>();
        }

        /// <summary>
        /// Create <see cref="Sequence" /> with divisionType, resolution and numberOfTracks.
        /// </summary>
        /// <param name="divisionType">
        /// <see cref="Ppq" />, <see cref="Smpte24" />, <see cref="Smpte25" />,
        /// <see cref="Smpte30Drop" />, or <see cref="Smpte30" />.
        /// </param>
        /// <param name="resolution">
        ///     <ul>
        ///         <li>divisionType == <see cref="Ppq" /> : 0 - 0x7fff. typically 24, 480</li>
        ///         <li>
        ///         divisionType == <see cref="Smpte24" />, <see cref="Smpte25" />, <see cref="Smpte30Drop" />,
        ///         <see cref="Smpte30" /> : 0 - 0xff
        ///         </li>
        ///     </ul>
        /// </param>
        /// <param name="numberOfTracks"> &gt; 0</param>
        public Sequence(float divisionType, int resolution, int numberOfTracks) : this(divisionType, resolution)
        {
            if (numberOfTracks <= 0)
            {
                return;
            }

            for (var i = 0; i < numberOfTracks; i++)
            {
                tracks.Add(new Track());
            }
        }

        /// <summary>
        /// Check if the divisionType supported
        /// </summary>
        /// <param name="divisionType">the divisionType</param>
        /// <returns>true if the specified divisionType is supported</returns>
        private static bool IsSupportingDivisionType(float divisionType)
        {
            return SupportingDivisionTypes.Any(supportingDivisionType => DivisionTypeEquals(divisionType, supportingDivisionType));
        }

        /// <summary>
        /// Create an empty <see cref="Track" />
        /// </summary>
        /// <returns>an empty <see cref="Track" /></returns>
        public Track CreateTrack()
        {
            // new Tracks accrue to the end of vector
            var track = new Track();
            tracks.Add(track);
            return track;
        }

        /// <summary>
        /// Delete specified <see cref="Track" />
        /// </summary>
        /// <param name="track">to delete</param>
        /// <returns>true if the track is successfully deleted</returns>
        public bool DeleteTrack(Track track)
        {
            return tracks.Remove(track);
        }

        /// <summary>
        /// Get the divisionType of the <see cref="Sequence" />
        /// </summary>
        /// <returns>the divisionType of the <see cref="Sequence" /></returns>
        public float GetDivisionType()
        {
            return divisionType;
        }

        /// <summary>
        /// Get the <see cref="Sequence" /> length in microseconds
        /// </summary>
        /// <returns>the <see cref="Sequence" /> length in microseconds</returns>
        public long GetMicrosecondLength()
        {
            return (long)(1000000.0f * GetTickLength() /
                          ((divisionType == 0.0f ? 2 : divisionType) * resolution * 1.0f));
        }

        /// <summary>
        /// Get the resolution
        /// </summary>
        /// <returns>the resolution</returns>
        public int GetResolution()
        {
            return resolution;
        }

        /// <summary>
        /// Get the biggest tick length
        /// </summary>
        /// <returns>tick length</returns>
        public long GetTickLength()
        {
            /*
			 * this method return the biggest value of tick of all tracks contain in the Sequence
			 */
            return tracks.Select(track => track.Ticks()).Max();
        }

        /// <summary>
        /// Get the array of <see cref="Track" />s
        /// </summary>
        /// <returns>array of tracks</returns>
        public Track[] GetTracks()
        {
            return tracks.ToArray();
        }
    }
}