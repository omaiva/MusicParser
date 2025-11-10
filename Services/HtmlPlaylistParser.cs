using Microsoft.Playwright;
using MusicParser.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MusicParser.Services
{
    public class HtmlPlaylistParser : IPlaylistParser
    {
        private const string CSS_HEADER_NODE = "music-detail-header[headline]";
        private const string CSS_SONG_ROWS = "music-image-row, music-text-row";
        private const string CSS_TITLE_REL = "div.col1 music-link a";
        private const string CSS_ARTIST_REL = "div.col2 music-link:nth-of-type(1) a";
        private const string CSS_ALBUM_REL = "div.col2 music-link:nth-of-type(2) a";
        private const string CSS_DURATION_REL = "div.col4 music-link span";

        public async Task<Playlist> ParseAsync(string url)
        {
            var playlist = new Playlist();

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = true
            });
            var context = await browser.NewContextAsync(new()
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });
            var page = await context.NewPageAsync();

            try
            {
                await page.GotoAsync(url, new()
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 30000
                });

                const string loadedSongSelector = "music-image-row[primary-text], music-text-row[primary-text]";

                try
                {
                    await page.Locator(loadedSongSelector).First.WaitForAsync(new() { Timeout = 15000 });
                }
                catch (TimeoutException)
                {
                    throw new TimeoutException("Could not wait for the LOADED song line (with the [primary-text] attribute).");
                }

                await Task.Delay(1000);

                var headerLocator = page.Locator(CSS_HEADER_NODE);
                playlist.Name = await headerLocator.GetAttributeAsync("headline");
                playlist.AvatarUrl = await headerLocator.GetAttributeAsync("image-src");

                var sbDescription = new StringBuilder();
                sbDescription.Append(await headerLocator.GetAttributeAsync("secondary-text"));
                string tertiary = await headerLocator.GetAttributeAsync("tertiary-text");
                if (!string.IsNullOrEmpty(tertiary))
                {
                    sbDescription.Append($" ({tertiary})");
                }
                playlist.Description = sbDescription.ToString();
                string mainArtistOrOwner = await headerLocator.GetAttributeAsync("primary-text");

                var songLocators = await page.Locator(CSS_SONG_ROWS).AllAsync();

                if (songLocators.Count == 0)
                {
                    throw new DllNotFoundException("Playwright found the title, but did not find the songs.");
                }

                foreach (var row in songLocators)
                {
                    var title = await TryGetTextAsync(row.Locator(CSS_TITLE_REL));

                    if (string.IsNullOrEmpty(title)) continue;

                    string artist = await TryGetTextAsync(row.Locator(CSS_ARTIST_REL)) ?? mainArtistOrOwner;
                    string album = await TryGetTextAsync(row.Locator(CSS_ALBUM_REL)) ?? playlist.Name;
                    string duration = await TryGetTextAsync(row.Locator(CSS_DURATION_REL));

                    var song = new Song
                    {
                        Title = title,
                        Duration = duration,
                        Artist = artist,
                        Album = album
                    };
                    playlist.Songs.Add(song);
                }

                return playlist;
            }
            catch (Exception ex)
            {
                return GetFallbackPlaylist(url, $"Critical error Playwright: {ex.Message}.");
            }
            finally
            {
                await context.CloseAsync();
                await browser.CloseAsync();
            }
        }

        private static async Task<string> TryGetTextAsync(ILocator locator)
        {
            try
            {
                return await locator.InnerTextAsync(new() { Timeout = 100 });
            }
            catch (TimeoutException)
            {
                return null;
            }
        }

        private static Playlist GetFallbackPlaylist(string url, string errorMessage)
        {
            var playlist = new Playlist
            {
                Name = "Parsing exception",
                Description = $"Failed to parse data. \nReason: {errorMessage}",
                AvatarUrl = url
            };
            return playlist;
        }
    }
}