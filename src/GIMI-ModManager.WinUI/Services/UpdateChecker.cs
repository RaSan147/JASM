﻿using System.Reflection;
using GIMI_ModManager.WinUI.Contracts.Services;
using GIMI_ModManager.WinUI.Models.Options;
using Newtonsoft.Json;
using Serilog;

namespace GIMI_ModManager.WinUI.Services;

public sealed class UpdateChecker : IDisposable
{
    private readonly ILogger _logger;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly NotificationManager _notificationManager;

    public Version CurrentVersion { get; private set; }
    public GtiHubRelease? LatestGitHubRelease { get; private set; }
    public Version? LatestRetrievedVersion { get; private set; }
    public event EventHandler<NewVersionEventArgs>? NewVersionAvailable;
    private Version? _ignoredVersion;
    public Version? IgnoredVersion => _ignoredVersion;
    private bool DisableChecker;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private const string ReleasesApiUrl = "https://api.github.com/repos/Jorixon/JASM/releases?per_page=100";

    public UpdateChecker(ILogger logger, ILocalSettingsService localSettingsService,
        NotificationManager notificationManager, CancellationToken cancellationToken = default)
    {
        _logger = logger.ForContext<UpdateChecker>();
        _localSettingsService = localSettingsService;
        _notificationManager = notificationManager;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var version = Assembly.GetExecutingAssembly().GetName().Version;

        if (version is null)
        {
            _logger.Error("Failed to get current version");
            DisableChecker = true;
            CurrentVersion = new(0, 0, 0, 0);
            return;
        }

        CurrentVersion = version;
        App.MainWindow.Closed += (sender, args) => Dispose();
    }

    public async Task InitializeAsync()
    {
        var options = await _localSettingsService.ReadSettingAsync<UpdateCheckerOptions>(UpdateCheckerOptions.Key) ??
                      new();
        if (options.IgnoreNewVersion is not null)
            _ignoredVersion = options.IgnoreNewVersion;

        if (options.IgnoreNewVersion is not null && options.IgnoreNewVersion <= CurrentVersion)
        {
            options.IgnoreNewVersion = null;
            await _localSettingsService.SaveSettingAsync(UpdateCheckerOptions.Key, options);
        }


        InitCheckerLoop(_cancellationTokenSource.Token);
    }

    public async Task IgnoreCurrentVersionAsync()
    {
        var options = await _localSettingsService.ReadSettingAsync<UpdateCheckerOptions>(UpdateCheckerOptions.Key) ??
                      new();
        options.IgnoreNewVersion = LatestRetrievedVersion;
        await _localSettingsService.SaveSettingAsync(UpdateCheckerOptions.Key, options);
        _ignoredVersion = LatestRetrievedVersion;
        OnNewVersionAvailable(new Version());
    }

    private void InitCheckerLoop(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForUpdatesAsync(cancellationToken);
                    await Task.Delay(TimeSpan.FromHours(2), cancellationToken);
                }
                catch (TaskCanceledException e)
                {
                    _logger.Debug(e, "Update checker stopped");
                    break;
                }
                catch (OperationCanceledException e)
                {
                    _logger.Debug(e, "Update checker canceled");
                    break;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to check for updates. Stopping Update checker");
                    break;
                }
            }
        }, CancellationToken.None);
    }


    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        if (DisableChecker)
            return;

        var gitHubRelease = await GetLatestVersionAsync(cancellationToken);

        if (gitHubRelease is null || gitHubRelease.Version.Equals(new Version()))
        {
            _logger.Warning("No versions found, latestVersion is null");
            return;
        }

        var latestVersion = gitHubRelease.Version;


        if (CurrentVersion == latestVersion || LatestRetrievedVersion == latestVersion)
        {
            _logger.Debug("No new version available");
            return;
        }


        if (CurrentVersion < latestVersion)
        {
            if (_ignoredVersion is not null && _ignoredVersion >= latestVersion)
                return;
            LatestRetrievedVersion = latestVersion;
            OnNewVersionAvailable(latestVersion);
        }
    }

    private async Task<GtiHubRelease?> GetLatestVersionAsync(CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();

        var result = await httpClient.GetAsync(ReleasesApiUrl, cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            _logger.Error("Failed to get latest version from GitHub. Status Code: {StatusCode}, Reason: {ReasonPhrase}",
                result.StatusCode, result.ReasonPhrase);
            return null;
        }

        var text = await result.Content.ReadAsStringAsync(cancellationToken);
        var gitHubReleases =
            (JsonConvert.DeserializeObject<GitHubReleaseApi[]>(text)) ?? Array.Empty<GitHubReleaseApi>();

        var releases = new List<GtiHubRelease>();


        foreach (var gitHubReleaseApi in gitHubReleases)
        {
            var release = ParseGitHubRelease(gitHubReleaseApi);
            releases.Add(release);
        }

        var latestVersion = releases.Where(x => !x.PreRelease).MaxBy(x => x.PublishedAt);
        return latestVersion;
    }

    private GtiHubRelease ParseGitHubRelease(GitHubReleaseApi gitHubReleaseApi)
    {
        var version = Version.TryParse(gitHubReleaseApi.tag_name?.Trim('v') ?? "", out var v)
            ? v
            : new Version();
        var preRelease = gitHubReleaseApi.prerelease;
        var publishedAt = gitHubReleaseApi.published_at;
        var body = gitHubReleaseApi.body;
        return new(version, preRelease, publishedAt, body);
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "JASM-Just_Another_Skin_Manager-Update-Checker");
        return httpClient;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }

    private void OnNewVersionAvailable(Version e)
        => NewVersionAvailable?.Invoke(this, new(e));


    public class NewVersionEventArgs : EventArgs
    {
        public Version Version { get; }

        public NewVersionEventArgs(Version version)
        {
            Version = version;
        }
    }


    private class GitHubReleaseApi
    {
        public string? target_commitish;
        public string? tag_name;
        public bool prerelease;
        public string? body;
        public DateTime published_at = DateTime.MinValue;
    }
}

public record GtiHubRelease(Version Version, bool PreRelease, DateTime PublishedAt, string? Body = null);