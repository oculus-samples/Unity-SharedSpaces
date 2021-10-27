namespace Photon.Voice.Unity
{
    using System.Linq;

    // this was added for backwards compatibility
    public class AudioInEnumeratorEx : Voice.AudioInEnumerator
    {
        public AudioInEnumeratorEx(ILogger logger) : base(logger)
        {
        }

        public int Count
        {
            get
            {
                return this.Devices.Count();
            }
        }

        public bool IDIsValid(int id)
        {
            foreach (var d in this.Devices)
            {
                if (d.IDInt == id)
                {
                    return true;
                }
            }
            return false;
        }

        public string NameAtIndex(int index)
        {
            int i = 0;
            foreach (var d in this.Devices)
            {
                if (i == index)
                {
                    return d.Name;
                }
                i++;
            }
            return string.Empty;
        }

        public int IDAtIndex(int index)
        {
            int i = 0;
            foreach (var d in this.Devices)
            {
                if (i == index)
                {
                    return d.IDInt;
                }
                i++;
            }
            return -1;
        }
    }
}

