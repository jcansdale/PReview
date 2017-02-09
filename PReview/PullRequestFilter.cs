using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using PReview.GitHub;

namespace PReview
{
    [SolutionTreeFilterProvider(PullRequestFilterPackageGuids.guidPullRequestFilterPackageCmdSetString, PullRequestFilterPackageGuids.PullRequestFilterId)]
    [Export]
    public class PullRequestFilterProvider : HierarchyTreeFilterProvider
    {
        private readonly IVsHierarchyItemCollectionProvider _hierarchyCollectionProvider;
        private readonly SVsServiceProvider _svcProvider;
        private readonly SessionManager _sessionManager;
        public Dictionary<string, UnifiedDiff> UnifiedDiffs = new Dictionary<string, UnifiedDiff>();

        [ImportingConstructor]
        public PullRequestFilterProvider(SVsServiceProvider serviceProvider, IVsHierarchyItemCollectionProvider hierarchyCollectionProvider,
            SessionManager sessionManager)
        {
            _svcProvider = serviceProvider;
            _hierarchyCollectionProvider = hierarchyCollectionProvider;
            _sessionManager = sessionManager;
        }

        protected override HierarchyTreeFilter CreateFilter()
        {
            return new PullRequestFilter(_svcProvider, _hierarchyCollectionProvider, _sessionManager, this);
        }

        private sealed class PullRequestFilter : HierarchyTreeFilter
        {
            private readonly IVsHierarchyItemCollectionProvider _hierarchyCollectionProvider;
            private readonly SessionManager _sessionManager;
            private readonly PullRequestFilterProvider _pullRequestFilterProvider;
            private readonly DiffParser _diffParser;

            public PullRequestFilter(IServiceProvider serviceProvider, IVsHierarchyItemCollectionProvider hierarchyCollectionProvider,
                SessionManager sessionManager, PullRequestFilterProvider pullRequestFilterProvider)
            {
                _hierarchyCollectionProvider = hierarchyCollectionProvider;
                _sessionManager = sessionManager;
                _pullRequestFilterProvider = pullRequestFilterProvider;

                var dte = (DTE)serviceProvider.GetService(typeof(DTE));
                var solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
                _diffParser = new DiffParser(solutionDir);
            }

            // Gets the items to be included from this filter provider.
            // rootItems is a collection that contains the root of your solution
            // Returns a collection of items to be included as part of the filter
            protected override async Task<IReadOnlyObservableSet> GetIncludedItemsAsync(IEnumerable<IVsHierarchyItem> rootItems)
            {
                var root = HierarchyUtilities.FindCommonAncestor(rootItems);
                var sourceItems = await _hierarchyCollectionProvider.GetDescendantsAsync(root.HierarchyIdentity.NestedHierarchy, CancellationToken);

                using (var reader = FindPatchReader())
                {
                    if (reader != null)
                    {
                        _pullRequestFilterProvider.UnifiedDiffs = await _diffParser.ParseAsync(reader);
                    }
                    else
                    {
                        _pullRequestFilterProvider.UnifiedDiffs.Clear();
                    }
                }

                return await _hierarchyCollectionProvider.GetFilteredHierarchyItemsAsync(sourceItems, ShouldIncludeInFilter, CancellationToken);
            }


            private TextReader FindPatchReader()
            {
                var diffUrl = _sessionManager.DiffUrl;
                if (diffUrl != null)
                {
                    var webClient = new WebClient();
                    var stream = webClient.OpenRead(diffUrl);
                    return new StreamReader(stream);
                }

                return null;
            }

            // Returns true if filters hierarchy item name for given filter; otherwise, false</returns>
            private bool ShouldIncludeInFilter(IVsHierarchyItem hierarchyItem)
            {
                if (hierarchyItem?.CanonicalName == null) return false;

                UnifiedDiff diff;
                return _pullRequestFilterProvider.UnifiedDiffs.TryGetValue(hierarchyItem.CanonicalName, out diff);
            }
        }
    }
}