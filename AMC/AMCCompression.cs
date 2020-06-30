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

namespace AMC {
    public class AMCCompression {

        string key = "";
        Encoding encoding = Encoding.UTF8;

        Dictionary<int, string>[] encodeGroups = new Dictionary<int, string>[] {
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>()
        };

        Dictionary<string, int>[] decodeGroups = new Dictionary<string, int>[] {
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
        };

        Dictionary<byte, int> byteToGroup = new Dictionary<byte, int>();

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
            byte[] b = encoding.GetBytes(decodedData);

            string bin = "";
            int lastGroup = 0;

            for(int i = 0; i < b.Length; i++) {
                int group = byteToGroup[b[i]];

                if(group != lastGroup) {
                    bin += GroupChangePath(lastGroup, group);
                    lastGroup = group;
                }

                bin += encodeGroups[lastGroup][b[i]];
            }

            string asciiBin = ByteToBin(decodedData);
            if(asciiBin.Length > bin.Length)
                bin = "0" + bin;
            else
                bin = "1" + asciiBin;

            while(bin.Length % 8 != 0)
                bin += "1";

            return BinToByte(bin);
        }

        /// <summary>
        /// Decompresses and decodes the given string from AMC
        /// </summary>
        /// <param name="encodedData">String for decompression and decoding</param>
        /// <returns>Returns a decompressed and decoded string</returns>
        public string Decode(string encodedData) {
            string allBin = ByteToBin(encodedData);
            string bin = allBin.Substring(1);

            if(allBin.Substring(0, 1) == "0") {
                List<byte> result = new List<byte>();
                int group = 0;

                for(int i = 4; i < bin.Length; i += 5) {
                    string localBin = bin[i - 4].ToString() + bin[i - 3] + bin[i - 2] + bin[i - 1] + bin[i];
                    int element = decodeGroups[group][localBin];

                    if(element >= 1000)
                        group = element - 1000;
                    else
                        result.Add((byte)element);
                }

                return encoding.GetString(result.ToArray());
            } else {
                return BinToByte(bin);
            }
        }

        string ByteToBin(string ascii) {
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

        string BinToByte(string bin) {
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
                case 8:
                case -8:
                    return encodeGroups[oldGroup][1000 + newGroup];
                case -2:
                    transitionGroup = MaxGroup(oldGroup + 1);
                    return encodeGroups[oldGroup][1000 + transitionGroup] + encodeGroups[transitionGroup][1000 + newGroup];
                case 2:
                    transitionGroup = MaxGroup(oldGroup - 1);
                    return encodeGroups[oldGroup][1000 + transitionGroup] + encodeGroups[transitionGroup][1000 + newGroup];
                case -3:
                    transitionGroup = MaxGroup(oldGroup + 4);
                    return encodeGroups[oldGroup][1000 + transitionGroup] + encodeGroups[transitionGroup][1000 + newGroup];
                case 3:
                case 7:
                    transitionGroup = MaxGroup(oldGroup - 1);
                    transitionGroup2 = MaxGroup(transitionGroup - 1);
                    return encodeGroups[oldGroup][1000 + transitionGroup] + encodeGroups[transitionGroup][1000 + transitionGroup2] + encodeGroups[transitionGroup2][1000 + newGroup];
                case -4:
                case 5:
                    return encodeGroups[oldGroup][1000 + newGroup];
                case 4:
                case 6:
                    transitionGroup = MaxGroup(oldGroup + 4);
                    return encodeGroups[oldGroup][1000 + transitionGroup] + encodeGroups[transitionGroup][1000 + newGroup];
                case -5:
                    transitionGroup = MaxGroup(oldGroup + 4);
                    return encodeGroups[oldGroup][1000 + transitionGroup] + encodeGroups[transitionGroup][1000 + newGroup];
                case -6:
                    transitionGroup = MaxGroup(oldGroup + 4);
                    transitionGroup2 = MaxGroup(transitionGroup + 1);
                    return encodeGroups[oldGroup][1000 + transitionGroup] + encodeGroups[transitionGroup][1000 + transitionGroup2] + encodeGroups[transitionGroup2][1000 + newGroup];
                case -7:
                    transitionGroup = MaxGroup(oldGroup + 1);
                    transitionGroup2 = MaxGroup(transitionGroup + 1);
                    return encodeGroups[oldGroup][1000 + transitionGroup] + encodeGroups[transitionGroup][1000 + transitionGroup2] + encodeGroups[transitionGroup2][1000 + newGroup];
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
            byteToGroup.Clear();

            string asciiKey = encoding.GetString(Convert.FromBase64String(key));

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

                    byte element = (byte)safeKey[i];
                    encodeGroups[x].Add(element, bin);
                    decodeGroups[x].Add(bin, element);

                    byteToGroup.Add(element, x);
                }

                if(safeKey.Length <= x * 29 + 29)
                    break;
            }

            for(int b = 0; b < 256; b++) {
                if(byteToGroup.ContainsKey((byte)b))
                    continue;
                
                for(int x = 0; x < encodeGroups.Length; x++) {
                    if(encodeGroups[x].Count < 29) {
                        string bin = UIntToLenghtFiveBinary((uint)encodeGroups[x].Count);

                        encodeGroups[x].Add(b, bin);
                        decodeGroups[x].Add(bin, b);
                        
                        byteToGroup.Add((byte)b, x);

                        break;
                    }
                }
            }

            for(int i = 0; i < encodeGroups.Length; i++) {
                int g = 1000 + MaxGroup(i - 1);
                string bin = UIntToLenghtFiveBinary(29);
                encodeGroups[i].Add(g, bin);
                decodeGroups[i].Add(bin, g);

                g = 1000 + MaxGroup(i + 1);
                bin = UIntToLenghtFiveBinary(30);
                encodeGroups[i].Add(g, bin);
                decodeGroups[i].Add(bin, g);

                g = 1000 + MaxGroup(i + 4);
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
