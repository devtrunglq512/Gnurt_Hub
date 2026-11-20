using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gnurt.Editor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Gnurt.Hub
{
    /// <summary>Trạng thái của một package trong catalog so với project hiện tại.</summary>
    public sealed class PackageState
    {
        public CatalogEntry Entry;

        /// <summary>Thông tin package đang cài, null nếu chưa cài.</summary>
        public PackageInfo Installed;

        /// <summary>Nguồn git nếu đang cài từ git.</summary>
        public GitSource? Git;

        /// <summary>Tag version trên repo, mới nhất trước.</summary>
        public IReadOnlyList<string> Tags = Array.Empty<string>();

        /// <summary>Lỗi khi hỏi git danh sách tag, null nếu không lỗi.</summary>
        public string TagsError;

        public bool IsInstalled => Installed != null;
        public bool IsGit => Installed?.source == PackageSource.Git;
        public bool IsLocal => Installed?.source == PackageSource.Local;

        /// <summary>Package nằm thẳng trong thư mục Packages của project (đang được phát triển tại chỗ).</summary>
        public bool IsEmbedded => Installed?.source == PackageSource.Embedded;

        public string Revision => Git?.Revision;
        public string LatestTag => Tags.Count > 0 ? Tags[0] : null;
        public bool HasUpdate => IsGit && HubPlanner.HasUpdate(LatestTag, Entry.tagPrefix, Installed.version);
    }

    /// <summary>
    /// Bộ não của Hub: giữ catalog và trạng thái từng package, làm các việc cài, gỡ, đổi version.
    /// Không hiện hộp thoại nào (việc đó để window lo), nên dùng được cả trong test tự động.
    /// </summary>
    public sealed class HubService : Singleton<HubService>
    {
        static readonly GLogger Log = GLog.Tag("Hub");

        readonly List<PackageState> _states = new List<PackageState>();
        bool _busy;

        public HubCatalog Catalog { get; private set; }
        public CatalogSource CatalogSource { get; private set; }

        /// <summary>Lỗi khi tải catalog trên mạng (Hub vẫn chạy bằng bản dự phòng). null nếu tải được.</summary>
        public string CatalogError { get; private set; }

        /// <summary>Lỗi của lần làm mới gần nhất, null nếu không lỗi.</summary>
        public string LastError { get; private set; }

        public bool IsLoading { get; private set; }
        public bool IsBusy => _busy || PackageOps.IsBusy;
        public IReadOnlyList<PackageState> States => _states;

        /// <summary>Bắn khi dữ liệu hoặc trạng thái bận thay đổi, để window vẽ lại.</summary>
        public event Action Changed;

        public PackageState StateOf(string name) => _states.Find(s => s.Entry.name == name);

        /// <summary>Tải lần đầu nếu chưa có dữ liệu.</summary>
        public void EnsureLoaded()
        {
            if (Catalog == null && !IsLoading) _ = RefreshAsync();
        }

        /// <summary>Tải lại catalog, danh sách package đã cài và tag version của từng repo.</summary>
        public async Task RefreshAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            LastError = null;
            Notify();
            try
            {
                var (catalog, source, remoteError) = await CatalogLoader.LoadAsync(HubInfo.CatalogUrl);
                Catalog = catalog;
                CatalogSource = source;
                CatalogError = remoteError;
                await ReloadInstalledAsync();
                await LoadTagsAsync();
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Log.Error("Làm mới Hub lỗi", e);
            }
            finally
            {
                IsLoading = false;
                Notify();
            }
        }

        /// <summary>Tag hoặc nhánh sẽ dùng khi cài: kênh dev dùng nhánh dev, kênh phát hành dùng tag mới nhất. null nếu chưa có bản nào.</summary>
        public string RevisionFor(PackageState state) => HubSettings.UseDevChannel ? HubSettings.DevBranch : state?.LatestTag;

        /// <summary>
        /// Các bước để cài <paramref name="name"/> (kèm dependency còn thiếu). revision null = theo kênh đang chọn.
        /// Trả null kèm lỗi nếu không cài được.
        /// </summary>
        public List<InstallStep> PlanInstall(string name, string revision, out string error)
        {
            var installed = InstalledNames();
            installed.Remove(name); // cài lại hoặc đổi version của chính nó
            var entries = HubPlanner.PlanInstall(Catalog, name, installed, out error);
            if (entries == null) return null;

            var steps = new List<InstallStep>();
            foreach (var entry in entries)
            {
                string rev = entry.name == name && revision != null ? revision : RevisionFor(StateOf(entry.name));
                if (rev == null)
                {
                    error = Loc.T($"{entry.Title} has no release yet. Turn on the Dev channel in Settings to install the version in development.",
                        $"{entry.Title} chưa có bản phát hành. Bật kênh Dev trong Cài đặt để cài bản đang phát triển.");
                    return null;
                }
                steps.Add(new InstallStep { Entry = entry, Revision = rev, Identifier = HubPlanner.Identifier(entry, rev) });
            }
            return steps;
        }

        /// <summary>Cài theo kế hoạch. Mọi package được thêm trong một lần, Unity chỉ compile một lần.</summary>
        public Task InstallAsync(IReadOnlyList<InstallStep> steps) =>
            RunAsync(() => PackageOps.AddAndRemoveAsync(steps.Select(s => s.Identifier), null));

        /// <summary>Cài một package (kèm dependency còn thiếu). Lỗi thì ném exception.</summary>
        public Task InstallAsync(string name, string revision = null)
        {
            var steps = PlanInstall(name, revision, out string error);
            if (steps == null) throw new InvalidOperationException(error);
            return InstallAsync(steps);
        }

        /// <summary>Cập nhật mọi package có bản mới (kênh dev: lấy commit mới nhất của nhánh). Trả về số package được cập nhật.</summary>
        public async Task<int> UpdateAllAsync()
        {
            var targets = _states
                .Where(s => s.IsGit && (HubSettings.UseDevChannel || s.HasUpdate))
                .Select(s => HubPlanner.Identifier(s.Entry, RevisionFor(s)))
                .ToList();
            if (targets.Count > 0) await RunAsync(() => PackageOps.AddAndRemoveAsync(targets, null));
            return targets.Count;
        }

        /// <summary>Tên các package đang cài mà cần <paramref name="name"/>. Danh sách không rỗng thì không gỡ được.</summary>
        public List<string> RemoveBlockers(string name) => HubPlanner.InstalledDependents(Catalog, name, InstalledNames());

        /// <summary>Gỡ package. Còn package khác cần nó thì ném exception.</summary>
        public Task RemoveAsync(string name)
        {
            var blockers = RemoveBlockers(name);
            if (blockers.Count > 0)
                throw new InvalidOperationException(Loc.T($"Needed by: {string.Join(", ", blockers)}", $"Đang được cần bởi: {string.Join(", ", blockers)}"));
            return RunAsync(() => PackageOps.RemoveAsync(name));
        }

        /// <summary>Thư mục local của package (repo nằm trong thư mục repo đã cài đặt), có thể chưa tồn tại.</summary>
        public string LocalFolderOf(PackageState state) => HubPlanner.LocalFolder(HubSettings.ReposRoot, state.Entry);

        public bool HasLocalFolder(PackageState state) =>
            !string.IsNullOrEmpty(state.Entry.repo) && File.Exists(Path.Combine(LocalFolderOf(state), "package.json"));

        /// <summary>Chuyển package sang bản trong thư mục local, để sửa và thử ngay.</summary>
        public Task UseLocalAsync(string name)
        {
            var state = StateOf(name) ?? throw new InvalidOperationException($"{name} không có trong catalog");
            if (!HasLocalFolder(state))
                throw new InvalidOperationException(Loc.T($"No package found at {LocalFolderOf(state)}", $"Không thấy package ở {LocalFolderOf(state)}"));

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string identifier = HubPlanner.LocalIdentifier(projectRoot, LocalFolderOf(state));
            return RunAsync(() => PackageOps.AddAsync(identifier));
        }

        /// <summary>Chuyển package về bản git, theo kênh đang chọn.</summary>
        public Task UseGitAsync(string name)
        {
            var state = StateOf(name) ?? throw new InvalidOperationException($"{name} không có trong catalog");
            string revision = RevisionFor(state);
            if (revision == null)
                throw new InvalidOperationException(Loc.T("No release yet. Turn on the Dev channel in Settings.", "Chưa có bản phát hành. Bật kênh Dev trong Cài đặt."));
            return RunAsync(() => PackageOps.AddAsync(HubPlanner.Identifier(state.Entry, revision)));
        }

        HashSet<string> InstalledNames() => new HashSet<string>(_states.Where(s => s.IsInstalled).Select(s => s.Entry.name));

        async Task RunAsync(Func<Task> action)
        {
            _busy = true;
            Notify();
            try
            {
                await action();
            }
            finally
            {
                _busy = false;
                // Unity có thể reload domain ngay sau đó; nếu chưa reload thì cập nhật lại danh sách đã cài.
                try { await ReloadInstalledAsync(); }
                catch (Exception e) { Log.Warning("Không đọc lại được danh sách package: " + e.Message); }
                Notify();
            }
        }

        async Task ReloadInstalledAsync()
        {
            if (Catalog == null) return;
            var installed = (await PackageOps.ListAsync()).ToDictionary(p => p.name);
            var previous = _states.ToDictionary(s => s.Entry.name);

            _states.Clear();
            foreach (var entry in Catalog.packages)
            {
                installed.TryGetValue(entry.name, out var info);
                var state = new PackageState { Entry = entry, Installed = info };
                if (PackageOps.TryGetGitSource(info, out var git)) state.Git = git;

                // Giữ danh sách tag đã tải, không phải hỏi git lại.
                if (previous.TryGetValue(entry.name, out var old))
                {
                    state.Tags = old.Tags;
                    state.TagsError = old.TagsError;
                }
                _states.Add(state);
            }
        }

        async Task LoadTagsAsync()
        {
            // Mỗi repo chỉ hỏi git một lần, rồi lọc tag theo tiền tố của từng package (core/, hub/...).
            var groups = _states
                .Where(s => !s.Entry.planned && !string.IsNullOrEmpty(s.Entry.repo))
                .GroupBy(s => s.Entry.repo);

            await Task.WhenAll(groups.Select(async group =>
            {
                try
                {
                    var tags = await PackageOps.ListTagsAsync(group.Key);
                    foreach (var state in group)
                    {
                        state.Tags = VersionTags.FilterAndSort(tags, state.Entry.tagPrefix);
                        state.TagsError = null;
                    }
                }
                catch (Exception e)
                {
                    foreach (var state in group) state.TagsError = e.Message;
                }
            }));
        }

        void Notify()
        {
            try { Changed?.Invoke(); }
            catch (Exception e) { Log.Error("Lỗi khi báo thay đổi cho window", e); }
        }
    }
}
