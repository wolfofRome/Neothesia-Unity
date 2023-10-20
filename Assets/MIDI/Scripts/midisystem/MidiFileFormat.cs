using System.Collections.Generic;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Represents MIDI File Format
    /// </summary>
    public class MidiFileFormat
    {
        public const int HeaderMThd = 0x4d546864;
        public const int HeaderMTrk = 0x4d54726b;
        public const int UnknownLength = -1;
        private readonly Dictionary<string, object> properties;

        private readonly int byteLength;
        private readonly float divisionType;
        private readonly long microsecondLength;
        private readonly int resolution;
        private readonly int type;

        /// <summary>
        /// Constructor without properties
        /// </summary>
        /// <param name="type">0(SMF 0), or 1(SMF 1)</param>
        /// <param name="divisionType">
        /// {@link Sequence#PPQ}, {@link Sequence#SMPTE_24}, {@link Sequence#SMPTE_25}, {@link
        /// Sequence#SMPTE_30DROP}, or {@link Sequence#SMPTE_30}.
        /// </param>
        /// <param name="resolution">
        ///     <ul>
        ///         <li>divisionType == <see cref="Sequence.Ppq" /> : 0 - 0x7fff. typically 24, 480</li>
        ///         <li>
        ///         divisionType == <see cref="Sequence.Smpte24" />, <see cref="Sequence.Smpte25" />,
        ///         <see cref="Sequence.Smpte30Drop" />, <see cref="Sequence.Smpte30" /> : 0 - 0xff
        ///         </li>
        ///     </ul>
        /// </param>
        /// <param name="bytes">the length of file</param>
        /// <param name="microseconds">the length of time(in micro seconds)</param>
        public MidiFileFormat(int type, float divisionType, int resolution, int bytes, long microseconds)
        {
            this.type = type;
            this.divisionType = divisionType;
            this.resolution = resolution;
            byteLength = bytes;
            microsecondLength = microseconds;
            properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Constructor with properties
        /// </summary>
        /// <param name="type">0(SMF 0), or 1(SMF 1)</param>
        /// <param name="divisionType">
        /// {@link Sequence#PPQ}, {@link Sequence#SMPTE_24}, {@link Sequence#SMPTE_25}, {@link
        /// Sequence#SMPTE_30DROP}, or {@link Sequence#SMPTE_30}.
        /// </param>
        /// <param name="resolution">
        ///     <ul>
        ///         <li>divisionType == <see cref="Sequence.Ppq" /> : 0 - 0x7fff. typically 24, 480</li>
        ///         <li>
        ///         divisionType == <see cref="Sequence.Smpte24" />, <see cref="Sequence.Smpte25" />,
        ///         <see cref="Sequence.Smpte30Drop" />, <see cref="Sequence.Smpte30" /> : 0 - 0xff
        ///         </li>
        ///     </ul>
        /// </param>
        /// <param name="bytes">the length of file</param>
        /// <param name="microseconds">the length of time(in micro seconds)</param>
        /// <param name="properties">the properties</param>
        public MidiFileFormat(int type, float divisionType, int resolution, int bytes, long microseconds,
            Dictionary<string, object> properties) : this(type, divisionType, resolution, bytes, microseconds)
        {
            foreach (var property in properties)
            {
                this.properties.Add(property.Key, property.Value);
            }
        }

        /// <summary>
        /// Get the length of <see cref="MidiFileFormat" />
        /// </summary>
        /// <returns>the length</returns>
        public int GetByteLength()
        {
            return byteLength;
        }

        /// <summary>
        /// Get the division type of <see cref="MidiFileFormat" />
        /// </summary>
        /// <returns>the division type</returns>
        public float GetDivisionType()
        {
            return divisionType;
        }

        /// <summary>
        /// Get the length in microseconds of <see cref="MidiFileFormat" />
        /// </summary>
        /// <returns>the length in microseconds</returns>
        public long GetMicrosecondLength()
        {
            return microsecondLength;
        }

        /// <summary>
        /// Get the property of <see cref="MidiFileFormat" />
        /// </summary>
        /// <param name="key">the property name</param>
        /// <returns>the property</returns>
        public object GetProperty(string key)
        {
            return properties[key];
        }

        /// <summary>
        /// Get the resolution of <see cref="MidiFileFormat" />
        /// </summary>
        /// <returns>the resolution</returns>
        public int GetResolution()
        {
            return resolution;
        }

        /// <summary>
        /// Get the type of <see cref="MidiFileFormat" />
        /// </summary>
        /// <returns>the type</returns>
        public int GetMessageType()
        {
            return type;
        }

        /// <summary>
        /// Get properties <see cref="Dictionary{TKey,TValue}" /> of <see cref="MidiFileFormat" />
        /// </summary>
        /// <returns>properties <see cref="Dictionary{TKey,TValue}" /></returns>
        public Dictionary<string, object> GetProperties()
        {
            return properties; //Collections.unmodifiableMap(properties);
        }
    }
}