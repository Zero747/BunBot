// FilterSystem.Excludes.cs
// This is a system that goes with the word filter system to exclude words, preventing them from triggering the word filter.
//
// EMIKO

using System;
using System.Linq;

namespace BigSister.Filter
{
    public static partial class FilterSystem
    {
        /// <summary>Check if a phrase is in the exclude list.</summary>
        /// <returns>A boolean value indicating if the phrase is excluded.</returns>
        public static bool IsExcluded(string phrase)
            => ExcludeCache.Contains(phrase.ToLower());

        /// <summary>Check if a specified bad word in a message is excluded.</summary>
        /// <returns>A boolean value indicating if the phrase is excluded.</returns>
        public static bool IsExcluded(string msgOriginal, string badWord, int badWordIndex)
        {
            // The default return value is false because if there are no excluded words, then nothing can be excluded.
            bool returnVal = false;
            string msgLwr = msgOriginal.ToLower();

            if (ExcludeCache.Length > 0)
            {
                // Let's loop through every excluded word to check them against the list.
                foreach (var excludedPhrase in ExcludeCache)
                {
                    if (returnVal)
                    {
                        break; // NON-SESE BREAK POINT! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! 
                    }

                    int excludedPhraseLength = excludedPhrase.Length;
                    int foundExcludeIndex = 0;
                    int scanIndex = 0;

                    do
                    {
                        if (scanIndex <= msgOriginal.Length)
                        {
                            foundExcludeIndex = msgLwr.IndexOf(excludedPhrase, scanIndex);
                        }
                        else
                        {
                            foundExcludeIndex = -1;
                        }

                        if (foundExcludeIndex != -1)
                        {
                            // A && B && C && D
                            // (A) the bad word starts at or after the found excluded word.
                            // (B) the bad word ends at or before the found excluded word ends.
                            // (C) exception protect: let's make sure the substring we want to get next is within bounds of the message.
                            // (D) the found excluded word contains the bad word.
                            returnVal = badWordIndex >= foundExcludeIndex &&
                                        badWordIndex + badWord.Length <= foundExcludeIndex + excludedPhraseLength &&
                                        foundExcludeIndex + excludedPhraseLength <= msgOriginal.Length &&
                                        msgLwr.Substring(foundExcludeIndex, excludedPhraseLength).IndexOf(excludedPhrase) != -1;

                            if (!returnVal)
                            {
                                scanIndex += foundExcludeIndex + excludedPhraseLength;
                            }
                        } // end if
                    } while (foundExcludeIndex != -1 && !returnVal);
                } // end foreach
            } // end if

            return returnVal;
        }
    }
}
