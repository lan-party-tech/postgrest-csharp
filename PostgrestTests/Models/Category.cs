using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PostgrestTests.Models;

[Table("category")]
public class Category : BaseModel
{
    [PrimaryKey("id")]
    public Guid  Id { get; set; }
    
    [Column("name")]
    public string? Name { get; set; }
}