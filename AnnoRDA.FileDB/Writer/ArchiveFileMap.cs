using System.Collections.Generic;
using System.Linq;

namespace AnnoRDA.FileDB.Writer
{
    public class ArchiveFileMap
    {
        private struct Entry
        {
            internal string loadPath;
            internal string name;
        }
        private List<Entry> entries = new List<Entry>();

        public void Add(string loadPath, string name)
        {
            this.entries.Add(new Entry() { loadPath = loadPath, name = name });
        }

        public int GetIndexForLoadPath(string loadPath)
        {
            return this.entries.FindIndex((Entry entry) => entry.loadPath == loadPath);
        }

        public IEnumerable<string> GetNames()
        {
            foreach (Entry entry in this.entries) {
                yield return entry.name;
            }
        }
        public string GetLastName()
        {
            return this.entries.Last().name;
        }
    }
}
