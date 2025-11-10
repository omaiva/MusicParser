using MusicParser.Models;
using System.Threading.Tasks;

namespace MusicParser.Services
{
    public interface IPlaylistParser
    {
        Task<Playlist> ParseAsync(string url);
    }
}