using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Mastermind
{
    public partial class MainPage : ContentPage
    {
        // Code pegs are the coloured pegs that the player uses.
        // Key pegs are the black and white pegs that are placed at the side of the board.

        const int NUMBER_OF_ROWS = 11;  // Number of rows in GameGrid.
        const int NUMBER_OF_COLS = 5;   // Number of columns in GameGrid.
        const int KEY_PEG_GRID_SIZE = 2;    // Size of key peg grid.
        const int AMOUNT_PER_ROW = 4;   // Amount of slots for code pegs per row.
        const int BLACK_PEG = 1;
        const int WHITE_PEG = 2;
        const int NO_COLOUR = -1;
        Color EMPTY_SLOT_COLOUR = Color.SaddleBrown;

        int currentRow = 0;     // Row at which the user can currently place code pegs.
        int selectedColour = NO_COLOUR;    // array index for the colours[] array. Originally set to -1 (no colour).
        bool allowDuplicates = false;   // If true allows the correct combination to use the same colour more than once.
        bool win = false;   // Set to true if user wins the game.
        bool savedGame = false; // Boolean showing if the current game is new or has been loaded from a previous save.
        bool onMainMenu = true; // Boolean showing if the user is currently on the main menu.
        string STATE_FILE = "save_slot";    // Name of file where the game is saved.
        string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);    // Path of the location where the saved file is.

        Random rnd = new Random();
        BoxView tempBoxView = new BoxView();
        BoxView cover;  // Boxview that covers the correct combination.
        TapGestureRecognizer tapGestureRecognizerPlacePeg = new TapGestureRecognizer();
        TapGestureRecognizer tapGestureRecognizerRemovePeg = new TapGestureRecognizer();

        int[,] codePegGrid = new int[NUMBER_OF_ROWS - 1, NUMBER_OF_COLS - 1];   // 2D array storing all of the code pegs locations and colours.
        int[] correctCombination = new int[AMOUNT_PER_ROW]; // Array storing the correct combination of code pegs.
        Color[] colours = { Color.Teal, Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Pink }; // Array storing all of the types of code peg colours.
        Grid GameGrid;  // Grid for the game
        Grid[] keyPeg;  // Array of Grids for the key pegs. Is a child of GameGrid.
        BoxView[,] boxViews;    // 2D array of the code peg Boxviews.

        public MainPage()
        {
            InitializeComponent();
        }

        // Creates the GameGrid, calls functions to set the board and hides/shows certain StackLayouts.
        private void StartGame()
        {
            GameStackLayout.Children.Clear();

            SLEndOfGame.IsVisible = false;
            GameDisplay.IsVisible = true;
            SelectableColours.IsVisible = true;
            BtnSaveGame.IsVisible = true;

            if (savedGame == false)
            {
                LblSaveStatus.IsVisible = false;
            }

            ShowDuplicateStatus();  // Displays current state of allowDuplicates.

            GameGrid = new Grid();
            keyPeg = new Grid[NUMBER_OF_ROWS - 1];
            boxViews = new BoxView[NUMBER_OF_ROWS - 1, NUMBER_OF_COLS];
            currentRow = 0;
            win = false;

            SetUpBoard();
        }

        // Calls StartGame() and hides/shows certain StackLayouts.
        private void StartGameClicked(object sender, EventArgs e)
        {
            StartGame();
            onMainMenu = false;
            savedGame = false;
            SLMainMenu.IsVisible = false;
            SLButtons.IsVisible = true;
            SelectableColours.IsVisible = true;
        }

        // Creates the board and adds the code peg and key peg BoxViews.
        private void SetUpBoard()
        {
            GameStackLayout.Children.Add(GameGrid);

            GameGrid.BackgroundColor = Color.BurlyWood;
            GameGrid.HeightRequest = 600;
            GameGrid.WidthRequest = 250;

            SetGameGridSize(NUMBER_OF_ROWS, NUMBER_OF_COLS);    // Sets up the GameGrid

            // Create all the key peg grids and places them on the GameGrid.
            for (int i = 0; i < keyPeg.Length; i++)
            {
                keyPeg[i] = new Grid();
                keyPeg[i].SetValue(Grid.RowProperty, i);
                keyPeg[i].SetValue(Grid.ColumnProperty, NUMBER_OF_COLS - 1);
                SetKeyPegGridSize(i, KEY_PEG_GRID_SIZE, KEY_PEG_GRID_SIZE);
                GameGrid.Children.Add(keyPeg[i]);
            }

            // Create and adds all Boxviews to the grid
            for (int i = 0; i < NUMBER_OF_ROWS - 1; i++)
            {
                for (int j = 0; j < NUMBER_OF_COLS; j++)
                {
                    if (j == NUMBER_OF_COLS - 1)
                    {
                        for (int k = 0; k < KEY_PEG_GRID_SIZE; k++)
                        {
                            for (int l = 0; l < KEY_PEG_GRID_SIZE; l++)
                            {
                                AddBackgroundKeyPegBoxView(i, k, l);
                            }
                        }
                    }
                    else
                    {
                        AddBackgroundCodePegBoxView(i, j);
                    }
                }
            }

            EnableAndDisableBoxViews();

            SetCorrectCombination();

            // Places Boxviews from a saved game if a game has been loaded.
            if (savedGame)
            {
                SetBoxViewsFromSave();
            }
        }

        // Sets the size of the GameGrid by adding row and column definitions.
        private void SetGameGridSize(int rows, int cols)
        {
            for (int i = 0; i < rows; i++)
            {
                 GameGrid.RowDefinitions.Add(new RowDefinition());
            }

            for (int i = 0; i < cols; i++)
            {
                 GameGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
        }

        // Sets the size of a key peg grid by adding row and column definitions.
        private void SetKeyPegGridSize(int arrayIndex, int rows, int cols)
        {
            for (int i = 0; i < rows; i++)
            {
                keyPeg[arrayIndex].RowDefinitions.Add(new RowDefinition());
            }

            for (int i = 0; i < cols; i++)
            {
                keyPeg[arrayIndex].ColumnDefinitions.Add(new ColumnDefinition());
            }
        }

        // Adds a code peg boxview to the grid.
        private void AddBackgroundCodePegBoxView(int row, int col)
        {
            tapGestureRecognizerPlacePeg.Tapped += (s, e) =>
            {
                BackgroundBoxView_Clicked(s, e);
            };

            boxViews[row, col] = new BoxView();

            if (savedGame == false)
            {
                codePegGrid[row, col] = NO_COLOUR;
            }

            boxViews[row, col].HeightRequest = 80;
            boxViews[row, col].WidthRequest = 80;
            boxViews[row, col].CornerRadius = 40;
            boxViews[row, col].HorizontalOptions = LayoutOptions.Center;
            boxViews[row, col].VerticalOptions = LayoutOptions.Center;
            boxViews[row, col].Color = EMPTY_SLOT_COLOUR;
            boxViews[row, col].GestureRecognizers.Add(tapGestureRecognizerPlacePeg);
            boxViews[row, col].IsEnabled = false;
            boxViews[row, col].SetValue(Grid.RowProperty, row);
            boxViews[row, col].SetValue(Grid.ColumnProperty, col);
            GameGrid.Children.Add(boxViews[row, col]);
        }

        private void AddBackgroundKeyPegBoxView(int arrayIndex, int row, int col)
        {
            tapGestureRecognizerPlacePeg.Tapped += (s, e) =>
            {
                BackgroundBoxView_Clicked(s, e);
            };

            BoxView boxView = new BoxView();

            boxView.HeightRequest = 80;
            boxView.WidthRequest = 80;
            boxView.CornerRadius = 40;
            boxView.HorizontalOptions = LayoutOptions.Center;
            boxView.VerticalOptions = LayoutOptions.Center;
            boxView.Color = EMPTY_SLOT_COLOUR;
            boxView.IsEnabled = false;
            boxView.SetValue(Grid.RowProperty, row);
            boxView.SetValue(Grid.ColumnProperty, col);
            keyPeg[arrayIndex].Children.Add(boxView);
        }

        // Sets the correct combination of code pegs, places them on the board and hides them using the cover Boxview.
        private void SetCorrectCombination()
        {
            int numOfDuplicates = 0;

            for (int i = 0; i < AMOUNT_PER_ROW; i++)
            {
                if (allowDuplicates && savedGame == false)  // Sets current index of correctCombination. Can have the same colour more than once.
                {
                    correctCombination[i] = rnd.Next(0, colours.Length);
                }
                else if (savedGame == false)    // Sets current index of correctCombination with unique colours.
                {
                    do
                    {
                        numOfDuplicates = 0;

                        correctCombination[i] = rnd.Next(0, colours.Length);

                        for (int j = 0; j < i; j++)
                        {
                            if (correctCombination[j] == correctCombination[i])
                            {
                                numOfDuplicates++;
                            }
                        }

                    } while (numOfDuplicates > 0);
                }

                // Create and places on the board the correctCombination Boxview at the current index.
                BoxView boxView = new BoxView();
                boxView.HeightRequest = 80;
                boxView.WidthRequest = 80;
                boxView.CornerRadius = 40;
                boxView.HorizontalOptions = LayoutOptions.Center;
                boxView.VerticalOptions = LayoutOptions.Center;
                boxView.Color = colours[correctCombination[i]];
                boxView.IsEnabled = false;
                boxView.IsVisible = true;
                boxView.SetValue(Grid.RowProperty, NUMBER_OF_ROWS - 1);
                boxView.SetValue(Grid.ColumnProperty, i);
                GameGrid.Children.Add(boxView);
            }

            // Create and places on the board the cover Boxview which hides the correct combination Boxviews.
            cover = new BoxView();
            cover.HeightRequest = 100;
            cover.WidthRequest = 100;
            cover.CornerRadius = 20;
            cover.Color = Color.Brown;
            cover.IsEnabled = false;
            cover.IsVisible = true;
            cover.SetValue(Grid.RowProperty, NUMBER_OF_ROWS - 1);
            cover.SetValue(Grid.ColumnProperty, 0);
            GameGrid.Children.Add(cover);
            Grid.SetColumnSpan(cover, 4);
        }

        // Places a coloured code peg on board by changing the colour of the background Boxview to a selected colour.
        private void PlaceCodePeg(object sender, int row, int col)
        {
            BoxView selectedBoxView = (BoxView)sender;

            tapGestureRecognizerRemovePeg.Tapped += (s, e2) =>
            {
                DeletePlacedCodePeg(s);
            };

            selectedBoxView.Color = colours[codePegGrid[row, col]];
            selectedBoxView.GestureRecognizers.Add(tapGestureRecognizerRemovePeg);

            CheckIfRowCompleted();
        }

        // Removes a coloured code peg from the board by changing the colour of the background Boxview to its original colour.
        private void DeletePlacedCodePeg(object sender)
        {
            BoxView selectedBoxView = (BoxView)sender;

            int row = (int)selectedBoxView.GetValue(Grid.RowProperty);
            int col = (int)selectedBoxView.GetValue(Grid.ColumnProperty);

            selectedBoxView.Color = EMPTY_SLOT_COLOUR;
            codePegGrid[(int)selectedBoxView.GetValue(Grid.RowProperty), (int)selectedBoxView.GetValue(Grid.ColumnProperty)] = NO_COLOUR;
            selectedBoxView.GestureRecognizers.Remove(tapGestureRecognizerRemovePeg);

            CheckIfRowCompleted();
        }

        // Places Boxviews from a saved game on the board.
        private void SetBoxViewsFromSave()
        {
            // Places the Boxviews from a saved game using the PlaceCodePeg() function.
            // This means that the CheckIfCorrect() function can be used to calculate the
            // key pegs which means that the key pegs do not have to be stored when saving a game.

            int amountEmpty = 0;

            // Goes through codePegGrid and places all valid BoxViews. Stops when it reaches the end of the grid or finds an empty row. 
            for (int i = 0; i < NUMBER_OF_ROWS - 1 && amountEmpty < AMOUNT_PER_ROW; i++)
            {
                amountEmpty = 0;

                for (int j = 0; j < NUMBER_OF_COLS - 1 && amountEmpty < AMOUNT_PER_ROW; j++)
                {
                    if (codePegGrid[i, j] == NO_COLOUR)
                    {
                        amountEmpty++;
                    }
                    else
                    {
                        PlaceCodePeg(boxViews[i, j], i, j);
                    }
                }
            }

            LblSaveStatus.IsVisible = true;
            LblSaveStatus.Text = "Game Successfully Loaded";
        }

        // Calls PlaceCodePeg() when a background Boxview (empty slot on the board) is clicked.
        private void BackgroundBoxView_Clicked(object sender, EventArgs e)
        {
            int row;
            int col;

            BoxView selectedBoxView = (BoxView)sender;

            row = (int)selectedBoxView.GetValue(Grid.RowProperty);
            col = (int)selectedBoxView.GetValue(Grid.ColumnProperty);

            // If the user has selected a valid colour and there is not already a code peg at this position.
            if (selectedColour != NO_COLOUR && codePegGrid[row, col] == NO_COLOUR)
            {
                selectedBoxView.GestureRecognizers.Add(tapGestureRecognizerRemovePeg);
                codePegGrid[row, col] = selectedColour;
                LblSaveStatus.IsVisible = false;

                PlaceCodePeg(selectedBoxView, row, col);
            }
        }

        // Checks if 4 code pegs have been placed on a row.
        private void CheckIfRowCompleted()
        {
            int spacesFilled = 0;

            // Counts number of code pegs on the current row.
            for (int i = 0; i < AMOUNT_PER_ROW && currentRow >= 0; i++)
            {
                if (codePegGrid[currentRow, i] != NO_COLOUR)
                {
                    spacesFilled++;
                }
            }

            // Checks if user was correct and changes row if there were 4 code pegson the row.
            if (spacesFilled == AMOUNT_PER_ROW)
            {
                CheckIfCorrect();
                ChangeRow();
            }
        }

        // Checks if the users guess on the current row matches the correct combination.
        private void CheckIfCorrect()
        {
            int numOfBlackPegs = 0;
            int[] keyPegs = new int[AMOUNT_PER_ROW];   // Stores the colour of the key pegs which get set in PlaceKeyPegs().
            int[] colorsUsed = new int[AMOUNT_PER_ROW];
            int currentCodePegSlot = 0; // Next available slot for a key peg in keyPegs[].
            bool colourUsed = false;

            // Checks if any peg meets the requirement for a black peg and if so sets keyPegs[currentCodePegSlot] to BLACK_PEG.
            for (int i = 0; i < AMOUNT_PER_ROW; i++)
            {
                keyPegs[i] = 0;

                if (codePegGrid[currentRow, i] == correctCombination[i])
                {
                    numOfBlackPegs++;
                    keyPegs[currentCodePegSlot] = BLACK_PEG;
                    colorsUsed[currentCodePegSlot] = codePegGrid[currentRow, i];
                    currentCodePegSlot++;
                }
            }

            // Checks if any peg meets the requirement for a white peg and if so sets keyPegs[currentCodePegSlot] to WHITE_PEG.
            for (int i = 0; i < AMOUNT_PER_ROW; i++)
            {
                colourUsed = false;

                // Checks if a black peg for this colour was already placed.
                for (int j = 0; j < currentCodePegSlot & colourUsed == false; j++)
                {
                    if (codePegGrid[currentRow, i] == colorsUsed[j])
                    {
                        colourUsed = true;
                    }
                }

                // If a peg black for this colour was not placed.
                if (colourUsed == false)
                {
                    // Checks if a white peg for this colour was already placed.
                    for (int k = 0; k < AMOUNT_PER_ROW & colourUsed == false; k++)
                    {
                        if (codePegGrid[currentRow, i] == correctCombination[k])
                        {
                            keyPegs[currentCodePegSlot] = WHITE_PEG;
                            colorsUsed[currentCodePegSlot] = codePegGrid[currentRow, i];
                            currentCodePegSlot++;
                            colourUsed = true;
                        }
                    }
                }

            }

            placeKeyPegs(keyPegs);

            // If all key pegs are black set win to true and end the game.
            if (numOfBlackPegs == AMOUNT_PER_ROW)
            {
                win = true;
                GameOver();
            }
        }

        // Increments row by 1 if there are still empty rows left. Otherwise ends the game (lose).
        private void ChangeRow()
        {
            if (currentRow < NUMBER_OF_ROWS - 2)
            {
                currentRow++;
                EnableAndDisableBoxViews();
            }
            else if (win == false)
            {
                cover.IsVisible = false;
                GameOver();
            }
        }

        // Enables the Boxviews on the current and disables the Boxviews on the previous row.
        private void EnableAndDisableBoxViews()
        {
            for (int i = 0; i < NUMBER_OF_COLS - 1; i++)
            {
                if (currentRow < NUMBER_OF_ROWS - 1)
                {
                    boxViews[currentRow, i].IsEnabled = true;
                }

                if (currentRow > 0)
                {
                    boxViews[currentRow - 1, i].IsEnabled = false;
                }
            }
        }

        // Places key pegs for current row based on keyPegs[] from CheckIfCorrect().
        private void placeKeyPegs(int[] keyPegs)
        {
            int currentPeg = 0;

            for (int i = 0; i < KEY_PEG_GRID_SIZE; i++)
            {
                for (int j = 0; j < KEY_PEG_GRID_SIZE; j++)
                {
                    BoxView boxView = new BoxView();

                    boxView.HeightRequest = 80;
                    boxView.WidthRequest = 80;
                    boxView.CornerRadius = 40;
                    boxView.HorizontalOptions = LayoutOptions.Center;
                    boxView.VerticalOptions = LayoutOptions.Center;

                    if (keyPegs[currentPeg] == BLACK_PEG)
                    {
                        boxView.Color = Color.Black;
                    }
                    else if (keyPegs[currentPeg] == WHITE_PEG)
                    {
                        boxView.Color = Color.White;
                    }
                    else
                    {
                        boxView.Color = Color.Transparent;
                    }

                    boxView.SetValue(Grid.RowProperty, i);
                    boxView.SetValue(Grid.ColumnProperty, j);
                    keyPeg[currentRow].Children.Add(boxView);

                    currentPeg++;
                }
            }
        }

        // Sets selected colour based on colour of Boxview that was tapped.
        private void Colour_Tapped(object sender, EventArgs e)
        {
            tempBoxView.Opacity = 1;    // Sets opacity of previously selected Boxview (tempBoxView) back to 1.

            BoxView boxView = (BoxView)sender;
            boxView.Opacity = 0.35;   // Sets opacity of current Boxview to 0.35.

            tempBoxView = boxView;    // Sets cuurent Boxview to tempBoxView.

            selectedColour = Convert.ToInt32(boxView.StyleId);
        }

        // Displays if allowDuplicates is true or false and changes the text on the button accordingly.
        private void ShowDuplicateStatus()
        {
            if (allowDuplicates)
            {
                LblChangeDuplicateStatus.Text = "   Allow Duplicates: True";
                LblChangeDuplicateStatus2.Text = "   Allow Duplicates: True";
                BtnChangeDuplicateStatus2.Text = "Disallow";
            }
            else
            {
                LblChangeDuplicateStatus.Text = "   Allow Duplicates: False";
                LblChangeDuplicateStatus2.Text = "   Allow Duplicates: False";
                BtnChangeDuplicateStatus2.Text = "Allow";
            }
        }

        // Swaps the current state of allowDuplicates and resets the game if not on the main menu.
        private void BtnChangeDuplicateStatus_Clicked(object sender, EventArgs e)
        {
            if (allowDuplicates)
            {
                allowDuplicates = false;
            }
            else
            {
                allowDuplicates = true;
            }

            if (onMainMenu == false)
            {
                ResetGame();
            }

            ShowDuplicateStatus();
        }

        // Sets savedFame to false and calls StartGame().
        private void ResetGame()
        {
            savedGame = false;
            StartGame();
        }

        // Calls ResetGame().
        private void BtnResetGame_Clicked(object sender, EventArgs e)
        {
            ResetGame();
        }

        // Hides the cover Boxview so that the correct combination can be seen.
        private void ShowAnswer()
        {
            cover.IsVisible = false;
        }

        // Displays a win/lose message based on the result of the game. Also temporarily removes the users ability to save until they start a new game.
        private void GameOver()
        {
            currentRow = -1;
            ShowAnswer();

            BtnSaveGame.IsVisible = false;
            SelectableColours.IsVisible = false;
            SLEndOfGame.IsVisible = true;

            if (win)
            {
                LblEndOfGame.Text = "GAME OVER YOU WIN!";
            }
            else
            {
                LblEndOfGame.Text = "GAME OVER YOU LOSE";
            }

            LblEndOfGame.HorizontalTextAlignment = TextAlignment.Center;
            LblEndOfGame.FontSize = 45;
            LblEndOfGame.FontAttributes = FontAttributes.Bold;
        }

        // Saves the game in a file.
        private void SaveGame()
        {
            // Structure of file:
            // Line 1: 2D array - codePegGrid
            // Line 2: Array - correctCombination
            // Line 3: Boolean - allowDuplicates

            string filename = Path.Combine(path, STATE_FILE);
            string fileText = "";

            using (var streamWriter = new StreamWriter(filename, false))
            {
                // Appends the codePegGrid array to the first line of fileText.
                for (int i = 0; i < NUMBER_OF_ROWS - 1; i++)
                {
                    for (int j = 0; j < NUMBER_OF_COLS - 1; j++)
                    {
                        fileText += codePegGrid[i, j] + " ";
                    }
                }

                fileText += "\n";

                // Appends the correctCombination array to the second line of fileText.
                for (int i = 0; i < AMOUNT_PER_ROW; i++)
                {
                    fileText += correctCombination[i] + " ";
                }

                fileText += "\n";

                // Appends the allowDuplicates boolean to the third line of fileText.
                if (allowDuplicates)
                {
                    fileText += "1";
                }
                else
                {
                    fileText += "0";
                }

                fileText += " ";

                streamWriter.WriteLine(fileText);  // Writes fileText to the file.
            }

            LblSaveStatus.Text = "Game saved";
            LblSaveStatus.IsVisible = true;
        }

        // Loads game from file if a game has previously been saved.
        private void LoadGame()
        {
            // Structure of file:
            // Line 1: 2D array - codePegGrid
            // Line 2: Array - correctCombination
            // Line 3: Boolean - allowDuplicates

            string filename = Path.Combine(path, STATE_FILE);
            string fileText;
            string currentNumber;
            int currentCharIndex = 0;

            if (File.Exists(filename))
            {
                using (var streamReader = new StreamReader(filename))
                {
                    fileText = streamReader.ReadToEnd();   // Reads the file and stores it in fileText.
                }
            }
            else
            {
                fileText = "";
            }

            // Sets variables if fileText in not empty.
            if (fileText.Length > 0)
            {
                LblSaveStatus.IsVisible = true;
                LblSaveStatus.Text = "Loading...";

                // Stores the first line of fileText in the codePegGrid array.
                for (int i = 0; i < NUMBER_OF_ROWS - 1; i++)
                {
                    for (int j = 0; j < NUMBER_OF_COLS - 1; j++)
                    {
                        currentNumber = "";

                        while (fileText[currentCharIndex] != ' ')
                        {
                            currentNumber += fileText[currentCharIndex];
                            currentCharIndex++;
                        }

                        codePegGrid[i, j] = Convert.ToInt32(currentNumber);

                        currentCharIndex++;
                    }
                }

                // Stores the second line of fileText in the correctCombination array.
                for (int i = 0; i < AMOUNT_PER_ROW; i++)
                {
                    currentNumber = "";

                    while (fileText[currentCharIndex] != ' ')
                    {
                        currentNumber += fileText[currentCharIndex];
                        currentCharIndex++;
                    }

                    correctCombination[i] = Convert.ToInt32(currentNumber);

                    currentCharIndex++;
                }

                // Stores the third line of fileText in the allowDuplicates boolean.
                currentCharIndex++;
                currentNumber = "";

                while (fileText[currentCharIndex] != ' ')
                {
                    currentNumber += fileText[currentCharIndex];
                    currentCharIndex++;
                }

                if (currentNumber == "0")
                {
                    allowDuplicates = false;
                }
                else
                {
                    allowDuplicates = true;
                }

                savedGame = true;
                StartGame();
            }
            else   // Displays message if there was no game saved.
            {
                LblSaveStatus.Text = "There is no game to load";
                LblSaveStatus.IsVisible = true;
            }
        }

        // Calls SaveGame().
        private void BtnSaveGame_Clicked(object sender, EventArgs e)
        {
            SaveGame();
        }

        // Calls LoadGame().
        private void BtnLoadGame_Clicked(object sender, EventArgs e)
        {
            LoadGame();
        }
    }
}
