using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShoeStore_Syrgashev
{
    public partial class AddEditTovarWindow : Window
    {
        private Products _currentTovar = new Products();
        private bool _isEditMode = false;
        private string _photoPath = "picture.png";

        // Папка Images рядом с .exe
        private readonly string _targetDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

        public AddEditTovarWindow(Products selTovar)
        {
            InitializeComponent();

            if (!Directory.Exists(_targetDirectory))
                Directory.CreateDirectory(_targetDirectory);

            LoadCategories();
            LoadManufacturers();
            LoadSuppliers();

            if (selTovar != null && selTovar.Product_ID != 0)
            {
                // Редактирование
                _isEditMode = true;
                _currentTovar = selTovar;
                Title = "Редактирование товара";
                HeaderText.Text = "Редактирование товара";
                DeleteBtn.Visibility = Visibility.Visible;
                FillFieldsFromTovar();
            }
            else
            {
                // Добавление
                _isEditMode = false;
                _currentTovar = new Products();
                Title = "Добавление товара";
                HeaderText.Text = "Добавление товара";

                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    int maxId = context.Products.Any()
                        ? context.Products.Max(p => p.Product_ID)
                        : 0;

                    _currentTovar.Product_ID = maxId + 1;
                    _currentTovar.Article = $"ART-{_currentTovar.Product_ID:D4}";
                }

                IdTextBox.Text = _currentTovar.Product_ID.ToString();
                _photoPath = "picture.png";
                FileNameTB.Text = _photoPath;
                ShowPlaceholder();
            }
        }

        // Справочники

        private void LoadCategories()
        {
            using (var context = ShoeStore_SyrgashevEntities.GetContext())
                CategoryComboBox.ItemsSource = context.Categories.ToList();
        }

        private void LoadManufacturers()
        {
            using (var context = ShoeStore_SyrgashevEntities.GetContext())
                ManufacturerComboBox.ItemsSource = context.Manufacturers.ToList();
        }

        private void LoadSuppliers()
        {
            using (var context = ShoeStore_SyrgashevEntities.GetContext())
                SupplierComboBox.ItemsSource = context.Suppliers.ToList();
        }

        // Заполнение

        private void FillFieldsFromTovar()
        {
            IdTextBox.Text = _currentTovar.Product_ID.ToString();
            NameTextBox.Text = _currentTovar.Name ?? string.Empty;
            DescriptionTextBox.Text = _currentTovar.Description ?? string.Empty;
            PriceTextBox.Text = _currentTovar.Price.ToString("0.00");
            UnitTextBox.Text = _currentTovar.Unit ?? string.Empty;
            QuantityTextBox.Text = (_currentTovar.Stock_Quantity ?? 0).ToString();
            DiscountTextBox.Text = (_currentTovar.Discount ?? 0).ToString("0.00");

            CategoryComboBox.SelectedValue = _currentTovar.Category_ID;
            ManufacturerComboBox.SelectedValue = _currentTovar.Manufacturer_ID;
            SupplierComboBox.SelectedValue = _currentTovar.Supplier_ID;

            _photoPath = string.IsNullOrWhiteSpace(_currentTovar.Photo)
                ? "picture.png"
                : Path.GetFileName(_currentTovar.Photo);

            FileNameTB.Text = _photoPath;
            PhotoTovarIm.Source = LoadImageOrDefault(_photoPath);
        }

        // Фото

        private BitmapImage LoadImageOrDefault(string fileName)
        {
            string fullPath = Path.Combine(_targetDirectory, fileName);

            if (File.Exists(fullPath))
                return LoadBitmapWithoutLock(fullPath);

            return null;
        }

        private void ShowPlaceholder()
        {
            string placeholderPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Resources", "picture.png");

            if (File.Exists(placeholderPath))
                PhotoTovarIm.Source = LoadBitmapWithoutLock(placeholderPath);
        }

        private BitmapImage LoadBitmapWithoutLock(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.DecodePixelWidth = 300;
            bmp.DecodePixelHeight = 200;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private void SelectImageBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = ".png",
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
                };

                if (dialog.ShowDialog() != true)
                    return;

                string sourceFilePath = dialog.FileName;
                string oldFileName = _photoPath;

                // Всегда сохраняем как PNG (единый формат в папке Images)
                string newName = Path.GetFileNameWithoutExtension(dialog.SafeFileName) + ".png";
                _photoPath = newName;
                FileNameTB.Text = _photoPath;

                string newPath = Path.Combine(_targetDirectory, _photoPath);

                // Ресайз с сохранением пропорций до 300×200
                var original = new BitmapImage();
                original.BeginInit();
                original.UriSource = new Uri(sourceFilePath);
                original.CacheOption = BitmapCacheOption.OnLoad;   // освобождает файл
                original.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                original.EndInit();
                original.Freeze();

                double ratioX = 300.0 / original.PixelWidth;
                double ratioY = 200.0 / original.PixelHeight;
                double scale = Math.Min(ratioX, ratioY);

                int newW = Math.Max(1, (int)Math.Round(original.PixelWidth * scale));
                int newH = Math.Max(1, (int)Math.Round(original.PixelHeight * scale));

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.DrawImage(original, new Rect(0, 0, newW, newH));
                }

                var render = new RenderTargetBitmap(newW, newH, 96, 96, PixelFormats.Pbgra32);
                render.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(render));

                using (var fs = new FileStream(newPath, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                // Удаляем старое фото
                if (!string.IsNullOrEmpty(oldFileName) &&
                    oldFileName != "picture.png" &&
                    oldFileName != _photoPath)
                {
                    string oldPath = Path.Combine(_targetDirectory, oldFileName);

                }

                // Обновляем модель и БД
                _currentTovar.Photo = _photoPath;
                PhotoTovarIm.Source = LoadBitmapWithoutLock(newPath);

                if (_isEditMode)
                {
                    using (var context = ShoeStore_SyrgashevEntities.GetContext())
                    {
                        var existing = context.Products
                            .FirstOrDefault(p => p.Product_ID == _currentTovar.Product_ID);
                        if (existing != null)
                        {
                            existing.Photo = _photoPath;
                            context.SaveChanges();
                        }
                    }
                }

                MessageBox.Show("Изображение товара успешно установлено!",
                                "Успех", MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка работы с изображением: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // Валидация

        private void PriceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.,]+$");

        private void DiscountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.,]+$");

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");

        // Сохранение

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите наименование товара!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CategoryComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию товара!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (ManufacturerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите производителя товара!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SupplierComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите поставщика товара!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(UnitTextBox.Text))
            {
                MessageBox.Show("Введите единицу измерения!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text.Replace('.', ','),
                                  out decimal price) || price < 0)
            {
                MessageBox.Show("Цена должна быть неотрицательным числом!",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity < 0)
            {
                MessageBox.Show("Количество не может быть отрицательным!",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(DiscountTextBox.Text))
            {
                if (!decimal.TryParse(DiscountTextBox.Text.Replace('.', ','),
                                      out discount) || discount < 0)
                {
                    MessageBox.Show("Скидка должна быть неотрицательным числом!",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string finalPhoto = string.IsNullOrWhiteSpace(_photoPath)
                ? "picture.png"
                : _photoPath;

            try
            {
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    if (!_isEditMode)
                    {
                        // Добавление
                        var newTovar = new Products
                        {
                            Product_ID = _currentTovar.Product_ID,
                            Article = _currentTovar.Article,
                            Name = NameTextBox.Text.Trim(),
                            Category_ID = (int)CategoryComboBox.SelectedValue,
                            Description = DescriptionTextBox.Text?.Trim(),
                            Manufacturer_ID = (int)ManufacturerComboBox.SelectedValue,
                            Supplier_ID = (int)SupplierComboBox.SelectedValue,
                            Price = price,
                            Unit = UnitTextBox.Text.Trim(),
                            Stock_Quantity = quantity,
                            Discount = discount,
                            Photo = finalPhoto
                        };

                        context.Products.Add(newTovar);
                        context.SaveChanges();

                        MessageBox.Show("Товар успешно добавлен!",
                                        "Успех", MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                    }
                    else
                    {
                        // Редактирование
                        var existing = context.Products
                            .FirstOrDefault(p => p.Product_ID == _currentTovar.Product_ID);

                        if (existing == null)
                        {
                            MessageBox.Show("Товар не найден в базе данных!",
                                            "Ошибка", MessageBoxButton.OK,
                                            MessageBoxImage.Error);
                            return;
                        }

                        existing.Name = NameTextBox.Text.Trim();
                        existing.Category_ID = (int)CategoryComboBox.SelectedValue;
                        existing.Description = DescriptionTextBox.Text?.Trim();
                        existing.Manufacturer_ID = (int)ManufacturerComboBox.SelectedValue;
                        existing.Supplier_ID = (int)SupplierComboBox.SelectedValue;
                        existing.Price = price;
                        existing.Unit = UnitTextBox.Text.Trim();
                        existing.Stock_Quantity = quantity;
                        existing.Discount = discount;
                        existing.Photo = finalPhoto;

                        context.SaveChanges();

                        MessageBox.Show("Товар успешно обновлён!",
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

        // Удаление

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode || _currentTovar == null || _currentTovar.Product_ID == 0)
            {
                MessageBox.Show("Нечего удалять — товар ещё не сохранён.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    bool inOrders = context.OrderDetails
                        .Any(od => od.Product_ID == _currentTovar.Product_ID);

                    if (inOrders)
                    {
                        MessageBox.Show("Нельзя удалить товар, который присутствует в заказе!",
                                        "Ошибка удаления", MessageBoxButton.OK,
                                        MessageBoxImage.Error);
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

            var result = MessageBox.Show(
                $"Вы точно хотите удалить товар \"{_currentTovar.Name}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                PhotoTovarIm.Source = null;

                using (var context = ShoeStore_SyrgashevEntities.GetContext())
                {
                    var toDelete = context.Products
                        .FirstOrDefault(p => p.Product_ID == _currentTovar.Product_ID);

                    if (toDelete != null)
                    {
                        context.Products.Remove(toDelete);
                        context.SaveChanges();
                    }
                }

                MessageBox.Show("Товар успешно удалён!",
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