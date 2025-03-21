using System;
using System.Collections.Generic;
using System.IO;

namespace XmlNotepad
{
    public class MostRecentlyUsedEventArgs : EventArgs
    {
        public string Selection { get; set; }
    }

    public class RecentFiles
    {
        private List<Uri> recentFiles = new List<Uri>();
        private const int MaxRecentFiles = 10;

        public event EventHandler<MostRecentlyUsedEventArgs> RecentFileSelected;
        public event EventHandler RecentFilesChanged;
        
        public Uri[] Items 
        {
            get { return recentFiles.ToArray(); }
        }
        
        public void Add(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            
            Uri uri;
            try
            {
                uri = new Uri(filePath);
            }
            catch
            {
                // Handle invalid URI format by creating a file URI
                uri = new Uri("file://" + filePath);
            }
            
            // Remove existing entry if present
            for (int i = 0; i < recentFiles.Count; i++)
            {
                if (recentFiles[i].LocalPath.Equals(uri.LocalPath, StringComparison.OrdinalIgnoreCase))
                {
                    recentFiles.RemoveAt(i);
                    break;
                }
            }
            
            // Add at beginning of list
            recentFiles.Insert(0, uri);
            
            // Trim list if too long
            while (recentFiles.Count > MaxRecentFiles)
            {
                recentFiles.RemoveAt(recentFiles.Count - 1);
            }
            
            OnRecentFilesChanged();
        }

        public Uri[] GetFiles()
        {
            return Items;
        }

        public Uri[] GetRelativeUris()
        {
            // This method returns the URIs, possibly converting absolute URIs to relative ones
            // For simplicity, just return the items directly
            return GetFiles();
        }
        
        public void SetItems(Uri[] files)
        {
            recentFiles.Clear();
            if (files != null)
            {
                recentFiles.AddRange(files);
            }
            OnRecentFilesChanged();
        }

        public void SetFiles(Uri[] files)
        {
            SetItems(files);
        }
        
        public void Clear()
        {
            recentFiles.Clear();
            OnRecentFilesChanged();
        }
        
        // Changed from protected to public
        public void OnRecentFileSelected(string selection)
        {
            RecentFileSelected?.Invoke(this, new MostRecentlyUsedEventArgs { Selection = selection });
        }

        // Added overload to accept Uri parameter
        public void OnRecentFileSelected(Uri uri)
        {
            if (uri != null)
            {
                OnRecentFileSelected(uri.IsFile ? uri.LocalPath : uri.ToString());
            }
        }
        
        protected void OnRecentFilesChanged()
        {
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
