using FactorioModManager.App.Factorio;
using FactorioModManager.App.Services;
using FactorioModManager.App.ViewModels;

namespace FactorioModManager.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task InitializeAsync_does_not_crash_when_detected_list_exists_with_duplicate_mod_zips()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [1, 2, 3]);
        ModScannerTests.CreateZip(Path.Combine(temp.Path, "duplicate-mod_1.0.0.zip"), "duplicate-mod", "Duplicate Mod", "1.0.0", "Author", "Old");
        ModScannerTests.CreateZip(Path.Combine(temp.Path, "duplicate-mod_1.1.0.zip"), "duplicate-mod", "Duplicate Mod", "1.1.0", "Author", "New");

        var listFolder = ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "Created");
        Directory.CreateDirectory(listFolder);
        File.WriteAllText(Path.Combine(listFolder, FactorioFileNames.ModListJson), """
        {
          "mods": [
            { "name": "base", "enabled": true },
            { "name": "duplicate-mod", "enabled": true }
          ]
        }
        """);
        File.WriteAllBytes(Path.Combine(listFolder, FactorioFileNames.ModSettingsDat), [1, 2, 3]);

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings { LastModsFolderPath = temp.Path });
        var dialogs = new TestDialogService();
        var viewModel = CreateViewModel(dialogs, appSettingsService);

        await viewModel.InitializeAsync();

        Assert.Empty(dialogs.Errors);
        Assert.Equal(1, viewModel.AvailableModCount);
        Assert.Single(viewModel.ModLists);
        var selectedMod = Assert.Single(viewModel.SelectedMods);
        Assert.Equal("duplicate-mod", selectedMod.Name);
        Assert.Equal("1.1.0", selectedMod.Version);
    }

    [Fact]
    public async Task List_filter_tab_switching_and_inline_create_draft_update_view_state()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [1, 2, 3]);
        ModScannerTests.CreateZip(Path.Combine(temp.Path, "space-exploration_0.6.128.zip"), "space-exploration", "Space Exploration", "0.6.128", "Earendel", "Explore planets");
        CreateManagedList(temp.Path, "Space Exploration", "Explore planets");
        CreateManagedList(temp.Path, "Vanilla Plus", "Light-touch QoL");

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings { LastModsFolderPath = temp.Path });
        var dialogs = new TestDialogService();
        var viewModel = CreateViewModel(dialogs, appSettingsService);

        await viewModel.InitializeAsync();
        viewModel.ListSearchText = "space";
        viewModel.ShowModsCommand.Execute(null);
        viewModel.CreateModListCommand.Execute(null);

        Assert.True(viewModel.IsListsTabActive);
        Assert.True(viewModel.IsEditMode);
        Assert.StartsWith("New Mod List", viewModel.DraftName);
        Assert.Single(viewModel.ModLists);
        Assert.Single(viewModel.InstalledMods);
    }

    [Fact]
    public async Task Edit_sort_can_switch_between_name_and_active_mod_order()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [1, 2, 3]);
        ModScannerTests.CreateZip(Path.Combine(temp.Path, "alpha-mod_1.0.0.zip"), "alpha-mod", "Alpha Mod", "1.0.0");
        ModScannerTests.CreateZip(Path.Combine(temp.Path, "beta-mod_1.0.0.zip"), "beta-mod", "Beta Mod", "1.0.0");
        ModScannerTests.CreateZip(Path.Combine(temp.Path, "gamma-mod_1.0.0.zip"), "gamma-mod", "Gamma Mod", "1.0.0");
        CreateManagedList(temp.Path, "Selection", "Selected beta", "beta-mod");

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings { LastModsFolderPath = temp.Path });
        var dialogs = new TestDialogService();
        var viewModel = CreateViewModel(dialogs, appSettingsService);

        await viewModel.InitializeAsync();
        viewModel.EditSelectedCommand.Execute(null);

        Assert.True(viewModel.IsEditorSortedByActive);
        Assert.Equal(["Beta Mod", "Alpha Mod", "Gamma Mod"], viewModel.EditableMods.Select(mod => mod.Title));

        viewModel.EditableMods.Single(mod => mod.Name == "alpha-mod").IsSelected = true;

        Assert.Equal(["Alpha Mod", "Beta Mod", "Gamma Mod"], viewModel.EditableMods.Select(mod => mod.Title));

        viewModel.SortEditorByNameCommand.Execute(null);

        Assert.True(viewModel.IsEditorSortedByName);
        Assert.Equal(["Alpha Mod", "Beta Mod", "Gamma Mod"], viewModel.EditableMods.Select(mod => mod.Title));
    }

    [Fact]
    public async Task DuplicateSelectedCommand_creates_new_list_without_overwriting_source()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [0]);
        ModScannerTests.CreateZip(Path.Combine(temp.Path, "space-exploration_0.6.128.zip"), "space-exploration", "Space Exploration", "0.6.128");
        var sourceFolder = CreateManagedList(temp.Path, "Original", "Original description", "space-exploration");
        File.WriteAllBytes(Path.Combine(sourceFolder, FactorioFileNames.ModSettingsDat), [5, 6, 7]);

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings { LastModsFolderPath = temp.Path });
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService);

        await viewModel.InitializeAsync();
        viewModel.SelectedModList = viewModel.ModLists.Single(list => list.Name == "Original");
        viewModel.DuplicateSelectedCommand.Execute(null);

        Assert.True(viewModel.IsEditMode);
        Assert.Equal("Original Copy", viewModel.DraftName);
        Assert.Equal("Original description", viewModel.DraftDescription);

        await viewModel.SaveEditCommand.ExecuteAsync();

        var copyFolder = ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "Original Copy");
        Assert.True(Directory.Exists(sourceFolder));
        Assert.True(Directory.Exists(copyFolder));
        Assert.Equal([5, 6, 7], File.ReadAllBytes(Path.Combine(copyFolder, FactorioFileNames.ModSettingsDat)));
        var copiedMods = new ModListReader().ReadSelectedMods(copyFolder);
        Assert.Equal(["space-exploration"], copiedMods);
        Assert.Equal("Original Copy", viewModel.SelectedModList?.Name);
    }

    [Fact]
    public async Task InitializeAsync_marks_only_the_remembered_matching_list_active()
    {
        using var temp = new TempDirectory();
        var rememberedFolder = CreateManagedList(temp.Path, "Remembered", "Remembered active list");
        CreateManagedList(temp.Path, "SameFiles", "Same files but not remembered");
        CopyListFilesToRoot(temp.Path, rememberedFolder);

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings
        {
            LastModsFolderPath = temp.Path,
            ActiveModListFolderPath = rememberedFolder
        });
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService);

        await viewModel.InitializeAsync();

        Assert.Equal("Remembered", viewModel.ActiveListName);
        Assert.True(viewModel.ModLists.Single(list => list.Name == "Remembered").IsActive);
        Assert.False(viewModel.ModLists.Single(list => list.Name == "SameFiles").IsActive);
    }

    [Fact]
    public async Task InitializeAsync_clears_remembered_active_list_when_root_files_no_longer_match()
    {
        using var temp = new TempDirectory();
        var rememberedFolder = CreateManagedList(temp.Path, "Remembered", "Remembered active list");
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """{"mods":[{"name":"base","enabled":true}]}""");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [9, 9, 9]);

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings
        {
            LastModsFolderPath = temp.Path,
            ActiveModListFolderPath = rememberedFolder
        });
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService);

        await viewModel.InitializeAsync();

        Assert.False(viewModel.HasActiveList);
        Assert.All(viewModel.ModLists, list => Assert.False(list.IsActive));
        var settings = await appSettingsService.LoadAsync();
        Assert.Null(settings.ActiveModListFolderPath);
    }

    [Fact]
    public async Task Activating_mod_list_remembers_it_as_active()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), "old-root-list");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [0]);
        var activeFolder = CreateManagedList(temp.Path, "Pack", "Pack description");

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings { LastModsFolderPath = temp.Path });
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService);

        await viewModel.InitializeAsync();
        viewModel.SelectedModList = viewModel.ModLists.Single(list => list.Name == "Pack");
        await viewModel.ActivateSelectedCommand.ExecuteAsync();

        var settings = await appSettingsService.LoadAsync();
        Assert.Equal(activeFolder, settings.ActiveModListFolderPath);
        Assert.Equal("Pack", viewModel.ActiveListName);
        Assert.True(viewModel.ModLists.Single(list => list.Name == "Pack").IsActive);
    }

    [Fact]
    public async Task SelectActiveListCommand_switches_to_lists_and_selects_active_list()
    {
        using var temp = new TempDirectory();
        var activeFolder = CreateManagedList(temp.Path, "ActivePack", "Active list", "space-exploration");
        CreateManagedList(temp.Path, "OtherPack", "Other list");
        CopyListFilesToRoot(temp.Path, activeFolder);

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings
        {
            LastModsFolderPath = temp.Path,
            ActiveModListFolderPath = activeFolder
        });
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService);

        await viewModel.InitializeAsync();
        viewModel.ListSearchText = "Other";
        viewModel.ShowModsCommand.Execute(null);
        viewModel.SelectedModList = viewModel.ModLists.Single(list => list.Name == "OtherPack");

        viewModel.SelectActiveListCommand.Execute(null);

        Assert.True(viewModel.IsListsTabActive);
        Assert.Equal("ActivePack", viewModel.SelectedModList?.Name);
        Assert.Equal(2, viewModel.VisibleModListCount);
        Assert.Empty(viewModel.ListSearchText);
    }

    [Fact]
    public async Task RefreshCommand_rescans_disk_and_revalidates_active_state()
    {
        using var temp = new TempDirectory();
        var activeFolder = CreateManagedList(temp.Path, "ActivePack", "Initially active");
        CopyListFilesToRoot(temp.Path, activeFolder);

        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings
        {
            LastModsFolderPath = temp.Path,
            ActiveModListFolderPath = activeFolder
        });
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService);

        await viewModel.InitializeAsync();
        Assert.Single(viewModel.ModLists);
        Assert.Equal("ActivePack", viewModel.ActiveListName);

        CreateManagedList(temp.Path, "ManualPack", "Created outside the app");
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """{"mods":[{"name":"base","enabled":true}]}""");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [7, 7, 7]);

        await viewModel.RefreshCommand.ExecuteAsync();

        Assert.Equal(2, viewModel.ModLists.Count);
        Assert.Contains(viewModel.ModLists, list => list.Name == "ManualPack");
        Assert.False(viewModel.HasActiveList);
        Assert.All(viewModel.ModLists, list => Assert.False(list.IsActive));
        var settings = await appSettingsService.LoadAsync();
        Assert.Null(settings.ActiveModListFolderPath);
    }

    [Fact]
    public async Task InitializeAsync_loads_factorio_data_mods_from_saved_install_folder()
    {
        using var modsTemp = new TempDirectory();
        using var installTemp = new TempDirectory();
        File.WriteAllText(Path.Combine(modsTemp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(modsTemp.Path, FactorioFileNames.ModSettingsDat), [1, 2, 3]);
        ModScannerTests.CreateZip(Path.Combine(modsTemp.Path, "root-mod_1.0.0.zip"), "root-mod", "Root Mod", "1.0.0");
        ModScannerTests.CreateUnpackedMod(Path.Combine(installTemp.Path, "data"), "quality", "quality", "Quality", "2.0.0");

        var settingsPath = Path.Combine(modsTemp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings
        {
            LastModsFolderPath = modsTemp.Path,
            FactorioInstallFolderPath = installTemp.Path
        });
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService);

        await viewModel.InitializeAsync();

        Assert.Equal(2, viewModel.AvailableModCount);
        Assert.Contains(viewModel.InstalledMods, mod => mod.Name == "quality");
    }

    [Fact]
    public async Task BrowseInstallFolderCommand_saves_install_folder_and_refreshes_available_mods()
    {
        using var modsTemp = new TempDirectory();
        using var installTemp = new TempDirectory();
        File.WriteAllText(Path.Combine(modsTemp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(modsTemp.Path, FactorioFileNames.ModSettingsDat), [1, 2, 3]);
        ModScannerTests.CreateUnpackedMod(Path.Combine(installTemp.Path, "data"), "space-age", "space-age", "Space Age", "2.0.0");

        var settingsPath = Path.Combine(modsTemp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings { LastModsFolderPath = modsTemp.Path });
        var dialogs = new TestDialogService();
        dialogs.PickedFolders.Enqueue(installTemp.Path);
        var viewModel = CreateViewModel(dialogs, appSettingsService);

        await viewModel.InitializeAsync();
        await viewModel.BrowseInstallFolderCommand.ExecuteAsync();

        var settings = await appSettingsService.LoadAsync();
        Assert.Equal(installTemp.Path, settings.FactorioInstallFolderPath);
        Assert.Equal(installTemp.Path, viewModel.FactorioInstallFolderPath);
        Assert.Contains(viewModel.InstalledMods, mod => mod.Name == "space-age");
    }

    [Fact]
    public async Task InitializeAsync_auto_detects_factorio_install_folder_when_saved_path_is_missing()
    {
        using var modsTemp = new TempDirectory();
        using var installTemp = new TempDirectory();
        File.WriteAllText(Path.Combine(modsTemp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(modsTemp.Path, FactorioFileNames.ModSettingsDat), [1, 2, 3]);
        ModScannerTests.CreateUnpackedMod(Path.Combine(installTemp.Path, "data"), "elevated-rails", "elevated-rails", "Elevated Rails", "2.0.0");

        var settingsPath = Path.Combine(modsTemp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings { LastModsFolderPath = modsTemp.Path });
        var locator = new FactorioInstallLocator([installTemp.Path], []);
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService, locator);

        await viewModel.InitializeAsync();

        var settings = await appSettingsService.LoadAsync();
        Assert.Equal(installTemp.Path, settings.FactorioInstallFolderPath);
        Assert.Equal(installTemp.Path, viewModel.FactorioInstallFolderPath);
        Assert.Contains(viewModel.InstalledMods, mod => mod.Name == "elevated-rails");
    }

    [Fact]
    public async Task RefreshCommand_clears_invalid_saved_install_folder_when_no_detection_matches()
    {
        using var modsTemp = new TempDirectory();
        using var invalidInstallTemp = new TempDirectory();
        File.WriteAllText(Path.Combine(modsTemp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(modsTemp.Path, FactorioFileNames.ModSettingsDat), [1, 2, 3]);

        var settingsPath = Path.Combine(modsTemp.Path, "settings.json");
        var appSettingsService = new AppSettingsService(settingsPath);
        await appSettingsService.SaveAsync(new AppSettings
        {
            LastModsFolderPath = modsTemp.Path,
            FactorioInstallFolderPath = invalidInstallTemp.Path
        });
        var viewModel = CreateViewModel(new TestDialogService(), appSettingsService, new FactorioInstallLocator([], []));

        await viewModel.InitializeAsync();

        var settings = await appSettingsService.LoadAsync();
        Assert.Null(settings.FactorioInstallFolderPath);
        Assert.Null(viewModel.FactorioInstallFolderPath);
    }

    private static MainWindowViewModel CreateViewModel(
        TestDialogService dialogService,
        AppSettingsService appSettingsService,
        FactorioInstallLocator? factorioInstallLocator = null)
    {
        var modInfoReader = new ModInfoReader();
        var modListReader = new ModListReader();
        var metadataService = new ModListMetadataService();
        return new MainWindowViewModel(
            dialogService,
            appSettingsService,
            new FolderValidator(),
            factorioInstallLocator ?? new FactorioInstallLocator([], []),
            new ModScanner(modInfoReader),
            new ModListDetector(modListReader, metadataService),
            new ModListWriter(),
            metadataService,
            new ModSettingsManager(),
            new BackupService(),
            new ModListActivator(new BackupService()),
            new ModListFileManager(),
            new NameValidator(),
            new ActiveModListDetector());
    }

    private static string CreateManagedList(string root, string name, string description, params string[] selectedMods)
    {
        if (selectedMods.Length == 0)
        {
            selectedMods = ["space-exploration"];
        }

        var folder = ManagerWorkspacePaths.GetManagedListFolder(root, name);
        Directory.CreateDirectory(folder);
        var modEntries = string.Join(
            $",{Environment.NewLine}",
            selectedMods.Select(mod => $"    {{ \"name\": \"{mod}\", \"enabled\": true }}"));
        File.WriteAllText(
            Path.Combine(folder, FactorioFileNames.ModListJson),
            $"{{{Environment.NewLine}" +
            $"  \"mods\": [{Environment.NewLine}" +
            $"    {{ \"name\": \"base\", \"enabled\": true }},{Environment.NewLine}" +
            $"{modEntries}{Environment.NewLine}" +
            $"  ]{Environment.NewLine}" +
            $"}}{Environment.NewLine}");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [1, 2, 3]);
        new ModListMetadataService().Save(
            folder,
            description,
            selectedMods.ToDictionary(mod => mod, _ => "1.0.0"),
            null,
            null);
        return folder;
    }

    private static void CopyListFilesToRoot(string root, string modListFolder)
    {
        File.Copy(
            Path.Combine(modListFolder, FactorioFileNames.ModListJson),
            Path.Combine(root, FactorioFileNames.ModListJson),
            overwrite: true);
        File.Copy(
            Path.Combine(modListFolder, FactorioFileNames.ModSettingsDat),
            Path.Combine(root, FactorioFileNames.ModSettingsDat),
            overwrite: true);
    }
}
