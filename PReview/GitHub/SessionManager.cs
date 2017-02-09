using GitHub.Services;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace PReview.GitHub
{
    [Export]
    public class SessionManager
    {
        [ImportingConstructor]
        internal SessionManager([GitHubImport("GitHub.Exports.Reactive", "GitHub.Services.IPullRequestReviewSessionManager")] object obj)
        {
            var sessionManager = (IPullRequestReviewSessionManager)obj;
            sessionManager.SessionChanged.Where(m => m != null).Subscribe(m => SetDiffUrl(m));
        }

        void SetDiffUrl(IPullRequestReviewSession session)
        {
            DiffUrl = $"https://patch-diff.githubusercontent.com/raw/{session.Repository.Owner}/{session.Repository.Name}/pull/{session.PullRequest.Number}.diff";
            Trace.WriteLine("DiffUrl: " + DiffUrl);
        }

        internal string DiffUrl
        {
            get; private set;
        }
    }
}
