// SaveFile.cs
// Each SaveFile is a reference to a dyad of json files, which are switched between whenever a file is being saved. Whenever a file is loaded, its 
// MD5 is checked against it. Likewise, whenever a file is saved, its MD5 is saved as well. If a file appears corrupt, then the opposite dyad file is
// loaded. If both files are corrupt, the program throws an exception.
//

// TODO: get rid of the md5 junk

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BigSister.Settings
{
    public class SaveFile
    {
        // When it's called base file, what this means is that this is its file name without anything fancy. For example, with every file, there will
        // be a...
        //
        // File.A
        // File.A.MD5
        // File.B
        // File.B.MD5
        //
        // ... And the file system will switch between each file and saving in the MD5.
        private readonly string baseFileName;

        /// <summary>Get a string describing BaseFile.A</summary>
        private string BaseFileA => $"{baseFileName}.A";
        /// <summary>Get a string describing BaseFile.B</summary>
        private string BaseFileB => $"{baseFileName}.B";

        private readonly SemaphoreSlim semaphoreSlim;

        public SaveFile() { }
        public SaveFile(string baseFile)
        {
            baseFileName = baseFile;
            semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        ~SaveFile()
        {
            semaphoreSlim.Dispose();
        }

        #region Public Methods

        /// <summary>Save information to savefile.</summary>
        public void Save<T>(T saveData)
            => Save(JsonConvert.SerializeObject(saveData));


        /// <summary>Save json information to savefile.</summary>
        public void Save(string json)
        {
            string saveFile = GetNextSaveFile();

            // If there's no value, that means we need to default to bfA.
            if (saveFile.Equals(String.Empty))
            {
                saveFile = BaseFileA;
            }

            Task.Run(() =>
            {
                // Let's wait out any other threads using the files.
                semaphoreSlim.Wait();

                try
                {
                    // Let's save the SaveFile and the MD5.
#pragma warning disable IDE0063
                    using (var jsonWriter = new StreamWriter(saveFile, false))
                    using (var md5Writer = new StreamWriter(GetMD5File(saveFile), false))
                    {
                        // Write data.
                        var a = jsonWriter.WriteAsync(json);
                        var b = md5Writer.WriteAsync(GetHash(json));

                        Task.WaitAll(a, b); // Block thread until all data is written.

                        // Flush streams.
                        var c = jsonWriter.FlushAsync();
                        var d = md5Writer.FlushAsync();

                        Task.WaitAll(c, d); // Block thread until all streams are flushed.

                        jsonWriter.Close();
                        md5Writer.Close();
                    }
#pragma warning restore IDE0063
                }
                catch (Exception e)
                {
                    throw new SaveFileException("Unable to save settings.", e);
                }
                finally
                {
                    // Release the semaphore.
                    semaphoreSlim.Release();
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>Load information from SaveFile.</summary>
        /// <typeparam name="T">Json serializable struct holding information.</typeparam>
        /// <param name="lockObj">Locking object for class.</param>
        public T Load<T>()
        {
            T returnVal;

            string loadFile = GetLoadFile();

            if (!loadFile.Equals(String.Empty))
            {
                string fileContents;

                fileContents = ReadFile(loadFile);

                if (!fileContents.Equals(String.Empty))
                {
                    returnVal = (T)JsonConvert.DeserializeObject(fileContents, typeof(T));
                }
                else
                {
                    throw new SaveFileException("SaveFile found, unable to load contents.");
                }
            }
            else throw new SaveFileException("Unable to load a SaveFile.");

            return returnVal;
        }

        // If either of these exists, it's an existing save file.
        /// <summary>Check if the save file exists.</summary>
        /// <returns>True if save file exists.</returns>
        public bool IsExistingSaveFile() => File.Exists(BaseFileA) || File.Exists(BaseFileB);

#pragma warning disable IDE0063
        /// <summary>Read information from file.</summary>
        /// <param name="file">File to read from.</param>
        /// <returns>File contents.</returns>
        internal static string ReadFile(string file)
        {
            string returnVal;

            using (FileStream fs = File.OpenRead(file))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    returnVal = sr.ReadToEnd();
                }
            }

            return returnVal;
        }
#pragma warning restore IDE0063

        /// <summary>Get the file's MD5.</summary>
        /// <param name="file">File to get MD5 from.</param>
        /// <returns>The file's MD5.</returns>
        internal static string GetFileMD5(string file)
            => GetHash(ReadFile(file));

        /// <summary>Gets a hash of a string in Base64.</summary>
        internal static string GetHash(string contents)
        {
            byte[] hash;

            using (MD5 fileMD5 = MD5.Create())
            {
                hash = fileMD5.ComputeHash(Encoding.UTF8.GetBytes(contents));
            }

            return Convert.ToBase64String(hash);
        }

        #endregion Public Methods
        // ################################
        #region Private Methods

        /// <summary>Gets the save file we should save to.</summary>
        private string GetNextSaveFile()
        {
            string returnVal;

            bool bfAExists = File.Exists(BaseFileA);
            bool bfBExists = File.Exists(BaseFileB);

            if (bfAExists && bfBExists) // If both files exists, we need to see which one is the oldest one and return it.
            {
                returnVal = // If bfA creation time is greater than (more recent) than bfB, then bfA is the most recent, therefore bfB is the
                            // oldest, and we return that (1: bfB) instead. On the contrary, if bfA is NOT greater (less recent) than bfB, that means
                            // bfA is the oldest, and we return that (2: bfA) instead.
                    File.GetLastWriteTime(BaseFileA).Ticks >= File.GetLastWriteTime(BaseFileB).Ticks ?
                            /*(1)*/ BaseFileB :
                            /*(2)*/ BaseFileA;
            }
            else if (bfAExists && !bfBExists) // If bfA exists AND bfB does NOT exist, return bfB. 
            {
                returnVal = BaseFileB;
            }
            else if (!bfAExists && bfBExists) // If BfA does NOT exist and BfB exists, return bfA.
            {
                returnVal = BaseFileA;
            }
            else // Just return BaseFileA if there's nothing else.
            {
                returnVal = BaseFileA;
            }
            return returnVal;
        }

        /// <summary>Gets the savefile we should load from.</summary>
        private string GetLoadFile()
        {
            string returnVal;

            string oldestFile;
            string newestFile;

            bool bfAExists = File.Exists(BaseFileA);
            bool bfBExists = File.Exists(BaseFileB);

            if (!bfAExists && !bfBExists)
            {
                throw new SaveFileException("Save files could not load: Not found.");
            }


            // If both files exists, we need to see which is the oldest and newest.
            if (bfAExists && bfBExists)
            {
                // If bfA creation time is greater (more recent) than bfB, it means bfA is the newest file. 
                bool bfANewest = File.GetLastWriteTime(BaseFileA).Ticks > File.GetLastWriteTime(BaseFileB).Ticks;

                if (bfANewest)
                {
                    newestFile = BaseFileA;
                    oldestFile = BaseFileB;
                }
                else
                {
                    newestFile = BaseFileB;
                    oldestFile = BaseFileA;
                } // end else
            } // end if
            else if (bfAExists && !bfBExists)
            {
                newestFile = BaseFileA;
                oldestFile = String.Empty;
            } // end else if
            else if (!bfAExists && bfBExists)
            {
                newestFile = BaseFileB;
                oldestFile = String.Empty;
            } // end else if
            else
            {
                newestFile = String.Empty;
                oldestFile = String.Empty;
            } // end else

            // Great. Now we have our newest file and oldest file with String.Empty as a sentinel value indicating it couldn't be found for some
            // reason. Hopefully it was found.


            return newestFile;
        }

        /// <summary>Get MD5 file string from FileBase.</summary>
        private string GetMD5File(string fileBase)
            => $"{fileBase}.md5";


        private bool IsValidJson(string file)
        {
            bool valid = true; // It's valid until proven otherwise.
            string jsonContents = ReadFile(file);

            try
            {
                JsonConvert.DeserializeObject(jsonContents);
            }
            catch
            {
                valid = false;
            }

            return valid;
        }

        /// <summary>Compare two MD5s.</summary>
        /// <param name="md5_1">First MD5.</param>
        /// <param name="md5_2">Second MD5.</param>
        /// <returns>True if the two MD5s are equal.</returns>
        private bool CompareMD5(string md5_1, string md5_2)
        {
            StringComparer sc = StringComparer.OrdinalIgnoreCase;

            return sc.Compare(md5_1, md5_2) == 0;
        }

        #endregion Private Methods
    }
}