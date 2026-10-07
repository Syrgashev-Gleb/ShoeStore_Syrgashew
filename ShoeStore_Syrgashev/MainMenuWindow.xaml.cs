using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShoeStore_Syrgashev
{
    public partial class MainMenuWindow : Window
    {
        private List<Products> _allProducts = new List<Products>();
        private readonly Employees _currentUser;

        private bool? _sortAscending = null;
        private static bool _isEditWindowOpen = false;

        public MainMenuWindow(Employees user)
        {
            InitializeComponent();
            _currentUser = user;

            UserInfoTextBlock.Text = user != null ? user.FIO : "Гость";
            RoleInfoTextBlock.Text = user != null ? user.Roles.RoleName : "Гость";

            int roleId = user != null ? user.Role_ID : 0;

            // Фильтры — только менеджер (2) и администратор (1)
            bool canFilter = roleId == 1 || roleId == 2;
            if (!canFilter)
                FilterPanel.Visibility = Visibility.Collapsed;

            // Кнопка «Заказы» — админ (1) и менеджер (2)
            BtnOrders.Visibility = (roleId == 1 || roleId == 2)
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Кнопки добавления/удаления товара — только администратор (1)
            if (roleId == 1)
            {
                BtnAddTovar.Visibility = Visibility.Visible;
                BtnDeleteTovar.Visibility = Visibility.Visible;
            }
            else
            {
                BtnAddTovar.Visibility = Visibility.Collapsed;
                BtnDeleteTovar.Visibility = Visibility.Collapsed;
            }

            LoadData();
            LoadSuppliers();
            Filtr();
        }

        // ================= Загрузка товаров =================
        private void LoadData()
        {
            try
            {
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    _allProducts = context.Products
                        .Include(p => p.Categories)
                        .Include(p => p.Manufacturers)
                        .Include(p => p.Suppliers)
                        .OrderBy(p => p.Product_ID)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= Поставщики =================
        private void LoadSuppliers()
        {
            try
            {
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    var suppliers = context.Suppliers.OrderBy(s => s.Supplier).ToList();
                    suppliers.Insert(0, new Suppliers { Supplier_ID = 0, Supplier = "Все поставщики" });

                    CmbSupplier.ItemsSource = suppliers;
                    CmbSupplier.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= Фильтрация/поиск/сортировка =================
        private void Filtr()
        {
            var searchList = _allProducts.ToList();

            // Фильтр по поставщику
            if (CmbSupplier.SelectedItem is Suppliers selectedSupplier &&
                selectedSupplier.Supplier_ID != 0)
            {
                searchList = searchList
                    .Where(p => p.Supplier_ID == selectedSupplier.Supplier_ID)
                    .ToList();
            }

            // Поиск
            if (!string.IsNullOrWhiteSpace(TbSearch.Text))
            {
                string query = TbSearch.Text.ToLower().Trim();

                searchList = searchList.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(query)) ||
                    (p.Article != null && p.Article.ToLower().Contains(query)) ||
                    (p.Description != null && p.Description.ToLower().Contains(query)) ||
                    (p.Unit != null && p.Unit.ToLower().Contains(query)) ||
                    (p.Categories != null && p.Categories.Category != null &&
                        p.Categories.Category.ToLower().Contains(query)) ||
                    (p.Manufacturers != null && p.Manufacturers.Name != null &&
                        p.Manufacturers.Name.ToLower().Contains(query)) ||
                    (p.Suppliers != null && p.Suppliers.Supplier != null &&
                        p.Suppliers.Supplier.ToLower().Contains(query))
                ).ToList();
            }

            // Сортировка
            if (_sortAscending == true)
                searchList = searchList.OrderBy(p => p.Stock_Quantity ?? 0).ToList();
            else if (_sortAscending == false)
                searchList = searchList.OrderByDescending(p => p.Stock_Quantity ?? 0).ToList();

            ListTovar.ItemsSource = searchList;
        }

        // ================= Обработчики фильтров =================
        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e) => Filtr();
        private void CmbSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e) => Filtr();

        private void BtnSortAsc_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = true;
            Filtr();
        }

        private void BtnSortDesc_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = false;
            Filtr();
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            TbSearch.Text = string.Empty;
            CmbSupplier.SelectedIndex = 0;
            _sortAscending = null;
            Filtr();
        }

        // ================= Добавление товара =================
        private void BtnAddTovar_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentUser.Role_ID != 1)
            {
                MessageBox.Show("Добавлять товары может только администратор!",
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
                var addWindow = new AddEditTovarWindow(null);
                if (addWindow.ShowDialog() == true)
                {
                    LoadData();
                    Filtr();
                }
            }
            finally
            {
                _isEditWindowOpen = false;
            }
        }

        // ================= Редактирование по двойному клику =================
        private void ListTovar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
                ListTovar, e.OriginalSource as DependencyObject) as ListViewItem;

            if (item == null) return;

            var selectedTovar = item.DataContext as Products;
            if (selectedTovar == null) return;

            _isEditWindowOpen = true;
            try
            {
                var editWindow = new AddEditTovarWindow(selectedTovar);
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                    Filtr();
                }
            }
            finally
            {
                _isEditWindowOpen = false;
            }
        }

        // ================= Массовое удаление товаров =================
        private void BtnDeleteTovar_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentUser.Role_ID != 1)
            {
                MessageBox.Show("Удалять товары может только администратор!",
                                "Доступ запрещён", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var selected = ListTovar.SelectedItems.Cast<Products>().ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один товар для удаления!",
                                "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            // Проверка: есть ли выделенные товары в заказах
            try
            {
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    var ids = selected.Select(p => p.Product_ID).ToList();

                    var inOrders = context.OrderDetails
                        .Where(od => ids.Contains(od.Product_ID))
                        .Select(od => od.Products.Name)
                        .Distinct()
                        .ToList();

                    if (inOrders.Any())
                    {
                        string names = string.Join("\n• ", inOrders);
                        MessageBox.Show(
                            $"Следующие товары присутствуют в заказах и не могут быть удалены:\n\n• {names}",
                            "Ошибка удаления",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки заказов: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string confirmMsg = selected.Count == 1
                ? $"Вы точно хотите удалить товар \"{selected[0].Name}\"?"
                : $"Вы точно хотите удалить выбранные товары ({selected.Count} шт.)?";

            var result = MessageBox.Show(confirmMsg, "Подтверждение удаления",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                ListTovar.ItemsSource = null;

                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    var ids = selected.Select(p => p.Product_ID).ToList();
                    var toDelete = context.Products.Where(p => ids.Contains(p.Product_ID)).ToList();

                    foreach (var p in toDelete)
                        context.Products.Remove(p);

                    context.SaveChanges();
                }

                MessageBox.Show(selected.Count == 1
                                    ? "Товар удалён!"
                                    : $"Удалено товаров: {selected.Count}",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                Filtr();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadData();
                Filtr();
            }
        }

        private void ListTovar_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                BtnDeleteTovar_Click(sender, e);
                e.Handled = true;
            }
        }

        // ================= Открытие окна заказов =================
        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            var ordersWindow = new OrdersWindow(_currentUser);
            ordersWindow.ShowDialog();
        }

        // ================= Выход =================
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из системы?",
                                         "Выход",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var auth = new AuthorizationWindow();
            auth.Show();
            this.Close();
        }
    }
}