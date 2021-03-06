﻿// Copyright 2008 - Paul den Dulk (Geodan)
// 
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Globalization;
using System.IO;

namespace BruTile.Cache
{
    public class FileCache : ITileCache<byte[]>
    {
        #region Fields

        private object _syncRoot = new object();
        private string _directory;
        private string _format;
        private int _mod = 0;



        #endregion

        #region Public Methods
        /// <remarks>The constructor creates the storage _directory if it does not exist.</remarks>
        public FileCache(string directory, string format)
        {
            this._directory = directory;
            this._format = format;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <remarks>Added by gheloBytes</remarks>
        public FileCache(string directory, int mod)
        {
            this._directory = directory;
            this._mod = mod;

            if (this._mod < 1)
            {
                throw new Exception("Modulo parameter must be greater than zero.");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }


        public void Add(TileKey key, byte[] image)
        {
            lock (this._syncRoot)
            {
                if (this.Exists(key))
                {
                    return; // ignore
                }
                string dir = GetDirectoryName(key);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                this.WriteToFile(image, key);
            }
        }

        public byte[] Find(TileKey key)
        {
            lock (_syncRoot)
            {
                if (!Exists(key)) return null; // to indicate not found
                using (FileStream fileStream = new FileStream(GetFileName(key), FileMode.Open, FileAccess.Read))
                {
                    return Utilities.ReadFully(fileStream);
                }
            }
        }

        public void Remove(TileKey key)
        {
            lock (_syncRoot)
            {
                if (Exists(key))
                {
                  File.Delete(GetFileName(key));
                }
            }
        }

        private bool Exists(TileKey key)
        {
            string s = GetFileName(key);
          return File.Exists(GetFileName(key));
        }

        #endregion

        #region Private Methods

        private string GetFileName(TileKey key)
        {
            if (this._mod == 0)
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}\\{1}.{2}", GetDirectoryName(key), key.Row, _format);
            }
            else
            {
                string subFolder = ((key.Col * 256 + key.Row) % _mod).ToString();
                return String.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\{2}\\{3}_{4}.mgm", _directory,key.Level, subFolder, key.Col, key.Row);
            }
        }

        private string GetDirectoryName(TileKey key)
        {
            return String.Format(CultureInfo.InvariantCulture,
              "{0}\\{1}\\{2}", _directory, key.Level, key.Col);
        }

        private void WriteToFile(byte[] image, TileKey key)
        {
          using (FileStream fileStream = File.Open(GetFileName(key), FileMode.CreateNew))
            {
                fileStream.Write(image, 0, (int)image.Length);
                fileStream.Flush();
                fileStream.Close();
            }
        }

        #endregion
    }
}
