using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
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
        public static List<Coordinates> alreadyGuessed = new List<Coordinates>();
        public static Coordinates lastCorrectGuess = new Coordinates(-1,-1);
        public static int[] directions = { 1, -1};
        public static int limiter = 0;
        private static string errorMessage = $"Ooops something went wrong :/";
        private static int gridSize = 10;

        private static Dictionary<char, int> charValueDict = new Dictionary<char, int>()
                    {
                        { 'a', 1 }, { 'b', 2 }, { 'c', 3 }, { 'd', 4 }, { 'e', 5 }, { 'f', 6 }, { 'g', 7 }, { 'h', 8 }, { 'i', 9 }, { 'j', 10 }, { 'k', 11 }, { 'l', 12 }, { 'm', 13 },
                        { 'n', 14 }, { 'o', 15 }, { 'p', 16 }, { 'q', 17 }, { 'r', 18 }, { 's', 19 }, { 't', 20 }, { 'u', 21 }, { 'v', 22 }, { 'w', 23 }, { 'x', 24 }, { 'y', 25 }, { 'z', 26 }
                    };

        IPAddress hostAddress = IPAddress.None;
        IPAddress clientAddress = IPAddress.None;

        public void ComputerStart()
        {
            char[][] userGrid = SetUserGrid(gridSize);
            char[][] computerGrid = RandomGrid(gridSize, userGrid);
            ComputerGame(userGrid, computerGrid);
        }

        public void OnlineStart()
        {
            Console.WriteLine("0 - Connect | 1 - Listen for connections");
            var option = Console.ReadLine();
            bool isClient = false;

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

                while (true)
                {
                    try
                    {
                        ListenForConnectionClient();
                        break;
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine(errorMessage+$"\nError code: {se.ErrorCode}\nError message: {se.Message}\nRetrying ...");
                        Thread.Sleep(1000);
                    }
                }
            }

            //  Assuming this went correct: connection is possible and was made!
            Console.WriteLine("The game starts now!");
            if(isClient)
            {
                Console.WriteLine("The host is placing his battleships ...");
                string hostStringGrid = ListenForConnectionClient(true);
                char[][] hostGrid = StringToGrid(hostStringGrid, gridSize);
                char[][] clientGrid;
                int[] hostShips= GetShipsAndOnesAmount(hostGrid);
                int hostOnesAmount = CountOnes(hostGrid);

                Console.WriteLine();
                for (int i = 0; i < hostShips.Length; i++)
                {
                    Console.WriteLine($"Ship sizes: {hostShips[i]}");
                }
                Console.WriteLine($"amount of ships (1): {hostOnesAmount}");

                while (true)
                {
                    clientGrid = SetUserGrid(gridSize);
                    string clientGridPackage = GridToString(clientGrid);
                    int clientOnesAmount = CountOnes(clientGrid);

                    if (clientOnesAmount == hostOnesAmount)
                    {
                        SendMessage(hostAddress, clientGridPackage);
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"The amount of ships (1) isn't equal | Client (you) : {clientOnesAmount} Host: {hostOnesAmount}");
                    }
                }

                OnlineGame(hostGrid, clientGrid, false);
            }
            else
            {
                Console.WriteLine("You are setting a grid | Based on your grid the 2 player will set his grid");
                char[][] hostGrid = SetUserGrid(gridSize);
                string hostGridPackage = GridToString(hostGrid);
                string clientStringGrid;
                SendMessage(clientAddress, hostGridPackage);
                while (true)
                {
                    try
                    {
                        clientStringGrid = ListenForConnectionHost(true);
                        break;
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine(errorMessage + $"\nError code: {se.ErrorCode}\nError message: {se.Message}\nRetrying ...");
                        Thread.Sleep(1000);
                    }
                }

                char[][] clientGrid = StringToGrid(clientStringGrid, gridSize);
                OnlineGame(hostGrid, clientGrid, true);
            }
        }

        public void OnlineGame(char[][] host, char[][] client, bool isHost)
        {
            Console.WriteLine("Game begun! | host starts the guessing ...");
            int clientResult = 0;
            int hostResult = 0;

            char[][] hostTrackingGrid = FillGrid(host.Length);
            char[][] clientTrackingGrid = FillGrid(client.Length);

            int maxPoints = CountOnes(host);

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

                    int result = Strike(client, hostTrackingGrid);

                    hostResult += result;


                    SendMessage(clientAddress, hostResult.ToString()); // send message 1 - result

                    string clientString = GridToString(client);

                    Thread.Sleep(1000);

                    SendMessage(clientAddress, clientString); // send message 2 - result


                    while (true)
                    {
                        try
                        {
                            // listener 1 for results
                            string stringClientResult = ListenForConnectionHost(true); // listener 1 - results
                            clientResult = int.Parse(stringClientResult);
                            break;
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine($"\nWaiting for results ...");
                            Thread.Sleep(1000);
                        }
                    }

                    while (true)
                    {
                        try
                        {
                            string newHostString = ListenForConnectionHost(true); // listener 2 - grid
                            host = StringToGrid(newHostString, client.Length);
                            break;
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine($"\nWaiting for grid ...");
                            Thread.Sleep(1000);
                        }
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

                    while (true)
                    {
                        try
                        {
                            stringHostResults = ListenForConnectionClient(true);
                            hostResult = int.Parse(stringHostResults);
                            break;
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine($"\nWaiting for results ...");
                            Thread.Sleep(1000);
                        }
                    }


                    while (true)
                    {
                        try
                        {
                            newClientString = ListenForConnectionClient(true);
                            break;
                        }
                        catch (SocketException)
                        {
                            Console.WriteLine($"\nWaiting for grid ...");
                            Thread.Sleep(1000);
                        }
                    }
                        
                    client = StringToGrid(newClientString,host.Length);

                    string scores = $"\n--- SCORES ---\nHOST (PLAYER1) - {hostResult}\nCLIENT (PLAYER2) - {clientResult}";
                    Console.WriteLine(scores);

                    int result = Strike(host, clientTrackingGrid);
                    clientResult += result;
                    SendMessage(hostAddress, clientResult.ToString()); // send message 1 - result

                    Thread.Sleep(1000);

                    string hostString = GridToString(host);
                    SendMessage(hostAddress, hostString); // send message 2 - grid  
                }

                bool clientWon = clientResult >= maxPoints, hostWon = hostResult >= maxPoints, tie = (hostResult + clientResult) == 2*maxPoints;

                if (clientWon){
                    string clientWinMessage = $"\nCLIENT (PLAYER2) WINS!\n--- RESULTS ---\nCLIENT (PLAYER2) - {clientResult}\nhost (player1) - {hostResult}";
                    Console.WriteLine(clientWinMessage);
                    break;
                }else if (hostWon){
                    string hostWinMessage = $"\nHOST (PLAYER1) WINS!\n--- RESULTS ---\nHOST (PLAYER1) - {hostResult}\nclient (player2) - {clientResult}";
                    Console.WriteLine(hostWinMessage);
                    break;
                }else if(tie){
                    string tieMessage = $"\nTIE!\n--- RESULTS ---\nHOST (PLAYER1) - {hostResult}\nclient (player2) - {clientResult}";
                    Console.WriteLine(tieMessage);
                    break;
                }
            }
            Console.ReadLine();
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
                IPAddress? hostIP = null;
                IPEndPoint? hostEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                if (hostEndPoint != null)
                {
                    hostIP = hostEndPoint.Address;
                    int clientPort = hostEndPoint.Port;
                }

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (hostIP != null)
                    {
                        hostAddress = hostIP;
                    }

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
                IPEndPoint? clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                IPAddress? clientIP = null;

                if (clientEndPoint != null)
                {
                    clientIP = clientEndPoint.Address;
                    int clientPort = clientEndPoint.Port;
                }
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (clientIP != null)
                    {
                        clientAddress = clientIP;
                    }

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

            var inputIp = "127.0.0.1";
            bool isUnsuccessful = false;

            do
            {
                if (ip == IPAddress.None || ip == null)
                {
                    Console.WriteLine("Write the IP you want to play with: ");
                    inputIp = Console.ReadLine();
                    inputIp ??= "127.0.0.1";
                }

                IPAddress? iPAddress;
                IPEndPoint clientIp;
                bool isValid = IPAddress.TryParse(inputIp, out iPAddress);

                if (isValid && iPAddress != null)
                {
                    isUnsuccessful = false;
                    clientIp = new IPEndPoint(iPAddress, 9090);
                }
                else
                {
                    isUnsuccessful = true;
                    continue;
                }



                if (ip != IPAddress.None && ip != null)
                {
                    isUnsuccessful = false;
                    clientIp = new IPEndPoint(ip, 9090);
                }

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    try
                    {
                        socket.Connect(clientIp);
                        socket.Send(messageBytes);
                    }catch(SocketException e)
                    {
                        int noClientListeningErrorCode = 10061;
                        if (e.ErrorCode == noClientListeningErrorCode)
                        {
                            Console.WriteLine($"\n[TROUBLESHOOT] Possible reasons: \n1. The client (player 2) doesn't listen for connections\n2. The console app is not allowed in client firewall\n3. Mistake in the ip: [{inputIp}] - which changes to Localhost in some cases");
                        }
                        Console.WriteLine($"\n(?) Error: {e.Message}\nTry again ...\n");
                        isUnsuccessful = true;
                    }
                }
            } while (isUnsuccessful == true);
        }

        public void ComputerGame(char[][] userGrid, char[][] computerGrid)
        {
            Console.Clear();
            int userOnes = CountOnes(userGrid), computerOnes = CountOnes(computerGrid);
            int userCorrectStrikes = 0, computerCorrectStrikes = 0;
            char[][] trackingGrid = FillGrid(10);

            ShowGrid(trackingGrid);
            bool gameStillOngoing = userCorrectStrikes != computerOnes && computerCorrectStrikes != userOnes;
            while (gameStillOngoing)
            {
                Console.WriteLine($"\nYOUR SCORE - {userCorrectStrikes} | YOUR ONES - {userOnes}");
                Console.WriteLine($"COMPUTER SCORE - {computerCorrectStrikes} | COMPUTER ONES - {computerOnes}");

                int result = Strike(computerGrid, trackingGrid);
                userCorrectStrikes += result;

                int computerResult = ComputerStrike(userGrid);
                computerCorrectStrikes += computerResult;

                Console.WriteLine("\nTRACKING GRID: ");
                ShowGrid(trackingGrid);
                Console.WriteLine("\nYOUR GRID: ");
                ShowGrid(userGrid);
                Console.WriteLine("\nCHEAT GRID: ");
                ShowGrid(computerGrid);

                gameStillOngoing = userCorrectStrikes != computerOnes && computerCorrectStrikes != userOnes;
            }
            
            string winner = (userCorrectStrikes >= computerOnes) ? "User" : "Computer";
            string endMessage = $"\nWinner is {winner}!!\n\nRESULTS:\nComputer - {computerCorrectStrikes}\nUser - {userCorrectStrikes}";

            Console.WriteLine(endMessage);
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
            while (true)
            {
                int randomX = random.Next(0, target.Length);
                int randomY = random.Next(0, target.Length);

                bool isAlreadyGuessed = WasAlreadyGuessed(randomX, randomY);

                if (isAlreadyGuessed == false)
                {
                    alreadyGuessed.Add(new Coordinates(randomX, randomY));
                    if (target[randomX][randomY] == '1')
                    {
                        target[randomX][randomY] = 'O';
                        lastCorrectGuess = new Coordinates(randomX, randomY);
                        return 1;
                    }
                    else
                    {
                        target[randomX][randomY] = 'X';
                        return 0;
                    }
                }
            }
        }

        public static int Strike(char[][] target, char[][] trackingGrid)
        {
            string patternNumberFirst = @"(\d+)([a-z])";
            int result = -1;

            do
            {
                Console.WriteLine("\nWrite coordinates you want to strike!");
                string? coordinates = Console.ReadLine();

                if(coordinates == null)
                {
                    Console.WriteLine("Wrong input (null)\nTry Again ...");
                    continue;
                }

                Match matchNumberFirst = Regex.Match(coordinates, patternNumberFirst);
                if (matchNumberFirst.Success)
                {
                    string rawInputString = matchNumberFirst.Groups[1].Value;
                    string characters = matchNumberFirst.Groups[2].Value;

                    (int x, int y) = ExtractCoordinatesFromInput(characters, rawInputString);
                    result = MarkAndCheckStrike(x, y, target, trackingGrid);
                }
                else
                {
                    string patternLetterFirst = @"([a-z])(\d+)";
                    Match matchLetterFirst = Regex.Match(coordinates, patternLetterFirst);
                    if (matchLetterFirst.Success)
                    {
                        string characters = matchLetterFirst.Groups[1].Value;
                        string rawInputString = matchLetterFirst.Groups[2].Value;

                        (int x, int y) = ExtractCoordinatesFromInput(characters, rawInputString);
                        result = MarkAndCheckStrike(x, y, target, trackingGrid);
                    }
                    else
                    {
                        Console.WriteLine(errorMessage);
                        result = -1;
                    }
                }
                
            }while(result == -1);
            return result;
        }

        private static int MarkAndCheckStrike(int x, int y, char[][] target, char[][] trackingGrid)
        {
            int result = -1;

            if (x > gridSize || y > gridSize)
            {
                Console.WriteLine($"\n{errorMessage}\nUser error: The number or character typed is beyond grid size.");
                return -1;
            }

            if (target[x - 1][y - 1] == '1')
            {
                result = 1;
                trackingGrid[x - 1][y - 1] = 'O';
                target[x - 1][y - 1] = 'O';
            }
            else if (target[x - 1][y - 1] != '1')
            {
                result = 0;
                trackingGrid[x - 1][y - 1] = 'X';
                target[x - 1][y - 1] = 'X';
            }

            return result;
        }
        private static (int x, int y) ExtractCoordinatesFromInput(string characters, string rawInputString)
        {
            int x = -1, y = -1;
            if(int.TryParse(rawInputString, out x) == false)
            {
                Console.WriteLine(errorMessage + "\nError in parsing input");
            }

            if (charValueDict.TryGetValue(characters[0], out y) == false)
            {
                Console.WriteLine(errorMessage + "\nCharacter not found in dictionary");
            }

            return (x, y);
        }

        public char[][] RandomGrid(int gridSize, char[][] userGrid)
        {
            Random random = new Random();
            char[][] computerGrid = FillGrid(gridSize);
            int[] shipSizes;
            string[] directions = { "vertical", "horizontal" };


            shipSizes = GetShipsAndOnesAmount(userGrid);

            for (int i = 0; i < shipSizes.Length; i++)
            {
                int randomAxis = random.Next(0, directions.Length);
                int pointA = random.Next(0, gridSize - shipSizes[i]);
                int pointB = random.Next(0, gridSize - shipSizes[i]);

                List<int> backupX = new List<int>();
                List<int> backupY = new List<int>();

                if (directions[randomAxis] == "vertical")
                {
                    for (int o = 0; o < shipSizes[i]; o++)
                    {
                        if (computerGrid[pointB][pointA] != '1')
                        {
                            computerGrid[pointB][pointA] = '1';
                            backupX.Add(pointB);
                            backupY.Add(pointA);
                            pointB++;
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
                        if (computerGrid[pointA][pointB] != '1')
                        {
                            computerGrid[pointA][pointB] = '1';
                            backupX.Add(pointA);
                            backupY.Add(pointB);
                            pointB++;
                        }
                        else
                        {
                            for (int r = 0; r < backupX.Count; r++)
                            {
                                computerGrid[backupY[r]][backupX[r]] = '0';
                            }

                            i--;
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
                Console.WriteLine($"{errorMessage}\nThe user and computer amount is different, you might want to restart the game :(");
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

        public char[] FillCharArray(ref char[] array, char fillWith = '0')
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = fillWith;
            }
            return array;
        }

        public static int[] GetShipsAndOnesAmount(char[][] userGrid)
        {
            List<int> allSizesOfShips = new List<int>();
            char[][] userWorkOnGrid = new char[userGrid.Length][];

            for (int i = 0; i < userGrid.Length; i++)
            {
                userWorkOnGrid[i] = (char[])userGrid[i].Clone();
            }

            List<int> horizontalShips = GetHorizontalShipsAmount(userWorkOnGrid);
            List<int> verticalShips = GetVerticalShipsAmount(userWorkOnGrid);
            int remainingOnes = CountOnes(userWorkOnGrid);
            List<int> allShips = combineTwoIntLists(horizontalShips, verticalShips, remainingOnes);
            int[] shipSizes = new int[allShips.Count];

            return shipSizes;
        }


        private static List<int> combineTwoIntLists(List<int> firstList, List<int> secondList, [Optional] int remainingOnes)
        {
            List<int> combinedList = new List<int>();
            for (int i = 0; i < firstList.Count; i++)
            {
                combinedList.Add(firstList[i]);
            }

            for (int i = 0; i < secondList.Count; i++)
            {
                combinedList.Add(secondList[i]);
            }

            if(remainingOnes > 0)
            {
                for (int i = 0; i < remainingOnes; i++)
                {
                    combinedList.Add(1);
                }
            }
            
            return combinedList;
        }

        private static List<int> GetVerticalShipsAmount(char[][] workGrid)
        {
            ShowGrid(workGrid);
            List<int> verticalShips = new List<int>();
            int verticalSequenceSize = 0;
            bool previousCoordinatesWasOne = false;
            Coordinates lastPositiveCoordinates = new Coordinates();
            for (int x = 0; x < workGrid.Length; x++)
            {
                for(int y = 0; y < workGrid[x].Length; y++)
                {
                    if (workGrid[y][x] == '1')
                    {
                        previousCoordinatesWasOne = true;
                        verticalSequenceSize++;
                        lastPositiveCoordinates = new Coordinates(x,y);
                        workGrid[y][x] = '*';                     
                    }

                    if (workGrid[y][x] == '0' || x == workGrid[x].Length -1)
                    {
                        if(verticalSequenceSize >= 2)
                        {
                            verticalShips.Add(verticalSequenceSize);
                        }else if (previousCoordinatesWasOne)
                        {
                            workGrid[lastPositiveCoordinates.Y][lastPositiveCoordinates.X] = '1';
                        }
                        verticalSequenceSize = 0;
                        previousCoordinatesWasOne = false;
                    }
                }
            }
            Console.WriteLine("after: \n");
            ShowGrid(workGrid);
            Console.WriteLine("Ship sizes: ");
            foreach (var ship in verticalShips)
            {
                Console.Write(ship + ", ");
            }
            return verticalShips;
        }


        private static List<int> GetHorizontalShipsAmount(char[][] workGrid) // Don't pass real grid (just a duplicate)
        {
            List<int> horizontalShips = new List<int>();
            int horizontalSequenceSize = 0;
            bool previousCoordinatesWasOne = false;
            Coordinates lastPositiveCoordinates = new Coordinates();
            for (int x = 0; x < workGrid.Length; x++)
            {
                for (int y = 0; y < workGrid[x].Length; y++)
                {
                    if (workGrid[x][y] == '1')
                    {
                        previousCoordinatesWasOne = true;
                        horizontalSequenceSize++;
                        lastPositiveCoordinates = new Coordinates(x,y);
                        workGrid[x][y] = '*';
                    }

                    if (workGrid[x][y] == '0' || y == workGrid[x].Length -1)
                    {
                        if (horizontalSequenceSize >= 2)
                        {
                            horizontalShips.Add(horizontalSequenceSize);
                        }
                        else if (previousCoordinatesWasOne)
                        {
                            workGrid[lastPositiveCoordinates.X][lastPositiveCoordinates.Y] = '1';
                        }
                        horizontalSequenceSize = 0;
                        previousCoordinatesWasOne = false;
                    }
                }
            }
            Console.WriteLine("Ship sizes: ");
            foreach(var ship in horizontalShips)
            {
                Console.Write(ship + ", ");
            }
            return horizontalShips;
        }

        private static char[] ValidatingAndCombiningCharArray(char[] mainArray, char[] componentsToBeAdded)
        {
            for (int i = 0; i < componentsToBeAdded.Length; i++)
            {
                if (componentsToBeAdded[i] == '0' || componentsToBeAdded[i] == '1')
                {
                    mainArray[i] = componentsToBeAdded[i];
                }
                else
                {
                    Console.WriteLine("\nOnly \'1\' and \'0\' are allowed as characters in new lines");
                    return mainArray;
                }
            }
            return mainArray;
        }
 
        public char[][] SetUserGrid(int gridSize)
        {
            char[][] grid = FillGrid(gridSize);
            var line = "";
            while (line != "!s")
            {
                ShowGrid(grid);
                Console.WriteLine("\nWhich line do you want to change? | type !s to submit");
                line = Console.ReadLine();
                Console.WriteLine($"\nYou chose line: {line}");

                if (int.TryParse(line, out int number))
                {
                    if (number <= gridSize)
                    {
                        number -= 1;
                        for (int i = 0; i < grid[number].Length; i++)
                        {
                            Console.Write(grid[number][i] + " ");
                        }

                        Console.WriteLine("\n\nTo what do you want to change it? | without spaces");
                        var userInput = Console.ReadLine();
                        char[] newLine = new char[gridSize];
                        FillCharArray(ref newLine);

                        if (userInput != null)
                        {
                            char[] userNewLine = userInput.ToCharArray();
                            newLine = ValidatingAndCombiningCharArray(newLine, userNewLine);
                        }
                        
                        for (int i = 0; i < grid[number].Length; i++)
                        {
                            grid[number][i] = newLine[i];
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Line is not a number!");
                }
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
        private static void ShowArray(int[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = array[i];
                Console.Write(array[i] + ", ");
            }
        }

        private static void ShowList(List<int> values, string message = "Sizes of ships: \n")
        {
            Console.WriteLine(message);
            foreach (var val in values)
            {
                Console.Write(val + ", ");
            }
        }
    }
}
