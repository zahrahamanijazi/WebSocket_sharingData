using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FileSharingSystem
{
    //=======================================================================================================
    //============================== THis is the class for implementing Nodes ===============================
    //=======================================================================================================
    public class Node
    {
        private string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (value != _Name)
                    LastUpdateTime = DateTime.Now;
                _Name = value;
            }
        }

        private string _Url;
        public string Url
        {
            get
            {
                return _Url;
            }

            set {
                if (value != _Url)
                    LastUpdateTime = DateTime.Now;
                _Url = value;

            }
        }
        private string _ServerList="";
        public string ServerList
        {
            get
            {
                return _ServerList;
            }

            set
            {
                if (value != _ServerList)
                    LastUpdateTime = DateTime.Now;
                _ServerList = value;

            }
        }

        private string _FileName;
        public string FileName
        {
            get
            {
                return _FileName;
            }

            set
            {
                if (value != _FileName)
                    LastUpdateTime = DateTime.Now;
                _FileName = value;

            }
        }
        private string _HashValue;
        public string HashValue
        {
            get
            {
                return _HashValue;
            }

            set
            {
                if (value != _HashValue)
                    LastUpdateTime = DateTime.Now;
                _HashValue = value;

            }
        }
        private int _PublicKey;
        public int PublicKey
        {
            get
            {
                return _PublicKey;
            }

            set
            {
                if (value != _PublicKey)
                    LastUpdateTime = DateTime.Now;
                _PublicKey = value;

            }
        }
        private bool _IsActive = true;
        public bool IsActive {
            get
            {
                return _IsActive;
            }

            set
            {
                if (value != _IsActive)
                    LastUpdateTime = DateTime.Now;
                _IsActive = value;

            }
        }

        public DateTime LastUpdateTime { get; set; }

        public string CalculateSha256HashValue(string input)
        {
            // Calculate SHA256 hash from input 
            var sh = SHA256.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = sh.ComputeHash(inputBytes);

            // Convert byte array to hex string 
            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

    }
}
