using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SurplusLink.Api.Materials;

public sealed class PhotoUrlAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string path) return false;
        if (Regex.IsMatch(path, @"^/api/material-photos/[0-9a-fA-F-]{36}/[0-9a-fA-F-]{36}/(jpg|png|webp)$")) return true;
        return Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http";
    }
}
