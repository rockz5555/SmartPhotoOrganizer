using System.Collections.Generic;
using System.Linq;
using SmartPhotoOrganizer.DataStructures;

namespace SmartPhotoOrganizer
{
    public class DirectorySet
    {
        public List<BaseDirectory> BaseDirectories { get; set; }

        public DirectorySet()
        {
            BaseDirectories = new List<BaseDirectory>();
        }

        public bool PathIsIncluded(string path)
        {
            path = path.ToLowerInvariant();
            var included = false;

            foreach (var baseDirectory in BaseDirectories)
            {
                if (!baseDirectory.Missing && path.StartsWith(baseDirectory.Path.ToLowerInvariant()))
                {
                    included = true;

                    foreach (var excludedPath in baseDirectory.Exclusions)
                    {
                        if (path.StartsWith(excludedPath.ToLowerInvariant()))
                        {
                            included = false;
                            break;
                        }
                    }
                    break;
                }
            }
            return included;
        }

        public bool Equals(DirectorySet otherDirectorySet)
        {
            if (BaseDirectories.Count != otherDirectorySet.BaseDirectories.Count)
            {
                return false;
            }

            for (var i = 0; i < BaseDirectories.Count; i++)
            {
                if (BaseDirectories[i].Missing != otherDirectorySet.BaseDirectories[i].Missing)
                {
                    return false;
                }

                if (BaseDirectories[i].Path != otherDirectorySet.BaseDirectories[i].Path)
                {
                    return false;
                }

                if (BaseDirectories[i].Exclusions.Count != otherDirectorySet.BaseDirectories[i].Exclusions.Count)
                {
                    return false;
                }

                if (BaseDirectories[i].Exclusions.Where((t, j) => t != otherDirectorySet.BaseDirectories[i].Exclusions[j]).Any())
                {
                    return false;
                }
            }

            return true;
        }

        public string GetRelativeName(string filePath)
        {
            var filePathLower = filePath.ToLowerInvariant();
            return (from baseDirectory in BaseDirectories where filePathLower.StartsWith(baseDirectory.Path.ToLowerInvariant()) select filePath.Substring(baseDirectory.Path.Length + 1)).FirstOrDefault();
        }
    }
}
