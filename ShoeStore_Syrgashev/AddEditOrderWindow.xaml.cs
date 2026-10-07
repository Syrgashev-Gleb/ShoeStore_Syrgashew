using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ShoeStore_Syrgashev
{
    public partial class AddEditOrderWindow : Window
    {
        private Orders _currentOrder = new Orders();
        private readonly Employees _currentUser;
        private bool _isEditMode = false;

        // Локальный список строк заказа (ещё не сохранённых в БД)
        private List<OrderDetails> _items = new List<OrderDetails>();

        public AddEditOrderWindow(Orders selOrder, Employees currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            LoadStatuses();
            LoadPickupPoints();
            LoadProducts();

            if (selOrder != null && selOrder.Order_ID != 0)
            {
                // Редактированм
                _isEditMode = true;
                _currentOrder = selOrder;
                Title = "Редактирование заказа";
                HeaderText.Text = "Редактирование заказа";
                DeleteBtn.Visibility = Visibility.Visible;

                FillFieldsFromOrder();
                LoadOrderItems();
            }
            else
            {
                // Добавление
                _isEditMode = false;
                _currentOrder = new Orders();
                Title = "Добавление заказа";
                HeaderText.Text = "Добавление заказа";

                OrderDatePicker.SelectedDate = DateTime.Today;
                DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(3);

                // Генерируем уникальный PickupCode (901, 902, …)
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    int maxNum = 900;
                    var codes = context.Orders
                        .Where(o => o.PickupCode.StartsWith("9"))
                        .Select(o => o.PickupCode)
                        .ToList();

                    foreach (var code in codes)
                    {
                        if (int.TryParse(code, out int n) && n > maxNum)
                            maxNum = n;
                    }

                    ArticleTextBox.Text = (maxNum + 1).ToString();
                }
            }

            RefreshItemsView();
        }

        // Заполнение полей
        private void FillFieldsFromOrder()
        {
            ArticleTextBox.Text = _currentOrder.PickupCode ?? string.Empty;
            StatusComboBox.SelectedValue = _currentOrder.Status_ID;
            PickupPointComboBox.SelectedValue = _currentOrder.PickupPoint_ID;
            OrderDatePicker.SelectedDate = _currentOrder.OrderDate;
            DeliveryDatePicker.SelectedDate = _currentOrder.DeliveryDate;
        }

        // Загрузка состава заказа
        private void LoadOrderItems()
        {
            using (var context = ShoeStore_SyrgashevEntities.GetContext())
            {
                _items = context.OrderDetails
                    .Include("Products")
                    .Where(od => od.Order_ID == _currentOrder.Order_ID)
                    .ToList();
            }
        }

        // Справочники
        private void LoadStatuses()
        {
            using (var context = ShoeStore_SyrgashevEntities.GetContext())
                StatusComboBox.ItemsSource = context.Statuses.ToList();
        }

        private void LoadPickupPoints()
        {
            using (var context = ShoeStore_SyrgashevEntities.GetContext())
                PickupPointComboBox.ItemsSource = context.PickupPoints
                    .OrderBy(p => p.Num)
                    .ToList();
        }

        private void LoadProducts()
        {
            using (var context = ShoeStore_SyrgashevEntities.GetContext())
            {
                ProductComboBox.ItemsSource = context.Products
                    .OrderBy(p => p.Article)
                    .ToList();
            }
        }

        // Обновление отображения состава
        private void RefreshItemsView()
        {
            ListItems.ItemsSource = null;
            ListItems.ItemsSource = _items;
        }

        // Добавить строку в заказ
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар из списка!", "Внимание",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Введите положительное количество!",
                                "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var product = ProductComboBox.SelectedItem as Products;
            if (product == null) return;

            // Если этот товар уже есть в составе — увеличиваем количество
            var existing = _items.FirstOrDefault(i => i.Product_ID == product.Product_ID);
            if (existing != null)
            {
                existing.Quantity += qty;
            }
            else
            {
                _items.Add(new OrderDetails
                {
                    Product_ID = product.Product_ID,
                    Products = product,
                    Quantity = qty
                });
            }

            // Очищаем поля ввода
            ProductComboBox.SelectedIndex = -1;
            QuantityTextBox.Text = string.Empty;

            RefreshItemsView();
        }

        // Удалить строку из состава
        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = ListItems.SelectedItem as OrderDetails;
            if (selected == null)
            {
                MessageBox.Show("Выберите строку для удаления!",
                                "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            _items.Remove(selected);
            RefreshItemsView();
        }

        // Валидация количества
        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // Сохранение
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArticleTextBox.Text))
            {
                MessageBox.Show("Введите артикул заказа!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (StatusComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус заказа!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (PickupPointComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите пункт выдачи!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (OrderDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату заказа!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DeliveryDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату доставки!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_items.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар в состав заказа!",
                                "Ошибка", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    if (!_isEditMode)
                    {
                        // Добавление
                        var newOrder = new Orders
                        {
                            PickupCode = ArticleTextBox.Text.Trim(),
                            Status_ID = (int)StatusComboBox.SelectedValue,
                            PickupPoint_ID = (int)PickupPointComboBox.SelectedValue,
                            OrderDate = OrderDatePicker.SelectedDate.Value,
                            DeliveryDate = DeliveryDatePicker.SelectedDate.Value,
                            Employee_ID = _currentUser.Employee_ID
                        };

                        context.Orders.Add(newOrder);
                        context.SaveChanges();   // получаем Order_ID

                        // Сохраняем состав
                        foreach (var item in _items)
                        {
                            context.OrderDetails.Add(new OrderDetails
                            {
                                Order_ID = newOrder.Order_ID,
                                Product_ID = item.Product_ID,
                                Quantity = item.Quantity
                            });
                        }
                        context.SaveChanges();

                        MessageBox.Show("Заказ успешно добавлен!",
                                        "Успех", MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                    }
                    else
                    {
                        // Релактирование
                        var existing = context.Orders
                            .FirstOrDefault(o => o.Order_ID == _currentOrder.Order_ID);
                        if (existing == null)
                        {
                            MessageBox.Show("Заказ не найден в базе данных!",
                                            "Ошибка", MessageBoxButton.OK,
                                            MessageBoxImage.Error);
                            return;
                        }

                        existing.PickupCode = ArticleTextBox.Text.Trim();
                        existing.Status_ID = (int)StatusComboBox.SelectedValue;
                        existing.PickupPoint_ID = (int)PickupPointComboBox.SelectedValue;
                        existing.OrderDate = OrderDatePicker.SelectedDate.Value;
                        existing.DeliveryDate = DeliveryDatePicker.SelectedDate.Value;

                        // Удаляем все старые строки состава и вставляем новые
                        var oldItems = context.OrderDetails
                            .Where(od => od.Order_ID == existing.Order_ID)
                            .ToList();
                        foreach (var od in oldItems)
                            context.OrderDetails.Remove(od);

                        foreach (var item in _items)
                        {
                            context.OrderDetails.Add(new OrderDetails
                            {
                                Order_ID = existing.Order_ID,
                                Product_ID = item.Product_ID,
                                Quantity = item.Quantity
                            });
                        }

                        context.SaveChanges();

                        MessageBox.Show("Заказ успешно обновлён!",
                                        "Успех", MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Удаление заказа
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode || _currentOrder == null || _currentOrder.Order_ID == 0)
            {
                MessageBox.Show("Нечего удалять — заказ ещё не сохранён.",
                                "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы точно хотите удалить заказ \"{_currentOrder.PickupCode}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    // Удаляем состав
                    var details = context.OrderDetails
                        .Where(od => od.Order_ID == _currentOrder.Order_ID)
                        .ToList();
                    foreach (var d in details)
                        context.OrderDetails.Remove(d);

                    var toDelete = context.Orders
                        .FirstOrDefault(o => o.Order_ID == _currentOrder.Order_ID);

                    if (toDelete != null)
                    {
                        context.Orders.Remove(toDelete);
                        context.SaveChanges();
                    }
                }

                MessageBox.Show("Заказ успешно удалён!",
                                "Успех", MessageBoxButton.OK,
                                MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
