using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShoeStore_Syrgashev
{
    public partial class OrdersWindow : Window
    {
        private List<Orders> _allOrders = new List<Orders>();
        private readonly Employees _currentUser;

        private static bool _isEditWindowOpen = false;

        public OrdersWindow(Employees user)
        {
            InitializeComponent();
            _currentUser = user;

            TbUserInfo.Text = user != null
                ? $"{user.FIO} ({user.Roles.RoleName})"
                : "Гость";

            int roleId = user != null ? user.Role_ID : 0;

            // Добавление/удаление — только администратор
            if (roleId == 1)
            {
                BtnAddOrder.Visibility = Visibility.Visible;
                BtnDeleteOrder.Visibility = Visibility.Visible;
            }

            LoadOrders();
        }

        // Загрузка заказов
        private void LoadOrders()
        {
            try
            {
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    _allOrders = context.Orders
                        .Include(o => o.Statuses)
                        .Include(o => o.PickupPoints)
                        .Include(o => o.Employees)
                        .Include(o => o.OrderDetails.Select(od => od.Products))
                        .OrderBy(o => o.Order_ID)
                        .ToList();
                }

                ListOrders.ItemsSource = _allOrders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавление заказа
        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentUser.Role_ID != 1)
            {
                MessageBox.Show("Добавлять заказы может только администратор!",
                                "Доступ запрещён", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (_isEditWindowOpen)
            {
                MessageBox.Show("Окно редактирования уже открыто!",
                                "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            _isEditWindowOpen = true;
            try
            {
                var addWindow = new AddEditOrderWindow(null, _currentUser);
                if (addWindow.ShowDialog() == true)
                {
                    LoadOrders();
                }
            }
            finally
            {
                _isEditWindowOpen = false;
            }
        }

        // Редактирование по двойному клику
        private void ListOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_currentUser == null || _currentUser.Role_ID != 1)
                return;

            if (_isEditWindowOpen)
            {
                MessageBox.Show("Окно редактирования уже открыто!",
                                "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var item = ItemsControl.ContainerFromElement(
                ListOrders, e.OriginalSource as DependencyObject) as ListViewItem;

            if (item == null) return;

            var selectedOrder = item.DataContext as Orders;
            if (selectedOrder == null) return;

            _isEditWindowOpen = true;
            try
            {
                var editWindow = new AddEditOrderWindow(selectedOrder, _currentUser);
                if (editWindow.ShowDialog() == true)
                {
                    LoadOrders();
                }
            }
            finally
            {
                _isEditWindowOpen = false;
            }
        }

        // Массовое удаление
        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentUser.Role_ID != 1)
            {
                MessageBox.Show("Удалять заказы может только администратор!",
                                "Доступ запрещён", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var selected = ListOrders.SelectedItems.Cast<Orders>().ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один заказ для удаления!",
                                "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            string confirmMsg = selected.Count == 1
                ? $"Вы точно хотите удалить заказ \"{selected[0].PickupCode}\"?"
                : $"Вы точно хотите удалить выбранные заказы ({selected.Count} шт.)?";

            var result = MessageBox.Show(confirmMsg, "Подтверждение удаления",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                ListOrders.ItemsSource = null;

                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    var ids = selected.Select(o => o.Order_ID).ToList();

                    // Удаляем связанные OrderDetails
                    var details = context.OrderDetails
                        .Where(od => ids.Contains(od.Order_ID))
                        .ToList();
                    foreach (var d in details)
                        context.OrderDetails.Remove(d);

                    var toDelete = context.Orders
                        .Where(o => ids.Contains(o.Order_ID))
                        .ToList();
                    foreach (var o in toDelete)
                        context.Orders.Remove(o);

                    context.SaveChanges();
                }

                MessageBox.Show(selected.Count == 1
                                    ? "Заказ удалён!"
                                    : $"Удалено заказов: {selected.Count}",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadOrders();
            }
        }

        // Удаление по клавише Delete
        private void ListOrders_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                BtnDeleteOrder_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}