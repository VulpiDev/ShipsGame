namespace ShipGame
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SelectionMenu();
        }

        static void SelectionMenu()
        {
            Console.Clear();
            string welcomeMessage = "Welcome to ships game, to play with someone enter their IP\nNOTE: they also need to run this application";
            string selectOption = "\n\nSelect an option:\n1. Connect with other\n2. Play with computer\n3. INFO\n";

            Console.WriteLine(welcomeMessage);
            Console.WriteLine(selectOption);
            var option = Console.ReadLine();
            OptionSelection(option);
        }

        static void OptionSelection(string option)
        {
            Computer computer = new Computer();
            Console.Clear();
            string choseOptionMessage = $"\nYou chose: {option} - ";

            if(option == "1")
            {
                Console.WriteLine(choseOptionMessage + "Connect with others");
                computer.OnlineStart();
            }
            else if(option == "2")
            {
                Console.WriteLine(choseOptionMessage + "Play with computer");
                computer.ComputerStart();   
            }
            else
            {
                Console.WriteLine(choseOptionMessage + "INFO");
                InfoAbout();
            }
            Console.WriteLine("\n");
        }

        static void InfoAbout()
        {
            string info = "Battleship is a two-player strategy game where the objective is to sink your opponent's fleet of ships before they sink yours. \nChose what you want to learn:";
            string choseOption = "Chose an option: ";
            string[] rules = { "Game Setup", "Ship Placement", "Gameplay", "Hit or Miss", "Sinking Ships", "Winning the Game"};

            string[] rulesContent = new string[]{
                "Game Setup:\nEach player has a grid (usually 10x10) on which they place their ships. The ships are represented by horizontal or vertical line segments of varying lengths. The sizes of the ships typically range from a single cell (length 1) to five cells (length 5).\r\n\r\n",
                "Ship Placement:\nPlayers take turns placing their ships on their own grid. Ships cannot overlap or extend beyond the boundaries of the grid. The placement of ships is kept hidden from the opponent\r\n\r\n",
                "Gameplay:\nOnce the ships are placed, the players take turns trying to \"shoot\" at their opponent's grid by calling out specific coordinates (e.g., \"B5\"). The opponent must respond with \"hit\" if one of their ships occupies that cell or \"miss\" if there's no ship.\r\n\r\n",
                "Hit or Miss:\nIf a player scores a hit, they mark the corresponding cell on their opponent's grid as a hit (often marked with a red peg or X). The opponent must announce which ship was hit, and the attacking player marks their own grid with a hit as well. If it's a miss, both players mark their grids with a miss (often marked with a white peg or O).\r\n\r\n",
                "Sinking Ships:\nWhen a player successfully hits all the cells of an opponent's ship, they announce \"sunk\" for that ship. The opponent must mark all the cells of that ship as hits.\r\n\r\n",
                "Winning the Game\nThe game continues with players taking turns until one player has successfully sunk all of their opponent's ships. That player is declared the winner.\r\n\r\n",
                "Play with computer\nComputers grid is chosen randomly by a system, it guesses your grid based on system",
                "Connect with other\nConnection is done over TCP, You need to input someones IP to connect to that person, but first tht"
            };

            // O - hit  | X - didn't hit

            Console.WriteLine($"\r\n{info}\n\n");


            start:
            int ruleNumber = 1;
            foreach (string rule in rules)
            {
                Console.WriteLine($"{ruleNumber} - {rule}");
                ruleNumber++;
            }

            Console.WriteLine($"0 - Go back\n\n");
            Console.Write(choseOption);     
            var option = Console.ReadLine();

            while (option != "0")
            {
                Console.Clear();
                for (int i = 0; i < rulesContent.Length+1; i++)
                {
                    if (option == i.ToString())
                    {
                        Console.WriteLine("\n" + rulesContent[i - 1]);
                    }
                }
                goto start;
            }
            SelectionMenu();
        }
    }
}