using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CWAccess.Installer
{
    /// <summary>The mod's GitHub releases: newest version, its notes and its files.</summary>
    public sealed class GitHubClient
    {
        public const string Owner = "wightfall";
        public const string Repo = "ChainsawWarriorAccess";

        public static string ReleasesPage => "https://github.com/" + Owner + "/" + Repo + "/releases";

        static GitHubClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        private static WebClient NewClient()
        {
            var wc = new WebClient();
            wc.Headers[HttpRequestHeader.UserAgent] = "CWAccessSetup/" + Assembly.GetExecutingAssembly().GetName().Version;
            wc.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
            wc.Encoding = Encoding.UTF8;
            return wc;
        }

        /// <summary>Newest published release, or null when nothing is published.</summary>
        public async Task<Release> GetLatestAsync()
        {
            using (WebClient wc = NewClient())
            {
                string json = await wc.DownloadStringTaskAsync("https://api.github.com/repos/" + Owner + "/" + Repo + "/releases?per_page=20").ConfigureAwait(false);
                return ReleaseJson.Newest(ReleaseJson.ParseReleases(json));
            }
        }

        /// <summary>Downloads a release file to a temporary file and returns its path.</summary>
        public async Task<string> DownloadAsync(ReleaseAsset asset, IProgress<int> progress)
        {
            string path = Path.Combine(Path.GetTempPath(), "CWAccess-" + Guid.NewGuid().ToString("N") + "-" + asset.Name);
            using (WebClient wc = NewClient())
            {
                wc.Headers[HttpRequestHeader.Accept] = "application/octet-stream";
                if (progress != null) wc.DownloadProgressChanged += (s, e) => progress.Report(e.ProgressPercentage);
                await wc.DownloadFileTaskAsync(new Uri(asset.Url), path).ConfigureAwait(false);
            }
            return path;
        }
    }

    [DataContract]
    public sealed class ReleaseAsset
    {
        [DataMember(Name = "name")] public string Name;
        [DataMember(Name = "browser_download_url")] public string Url;
        [DataMember(Name = "size")] public long Size;
    }

    public enum AssetKind
    {
        Package,
        Setup,
    }

    [DataContract]
    public sealed class Release
    {
        [DataMember(Name = "tag_name")] public string Tag;
        [DataMember(Name = "name")] public string Name;
        [DataMember(Name = "body")] public string Body;
        [DataMember(Name = "draft")] public bool Draft;
        [DataMember(Name = "prerelease")] public bool Prerelease;
        [DataMember(Name = "assets")] public List<ReleaseAsset> Assets = new List<ReleaseAsset>();

        public Version Version => Versions.Parse(Tag);

        /// <summary>The mod zip or the setup exe of this release.</summary>
        public ReleaseAsset Find(AssetKind kind)
        {
            foreach (ReleaseAsset a in Assets)
            {
                if (a == null || a.Name == null) continue;
                string n = a.Name.ToLowerInvariant();
                if (kind == AssetKind.Package && n.EndsWith(".zip")) return a;
                if (kind == AssetKind.Setup && n.EndsWith(".exe")) return a;
            }
            return null;
        }
    }

    public static class Versions
    {
        /// <summary>"v1.2.3", "1.2", "CWAccess 1.2.0" ... as a Version, null when there is none.</summary>
        public static Version Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            Match m = Regex.Match(text, @"\d+(\.\d+){0,3}");
            if (!m.Success) return null;
            string s = m.Value.Contains(".") ? m.Value : m.Value + ".0";
            return Version.TryParse(s, out Version v) ? Normalize(v) : null;
        }

        /// <summary>1.2 and 1.2.0.0 compare equal.</summary>
        public static Version Normalize(Version v)
        {
            if (v == null) return null;
            return new Version(v.Major, v.Minor, Math.Max(0, v.Build), Math.Max(0, v.Revision));
        }

        public static bool IsNewer(Version candidate, Version current)
        {
            if (candidate == null) return false;
            if (current == null) return true;
            return Normalize(candidate) > Normalize(current);
        }

        public static string Show(Version v)
        {
            if (v == null) return SetupText.T("not_installed", "not installed");
            v = Normalize(v);
            return v.Revision > 0 ? v.ToString(4) : v.ToString(3);
        }
    }

    public static class ReleaseJson
    {
        public static List<Release> ParseReleases(string json)
        {
            return Parse<List<Release>>(json) ?? new List<Release>();
        }

        /// <summary>Newest non-draft, non-prerelease release by version number.</summary>
        public static Release Newest(IEnumerable<Release> releases)
        {
            Release best = null;
            foreach (Release r in releases)
            {
                if (r == null || r.Draft || r.Prerelease || r.Version == null) continue;
                if (best == null || r.Version > best.Version) best = r;
            }
            return best;
        }

        private static T Parse<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                try
                {
                    return serializer.ReadObject(ms) as T;
                }
                catch (SerializationException)
                {
                    return null;
                }
            }
        }
    }

    /// <summary>GitHub release notes (Markdown) as plain lines for a list a screen reader reads line by line.</summary>
    public static class ReleaseNotes
    {
        private static readonly Regex Heading = new Regex(@"^\s{0,3}#+\s*");
        private static readonly Regex Link = new Regex(@"!?\[([^\]]*)\]\(([^)]*)\)");
        private static readonly Regex Bullet = new Regex(@"^(\s*)[-*+]\s+");
        private static readonly Regex Rule = new Regex(@"^\s*([-*_]\s*){3,}$");
        private static readonly Regex Html = new Regex(@"<[^>]+>");

        public static List<string> Lines(string markdown)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(markdown)) return result;
            foreach (string raw in markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.Trim().Length == 0 || Rule.IsMatch(line)) continue;
                line = Heading.Replace(line, string.Empty);
                line = Link.Replace(line, m => m.Groups[1].Value.Trim().Length > 0 ? m.Groups[1].Value : m.Groups[2].Value);
                line = Html.Replace(line, string.Empty);
                line = line.Replace("**", string.Empty).Replace("__", string.Empty).Replace("`", string.Empty);
                Match b = Bullet.Match(line);
                if (b.Success) line = b.Groups[1].Value + "- " + line.Substring(b.Length);
                if (line.Trim().Length > 0) result.Add(line.Trim());
            }
            return result;
        }
    }
}
