using System;
using SQLite.Net;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.Models
{
    public interface ITagData : ICommonData
    {
        string Name { get; }
        long WorkspaceRemoteId { get; }
        Guid WorkspaceId { get; }
        ITagData With(Action<TagData> transform);
    }

    [Table("TagModel")]
    public class TagData : CommonData, ITagData
    {
        public static ITagData Create(Action<TagData> transform = null)
        {
            return CommonData.Create(transform);
        }

        /// <summary>
        /// ATTENTION: This constructor should only be used by SQL and JSON serializers
        /// To create new objects, use the static Create method instead
        /// </summary>
        public TagData()
        {
        }

        TagData(TagData other) : base(other)
        {
            Name = other.Name;
            WorkspaceId = other.WorkspaceId;
            WorkspaceRemoteId = other.WorkspaceRemoteId;
        }

        public override object Clone()
        {
            return new TagData(this);
        }

        public ITagData With(Action<TagData> transform)
        {
            return base.With(transform);
        }

        public string Name { get; set; }

        public long WorkspaceRemoteId { get; set; }

        public Guid WorkspaceId { get; set; }
    }
}
