using System.Collections.Generic;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Interface for MIDI Device
    /// </summary>
    public interface IMidiDevice
    {
        /// <summary>
        /// Get the device information
        /// </summary>
        /// <returns>the device information</returns>
        Info GetDeviceInfo();

        /// <summary>
        /// Open the <see cref="IMidiDevice" />. This method must be called at getting the new instance.
        /// </summary>
        void Open();

        /// <summary>
        /// Close the <see cref="IMidiDevice" />. This method must be called at finishing to use the instance.
        /// </summary>
        void Close();

        /// <summary>
        /// Check if the {@link MidiDevice} opened.
        /// </summary>
        /// <returns>true if already opened</returns>
        bool GetIsOpen();

        /// <summary>
        /// Get the <see cref="IMidiDevice" />'s timeStamp.
        /// </summary>
        /// <returns>timestamp value. -1 if the timeStamp not supported.</returns>
        long GetMicrosecondPosition();

        /// <summary>
        /// Get the number of the <see cref="IReceiver" />s.
        /// </summary>
        /// <returns>the number of the <see cref="IReceiver" />s.</returns>
        int GetMaxReceivers();

        /// <summary>
        /// Get the number of the <see cref="ITransmitter" />s.
        /// </summary>
        /// <returns>the number of the <see cref="ITransmitter" />s.</returns>
        int GetMaxTransmitters();

        /// <summary>
        /// Get the default <see cref="IReceiver" />.
        /// </summary>
        /// <returns>the default <see cref="IReceiver" />.</returns>
        IReceiver GetReceiver();

        /// <summary>
        /// Get the all of <see cref="IReceiver" />s.
        /// </summary>
        /// <returns>the all of <see cref="IReceiver" />s.</returns>
        List<IReceiver> GetReceivers();

        /// <summary>
        /// Get the default <see cref="ITransmitter" />.
        /// </summary>
        /// <returns>the default <see cref="ITransmitter" />.</returns>
        ITransmitter GetTransmitter();

        /// <summary>
        /// Get the all of <see cref="ITransmitter" />s.
        /// </summary>
        /// <returns>the all of <see cref="ITransmitter" />s.</returns>
        List<ITransmitter> GetTransmitters();
    }

    /// <summary>
    /// Represents the <see cref="IMidiDevice" />'s information
    /// </summary>
    public class Info
    {
        private readonly string name;
        private readonly string vendor;
        private readonly string description;
        private readonly string version;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">the name string</param>
        /// <param name="vendor">the vendor string</param>
        /// <param name="description">the description string</param>
        /// <param name="version">the version string</param>
        public Info(string name, string vendor, string description, string version)
        {
            this.name = name;
            this.vendor = vendor;
            this.description = description;
            this.version = version;
        }

        /// <summary>
        /// Get the name of <see cref="IMidiDevice" />
        /// </summary>
        /// <returns>the name of <see cref="IMidiDevice" /></returns>
        public string GetName()
        {
            return name;
        }

        /// <summary>
        /// Get the vendor of <see cref="IMidiDevice" />
        /// </summary>
        /// <returns>the vendor of <see cref="IMidiDevice" /></returns>
        public string GetVendor()
        {
            return vendor;
        }

        /// <summary>
        /// Get the description of <see cref="IMidiDevice" />
        /// </summary>
        /// <returns>the description of <see cref="IMidiDevice" /></returns>
        public string GetDescription()
        {
            return description;
        }

        /// <summary>
        /// Get the version of <see cref="IMidiDevice" />
        /// </summary>
        /// <returns>the version of <see cref="IMidiDevice" /></returns>
        public string GetVersion()
        {
            return version;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            return name;
        }

        /// <inheritdoc cref="object.GetHashCode"/>
        public override int GetHashCode()
        {
            var prime = 31;
            var result = 1;
            result = prime * result + description.GetHashCode();
            result = prime * result + name.GetHashCode();
            result = prime * result + vendor.GetHashCode();
            result = prime * result + version.GetHashCode();
            return result;
        }

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Info)obj;
            if (!description.Equals(other.description))
            {
                return false;
            }

            if (!name.Equals(other.name))
            {
                return false;
            }

            if (!vendor.Equals(other.vendor))
            {
                return false;
            }

            if (!version.Equals(other.version))
            {
                return false;
            }

            return true;
        }
    }
}