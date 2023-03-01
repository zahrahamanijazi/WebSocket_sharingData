using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSharingSystem
{
    public class Crypto
    {
        public static List<int> CyclicGroupNumbers;
        public static int q;
        public static int ClientPublicKey;
        // public static int PrivateKey;


        //==============================================================================================
        //========================Implementing BBS pseudo-random number generator ======================
        //==============================================================================================
        public static int[] BBShub_GenerateKey(int seed, int p)
        {
            int q = (p - 1) / 2;
            int n = p * q;

            int x0 = seed;
            int[] key = new int[5];
            int[] X = new int[5];


            X[0] = seed;
            key[0] = seed % 2;

            for (int i = 1; i < key.Length; i++)
            {

                X[i] = CyclicGroup.power((X[i - 1]), 2, n);
                if (X[i] < 0) X[i] = X[i] + n;


                key[i] = X[i] % 2;
            }
            return X;
        }
        //==============================================================================================
        //========================Returns the secret key indexes in an array in order===================
        //==============================================================================================
        private static int[] GetKeyIndexes(int[] key)
        {
            int keyLength = key.Length;
            int[] key_indexes = new int[keyLength];
            for (int k = 0; k < key.Length; k++)
            {
                int cnt = 0;
                for (int l = 0; l < key.Length; l++)
                {
                    if (key[l] < key[k]) cnt++;
                }

                key_indexes[k] = cnt;
            }

            return key_indexes;
        }
        //==============================================================================================
        //================================Encrypting with KES algorithm ================================
        //==============================================================================================
        public static string Encrypt(string plainText, int prime, int publickey, int privatekey)
        {
            var SecretKey = Calculate_Secret_key(prime, publickey, privatekey);
            
            while (true)
            {
                plainText = (plainText.Length % SecretKey.Length == 0) ? plainText : plainText.PadRight(plainText.Length - (plainText.Length % SecretKey.Length) + SecretKey.Length, '*');
                string output = "";
                int totalChars = plainText.Length;
                int ColCount = SecretKey.Length;
                int RowCount = (int)Math.Ceiling((double)totalChars / ColCount);
                char[,] colChars = new char[ColCount, RowCount];
                char[,] sortedColChars = new char[ColCount, RowCount];
                int currentRow, currentColumn, i, j;
                int[] KeyIndexes = GetKeyIndexes(SecretKey);

                for (i = 0; i < totalChars; ++i)
                {
                    currentRow = i / ColCount;
                    currentColumn = i % ColCount;
                    colChars[currentColumn, currentRow] = plainText[i];
                }

                for (i = 0; i < ColCount; ++i)
                    for (j = 0; j < RowCount; ++j)
                        sortedColChars[KeyIndexes[i], j] = colChars[i, j];

                for (i = 0; i < totalChars; ++i)
                {
                    currentRow = i / RowCount;
                    currentColumn = i % RowCount;
                    output += sortedColChars[currentRow, currentColumn];
                }
                output = output.Replace("\0", string.Empty);

                if (plainText.Length <= output.Length)
                {
                    return output;
                }
            }
        }
        //==============================================================================================
        //================================Decrypting with KES algorithm ================================
        //==============================================================================================
        public static string Decrypt(string CipherText, int prime, int publickey, int privatekey)
        {
            var SecretKey = Calculate_Secret_key(prime, publickey, privatekey);
            string output = "";
            int totalChars = CipherText.Length;
            int RowCount = (int)Math.Ceiling((double)totalChars / SecretKey.Length);
            int ColCount = SecretKey.Length;
            char[,] colChars = new char[RowCount, ColCount];
            char[,] unsortedColChars = new char[RowCount, ColCount];
            int currentRow, currentColumn, i, j;
            int[] KeyIndexes = GetKeyIndexes(SecretKey);

            for (i = 0; i < totalChars; ++i)
            {
                currentRow = i / RowCount;
                currentColumn = i % RowCount;
                colChars[currentColumn, currentRow] = CipherText[i];
            }

            for (i = 0; i < RowCount; ++i)
                for (j = 0; j < ColCount; ++j)
                    unsortedColChars[i, j] = colChars[i, KeyIndexes[j]];

            for (i = 0; i < totalChars; ++i)
            {
                currentRow = i / ColCount;
                currentColumn = i % ColCount;
                output += unsortedColChars[currentRow, currentColumn];
            }
            output = output.Replace("*", " ");
            return output;
        }

        //==============================================================================================
        //=================================== Calculate Public key =====================================
        //==============================================================================================
        public static int CalculatePublicKey(int Prime, out int PrivateKey)
        {
            //Generating Prime number, g, q
            CyclicGroup cyclicGroup = new CyclicGroup();
            var g = CyclicGroup.findPrimitiveRoot(Prime);

            CyclicGroupNumbers = CyclicGroup.FormCyclicGroup(Prime);
            q = (Prime - 1) / 2;

            // public and private key
            Random R = new Random();
            int random = R.Next(3, Prime - 1);
            PrivateKey = CyclicGroupNumbers[random];

            var PublicKey = CyclicGroup.power(g, PrivateKey, Prime);

            if (PublicKey < 0) 
                PublicKey = PublicKey + Prime;

            return PublicKey;
        }
        //==============================================================================================
        //=================================== Calculate Secret key =====================================
        //==============================================================================================
        public static int[] Calculate_Secret_key(int prime, int publickey, int privatekey)
        {
            var SharedKey = CyclicGroup.power(publickey, privatekey, prime);
            Program.Log($"{SharedKey}");
            Program.Log("--------------------------------------------");
            var SecretKey = BBShub_GenerateKey(SharedKey, prime);
            return SecretKey;
        }

        //==============================================================================================
        //====================================Convert string to binary==================================
        //==============================================================================================
        public static string StringToBinary(string data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in data.ToCharArray())
            {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }

        //==============================================================================================
        //===================================Convert binary to string===================================
        //==============================================================================================
        public static string BinaryToString(string data)
        {
            List<Byte> byteList = new List<Byte>();

            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }

        //==============================================================================================
        //=================================== LoadFile ===================================
        //==============================================================================================
        public static string LoadFile(string filename)
        {
            if (File.Exists(filename))
            {
                var Data = File.ReadAllText(filename);
                return Data;
            }
            else
                return null;
        }

        //==============================================================================================
        //==============================================================================================
        //==============================================================================================
    }
}
