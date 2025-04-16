using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Coffe_Grinder
{
    public partial class CreateOrderPage : Page
    {
        private readonly Coffe_Grinder_DBEntities db = new Coffe_Grinder_DBEntities();
        private List<OrderItemViewModel> availableCoffees = new List<OrderItemViewModel>();
        private List<OrderItemViewModel> selectedItems = new List<OrderItemViewModel>();

        public CreateOrderPage()
        {
            InitializeComponent();
            LoadAvailableCoffees();

            // Add input validation handlers
            CustomerName.PreviewTextInput += CustomerName_PreviewTextInput;
            CustomerPhone.PreviewTextInput += CustomerPhone_PreviewTextInput;

            // Prevent pasting invalid characters
            DataObject.AddPastingHandler(CustomerName, OnPasteTextHandler);
            DataObject.AddPastingHandler(CustomerPhone, OnPastePhoneHandler);

            // Add text changed handlers for real-time validation
            CustomerName.TextChanged += CustomerName_TextChanged;
            CustomerPhone.TextChanged += CustomerPhone_TextChanged;
        }

        private void CustomerName_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow letters and spaces
            Regex regex = new Regex("^[a-zA-Z ]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void CustomerPhone_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            Regex regex = new Regex("^[0-9]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void OnPasteTextHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("^[a-zA-Z ]+$");
                if (!regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void OnPastePhoneHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("^[0-9]+$");
                if (!regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void CustomerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Visual feedback for invalid input
            if (!Regex.IsMatch(CustomerName.Text, @"^[a-zA-Z ]*$"))
            {
                CustomerName.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
            }
            else
            {
                CustomerName.Background = System.Windows.Media.Brushes.White;
            }
        }

        private void CustomerPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Visual feedback for invalid input
            if (!Regex.IsMatch(CustomerPhone.Text, @"^[0-9]*$"))
            {
                CustomerPhone.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
            }
            else
            {
                CustomerPhone.Background = System.Windows.Media.Brushes.White;
            }
        }

        private void LoadAvailableCoffees()
        {
            try
            {
                var coffees = db.CoffeeInventories
                    .Include(c => c.CoffeeType)
                    .Where(c => c.QuantityInStock > 0)
                    .ToList();

                availableCoffees = coffees.Select(c => new OrderItemViewModel
                {
                    CoffeeID = c.CoffeeID,
                    CoffeeName = c.CoffeeName,
                    CoffeeType = c.CoffeeType,
                    UnitPrice = c.PricePerKg.GetValueOrDefault(),
                    Quantity = 0,
                    MaxQuantity = c.QuantityInStock.GetValueOrDefault(),
                }).ToList();

                CoffeeSelectionGrid.ItemsSource = availableCoffees;
                OrderItemsGrid.ItemsSource = selectedItems;
                OrderTotal.Text = "0.00";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading coffees: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateOrderItems()
        {
            selectedItems = availableCoffees
                .Where(c => c.Quantity > 0)
                .ToList();

            OrderItemsGrid.ItemsSource = selectedItems;
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            decimal total = selectedItems.Sum(item => item.Subtotal);
            OrderTotal.Text = total.ToString("C");
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int coffeeId)
            {
                var coffee = availableCoffees.FirstOrDefault(c => c.CoffeeID == coffeeId);
                if (coffee != null && coffee.Quantity < coffee.MaxQuantity)
                {
                    coffee.Quantity++;
                    UpdateOrderItems();
                }
                else if (coffee != null && coffee.Quantity >= coffee.MaxQuantity)
                {
                    MessageBox.Show($"Cannot order more than available stock ({coffee.MaxQuantity}kg)",
                        "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int coffeeId)
            {
                var coffee = availableCoffees.FirstOrDefault(c => c.CoffeeID == coffeeId);
                if (coffee != null && coffee.Quantity > 0)
                {
                    coffee.Quantity--;
                    UpdateOrderItems();
                }
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is OrderItemViewModel item)
            {
                var coffee = availableCoffees.FirstOrDefault(c => c.CoffeeID == item.CoffeeID);
                if (coffee != null)
                {
                    coffee.Quantity = 0;
                    UpdateOrderItems();
                }
            }
        }

        private void ClearOrder_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Clear your current order?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var coffee in availableCoffees)
                {
                    coffee.Quantity = 0;
                }
                UpdateOrderItems();
                CustomerName.Text = "";
                CustomerPhone.Text = "";
                OrderNotes.Text = "";
                CustomerName.Background = System.Windows.Media.Brushes.White;
                CustomerPhone.Background = System.Windows.Media.Brushes.White;
            }
        }

        private void SubmitOrder_Click(object sender, RoutedEventArgs e)
        {
            // Validate customer name (only letters and spaces)
            if (string.IsNullOrWhiteSpace(CustomerName.Text) || !Regex.IsMatch(CustomerName.Text, @"^[a-zA-Z ]+$"))
            {
                MessageBox.Show("Please enter a valid customer name (letters and spaces only)", "Invalid Name",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerName.Focus();
                CustomerName.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                return;
            }

            // Validate phone number (mandatory and only numbers)
            if (string.IsNullOrWhiteSpace(CustomerPhone.Text) || !Regex.IsMatch(CustomerPhone.Text, @"^[0-9]+$"))
            {
                MessageBox.Show("Please enter a valid phone number (numbers only)", "Phone Number Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerPhone.Focus();
                CustomerPhone.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                return;
            }

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please add at least one item to your order", "Order Empty",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create new order
                var newOrder = new Order
                {
                    OrderDate = DateTime.Now,
                    StatusID = 1, // Pending status
                    CustomerName = CustomerName.Text.Trim(),
                    PhoneNumber = CustomerPhone.Text.Trim(),
                    Notes = OrderNotes.Text,
                    TotalPrice = selectedItems.Sum(i => i.Subtotal)
                };

                db.Orders.Add(newOrder);
                db.SaveChanges();

                // Add order items and update inventory
                foreach (var item in selectedItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderID = newOrder.OrderID,
                        CoffeeID = item.CoffeeID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    db.OrderDetails.Add(orderDetail);

                    // Update inventory
                    var coffee = db.CoffeeInventories.Find(item.CoffeeID);
                    if (coffee != null)
                    {
                        coffee.QuantityInStock -= item.Quantity;
                    }
                }

                db.SaveChanges();

                MessageBox.Show($"Order #{newOrder.OrderID} created successfully!\nTotal: {newOrder.TotalPrice:C}",
                    "Order Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset the form
                ClearOrder_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class OrderItemViewModel : INotifyPropertyChanged
        {
            public int CoffeeID { get; set; }
            public string CoffeeName { get; set; }
            public CoffeeType CoffeeType { get; set; }
            public decimal UnitPrice { get; set; }
            public int MaxQuantity { get; set; }

            private int _quantity;
            public int Quantity
            {
                get => _quantity;
                set
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(Subtotal));
                }
            }

            public decimal Subtotal => Quantity * UnitPrice;

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}