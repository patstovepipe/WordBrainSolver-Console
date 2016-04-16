using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace WordBrainSolver
{
    class Program
    {
        public static string fileName = "words.txt";

        public static string ConnectionString = "Data Source=STOVEPIPE;Initial Catalog=WordsDB;Integrated Security=True;";

        static void Main(string[] args)
        {
            //GetWord(new List<List<Letter>>(), 6, 4);


            while (true)
            {

                int puzzleSize = 0;

                while (puzzleSize == 0)
                {
                    Console.Clear();
                    Console.WriteLine("Size of puzzle? (Enter 3 for 3x3, 4 for 4x4, etc.)");
                    var readLine = Console.ReadLine();

                    int result;
                    if (int.TryParse(readLine, out result))
                        puzzleSize = result;
                }

                List<List<char>> letters = new List<List<char>>();

                while (letters.Count < puzzleSize)
                {
                    Console.Clear();
                    Console.WriteLine("Puzzle Size: {0}x{1}", puzzleSize, puzzleSize);
                    Console.WriteLine();
                    
                    Console.WriteLine("Enter letters for each row starting from the top: (Use - to represent blank.)");
                    Console.WriteLine();

                    letters.ForEach(l => Console.WriteLine(string.Join("", l)));

                    var readLine = Console.ReadLine();
                    if (readLine.Count() == puzzleSize)
                    {
                        var chars = readLine.ToUpper().ToList();
                        letters.Add(chars);
                    }
                }

                List<int> wordLengths = new List<int>();

                while (wordLengths.Count == 0)
                {
                    Console.Clear();
                    Console.WriteLine("Puzzle Size: {0}x{1}", puzzleSize, puzzleSize);
                    Console.WriteLine();
                    letters.ForEach(l => Console.WriteLine(string.Join("", l)));
                    Console.WriteLine();
                    Console.WriteLine("Enter word length:");
                    var readLine = Console.ReadLine();

                    foreach (var num in readLine.Split(new string[] { " " }, StringSplitOptions.None))
                    {
                        var result = 0;
                        if (int.TryParse(num, out result))
                            wordLengths.Add(result);
                        else
                        {
                            wordLengths.Clear();
                            break;
                        }
                    }
                }

                List<string> wordList = new List<string>();
                GetWords(letters, wordLengths, puzzleSize, wordList);

                var commaDelim = string.Join(",", wordList.Select(wl => wl.ToLower()));

                DataTable results = new DataTable();

                var query = string.Format("select distinct ss.* from dbo.fnSplitString('{0}',',') ss inner join words w on w.Word = ss.splitdata", commaDelim);

                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 120;
                        using (SqlDataAdapter dataAdapter = new SqlDataAdapter(cmd))
                        {
                            dataAdapter.Fill(results);
                        }
                    }
                }

                wordList.Clear();
                results.AsEnumerable().ToList().ForEach(dr => wordList.Add(dr[0].ToString()));

                var count = 0;

                if (wordList.Count() > 1)
                {
                    do
                    {
                        var tempWord = wordList[count] + ", " + wordList[count + 1];

                        if (tempWord.Count() > 80)
                            count++;
                        else
                        {
                            wordList[count] = tempWord;
                            wordList.RemoveAt(count + 1);
                        }
                    } while (count < (wordList.Count() - 1));
                        
                }

                Console.Clear();
                wordList.ForEach(wl => Console.WriteLine(wl));

                Console.ReadLine();
            }
        }

        public static void GetWords(List<List<char>> letters, List<int> wordLengths, int puzzleSize, List<string> wordList)
        {
            var lettersGrid = new List<List<Letter>>();

            var word = "";

            // Assign letters to letters grid
            for (int i = 0; i < puzzleSize; i++)
            {
                var letterRow = new List<Letter>();

                for (int j = 0; j < puzzleSize; j++)
                {
                    var letter = new Letter();
                    letter.Used = false;
                    letter.Value = letters.ElementAt(i)[j];
                    letter.Pos = new List<int> { j, i };

                    letterRow.Add(letter);
                }

                lettersGrid.Add(letterRow);
            }

            for (int i = 0; i < puzzleSize; i++)
            {
                for (int j = 0; j < puzzleSize; j++)
                {
                    var curLetter = lettersGrid.ElementAt(i).ElementAt(j);

                    if (curLetter.Value != '-')
                    {
                        curLetter.Used = true;

                        GetWord(lettersGrid, wordLengths.ElementAt(0), puzzleSize, curLetter.Pos, wordList, word + curLetter.Value);

                        curLetter.Used = false;
                    }
                }
            }
        }

        public class Letter
        {
            public bool Used { get; set; }
            public char Value { get; set; }
            public List<int> Pos { get; set; }
        }

        public static void GetWord(List<List<Letter>> lettersGrid, int wordLength, int puzzleSize, List<int> curPos, List<string> wordList, string word)
        {
            if (wordLength > 1)
            {
                var possibleMoves = new List<List<int>>
                {
                    new List<int> { curPos.First() , curPos.Last() - 1 }, // top
                    new List<int> { curPos.First() + 1 , curPos.Last() - 1 }, // top right
                    new List<int> { curPos.First() + 1 , curPos.Last() }, // right
                    new List<int> { curPos.First() + 1 , curPos.Last() + 1 }, // bottom right
                    new List<int> { curPos.First() , curPos.Last() + 1 }, // bottom
                    new List<int> { curPos.First() -1 , curPos.Last() + 1 }, // bottom left
                    new List<int> { curPos.First() - 1 , curPos.Last() }, // left
                    new List<int> { curPos.First() -1 , curPos.Last() - 1 } // top left
                };

                // Remove possible moves outside of the letters grid
                possibleMoves.RemoveAll(pm => pm.First() < 0 || pm.Last() < 0 || pm.First() > (puzzleSize - 1) || pm.Last() > (puzzleSize - 1));

                // Remove possible move if it goes on an already used letter
                possibleMoves.RemoveAll(pm => lettersGrid.Any(lr => lr.Any(l => (l.Pos.SequenceEqual(pm) && (l.Used == true || l.Value == '-')))));

                for (int i = 0; i < possibleMoves.Count(); i++)
                {
                    var move = possibleMoves.ElementAt(i);
                    var letter = lettersGrid.ElementAt(move.Last()).ElementAt(move.First());
                    letter.Used = true;

                    GetWord(lettersGrid, wordLength - 1, puzzleSize, move, wordList, word + letter.Value);

                    letter.Used = false;
                }
            }
            else
                wordList.Add(word);
        }
    }
}