using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reddit_fetch
{
    /// <summary>
    /// Represents the top-level Reddit listing response.
    /// </summary>
    public class RedditListing
    {
        public RedditData Data { get; set; }
    }

    /// <summary>
    /// Represents the data section containing the list of posts.
    /// </summary>
    public class RedditData
    {
        public List<RedditChild> Children { get; set; }
    }

    /// <summary>
    /// Represents a child object containing a post.
    /// </summary>
    public class RedditChild
    {
        public RedditPost Data { get; set; }
    }

    /// <summary>
    /// Represents a single Reddit post.
    /// </summary>
    public class RedditPost
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string PostHint { get; set; }
        public double CreatedUtc { get; set; }
        public bool IsVideo { get; set; }
    }
}
