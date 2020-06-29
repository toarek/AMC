/****************************** Module Header ******************************\
Module Name:  AMC.Compression
Project:      AMC
Copyright (c) Arkadiusz Tołwiński.

This file is responsible for encoding and decoding data using the AMC library.

This source is subject to the GNU General Public License v3.0.
See https://github.com/toarek/AMC/blob/master/LICENSE.
All other rights reserved.

THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AMC {
    public class AMCCompression {

        string key = "";

        Dictionary<string, string>[] encodeGroups = new Dictionary<string, string>[] {
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>()
        };

        Dictionary<string, string>[] decodeGroups = new Dictionary<string, string>[] {
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
        };

        Dictionary<char, int> charToGroup = new Dictionary<char, int>();

        /// <summary>
        /// Creates an AMCCompresion class
        /// </summary>
        /// <param name="key">The key determines the location of characters in groups, which directly translates into the quality of compression</param>
        public AMCCompression(string key = "") {
            SetKey(key);
        }

        /// <summary>
        /// Compresses and encodes the given string to AMC (currently only accepts ASCII)
        /// </summary>
        /// <param name="decodedData">String for compression and encoding</param>
        /// <returns>Returns a compressed and encoded ASCII string</returns>
        public string Encode(string decodedData) {
            string bin = "";
            int lastGroup = 0;

            for(int i = 0; i < decodedData.Length; i++) {
                int group = charToGroup[decodedData[i]];

                if(group != lastGroup) {
                    bin += GroupChangePath(lastGroup, group);
                    lastGroup = group;
                }

                bin += encodeGroups[lastGroup][decodedData[i].ToString()];
            }

            string asciiBin = AsciiToBin(decodedData);
            if(asciiBin.Length > bin.Length)
                bin = "0" + bin;
            else
                bin = "1" + asciiBin;

            while(bin.Length % 8 != 0)
                bin += "1";

            return BinToAscii(bin);
        }

        /// <summary>
        /// Decompresses and decodes the given string from AMC
        /// </summary>
        /// <param name="encodedData">String for decompression and decoding</param>
        /// <returns>Returns a decompressed and decoded string</returns>
        public string Decode(string encodedData) {
            string allBin = AsciiToBin(encodedData);
            string bin = allBin.Substring(1);

            string result = "";

            if(allBin.Substring(0, 1) == "0") {
                int group = 0;

                for(int i = 4; i < bin.Length; i += 5) {
                    string localBin = bin[i - 4].ToString() + bin[i - 3] + bin[i - 2] + bin[i - 1] + bin[i];
                    string element = decodeGroups[group][localBin];

                    if(element.Length > 1)
                        group = int.Parse(element.Substring(1, 1));
                    else
                        result += element;
                }
            } else {
                result = BinToAscii(bin);
            }

            return result;
        }

        string AsciiToBin(string ascii) {
            string result = "";

            for(int i = 0; i < ascii.Length; i++) {
                result += ByteToBin((byte)ascii[i]);
            }

            return result;
        }

        string ByteToBin(byte b) {
            string result = "";

            while(b > 1) {
                int remainder = b % 2;
                result = Convert.ToString(remainder) + result;
                b /= 2;
            }

            result = Convert.ToString(b) + result;

            while(result.Length < 8)
                result = "0" + result;

            return result;
        }

        string BinToAscii(string bin) {
            string result = "";

            for(int i = 8; i <= bin.Length; i += 8)
                result += (char)Convert.ToByte(bin.Substring(i - 8, 8), 2);

            return result;
        }

        string GroupChangePath(int oldGroup, int newGroup) {
            int transitionGroup;
            int transitionGroup2;

            switch(GroupDifference(oldGroup, newGroup)) {
                case 1:
                case -1:
                    return encodeGroups[oldGroup]["-" + newGroup];
                case -2:
                    transitionGroup = MaxGroup(oldGroup + 1);
                    return encodeGroups[oldGroup]["-" + transitionGroup] + encodeGroups[transitionGroup]["-" + newGroup];
                case 2:
                    transitionGroup = MaxGroup(oldGroup - 1);
                    return encodeGroups[oldGroup]["-" + transitionGroup] + encodeGroups[transitionGroup]["-" + newGroup];
                case -3:
                    transitionGroup = MaxGroup(oldGroup + 4);
                    return encodeGroups[oldGroup]["-" + transitionGroup] + encodeGroups[transitionGroup]["-" + newGroup];
                case 3:
                    transitionGroup = MaxGroup(oldGroup - 1);
                    transitionGroup2 = MaxGroup(transitionGroup - 1);
                    return encodeGroups[oldGroup]["-" + transitionGroup] + encodeGroups[transitionGroup]["-" + transitionGroup2] + encodeGroups[transitionGroup2]["-" + newGroup];
                case -4:
                    return encodeGroups[oldGroup]["-" + newGroup];
                case 4:
                    transitionGroup = MaxGroup(oldGroup + 4);
                    return encodeGroups[oldGroup]["-" + transitionGroup] + encodeGroups[transitionGroup]["-" + newGroup];
                case 5:
                    transitionGroup = MaxGroup(oldGroup + 4);
                    return encodeGroups[oldGroup]["-" + transitionGroup] + encodeGroups[transitionGroup]["-" + newGroup];
            }

            throw new Exception("Unexcepted case!");
        }

        int GroupDifference(int groupA, int groupB) {
            int difference = groupA - groupB;

            if(difference >= 8)
                return 1;
            return difference;
        }

        void SetGroups() {
            for(int i = 0; i < encodeGroups.Length; i++) {
                encodeGroups[i].Clear();
                decodeGroups[i].Clear();
            }
            charToGroup.Clear();

            string asciiKey = Encoding.ASCII.GetString(Convert.FromBase64String(key));

            string safeKey = "";
            for(int i = 0; i < asciiKey.Length; i++) {
                if(!safeKey.Contains(asciiKey[i].ToString()))
                    safeKey += asciiKey[i];
            }

            for(int x = 0; x < encodeGroups.Length; x++) {
                for(int y = 0; y < 29; y++) {

                    int i = x * 29 + y;

                    if(safeKey.Length <= i)
                        break;

                    string bin = UIntToLenghtFiveBinary((uint)y);

                    string element = safeKey[i].ToString();
                    encodeGroups[x].Add(element, bin);
                    decodeGroups[x].Add(bin, element);

                    charToGroup.Add(safeKey[i], x);
                }

                if(safeKey.Length <= x * 29 + 29)
                    break;
            }

            for(int b = 0; b < 256; b++) {
                if(charToGroup.ContainsKey((char)b))
                    continue;
                
                for(int x = 0; x < encodeGroups.Length; x++) {
                    if(encodeGroups[x].Count < 29) {
                        string bin = UIntToLenghtFiveBinary((uint)encodeGroups[x].Count);

                        string element = ((char)b).ToString();
                        encodeGroups[x].Add(element, bin);
                        decodeGroups[x].Add(bin, element);
                        
                        charToGroup.Add((char)b, x);

                        break;
                    }
                }
            }

            for(int i = 0; i < encodeGroups.Length; i++) {
                string g = "-" + MaxGroup(i - 1);
                string bin = UIntToLenghtFiveBinary(29);
                encodeGroups[i].Add(g, bin);
                decodeGroups[i].Add(bin, g);

                g = "-" + MaxGroup(i + 1);
                bin = UIntToLenghtFiveBinary(30);
                encodeGroups[i].Add(g, bin);
                decodeGroups[i].Add(bin, g);

                g = "-" + MaxGroup(i + 4);
                bin = UIntToLenghtFiveBinary(31);
                encodeGroups[i].Add(g, bin);
                decodeGroups[i].Add(bin, g);
            }
        }

        int MaxGroup(int i) {
            if(i < 0)
                return 8;

            while(i > 8)
                i -= 9;

            return i;
        }

        string UIntToLenghtFiveBinary(uint i) {
            if(i >= 32)
                throw new Exception("UInt in this function cannot be larger than 32!");

            string bin = Convert.ToString(i, 2);

            while(bin.Length < 5)
                bin = "0" + bin;

            return bin;
        }

        /// <summary>
        /// Returns the used key
        /// </summary>
        /// <returns>Returns the used key</returns>
        public string GetKey() {
            return key;
        }

        /// <summary>
        /// Sets a new key
        /// </summary>
        /// <param name="newKey">Sets a new key</param>
        public void SetKey(string newKey) {
            key = newKey;
            SetGroups();
        }

    }
}
