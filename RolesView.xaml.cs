using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class RolesView : UserControl
{
    private readonly RoleAdministrationService _roleService;
    private bool _loading;

    public RolesView(RoleAdministrationService roleService)
    {
        InitializeComponent();
        _roleService = roleService;
        Loaded += async (_, _) => await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        try
        {
            _loading = true;
            RolesListBox.ItemsSource = await _roleService.GetRolesAsync();
            if (RolesListBox.Items.Count > 0) RolesListBox.SelectedIndex = 0;
            StatusTextBlock.Text = $"{RolesListBox.Items.Count} wòl.";
        }
        catch (Exception ex) { ShowError(ex); }
        finally { _loading = false; }
        if (RolesListBox.SelectedItem is Role selectedRole) RenderPermissions(selectedRole);
    }

    private void RolesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || RolesListBox.SelectedItem is not Role role) return;
        RenderPermissions(role);
    }

    private void RenderPermissions(Role role)
    {
        PermissionsPanel.Children.Clear();
        foreach (var permission in Permissions.Catalog.OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
        {
            PermissionsPanel.Children.Add(new CheckBox
            {
                Content = permission,
                Tag = permission,
                IsChecked = role.Permissions.Any(grant => grant.PermissionName.Equals(permission, StringComparison.OrdinalIgnoreCase)),
                IsEnabled = !role.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase)
                    && !role.Name.Equals("Librarian", StringComparison.OrdinalIgnoreCase),
                Margin = new Thickness(5, 4, 12, 4),
                MinWidth = 185
            });
        }
    }

    private async void CreateRole_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var role = await _roleService.CreateAsync(NewRoleNameTextBox.Text, []);
            NewRoleNameTextBox.Clear();
            await LoadRolesAsync();
            RolesListBox.SelectedItem = RolesListBox.Items.Cast<Role>().First(item => item.Id == role.Id);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void SavePermissions_Click(object sender, RoutedEventArgs e)
    {
        if (RolesListBox.SelectedItem is not Role role) return;
        try
        {
            var grants = PermissionsPanel.Children.OfType<CheckBox>()
                .Where(checkBox => checkBox.IsChecked == true)
                .Select(checkBox => (string)checkBox.Tag);
            await _roleService.UpdateGrantsAsync(role.Id, grants);
            StatusTextBlock.Text = "Pèmisyon yo sove.";
            await LoadRolesAsync();
            RolesListBox.SelectedItem = RolesListBox.Items.Cast<Role>().First(item => item.Id == role.Id);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ShowError(Exception exception)
    {
        StatusTextBlock.Text = exception.Message;
        MessageBox.Show(exception.Message, "Jesyon wòl", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
