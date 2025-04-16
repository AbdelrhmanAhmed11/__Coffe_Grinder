using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using System.Text.RegularExpressions;

namespace Coffe_Grinder
{
    public partial class inventory : Page
    {
        private readonly Coffe_Grinder_DBEntities db = new Coffe_Grinder_DBEntities();

        public inventory()
        {
            InitializeComponent();
            LoadCoffeeInventory();
            LoadCoffeeTypes();
            AttachEventHandlers();
            SetupInputValidation();
        }

        private void SetupInputValidation()
        {
            // Numeric validation for amount and price
            Amount.PreviewTextInput += Numeric_PreviewTextInput;
            PricePerKg.PreviewTextInput += Decimal_PreviewTextInput;
            SearchId.PreviewTextInput += Numeric_PreviewTextInput;

            // Prevent paste for numeric fields
            DataObject.AddPastingHandler(Amount, OnPasteNumericHandler);
            DataObject.AddPastingHandler(PricePerKg, OnPasteDecimalHandler);
            DataObject.AddPastingHandler(SearchId, OnPasteNumericHandler);

            // Text changed handlers for visual feedback
            CoffeeName.TextChanged += CoffeeName_TextChanged;
            Amount.TextChanged += Amount_TextChanged;
            PricePerKg.TextChanged += PricePerKg_TextChanged;
            SearchId.TextChanged += SearchId_TextChanged;
        }

        #region Validation Event Handlers
        private void Numeric_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void Decimal_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Allow numbers and single decimal point
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            string newText = ((TextBox)sender).Text + e.Text;
            e.Handled = !regex.IsMatch(newText);
        }

        private void OnPasteNumericHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, "^[0-9]+$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void OnPasteDecimalHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, @"^[0-9]*(?:\.[0-9]*)?$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void CoffeeName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CoffeeName.Text))
            {
                CoffeeName.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
            }
            else
            {
                CoffeeName.Background = System.Windows.Media.Brushes.White;
            }
        }

        private void Amount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(Amount.Text, out int quantity) || quantity <= 0)
            {
                Amount.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
            }
            else
            {
                Amount.Background = System.Windows.Media.Brushes.White;
            }
        }

        private void PricePerKg_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!decimal.TryParse(PricePerKg.Text, out decimal price) || price <= 0)
            {
                PricePerKg.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
            }
            else
            {
                PricePerKg.Background = System.Windows.Media.Brushes.White;
            }
        }

        private void SearchId_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchId.Text) && !int.TryParse(SearchId.Text, out _))
            {
                SearchId.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
            }
            else
            {
                SearchId.Background = System.Windows.Media.Brushes.White;
            }
        }
        #endregion

        private void AddNewCoffeeType(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCoffeeTypeName.Text))
            {
                ShowErrorMessage("Please enter a coffee type name.");
                NewCoffeeTypeName.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                return;
            }

            try
            {
                var newCoffeeType = new CoffeeType
                {
                    TypeName = NewCoffeeTypeName.Text.Trim(),
                };

                db.CoffeeTypes.Add(newCoffeeType);
                db.SaveChanges();

                LoadCoffeeTypes();
                NewCoffeeTypeName.Text = string.Empty;
                NewCoffeeTypeName.Background = System.Windows.Media.Brushes.White;
                ShowSuccessMessage("New coffee type added successfully!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error adding new coffee type: {ex.Message}");
            }
        }

        private void FindById(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchId.Text))
            {
                ShowErrorMessage("Please enter an ID to search.");
                SearchId.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                return;
            }

            if (!int.TryParse(SearchId.Text, out int coffeeId))
            {
                ShowErrorMessage("Please enter a valid numeric ID.");
                SearchId.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                return;
            }

            try
            {
                var inventoryItem = db.CoffeeInventories
                    .Include(c => c.CoffeeType)
                    .FirstOrDefault(c => c.CoffeeID == coffeeId);

                if (inventoryItem == null)
                {
                    ShowErrorMessage($"No coffee found with ID: {coffeeId}");
                    return;
                }

                Id.Text = inventoryItem.CoffeeID.ToString();
                CoffeeName.Text = inventoryItem.CoffeeName;
                CoffeeType.SelectedValue = inventoryItem.CoffeeTypeID;
                Description.Text = inventoryItem.Description;
                Amount.Text = inventoryItem.QuantityInStock.ToString();
                PricePerKg.Text = inventoryItem.PricePerKg.ToString();

                CoffeeDataGrid.SelectedItem = inventoryItem;
                CoffeeDataGrid.ScrollIntoView(inventoryItem);

                ShowSuccessMessage($"Coffee ID {coffeeId} loaded successfully.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error finding coffee: {ex.Message}");
            }
        }

        private void AttachEventHandlers()
        {
            CoffeeDataGrid.SelectionChanged += (sender, e) =>
            {
                if (CoffeeDataGrid.SelectedItem is CoffeeInventory selectedItem)
                {
                    Id.Text = selectedItem.CoffeeID.ToString();
                    CoffeeName.Text = selectedItem.CoffeeName;
                    CoffeeType.SelectedValue = selectedItem.CoffeeTypeID;
                    Description.Text = selectedItem.Description;
                    Amount.Text = selectedItem.QuantityInStock.ToString();
                    PricePerKg.Text = selectedItem.PricePerKg.ToString();
                }
            };
        }

        private void LoadCoffeeTypes()
        {
            try
            {
                CoffeeType.ItemsSource = db.CoffeeTypes
                    .OrderBy(ct => ct.TypeName)
                    .ToList();
                CoffeeType.SelectedValuePath = "CoffeeTypeID";
                CoffeeType.DisplayMemberPath = "TypeName";
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading coffee types: {ex.Message}");
            }
        }

        private void refresh(object sender, RoutedEventArgs e)
        {
            LoadCoffeeInventory();
            LoadCoffeeTypes();
            ClearForm();
            SearchId.Text = string.Empty;
            SearchId.Background = System.Windows.Media.Brushes.White;
            ShowSuccessMessage("Inventory refreshed successfully.");
        }

        private void add(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                var selectedCoffeeType = (CoffeeType)CoffeeType.SelectedItem;

                var newInventory = new CoffeeInventory
                {
                    CoffeeName = CoffeeName.Text.Trim(),
                    CoffeeTypeID = selectedCoffeeType.CoffeeTypeID,
                    QuantityInStock = int.Parse(Amount.Text),
                    PricePerKg = decimal.Parse(PricePerKg.Text),
                    Description = Description.Text?.Trim()
                };

                db.CoffeeInventories.Add(newInventory);
                db.SaveChanges();

                LoadCoffeeInventory();
                ClearForm();
                ShowSuccessMessage("Coffee added to inventory successfully.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error adding coffee: {ex.Message}");
            }
        }

        private void update(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Id.Text))
            {
                ShowErrorMessage("Please select an item to update.");
                return;
            }

            if (!ValidateForm()) return;

            try
            {
                int coffeeId = int.Parse(Id.Text);
                var inventory = db.CoffeeInventories.Find(coffeeId);

                if (inventory == null)
                {
                    ShowErrorMessage("Selected coffee not found in database.");
                    return;
                }

                inventory.CoffeeName = CoffeeName.Text.Trim();
                inventory.CoffeeTypeID = (int)CoffeeType.SelectedValue;
                inventory.QuantityInStock = int.Parse(Amount.Text);
                inventory.PricePerKg = decimal.Parse(PricePerKg.Text);
                inventory.Description = Description.Text?.Trim();

                db.SaveChanges();
                LoadCoffeeInventory();
                ClearForm();
                ShowSuccessMessage("Coffee updated successfully.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error updating coffee: {ex.Message}");
            }
        }

        private void delete(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Id.Text))
            {
                ShowErrorMessage("Please select an item to delete.");
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this coffee? This will also delete all related order details.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                int coffeeId = int.Parse(Id.Text);

                // First delete any related order details
                var relatedOrderDetails = db.OrderDetails.Where(od => od.CoffeeID == coffeeId).ToList();
                if (relatedOrderDetails.Any())
                {
                    db.OrderDetails.RemoveRange(relatedOrderDetails);
                }

                // Then delete the coffee inventory item
                var inventory = db.CoffeeInventories.FirstOrDefault(x => x.CoffeeID == coffeeId);
                if (inventory == null)
                {
                    ShowErrorMessage("Selected coffee not found in database.");
                    return;
                }

                db.CoffeeInventories.Remove(inventory);
                db.SaveChanges();

                // Reseed the identity column to fill gaps
                var maxId = db.CoffeeInventories.Any() ? db.CoffeeInventories.Max(c => c.CoffeeID) : 0;
                db.Database.ExecuteSqlCommand($"DBCC CHECKIDENT ('CoffeeInventory', RESEED, {maxId})");

                LoadCoffeeInventory();
                ClearForm();
                ShowSuccessMessage("Coffee deleted successfully. ID sequence has been reorganized.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error deleting coffee: {ex.Message}\n\nMake sure there are no orders referencing this coffee item.");
            }
        }

        private void LoadCoffeeInventory()
        {
            try
            {
                CoffeeDataGrid.ItemsSource = db.CoffeeInventories
                    .Include(c => c.CoffeeType)
                    .OrderBy(c => c.CoffeeID)
                    .ToList();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading coffee inventory: {ex.Message}");
            }
        }

        private bool ValidateForm()
        {
            bool isValid = true;

            // Coffee Name validation
            if (string.IsNullOrWhiteSpace(CoffeeName.Text))
            {
                CoffeeName.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                ShowErrorMessage("Please enter a coffee name.");
                isValid = false;
            }
            else
            {
                CoffeeName.Background = System.Windows.Media.Brushes.White;
            }

            // Coffee Type validation
            if (CoffeeType.SelectedItem == null)
            {
                ShowErrorMessage("Please select a coffee type.");
                isValid = false;
            }

            // Quantity validation
            if (!int.TryParse(Amount.Text, out int quantity) || quantity <= 0)
            {
                Amount.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                ShowErrorMessage("Please enter a valid quantity (positive number).");
                isValid = false;
            }
            else
            {
                Amount.Background = System.Windows.Media.Brushes.White;
            }

            // Price validation
            if (!decimal.TryParse(PricePerKg.Text, out decimal price) || price <= 0)
            {
                PricePerKg.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                ShowErrorMessage("Please enter a valid price (positive number).");
                isValid = false;
            }
            else
            {
                PricePerKg.Background = System.Windows.Media.Brushes.White;
            }

            return isValid;
        }

        private void ClearForm()
        {
            Id.Text = string.Empty;
            CoffeeName.Text = string.Empty;
            CoffeeName.Background = System.Windows.Media.Brushes.White;
            CoffeeType.SelectedIndex = -1;
            Description.Text = string.Empty;
            Amount.Text = string.Empty;
            Amount.Background = System.Windows.Media.Brushes.White;
            PricePerKg.Text = string.Empty;
            PricePerKg.Background = System.Windows.Media.Brushes.White;
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}