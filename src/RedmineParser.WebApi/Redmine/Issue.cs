using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace RedmineParser.WebApi.Redmine;

[Table("issues", Schema = "redmine")]
public sealed class IssueEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("project_name")]
    public string ProjectName { get; set; } = string.Empty;

    [Column("tracker_id")]
    public int TrackerId { get; set; }

    [Column("tracker_name")]
    public string TrackerName { get; set; } = string.Empty;

    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("status_name")]
    public string StatusName { get; set; } = string.Empty;

    [Column("is_closed")]
    public bool IsClosed { get; set; }

    [Column("priority_id")]
    public int PriorityId { get; set; }

    [Column("priority_name")]
    public string PriorityName { get; set; } = string.Empty;

    [Column("author_id")]
    public int AuthorId { get; set; }

    [Column("author_name")]
    public string AuthorName { get; set; } = string.Empty;

    [Column("assigned_to_id")]
    public int AssignedToId { get; set; }

    [Column("assigned_to_name")]
    public string AssignedToName { get; set; } = string.Empty;

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("done_ratio")]
    public short DoneRatio { get; set; }

    [Column("is_private")]
    public bool IsPrivate { get; set; }

    [Column("estimated_hours")]
    public decimal? EstimatedHours { get; set; }

    [Column("total_estimated_hours")]
    public decimal? TotalEstimatedHours { get; set; }

    [Column("created_on")]
    public DateTime CreatedOn { get; set; }

    [Column("updated_on")]
    public DateTime UpdatedOn { get; set; }

    [Column("closed_on")]
    public DateTime? ClosedOn { get; set; }

    public static IEnumerable<IssueEntity> FromXml(IssuesCollectionXml xml) =>
        xml.Issues.Select(x => FromXml(x));

    public static IssueEntity FromXml(IssueXml xml) =>
        new()
        {
            Id = xml.Id,
            ProjectId = xml.Project.Id,
            ProjectName = xml.Project.Name,
            TrackerId = xml.Tracker.Id,
            TrackerName = xml.Tracker.Name,
            StatusId = xml.Status.Id,
            StatusName = xml.Status.Name,
            IsClosed = xml.Status.IsClosed,
            PriorityId = xml.Priority.Id,
            PriorityName = xml.Priority.Name,
            AuthorId = xml.Author.Id,
            AuthorName = xml.Author.Name,
            AssignedToId = xml.AssignedTo?.Id ?? 0,
            AssignedToName = xml.AssignedTo?.Name ?? string.Empty,
            CategoryId = xml.Category?.Id ?? 0,
            CategoryName = xml.Category?.Name ?? string.Empty,
            Subject = xml.Subject ?? string.Empty,
            Description = xml.Description,
            StartDate = xml.StartDate ?? DateTime.MinValue,
            DueDate = xml.DueDate,
            DoneRatio = (short)xml.DoneRatio,
            IsPrivate = xml.IsPrivate,
            EstimatedHours = xml.EstimatedHours,
            TotalEstimatedHours = xml.TotalEstimatedHours,
            CreatedOn = xml.CreatedOn ?? DateTime.MinValue,
            UpdatedOn = xml.UpdatedOn ?? DateTime.MinValue,
            ClosedOn = xml.ClosedOn,
        };
}

[XmlRoot("issues")]
public sealed class IssuesCollectionXml
{
    private static readonly XmlSerializer Serializer = new(typeof(IssuesCollectionXml));

    [XmlElement("issue")]
    public List<IssueXml> Issues { get; set; } = new();

    [XmlAttribute("total_count")]
    public int TotalCount { get; set; }

    [XmlAttribute("offset")]
    public int Offset { get; set; }

    [XmlAttribute("limit")]
    public int Limit { get; set; }

    public static IssuesCollectionXml Parse(string xml)
    {
        using var reader = new StringReader(xml);
        return (IssuesCollectionXml)Serializer.Deserialize(reader);
    }

    public static IssuesCollectionXml Parse(Stream stream)
    {
        return (IssuesCollectionXml)Serializer.Deserialize(stream);
    }
}

public sealed class IssueXml
{
    [XmlElement("id")]
    public int Id { get; set; }

    [XmlElement("project")]
    public RefEntity Project { get; set; }

    [XmlElement("tracker")]
    public RefEntity Tracker { get; set; }

    [XmlElement("status")]
    public StatusEntity Status { get; set; }

    [XmlElement("priority")]
    public RefEntity Priority { get; set; }

    [XmlElement("author")]
    public RefEntity Author { get; set; }

    [XmlElement("assigned_to")]
    public RefEntity AssignedTo { get; set; }

    [XmlElement("category")]
    public RefEntity Category { get; set; }

    [XmlElement("subject")]
    public string Subject { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("start_date")]
    public string StartDateStr { get; set; }

    [XmlIgnore]
    public DateTime? StartDate => ParseDate(StartDateStr);

    [XmlElement("due_date")]
    public string DueDateStr { get; set; }

    [XmlIgnore]
    public DateTime? DueDate => ParseDate(DueDateStr);

    [XmlElement("done_ratio")]
    public int DoneRatio { get; set; }

    [XmlElement("is_private")]
    public bool IsPrivate { get; set; }

    [XmlElement("estimated_hours")]
    public string EstimatedHoursStr { get; set; }

    [XmlIgnore]
    public decimal? EstimatedHours => ParseDecimal(EstimatedHoursStr);

    [XmlElement("total_estimated_hours")]
    public string TotalEstimatedHoursStr { get; set; }

    [XmlIgnore]
    public decimal? TotalEstimatedHours => ParseDecimal(TotalEstimatedHoursStr);

    [XmlElement("created_on")]
    public string CreatedOnStr { get; set; }

    [XmlIgnore]
    public DateTime? CreatedOn => ParseDate(CreatedOnStr);

    [XmlElement("updated_on")]
    public string UpdatedOnStr { get; set; }

    [XmlIgnore]
    public DateTime? UpdatedOn => ParseDate(UpdatedOnStr);

    [XmlElement("closed_on")]
    public string ClosedOnStr { get; set; }

    [XmlIgnore]
    public DateTime? ClosedOn => ParseDate(ClosedOnStr);

    private static DateTime? ParseDate(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? null
            : DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);

    private static decimal? ParseDecimal(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? null
            : decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    public class RefEntity
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class StatusEntity : RefEntity
    {
        [XmlAttribute("is_closed")]
        public bool IsClosed { get; set; }
    }
}
