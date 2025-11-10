using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicParser.Models;
using MusicParser.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MusicParser.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IPlaylistParser _parser;

    [ObservableProperty]
    private string _urlToParse = "https://music.amazon.com/playlists/B01M11SBC8";

    [ObservableProperty]
    private string? _playlistName;

    [ObservableProperty]
    private string? _playlistDescription;

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<Song> Songs { get; } = [];

    public MainViewModel(IPlaylistParser parser)
    {
        _parser = parser;
    }

    [RelayCommand]
    private async Task ParsePlaylist()
    {
        if (IsLoading) return;

        IsLoading = true;
        ClearPlaylistData();

        try
        {
            Playlist playlist = await _parser.ParseAsync(UrlToParse);

            PlaylistName = playlist.Name;
            PlaylistDescription = playlist.Description;
            AvatarUrl = playlist.AvatarUrl;

            foreach (var song in playlist.Songs)
            {
                Songs.Add(song);
            }
        }
        catch (Exception ex)
        {
            PlaylistDescription = $"Ошибка парсинга: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearPlaylistData()
    {
        Songs.Clear();
        PlaylistName = null;
        PlaylistDescription = null;
        AvatarUrl = null;
    }
}