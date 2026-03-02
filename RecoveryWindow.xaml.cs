using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InScope.Services;

namespace InScope;

public partial class RecoveryWindow : Window
{
    private const string BlockBackupsFolderName = "BlockBackups";

    public RecoveryWindow()
    {
        InitializeComponent();
        LoadEntries();
    }

    private void LoadEntries()
    {
        var entries = BlockChangeLog.GetRecentEntries()
            .Where(e => !string.IsNullOrEmpty(e.BackupPath))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        EntriesList.ItemsSource = entries;

        if (entries.Count == 0)
        {
            StatusText.Text = "No block backups found. Backups are created when you save changes to a block in the Block Library Editor. Backups are kept for 14 days.";
        }
    }

    private void EntriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RestoreButton.IsEnabled = EntriesList.SelectedItem != null;
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesList.SelectedItem is not BlockChangeLog.ChangeEntry entry)
            return;

        if (string.IsNullOrEmpty(entry.BackupPath))
            return;

        var backupFullPath = BlockChangeLog.GetBackupFullPath(entry.BackupPath);
        if (string.IsNullOrEmpty(backupFullPath) || !File.Exists(backupFullPath))
        {
            MessageBox.Show("Backup file no longer exists.", "Recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoadEntries();
            return;
        }

        var contentPath = ContentPathResolver.GetEffectiveContentPath();
        var blocksPath = Path.Combine(contentPath, Constants.BlocksFolder);
        var targetPath = Path.Combine(blocksPath, $"{entry.BlockId}{Constants.RtfExtension}");

        if (!Directory.Exists(blocksPath))
        {
            MessageBox.Show("Content Blocks folder not found.", "Recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var message = $"Restore '{entry.BlockId}' to its previous version?\n\n" +
                      $"This will overwrite the current block file at:\n{targetPath}";
        var result = MessageBox.Show(message, "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            File.Copy(backupFullPath, targetPath, overwrite: true);
            AppLogger.Log(AppLogger.LogLevel.Info, "Recovery", "Block restored", new { blockId = entry.BlockId, from = entry.BackupPath });
            StatusText.Text = $"Restored {entry.BlockId}. Close and reopen the Block Library Editor to see the restored content.";
            MessageBox.Show($"Restored {entry.BlockId}. Refresh the Block Library Editor to see the change.", "Recovery", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            AppLogger.Log(AppLogger.LogLevel.Error, "Recovery", "Restore failed", new { blockId = entry.BlockId, message = ex.Message });
            MessageBox.Show($"Restore failed: {ex.Message}", "Recovery", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var baseDir = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "InScope");
        var backupsPath = Path.Combine(baseDir, BlockBackupsFolderName);

        if (!Directory.Exists(backupsPath))
        {
            Directory.CreateDirectory(backupsPath);
        }

        Process.Start(new ProcessStartInfo(backupsPath) { UseShellExecute = true });
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
