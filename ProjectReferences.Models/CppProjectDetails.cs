namespace ProjectReferences.Models
{
    public sealed class CppProjectDetails
    {
        public string Type { get; set; }

        public string StandardVersion { get; set; }

        public bool IsMfc { get; set; }

        public bool IsManaged { get; set; }
    }
}
