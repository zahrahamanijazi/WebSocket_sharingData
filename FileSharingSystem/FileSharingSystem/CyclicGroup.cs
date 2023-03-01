using System;
using System.Collections.Generic;
using System.Text;

namespace FileSharingSystem
{
    public class CyclicGroup
    {
        //static int PrimeNumber = FindP();
        //public static int PrimeNumber = 863;
        public static List<int> cyclicGroup = new List<int>();
        public static string gen_list;
        public static int g;

        //=======================================================================================================
        //======================================Form Prime Number================================================
        //=======================================================================================================
        public static int FindP()
        {
            while (true)
            {
                //int random_number = new Random().Next(0, fprimeList.Length);
                //int p = 2 * fprimeList[random_number] + 1;
                int p = new Random().Next(1000, 9999);
                if (isPrime(p))
                    return p;
            }
        }
        //=======================================================================================================
        //======================================Form Cyclic Group================================================
        //=======================================================================================================
        public static List<int> FormCyclicGroup(int p)
        {
            //List<double> Generators = new List<double>();
            //int random;
            //int g;   //generator
            //string gen_list = "";

            for (int i = 1; i < p; i++)
            {
                cyclicGroup.Add(i);
            }
            return cyclicGroup;
        }
        //=======================================================================================================
        //======================================Find Primitive Root of a Prime number============================
        //=======================================================================================================
        // Returns true if n is prime
        public static bool isPrime(int n)
        {

            if (n <= 1)
            {
                return false;
            }
            if (n <= 3)
            {
                return true;
            }

            // This is checked so that we can skip
            // middle five numbers in below loop
            if (n % 2 == 0 || n % 3 == 0)
            {
                return false;
            }

            for (int i = 5; i * i <= n; i = i + 6)
            {
                if (n % i == 0 || n % (i + 2) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        //this is an Iterative Function that calculates (x^n)%p 
        public static int power(int x, int y, int p)
        {
            int res = 1;

            x = x % p;

            while (y > 0)
            {

                if (y % 2 == 1)
                {
                    res = (res * x) % p;
                }


                y = y >> 1; // y = y/2
                x = (x * x) % p;
            }
            return res;
        }
        // function generate and save prime factors of our prime number
        public static void generatePrimefactors(HashSet<int> s, int n)
        {
            // the number of times that n can devide to 2
            while (n % 2 == 0)
            {
                s.Add(2);
                n = n / 2;
            }

            // n must be odd at this point. So we can skip
            // one element (Note i = i +2)
            for (int i = 3; i <= Math.Sqrt(n); i = i + 2)
            {
                // While i divides n, print i and divide n
                while (n % i == 0)
                {
                    s.Add(i);
                    n = n / i;
                }
            }

            // This condition is to handle the case when
            // n is a prime number greater than 2
            if (n > 2)
            {
                s.Add(n);
            }
        }

        //Finding smallest primitive root of our prime number
        public static int findPrimitiveRoot(int n)
        {
            HashSet<int> s = new HashSet<int>();
            int p = n - 1;

            // Find prime factors of p and store in a set
            generatePrimefactors(s, p);

            // Check for every number from 2 to p
            for (int i = 2; i <= p; i++)
            {
                // based on the definition of generator the following
                // power function should return 1
                bool check = false;
                foreach (int a in s)
                {
                    // this function investigates if i^((p)/primefactors) mod n
                    // is 1 or not
                    if (power(i, p / (a), n) == 1)
                    {
                        check = true;
                        break;
                    }
                }

                // If there was no power with value 1 and this i is generator
                if (check == false)
                {
                    return i;
                }
            }

            // If primitive root does not find for given prime number
            return -1;
        }
    }

}
