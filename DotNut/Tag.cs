namespace DotNut;

public class Tag
{
    public string Key { get; set; }
    public List<string> Value { get; set; }
    
    public Tag(string[] tag)
    {
        if (tag == null || tag.Length == 0)
        {
            throw new ArgumentException("Tag cannot be null or empty");
        }
        Key = tag[0];
        Value = tag.Skip(1).ToList();
    }

    public string[] ToArray()
    {
        return [Key, ..Value];
    }
}