using System.Linq;
using System.Windows;

namespace ShoeStore_Syrgashev
{
    public partial class AuthorizationWindow : Window
    {
        public AuthorizationWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();
            string password = PbPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = ShoeStore_SyrgashevEntities.GetContext())
            {
                var user = context.Employees
                                  .Include("Roles")
                                  .FirstOrDefault(u => u.Login == login && u.Password == password);

                if (user == null)
                {
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка авторизации",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OpenMainMenu(user);
            }
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            OpenMainMenu(null); // гость
        }

        private void OpenMainMenu(Employees user)
        {
            var mainMenu = new MainMenuWindow(user);
            mainMenu.Show();
            this.Close();
        }
    }
}
