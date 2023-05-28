using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ShipGame
{
    public struct Coordinates
    {
        public int X { get; }
        public int Y { get; }

        public Coordinates(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    internal class Computer
    {
        private const int Port = 9090;
        public static List<Coordinates> alreadyGuessed = new List<Coordinates>();
        public static Coordinates lastCorrectGuess = new Coordinates(-1,-1);
        public static List<int> doneDirections = new List<int>(); // left,right,up,down
        public static int[] directions = { 1, -1};
        public static int limiter = 0;

        IPAddress hostAddress;
        IPAddress clientAddress;
        public void ComputerStart()
        {
            //  Settings:
            int gridSize = 10;  //  Size of the grids
            //  End of Settings

            char[][] userGrid = SetUserGrid(gridSize);

            Console.WriteLine("Setting Computer grid"); 
            char[][] computerGrid = RandomGrid(gridSize, userGrid);

            ComputerGame(userGrid, computerGrid);
        }

        public void OnlineStart()
        {
            Console.WriteLine("0 - Connect | 1 - Listen for connections");
            var option = Console.ReadLine();
            bool isClient = false;
            int gridSize = 10;  //  Size of the grids


            if (option == "0")
            {
                SendMessage(clientAddress,"Hello User2!");
                ListenForConnectionHost();
                SendMessage(clientAddress, "Connection made");
            }
            else
            {
                isClient = true;
                ListenForConnectionClient();
                SendMessage(hostAddress, "Hello Host!");
                Thread.Sleep(1000);

                retry:
                try
                {
                    ListenForConnectionClient();
                }catch(SocketException se)
                {
                    Console.WriteLine(se.ErrorCode + se.Message);
                    Thread.Sleep(1000);
                    goto retry; 
                }
            }
            Console.ReadLine();

            //  Assuming this went correct: connection is possible and was made!

            Console.WriteLine("The game starts now!");
            if(isClient)
            {
                Console.WriteLine("The host is placing his battleships ...");
                string hostStringGrid = ListenForConnectionClient(true);
                char[][] hostGrid = StringToGrid(hostStringGrid, gridSize);
                (int[] hostShips, int hostOnesAmount) = GetShipsAndOnesAmount(hostGrid);

                Console.WriteLine();
                for (int i = 0; i < hostShips.Length; i++)
                {
                    Console.WriteLine($"Ship sizes: {hostShips[i]}");
                }
                Console.WriteLine($"amount of \"Ones\": {hostOnesAmount}");

                tryagain:
                char[][] clientGrid = SetUserGrid(gridSize);
                string clientGridPackage = GridToString(clientGrid);
                (int[] clientShips, int clientOnesAmount) = GetShipsAndOnesAmount(clientGrid);

                if (clientOnesAmount == hostOnesAmount)
                {
                    SendMessage(hostAddress, clientGridPackage);
                }
                else
                {
                    Console.WriteLine($"The \"Ones\" amount doesn't check up | see: client : {clientOnesAmount} host: {hostOnesAmount}");
                    goto tryagain;
                }

                OnlineGame(hostGrid, clientGrid, false);
            }
            else
            {
                Console.WriteLine("YOU ARE SETTING A GRID | BASED ON THAT GRID THE PLAYER 2 WILL SET HIS GRID");
                char[][] hostGrid = SetUserGrid(gridSize);
                string hostGridPackage = GridToString(hostGrid);
                string clientStringGrid = "err0";
                SendMessage(clientAddress, hostGridPackage);
                retry:
                try
                {
                    clientStringGrid = ListenForConnectionHost(true);
                }catch(SocketException)
                {
                    Console.WriteLine("Listening error: retrying in 1");
                    Thread.Sleep(1000);
                    goto retry;
                }


               char[][] clientGrid = StringToGrid(clientStringGrid, gridSize);


                OnlineGame(hostGrid, clientGrid, true);

            }
            Console.ReadLine();
        }

        public void OnlineGame(char[][] host, char[][] client, bool isHost)
        {
            Console.WriteLine("Game begun! | host starts the guessing ...");
            int clientResult = 0;
            int hostResult = 0;

            char[][] hostTrackingGrid = FillGrid(host.Length);
            char[][] clientTrackingGrid = FillGrid(client.Length);

            (int[] temp, int maxPoints) = GetShipsAndOnesAmount(host);

            while (clientResult < maxPoints && hostResult < maxPoints)
            {
                if (isHost) //  Host
                {
                    Console.WriteLine("You are the HOST!");

                    Console.WriteLine("\nYOUR GRID:");
                    ShowGrid(host);
                    Console.WriteLine("\nPLAYER 2 GRID:");
                    ShowGrid(hostTrackingGrid);

                    Console.WriteLine("\n--- SCORES ---");
                    Console.WriteLine($"HOST (PLAYER1) - {hostResult}");
                    Console.WriteLine($"CLIENT (PLAYER2) - {clientResult}");

                    Console.WriteLine("\nWrite coordinates you want to strike!");
                    var coordinates = Console.ReadLine();

                    int result = Strike(coordinates, client, hostTrackingGrid);
                    hostResult += result;

                    SendMessage(clientAddress, hostResult.ToString()); // send message 1 - result

                    string clientString = GridToString(client);

                    Thread.Sleep(1000);

                    SendMessage(clientAddress, clientString); // send message 2 - result


                    retryResult:
                    try
                    {
                        // listener 1 for results
                        string stringClientResult = ListenForConnectionHost(true); // listener 1 - results
                        clientResult = int.Parse(stringClientResult);
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Retrying in 1... (results - host)");
                        Thread.Sleep(1000);
                        goto retryResult;
                    }

                    retry:
                    try
                    {
                        string newHostString = ListenForConnectionHost(true); // listener 2 - grid
                        host = StringToGrid(newHostString, client.Length);
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Retrying in 1... (grid - host)");
                        Thread.Sleep(1000);
                        goto retry;
                    }
                }
                else // Client
                {
                    string newClientString;
                    Console.WriteLine("You are not the HOST!");

                    Console.WriteLine("\nYOUR GRID:");
                    ShowGrid(client);
                    Console.WriteLine("\nPLAYER 2 GRID:");
                    ShowGrid(clientTrackingGrid);

                    string stringHostResults;

                    retryResult:
                    try
                    {
                        stringHostResults = ListenForConnectionClient(true); // listener 1 - host result
                        hostResult = int.Parse(stringHostResults);
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Retrying in 1 (results - client)");
                        Thread.Sleep(1000);
                        goto retryResult;
                    }
                    

                    retry:
                    try
                    {
                        newClientString = ListenForConnectionClient(true); // listener 2 - grid
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Retrying in 1 (grid - client)");
                        Thread.Sleep(1000);
                        goto retry;
                    }
                        
                    client = StringToGrid(newClientString,host.Length);

                    Console.WriteLine("\n--- SCORES ---");
                    Console.WriteLine($"HOST (PLAYER1) - {hostResult}");
                    Console.WriteLine($"CLIENT (PLAYER2) - {clientResult}");

                    Console.WriteLine("\nWrite coordinates you want to strike!");
                    var coordinates = Console.ReadLine();

                    int result = Strike(coordinates, host, clientTrackingGrid);
                    clientResult += result;

                    SendMessage(hostAddress, clientResult.ToString()); // send message 1 - result

                    Thread.Sleep(1000);

                    string hostString = GridToString(host);
                    SendMessage(hostAddress, hostString); // send message 2 - grid  
                }

                if (clientResult >= maxPoints)
                {
                    Console.WriteLine("CLIENT (PLAYER2) WINS!");
                    Console.WriteLine($"--- RESULTS ---\nCLIENT (PLAYER2) - {clientResult}\nhost (player1) - {hostResult}");
                    break;
                }else if(hostResult >= maxPoints)
                {
                    Console.WriteLine("HOST (PLAYER1) WINS!");
                    Console.WriteLine($"--- RESULTS ---\nHOST (PLAYER1) - {hostResult}\nclient (player2) - {clientResult}");
                    break;
                }

            }
        }
        char[][] StringToGrid(string gridString, int gridSize)
        {
            char[][] grid = new char[gridSize][];

            int index = 0;
            for (int i = 0; i < gridSize; i++)
            {
                grid[i] = new char[gridSize];

                for (int j = 0; j < gridSize; j++)
                {
                    if (index < gridString.Length)
                    {
                        grid[i][j] = gridString[index];
                        index++;
                    }
                    else
                    {
                        // Handle case when gridString is shorter than expected
                        grid[i][j] = ' ';
                    }
                }
            }

            return grid;
        }

        public string GridToString(char[][] grid)
        {
            StringWriter writer = new StringWriter();

            for (int i = 0; i < grid.Length; i++)
            {
                writer.Write(new string(grid[i]));
            }

            return writer.ToString();
        }


        public string ListenForConnectionClient(bool messageHidden = false, int port = 9090)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine("Waiting for incoming connection...");

            using (TcpClient client = listener.AcceptTcpClient())
            {
                Console.WriteLine("Connection accepted.");
                IPEndPoint hostEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                IPAddress hostIP = hostEndPoint.Address;
                int clientPort = hostEndPoint.Port;

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    hostAddress = hostIP;
                    if (messageHidden == false)
                    {
                        Console.WriteLine("Received message: " + receivedMessage);
                    }

                    listener.Stop();
                    return receivedMessage;

                }
            }
        }

        public string ListenForConnectionHost(bool messageHidden = false, int port = 9090)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine("Waiting for incoming connection...");

            using (TcpClient client = listener.AcceptTcpClient())
            {
                Console.WriteLine("Connection accepted.");
                IPEndPoint clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                IPAddress clientIP = clientEndPoint.Address;
                int clientPort = clientEndPoint.Port;

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    clientAddress = clientIP;
                    if (messageHidden == false)
                    {
                        Console.WriteLine("Received message: " + receivedMessage);
                    }
                    listener.Stop();
                    return receivedMessage;
                }

            }
        }

        public void SendMessage(IPAddress ip, string message = "Hello")
        {
            bool isConnected = false;
            var inputIp = "127.0.0.1";

            if (ip == IPAddress.None || ip == null)
            {
                Console.WriteLine("Write the IP you want to play with: ");
                inputIp = Console.ReadLine();
            }
            
            IPAddress IPadress = IPAddress.Parse(inputIp);
            IPEndPoint clientIp = new IPEndPoint(IPadress, 9090);

            if (ip != IPAddress.None && ip != null)
            {
                    clientIp = new IPEndPoint(ip, 9090);
            }


            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            { 
                socket.Connect(clientIp);
                socket.Send(messageBytes);
            }
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
                Console.WriteLine($"\nYOUR SCORE - {userCorrectStrikes}");
                Console.WriteLine($"COMPUTER SCORE - {computerCorrectStrikes}");

                Console.WriteLine("\nStrike cordinates (ex. 1A | (first number))");
                var cordinates = Console.ReadLine();

                if(cordinates != null )
                {
                    cordinates = cordinates.ToLower();
                    int result;
                    bool isSuccessful = true;
                    result = Strike(cordinates, computerGrid, trackingGrid);
                    if(result > 0)
                    {
                        userCorrectStrikes += result;
                    }
                    else if(result == -1)
                    {
                        isSuccessful = false;
                    }

                    if(isSuccessful == true)
                    {
                        int computerResult = ComputerStrike(userGrid);
                        computerCorrectStrikes += computerResult;
                    }    
                }

                Console.WriteLine("\nTRACKING GRID: ");
                ShowGrid(trackingGrid);
                Console.WriteLine("\nYOUR GRID: ");
                ShowGrid(userGrid);
            } 
        }

        public static bool WasAlreadyGuessed(int randomOne,int randomTwo)
        {
            bool isGuessed = false;

            for (int i = 0; i < alreadyGuessed.Count; i++)
            {
                if (alreadyGuessed[i].X == randomOne && alreadyGuessed[i].Y == randomTwo)
                {
                    isGuessed = true;
                }
            }
            return isGuessed;
        }

        public static int ComputerStrike(char[][] target) //    userGrid
        {
            Random random = new Random();
            int randomX = random.Next(0, target.Length);
            int randomY = random.Next(0, target.Length);

            bool isAlreadyGuessed = WasAlreadyGuessed(randomX, randomY);

            if(isAlreadyGuessed == false)
            {
                alreadyGuessed.Add(new Coordinates(randomX, randomY));
                if (target[randomX][randomY] == '1')
                {
                    target[randomX][randomY] = 'O';
                    lastCorrectGuess = new Coordinates(randomX, randomY);
                    Console.WriteLine($"Last Correct Guess X: {lastCorrectGuess.X} | Last Correct Guess Y: {lastCorrectGuess.Y}");
                    return 1;
                }
                else
                {
                    target[randomX][randomY] = 'X';
                    return 0;
                }
            }
            else
            {
                return 0;
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
                    target[x - 1][y - 1] = 'O';
                }
                else if (target[x - 1][y - 1] != '1')
                {
                    trackingGrid[x - 1][y - 1] = 'X';
                    target[x - 1][y - 1] = 'X';
                }
                else
                {
                    result = 0;
                }

            }
            else
            {
                Console.WriteLine("Invalid input format. (HINT: first number then character | ex. 10A)");
                result = -1;
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
                int randomDirection = random.Next(0, directions.Length);
                int randomStartingPoint = random.Next(0, gridSize - shipSizes[i]);
                int randomStartingPointTwo = random.Next(0, gridSize - shipSizes[i]);

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

                            shipsAndItSizes.Add(verticalSequenceSize);
                            verticalSequenceSize = 0;
                        }

                        if (sequenceSize > 1)
                        {
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
            }
            return (array,amountOfOnes);
        }

        public char[][] SetUserGrid(int gridSize)
        {
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
            if (line != null)
            {
                line = line.ToLower();
            }
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

            Console.Clear();
            return grid;
           
        }

        public static void ShowGrid(char[][] grid)
        {
            
            Console.WriteLine();

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
