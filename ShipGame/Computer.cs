using System.Text.RegularExpressions;

namespace ShipGame
{
    internal class Computer
    {
        public void Start()
        {
            //  Settings:
            int gridSize = 10;  //  Size of the grids
            //  End of Settings

            char[][] userGrid = SetUserGrid(gridSize);

            Console.WriteLine("Setting Computer grid"); 
            char[][] computerGrid = RandomGrid(gridSize, userGrid);

            ComputerGame(userGrid, computerGrid);
        }

        public void ComputerGame(char[][] userGrid, char[][] computerGrid)
        {

            Console.Clear();
            int userOnes = CountOnes(userGrid);
            int computerOnes = CountOnes(computerGrid);

            char[][] trackingGrid = FillGrid(10);

            int userCorrectStrikes = 0;
            int computerCorrectStrikes = 0;

            ShowGrid(trackingGrid);

            while (userCorrectStrikes != computerOnes && computerCorrectStrikes != userOnes)
            {
                //Console.Clear();
                Console.WriteLine($"\nYOUR SCORE - {userCorrectStrikes}");
                Console.WriteLine($"COMPUTER SCORE - {computerCorrectStrikes}");

                Console.WriteLine("\nStrike cordinates (ex. 1A | (first number))");
                var cordinates = Console.ReadLine();

                if(cordinates != null )
                {
                    cordinates = cordinates.ToLower();
                    userCorrectStrikes += Strike(cordinates, computerGrid, trackingGrid);
                }

                Console.WriteLine("\n\nTRACKING GRID:");
                ShowGrid(trackingGrid);
            } 
        }

        public static int Strike(string cordinates, char[][] target, char[][] trackingGrid)
        {
            string pattern = @"(\d+)([a-z])";
            int result = 0;

            Dictionary<char, int> charValueDict = new Dictionary<char, int>()
            {
                { 'a', 1 },
                { 'b', 2 },
                { 'c', 3 },
                { 'd', 4 },
                { 'e', 5 },
                { 'f', 6 },
                { 'g', 7 },
                { 'h', 8 },
                { 'i', 9 },
                { 'j', 10 },
                { 'k', 11 },
                { 'l', 12 },
                { 'm', 13 },
                { 'n', 14 },
                { 'o', 15 },
                { 'p', 16 },
                { 'q', 17 },
                { 'r', 18 },
                { 's', 19 },
                { 't', 20 },
                { 'u', 21 },
                { 'v', 22 },
                { 'w', 23 },
                { 'x', 24 },
                { 'y', 25 },
                { 'z', 26 }
            };

            Console.WriteLine($"\nYour strike: {cordinates}");
            Match match = Regex.Match(cordinates, pattern);

            if (match.Success)
            {
                string StringNumber = match.Groups[1].Value;
                string character = match.Groups[2].Value;
                char[] characters = character.ToCharArray();

                int x = int.Parse(StringNumber);
                int y = -1;

                foreach (var item in charValueDict)
                {
                    if(item.Key == characters[0])
                    {
                        y = item.Value; break;
                    }
                }

                //  Checking for 1 in cordinates (x,y)

                if (y != -1 && target[x - 1][y - 1] == '1')
                {
                    result = 1;
                    trackingGrid[x - 1][y - 1] = 'O';
                }
                else if (target[x - 1][y - 1] != '1')
                {
                    trackingGrid[x - 1][y - 1] = 'X';
                }
                else
                {
                    Console.WriteLine("Something went wrong! try again");
                }

            }
            else
            {
                Console.WriteLine("Invalid input format. (HINT: first number then character | ex. 10A)");
            }
            return result;
        }

        public char[][] RandomGrid(int gridSize, char[][] userGrid)
        {
            Random random = new Random();
            char[][] computerGrid = FillGrid(gridSize);
            int[] shipSizes;
            int onesAmount;

            (shipSizes, onesAmount) = GetShipsAndOnesAmount(userGrid);
            string[] directions = {"vertical", "horizontal"};

            for (int i = 0; i < shipSizes.Length; i++)
            {
                Console.WriteLine($"Trying to set ship: {shipSizes[i]} of size");
                int randomDirection = random.Next(0, directions.Length);
                int randomStartingPoint = random.Next(0, gridSize - shipSizes[i]);
                int randomStartingPointTwo = random.Next(0, gridSize - shipSizes[i]);

                Console.WriteLine($"random direction: {randomDirection} | random starting point: {randomStartingPoint} | random starting point two {randomStartingPointTwo}");

                List<int> backupX = new List<int>();
                List<int> backupY = new List<int>();

                if (directions[randomDirection] == "vertical")
                {
                    for (int o = 0; o < shipSizes[i]; o++)
                    {
                        if (computerGrid[randomStartingPointTwo][randomStartingPoint] != '1')
                        {
                            computerGrid[randomStartingPointTwo][randomStartingPoint] = '1';
                            backupX.Add(randomStartingPointTwo);
                            backupY.Add(randomStartingPoint);
                            randomStartingPointTwo++;
                        }
                        else
                        {
                            for (int r = 0; r < backupX.Count; r++)
                            {
                                Console.WriteLine($"Deleting: X - {backupX[r]} and Y - {backupY[r]}");
                                computerGrid[backupX[r]][backupY[r]] = '0';
                            }

                            i--;
                            break;
                        }
                    }
                }
                else
                {
                    for (int o = 0; o < shipSizes[i]; o++)
                    {
                        if (computerGrid[randomStartingPoint][randomStartingPointTwo] != '1')
                        {
                            computerGrid[randomStartingPoint][randomStartingPointTwo] = '1';
                            backupX.Add(randomStartingPoint);
                            backupY.Add(randomStartingPointTwo);
                            randomStartingPointTwo++;
                        }
                        else
                        {
                            i--;
                            for (int r = 0; r < backupX.Count; r++)
                            {
                                Console.WriteLine($"Deleting: X - {backupX[r]} and Y - {backupY[r]}");
                                computerGrid[backupY[r]][backupX[r]] = '0';
                            }
                            break;
                        }
                    }
                }
            }

            int onesAmountComputer = CountOnes(computerGrid);
            int onesAmountUser = CountOnes(userGrid);
            Console.WriteLine($"\n\nThe amount of ones in computer array {onesAmountComputer}");
            Console.WriteLine($"The amount of ones in user array: {onesAmountUser}");

            if(onesAmountComputer != onesAmountUser)
            {
                Console.WriteLine("LOGICAL ERROR: The user and computer amount is different, you might want to restart the game :(");
            }
            return computerGrid;
        }

        public static int CountOnes(char[][] array)
        {
            int count = 0;
            for (int i = 0; i < array.Length; i++)
            {
                for (int o = 0; o < array[i].Length; o++)
                {
                    if (array[i][o] == '1')
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public char[][] FillGrid(int gridSize, char fillWith = '0')
        {
            char[][] grid = new char[gridSize][];
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = new char[gridSize];

                for (int j = 0; j < grid[i].Length; j++)
                {
                    grid[i][j] = fillWith;
                }
            }

            return grid;
        }

        public static (int[],int) GetShipsAndOnesAmount(char[][] userGrid)
        {
            int amountOfOnes = 0;
            List<int> shipsAndItSizes = new List<int>();
            int sequenceSize = 0;
            int verticalSequenceSize = 0;
            char[][] userWorkOnGrid = new char[userGrid.Length][];
            for (int i = 0; i < userGrid.Length; i++)
            {
                userWorkOnGrid[i] = (char[])userGrid[i].Clone();
            }

            for (int i = 0; i < userWorkOnGrid.Length; i++)
            {
                for (int j = 0; j < userWorkOnGrid[i].Length; j++)
                {
                    if (userWorkOnGrid[i][j] == '1')
                    {
                        amountOfOnes++;
                        sequenceSize++;

                        //  Vertical scanning:
                        for (int k = i; k < userWorkOnGrid[i].Length; k++)
                        {
                            if (userWorkOnGrid[k][j] == '1')
                            {
                                verticalSequenceSize++;
                                userWorkOnGrid[k][j] = '0';
                            }
                            else if (userWorkOnGrid[k][j] == '0')
                            {
                                verticalSequenceSize--;
                                break;
                            }
                            
                            if(k == userWorkOnGrid[i].Length - 1)
                            {
                                verticalSequenceSize--;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (verticalSequenceSize > 1 || sequenceSize == 1)
                        {
                            if(sequenceSize == 1)
                            {
                                verticalSequenceSize += 1;
                                sequenceSize = 0;
                            }

                            Console.WriteLine($"Adding: {verticalSequenceSize} in verticalSequenceSize");
                            shipsAndItSizes.Add(verticalSequenceSize);
                            verticalSequenceSize = 0;
                        }

                        if (sequenceSize > 1)
                        {
                            Console.WriteLine($"Adding: {sequenceSize} in sequenceSize");
                            shipsAndItSizes.Add(sequenceSize);
                        }
                        sequenceSize = 0;
                    }

                    if(sequenceSize == userWorkOnGrid[i].Length)
                    {
                        shipsAndItSizes.Add(sequenceSize);
                        sequenceSize = 0;
                    }
                }
            }

            int[] array = new int[shipsAndItSizes.Count];

            for (int i = 0; i < shipsAndItSizes.Count; i++)
            {
                array[i] = shipsAndItSizes[i];
                Console.WriteLine($"Ship {i} - {array[i]}");
            }
            Console.WriteLine("\n\n");
            return (array,amountOfOnes);
        }

        public char[][] SetUserGrid(int gridSize)
        {
            Console.Clear();
            Console.WriteLine("Make the grid: ");
            char[][] grid = new char[gridSize][];
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = new char[gridSize];

                for (int j = 0; j < grid[i].Length; j++)
                {
                    grid[i][j] = '0';
                }
            }

            changeLine:

            ShowGrid(grid);
            Console.WriteLine("\n\nWhich line do you want to change? | type !s to submit");
            var line = Console.ReadLine();
            line = line.ToLower();

            while (line != "!s")
            {
                Console.WriteLine($"\nYou chose line: {line}");
                if (int.Parse(line) <= gridSize)
                {
                    for (int i = 0; i < grid[int.Parse(line) - 1].Length; i++)
                    {
                        Console.Write(grid[int.Parse(line) - 1][i] + " ");
                    }
                    Console.WriteLine("\n\nTo what do you want to change it? | without spaces");
                    var newLine = Console.ReadLine();
                    char[] newLineChar = newLine.ToCharArray();

                    if (newLineChar.Length < gridSize)
                    {
                        Array.Resize(ref newLineChar, gridSize);
                        for (int i = newLineChar.Length - 1; i >= 0; i--)
                        {
                            if (newLineChar[i] == '\0')
                            {
                                newLineChar[i] = '0';
                            }
                        }
                    }

                    for (int i = 0; i < grid[int.Parse(line) - 1].Length; i++)
                    {
                        grid[int.Parse(line) - 1][i] = newLineChar[i];
                    }
                }
                goto changeLine;
            }

            return grid;
           
        }

        public static void ShowGrid(char[][] grid)
        {
            
            Console.WriteLine("\n");

            char[] alphabet = new char[]
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
                'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
            };

            Console.Write(" ".PadLeft(4));

            for (int i = 0; i < grid.Length; i++)
            {
                Console.Write(alphabet[i].ToString().PadLeft(2));
            }

            Console.WriteLine("\n");

            for (int i = 0; i < grid.Length; i++)
            {

                Console.Write($"{i + 1} ".PadRight(5));

                for (int j = 0; j < grid[i].Length; j++)
                {
                    if (grid[i][j] == '1' || grid[i][j] == 'O')
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }else if (grid[i][j] == 'X')
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    Console.Write(grid[i][j] + " ");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine(" ");
            }
        }
    }
}
