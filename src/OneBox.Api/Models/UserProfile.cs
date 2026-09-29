namespace OneBox.Api.Models;
public sealed class UserProfile
{
 public Guid Id{get;set;}=Guid.NewGuid(); public Guid UserId{get;set;}
 public string? AddressLine1{get;set;} public string? AddressLine2{get;set;} public string? City{get;set;} public string? State{get;set;} public string? PostalCode{get;set;} public string? Country{get;set;}
 public string? PreferredLanguage{get;set;}="en"; public string? EmergencyContactName{get;set;} public string? EmergencyContactPhone{get;set;}
 public DateTime CreatedAt{get;set;}=DateTime.UtcNow; public DateTime UpdatedAt{get;set;}=DateTime.UtcNow;
}