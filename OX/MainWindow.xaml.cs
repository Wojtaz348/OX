using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OX
{
    public partial class MainWindow : Window
    {
        private string currentPlayer = "X";
        private string[,] board = new string[3, 3];
        private TcpClient client;
        private NetworkStream stream;
        private const string ServerAddress = "127.0.0.1";
        private const int ServerPort = 12345;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
            ConnectToServer();
        }

        private void InitializeBoard()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    board[row, col] = "";
                }
            }
        }

        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ServerAddress, ServerPort);
                stream = client.GetStream();
                await ReceiveGameState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd połączenia z serwerem: " + ex.Message);
            }
        }

        private async void OnCellClicked(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || !string.IsNullOrEmpty(button.Content?.ToString())) return;

            int row = Grid.GetRow(button);
            int col = Grid.GetColumn(button);
            board[row, col] = currentPlayer;
            button.Content = currentPlayer;
            button.IsEnabled = false;

            await SendMoveToServer(row * 3 + col);
        }

        private async Task SendMoveToServer(int cellIndex)
        {
            if (stream == null) return;

            byte[] data = Encoding.UTF8.GetBytes(cellIndex.ToString());
            await stream.WriteAsync(data, 0, data.Length);
        }

        private async Task ReceiveGameState()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcessServerResponse(response);
            }
        }

        private void ProcessServerResponse(string response)
        {
            Dispatcher.Invoke(() =>
            {
                string[] parts = response.Split(',');
                for (int i = 0; i < 9; i++)
                {
                    int row = i / 3;
                    int col = i % 3;
                    board[row, col] = parts[i];
                    Button button = (Button)GameGrid.Children[i];
                    button.Content = parts[i];
                    button.IsEnabled = string.IsNullOrEmpty(parts[i]);
                }

                currentPlayer = parts[9];
                StatusLabel.Content = "Tura: " + currentPlayer;

                if (parts[10] != "")
                {
                    MessageBox.Show(parts[10]);
                    DisableBoard();
                    AddRestartButton();
                }
            });
        }

        private void DisableBoard()
        {
            foreach (var child in GameGrid.Children)
            {
                if (child is Button button)
                {
                    button.IsEnabled = false;
                }
            }
        }

        private void AddRestartButton()
        {
            Button restartButton = new Button
            {
                Content = "Restart",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            restartButton.Click += (s, e) => RestartGame();
            (this.Content as StackPanel).Children.Add(restartButton);
        }

        private void RestartGame()
        {
            foreach (var child in GameGrid.Children)
            {
                if (child is Button button)
                {
                    button.Content = "";
                    button.IsEnabled = true;
                }
            }
            InitializeBoard();
            currentPlayer = "X";
            StatusLabel.Content = "Tura: " + currentPlayer;
        }
    }
}
