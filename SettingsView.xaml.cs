using GestionBibliotheque.ViewModels;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace GestionBibliotheque;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _viewModel;
    public SettingsView(SettingsViewModel viewModel) { InitializeComponent(); _viewModel = viewModel; DataContext = viewModel; Loaded += SettingsView_Loaded; }
    private async void SettingsView_Loaded(object sender, RoutedEventArgs e) { await _viewModel.LoadAsync(); }
    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "SQLite backup (*.db)|*.db", FileName = $"library-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db" };
        if (dialog.ShowDialog() != true) return;
        try { await _viewModel.BackupAsync(dialog.FileName); MessageBox.Show("Backup la fini.", "Backup", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception exception) { MessageBox.Show(exception.Message, "Backup", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "SQLite backup (*.db)|*.db" };
        if (dialog.ShowDialog() != true) return;
        if (MessageBox.Show("Restore a pral ranplase done lokal yo. Kontinye?", "Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { await _viewModel.RestoreAsync(dialog.FileName); MessageBox.Show("Restore la fini. Rekòmanse aplikasyon an pou rechaje done yo.", "Restore", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception exception) { MessageBox.Show(exception.Message, "Restore", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}